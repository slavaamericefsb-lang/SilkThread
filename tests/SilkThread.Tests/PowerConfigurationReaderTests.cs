using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class PowerConfigurationReaderTests
{
    private static readonly Guid Plan = Guid.Parse(PowerOperations.HighPerformanceGuid);
    private static readonly Guid OtherPlan = Guid.Parse(PowerOperations.BalancedGuid);
    private static readonly Guid Subgroup = Guid.Parse("54533251-82be-4824-96c1-47b60b740d00");
    private static readonly Guid Setting = Guid.Parse("89ba4784-4ba4-4878-804e-4f0e929c69c3");

    private sealed class FakeApi : IPowerReadApi
    {
        public List<(uint Code, Guid Id)> Schemes { get; } = [(0, Plan), (0, OtherPlan)];
        public Dictionary<Guid, (uint Code, string? Name)> Names { get; } = new()
        {
            [Plan] = (0, "Производительность (игры)"), [OtherPlan] = (0, "Balanced")
        };
        public (uint Code, Guid Id) Active { get; set; } = (0, Plan);
        public (uint Code, uint Value) Ac { get; set; } = (0, 0);
        public (uint Code, uint Value) Dc { get; set; } = (0, uint.MaxValue);
        public List<(Guid Scheme, Guid Subgroup, Guid Setting, bool Ac)> ValueCalls { get; } = [];
        public int Calls { get; private set; }
        public Action? AfterAc { get; set; }

        public uint EnumerateScheme(uint index, out Guid schemeId)
        {
            Calls++;
            var entry = index < Schemes.Count ? Schemes[(int)index] : (259u, Guid.Empty);
            schemeId = entry.Item2;
            return entry.Item1;
        }
        public uint ReadActiveScheme(out Guid schemeId)
        {
            Calls++;
            schemeId = Active.Id;
            return Active.Code;
        }
        public uint ReadSchemeName(Guid schemeId, out string? name)
        {
            Calls++;
            var entry = Names[schemeId];
            name = entry.Name;
            return entry.Code;
        }
        public uint ReadValueIndex(Guid schemeId, Guid subgroupId, Guid settingId, bool ac, out uint value)
        {
            Calls++;
            ValueCalls.Add((schemeId, subgroupId, settingId, ac));
            var entry = ac ? Ac : Dc;
            value = entry.Value;
            if (ac) AfterAc?.Invoke();
            return entry.Code;
        }
    }

    [Fact]
    public async Task Catalog_PreservesIdsLocalizedNamesActivePlanAndCheckTime()
    {
        var before = DateTimeOffset.UtcNow;
        var result = await new PowerConfigurationReader(new FakeApi()).ReadSchemesAsync();
        Assert.Equal(new[] { Plan, OtherPlan }, result.Schemes.Select(s => s.Id));
        Assert.Equal("Производительность (игры)", result.Schemes[0].Name);
        Assert.All(result.Schemes, s => Assert.Null(s.NameError));
        Assert.Equal(Plan, result.ActiveSchemeId);
        Assert.Null(result.EnumerationError);
        Assert.Null(result.ActiveSchemeError);
        Assert.InRange(result.CheckedAtUtc, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Catalog_EmptyListIsNotFilledWithBuiltInSchemes()
    {
        var api = new FakeApi();
        api.Schemes.Clear();
        var result = await new PowerConfigurationReader(api).ReadSchemesAsync();
        Assert.Empty(result.Schemes);
        Assert.Null(result.EnumerationError);
        Assert.Equal(Plan, result.ActiveSchemeId);
    }

    [Fact]
    public async Task Catalog_PartialEnumerationRetainsDataAndReportsFailure()
    {
        var api = new FakeApi();
        api.Schemes[1] = (5, OtherPlan);
        var result = await new PowerConfigurationReader(api).ReadSchemesAsync();
        Assert.Single(result.Schemes);
        Assert.Equal(PowerReadFailure.AccessDenied, result.EnumerationError?.Kind);
        Assert.Equal("PowerEnumerate", result.EnumerationError?.Operation);
        Assert.Equal(Plan, result.ActiveSchemeId);
    }

    [Fact]
    public async Task Catalog_NameFailureKeepsGuidAndDoesNotInventName()
    {
        var api = new FakeApi();
        api.Names[Plan] = (5, "unusable output");
        var result = await new PowerConfigurationReader(api).ReadSchemesAsync();
        Assert.Equal(Plan, result.Schemes[0].Id);
        Assert.Null(result.Schemes[0].Name);
        Assert.Equal(PowerReadFailure.AccessDenied, result.Schemes[0].NameError?.Kind);
        Assert.Equal("Balanced", result.Schemes[1].Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Catalog_MissingNameIsAnExplicitError(string? name)
    {
        var api = new FakeApi();
        api.Names[Plan] = (0, name);
        var result = await new PowerConfigurationReader(api).ReadSchemesAsync();
        Assert.Null(result.Schemes[0].Name);
        Assert.Equal(13u, result.Schemes[0].NameError?.NativeCode);
    }

    [Theory]
    [InlineData(5u, PowerReadFailure.AccessDenied)]
    [InlineData(2u, PowerReadFailure.NotFound)]
    [InlineData(50u, PowerReadFailure.NotSupported)]
    [InlineData(87u, PowerReadFailure.Failed)]
    public async Task Catalog_ActiveReadErrorIsPreservedWithoutDefaultPlan(uint code, PowerReadFailure kind)
    {
        var result = await new PowerConfigurationReader(new FakeApi { Active = (code, Plan) }).ReadSchemesAsync();
        Assert.Null(result.ActiveSchemeId);
        Assert.Equal(kind, result.ActiveSchemeError?.Kind);
        Assert.Equal(code, result.ActiveSchemeError?.NativeCode);
        Assert.Equal(2, result.Schemes.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Catalog_DuplicateOrEmptyGuidIsNotReturnedAsComplete(bool duplicate)
    {
        var api = new FakeApi();
        api.Schemes[1] = (0, duplicate ? Plan : Guid.Empty);
        var result = await new PowerConfigurationReader(api).ReadSchemesAsync();
        Assert.Single(result.Schemes);
        Assert.Equal(13u, result.EnumerationError?.NativeCode);
    }

    [Fact]
    public async Task Catalog_EmptyActiveGuidIsNotSuccess()
    {
        var result = await new PowerConfigurationReader(new FakeApi { Active = (0, Guid.Empty) }).ReadSchemesAsync();
        Assert.Null(result.ActiveSchemeId);
        Assert.NotNull(result.ActiveSchemeError);
    }

    [Fact]
    public async Task Values_ReadExplicitInactivePlanAndPreserveFullUnsignedRange()
    {
        var api = new FakeApi();
        var before = DateTimeOffset.UtcNow;
        var result = await new PowerConfigurationReader(api).ReadSettingAsync(OtherPlan, Subgroup, Setting);
        Assert.Equal(0u, result.AcIndex);
        Assert.Equal(uint.MaxValue, result.DcIndex);
        Assert.True(result.IsComplete);
        Assert.Equal(OtherPlan, result.SchemeId);
        Assert.Equal(Subgroup, result.SubgroupId);
        Assert.Equal(Setting, result.SettingId);
        Assert.Equal(new[] { (OtherPlan, Subgroup, Setting, true), (OtherPlan, Subgroup, Setting, false) }, api.ValueCalls);
        Assert.Equal(2, api.Calls); // No lookup of the active plan or command execution.
        Assert.InRange(result.CheckedAtUtc, before, DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Values_PartialFailureRetainsSuccessfulSideAndRejectsFailedOutput(bool failAc)
    {
        var api = new FakeApi();
        if (failAc) api.Ac = (5, 123);
        else api.Dc = (2, 123);
        var result = await new PowerConfigurationReader(api).ReadSettingAsync(Plan, Subgroup, Setting);
        Assert.False(result.IsComplete);
        if (failAc)
        {
            Assert.Null(result.AcIndex);
            Assert.Equal(PowerReadFailure.AccessDenied, result.AcError?.Kind);
            Assert.Equal(uint.MaxValue, result.DcIndex);
            Assert.Null(result.DcError);
        }
        else
        {
            Assert.Null(result.DcIndex);
            Assert.Equal(PowerReadFailure.NotFound, result.DcError?.Kind);
            Assert.Equal(0u, result.AcIndex);
            Assert.Null(result.AcError);
        }
    }

    [Fact]
    public async Task Values_BothFailuresRemainSeparate()
    {
        var api = new FakeApi { Ac = (50, 0), Dc = (87, 0) };
        var result = await new PowerConfigurationReader(api).ReadSettingAsync(Plan, Subgroup, Setting);
        Assert.Null(result.AcIndex);
        Assert.Null(result.DcIndex);
        Assert.Equal("PowerReadACValueIndex", result.AcError?.Operation);
        Assert.Equal("PowerReadDCValueIndex", result.DcError?.Operation);
        Assert.Equal(PowerReadFailure.NotSupported, result.AcError?.Kind);
        Assert.Equal(PowerReadFailure.Failed, result.DcError?.Kind);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public async Task Values_EmptyIdentifiersRejectedBeforeNativeCalls()
    {
        var api = new FakeApi();
        var reader = new PowerConfigurationReader(api);
        await Assert.ThrowsAsync<ArgumentException>(() => reader.ReadSettingAsync(Guid.Empty, Subgroup, Setting));
        await Assert.ThrowsAsync<ArgumentException>(() => reader.ReadSettingAsync(Plan, Guid.Empty, Setting));
        await Assert.ThrowsAsync<ArgumentException>(() => reader.ReadSettingAsync(Plan, Subgroup, Guid.Empty));
        Assert.Equal(0, api.Calls);
    }

    [Fact]
    public async Task CancelledReadsDoNotCallNativeApi()
    {
        var api = new FakeApi();
        var reader = new PowerConfigurationReader(api);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadSchemesAsync(cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadSettingAsync(Plan, Subgroup, Setting, cts.Token));
        Assert.Equal(0, api.Calls);
    }

    [Fact]
    public async Task CancellationBetweenAcAndDcStopsFurtherCalls()
    {
        using var cts = new CancellationTokenSource();
        var api = new FakeApi { AfterAc = cts.Cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PowerConfigurationReader(api).ReadSettingAsync(Plan, Subgroup, Setting, cts.Token));
        Assert.Single(api.ValueCalls);
    }
}
