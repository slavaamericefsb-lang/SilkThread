using Microsoft.Win32;
using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class RegistrySnapshotEncodingTests
{
    [Fact]
    public void MultiString_RoundTripPreservesEmptyAndUnicodeEntries()
    {
        string[] original = ["one", "", "СЂСѓСЃСЃРєРёР№", "%PATH%"];
        var json = RegistryOperations.EncodeSnapshotValue(RegistryValueKind.MultiString, original);
        Assert.Equal(original, (string[])RegistryOperations.DecodeSnapshotValue(RegistryValueKind.MultiString, json));
    }

    [Theory]
    [InlineData(RegistryValueKind.DWord, "-2147483648")]
    [InlineData(RegistryValueKind.QWord, "9223372036854775807")]
    [InlineData(RegistryValueKind.ExpandString, "%SystemRoot%\\System32")]
    [InlineData(RegistryValueKind.Binary, "AAH/")]
    public void TypedValues_RoundTrip(RegistryValueKind kind, string encoded)
    {
        var value = RegistryOperations.DecodeSnapshotValue(kind, encoded);
        Assert.Equal(encoded, RegistryOperations.EncodeSnapshotValue(kind, value));
    }

    [Fact]
    public void CorruptBinary_DoesNotSilentlyBecomeEmpty()
    {
        Assert.Throws<FormatException>(() => RegistryOperations.DecodeSnapshotValue(RegistryValueKind.Binary, "invalid!"));
        Assert.False(RegistryOperations.RestoreFromSnapshot(new RegistryValueSnapshot { ReadSucceeded = false }));
    }
    [Fact]
    public async Task RegistryRollback_FailedSnapshotDoesNotFallBackToDeletingValue()
    {
        var tweak = SilkThread.Core.TweakDatabase.TweakBuilder.CreateRegistryDword(
            "test", "Test", "Test", "Test", "Test", "Test",
            SilkThread.Core.Models.TweakCategory.AdvancedTweaks,
            SilkThread.Core.Models.RiskLevel.Low, SilkThread.Core.Models.RecommendationLevel.Optional,
            "INVALID", "test", "value", 1);
        var snapshot = new RegistryValueSnapshot { ReadSucceeded = false, Hive = "INVALID", Exists = true };
        Assert.False((await tweak.RollbackAsync(snapshot.ToJson())).Success);
    }

}
