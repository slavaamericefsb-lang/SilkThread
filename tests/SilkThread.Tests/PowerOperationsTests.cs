using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class PowerOperationsTests
{
    private const string Plan = PowerOperations.HighPerformanceGuid;
    private const string Subgroup = "54533251-82be-4824-96c1-47b60b740d00";
    private const string Setting = "89ba4784-4ba4-4878-804e-4f0e929c69c3";

    private static (int, string, string) Active(string guid = Plan, string name = "Performance")
        => (0, $"Power Scheme GUID: {guid}  ({name})\r\n", "");

    private sealed class Commands(params (int Code, string Output, string Error)[] responses)
    {
        public List<string> Calls { get; } = [];
        private readonly Queue<(int, string, string)> _responses = new(responses);
        public PowerCommandClient Create() => new(arguments =>
        {
            Calls.Add(arguments);
            Assert.NotEmpty(_responses);
            return Task.FromResult(_responses.Dequeue());
        });
    }

    [Theory]
    [InlineData("Balanced")]
    [InlineData("Высокая производительность")]
    [InlineData("Custom (Gaming) plan")]
    public async Task Read_PreservesLocalizedAndNestedNames(string name)
    {
        var fake = new Commands(Active(Plan.ToUpperInvariant(), name));
        var result = await fake.Create().GetActiveSchemeAsync();
        Assert.Equal(Plan, result.Guid);
        Assert.Equal(name, result.Name);
    }

    [Theory]
    [InlineData(5, "", "Access denied")]
    [InlineData(-1, "", "Process timed out")]
    [InlineData(0, "", "")]
    [InlineData(0, "unexpected output", "")]
    public async Task Read_FailureNeverReturnsBalanced(int code, string output, string error)
    {
        var client = new Commands((code, output, error)).Create();
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetActiveSchemeAsync());
    }

    [Fact]
    public async Task Write_NoCommandsAfterFailedDiscovery()
    {
        var fake = new Commands((5, "", "Access denied"));
        Assert.False(await fake.Create().SetPowerValueIndexAsync(Subgroup, Setting, 100, 50));
        Assert.Equal(["/getactivescheme"], fake.Calls);
    }

    [Fact]
    public async Task Write_AcFailureStopsDcAndActivation()
    {
        var fake = new Commands(Active(), (5, "", "Access denied"));
        Assert.False(await fake.Create().SetPowerValueIndexAsync(Subgroup, Setting, 100, 50));
        Assert.Equal(["/getactivescheme", $"/setacvalueindex {Plan} {Subgroup} {Setting} 100"], fake.Calls);
    }

    [Fact]
    public async Task Write_DcFailureIsNotSuccess()
    {
        var fake = new Commands(Active(), (0, "", ""), (5, "", "Access denied"));
        Assert.False(await fake.Create().SetPowerValueIndexAsync(Subgroup, Setting, 100, 50));
        Assert.Equal($"/setdcvalueindex {Plan} {Subgroup} {Setting} 50", fake.Calls.Last());
        Assert.Equal(3, fake.Calls.Count);
    }

    [Fact]
    public async Task Write_ActivationFailureIsNotSuccess()
    {
        var fake = new Commands(Active(), (0, "", ""), Active(), (5, "", "Access denied"));
        Assert.False(await fake.Create().SetPowerValueIndexAsync(Subgroup, Setting, 100));
        Assert.Equal($"/setactive {Plan}", fake.Calls.Last());
    }

    [Fact]
    public async Task Write_ExternalPlanSwitchDoesNotReactivateOriginal()
    {
        var fake = new Commands(Active(), (0, "", ""), Active(PowerOperations.BalancedGuid));
        Assert.False(await fake.Create().SetPowerValueIndexAsync(Subgroup, Setting, 100));
        Assert.DoesNotContain(fake.Calls, x => x.StartsWith("/setactive"));
    }

    [Fact]
    public async Task Write_FailedConflictCheckDoesNotReactivate()
    {
        var fake = new Commands(Active(), (0, "", ""), (1, "", "Failed"));
        Assert.False(await fake.Create().SetPowerValueIndexAsync(Subgroup, Setting, 100));
        Assert.Equal(3, fake.Calls.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(50)]
    public async Task Write_SuccessRequiresAllRequestedCommandsAndVerifiedActivation(int? dc)
    {
        var responses = new List<(int, string, string)> { Active(), (0, "", "") };
        if (dc.HasValue) responses.Add((0, "", ""));
        responses.AddRange(new[] { Active(), (0, "", ""), Active() });
        var fake = new Commands(responses.ToArray());
        Assert.True(await fake.Create().SetPowerValueIndexAsync(Subgroup, Setting, 100, dc));
        var expected = new List<string> { "/getactivescheme", $"/setacvalueindex {Plan} {Subgroup} {Setting} 100" };
        if (dc.HasValue) expected.Add($"/setdcvalueindex {Plan} {Subgroup} {Setting} 50");
        expected.AddRange(new[] { "/getactivescheme", $"/setactive {Plan}", "/getactivescheme" });
        Assert.Equal(expected, fake.Calls);
    }

    [Theory]
    [InlineData(0, PowerOperations.BalancedGuid)]
    [InlineData(1, "")]
    public async Task Switch_FailedOrMismatchedVerificationIsNotSuccess(int code, string guid)
    {
        var fake = new Commands((0, "", ""), (code, $"GUID: {guid} (Plan)", "Failed"));
        Assert.False(await fake.Create().SetActiveSchemeAsync(Plan));
    }

    [Fact]
    public async Task Switch_NormalizesGuidAndVerifiesResult()
    {
        var fake = new Commands((0, "", ""), Active());
        Assert.True(await fake.Create().SetActiveSchemeAsync("{" + Plan.ToUpperInvariant() + "}"));
        Assert.Equal([$"/setactive {Plan}", "/getactivescheme"], fake.Calls);
    }

    [Fact]
    public async Task Switch_FailedCommandDoesNotReadBack()
    {
        var fake = new Commands((5, "", "Access denied"));
        Assert.False(await fake.Create().SetActiveSchemeAsync(Plan));
        Assert.Single(fake.Calls);
    }

    [Fact]
    public async Task InvalidIdentifiersAndNegativeIndicesNeverRunCommands()
    {
        var fake = new Commands();
        var client = fake.Create();
        await Assert.ThrowsAsync<ArgumentException>(() => client.SetActiveSchemeAsync("SCHEME_CURRENT /delete"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.DuplicateSchemeAsync("invalid"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SetPowerValueIndexAsync("invalid", Setting, 100));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SetPowerValueIndexAsync(Subgroup, "invalid", 100));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.SetPowerValueIndexAsync(Subgroup, Setting, -1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.SetPowerValueIndexAsync(Subgroup, Setting, 100, -1));
        Assert.Empty(fake.Calls);
    }

    [Theory]
    [InlineData(5, "", null)]
    [InlineData(0, "invalid output", null)]
    [InlineData(0, "GUID: " + Plan + " (Copy)", Plan)]
    public async Task Duplicate_ReportsFailureOrReturnedGuid(int code, string output, string? expected)
    {
        var fake = new Commands((code, output, ""));
        Assert.Equal(expected, await fake.Create().DuplicateSchemeAsync(Plan));
    }
}
