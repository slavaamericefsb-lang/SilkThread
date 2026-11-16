using SilkThread.App.Services;
using Xunit;

namespace SilkThread.Tests;

public class ElevationRestartTests
{
    [Theory]
    [InlineData("Dashboard")]
    [InlineData("SystemSettings")]
    [InlineData("Settings")]
    public void HandoffRoundTripsAndUsesWindowsElevation(string route)
    {
        var request = new ElevationRestartRequest(1234, DateTime.UtcNow.Ticks, route);
        var info = ElevationRestart.CreateStartInfo(@"C:\Program Files\SilkThread\SilkThread.App.exe", request);
        Assert.Equal("runas", info.Verb);
        Assert.True(info.UseShellExecute);
        Assert.Equal(@"C:\Program Files\SilkThread", info.WorkingDirectory);
        Assert.Equal(request, ElevationRestart.Parse(info.ArgumentList.ToArray()));
        Assert.Empty(info.Arguments);
    }

    [Theory]
    [InlineData("0", "1", "Dashboard")]
    [InlineData("-1", "1", "Dashboard")]
    [InlineData("123", "0", "Dashboard")]
    [InlineData("123", "9223372036854775807", "Dashboard")]
    [InlineData("123", "1", "Unknown")]
    [InlineData("123", "1", "Settings --anything")]
    public void InvalidHandoffIsRejected(string pid, string ticks, string route)
        => Assert.Null(ElevationRestart.Parse(new[] { ElevationRestart.Switch, pid, ticks, route }));

    [Fact]
    public void ExtraOrMissingArgumentsAreRejected()
    {
        Assert.Null(ElevationRestart.Parse(Array.Empty<string>()));
        Assert.Null(ElevationRestart.Parse(new[] { ElevationRestart.Switch, "123", "1", "Dashboard", "extra" }));
        Assert.Throws<ArgumentException>(() => ElevationRestart.CreateStartInfo("relative.exe", new(1, 1, "Dashboard")));
    }
}
