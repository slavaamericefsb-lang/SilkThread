using SilkThread.Core.Models;
using SilkThread.Core.TweakDatabase;
using Xunit;

namespace SilkThread.Tests;

public class TweakCatalogAuditTests
{
    [Fact]
    public void DocumentedExpansion_ContainsExactly50UniqueOptionalPolicies()
    {
        var additions = DocumentedWindowsTweaks.GetTweaks().ToList();
        Assert.Equal(50, additions.Count);
        Assert.Equal(50, additions.Select(t => t.RegistryWrites.First().Target).Distinct().Count());
        var writes = additions.SelectMany(t => t.RegistryWrites).ToList();
        Assert.Equal(writes.Count, writes.Select(w => w.Target).Distinct().Count());
        Assert.All(additions, t =>
        {
            Assert.Equal(RecommendationLevel.Optional, t.Recommendation);
            Assert.Contains("https://learn.microsoft.com/", t.HowItWorks);
            Assert.NotNull(t.CustomCompatibilityCheck);
            Assert.True(t.MinWindowsBuild >= 19044);
        });
        Assert.Equal(304, TweakCatalog.GetAllTweaks().Count);
        var sync = additions.Where(t => t.Id.StartsWith("doc_sync_") && t.Id != "doc_sync_metered").ToList();
        Assert.Equal(6, sync.Count);
        Assert.All(sync, t =>
        {
            Assert.Equal(2, t.RegistryWrites.Count);
            Assert.Contains(t.RegistryWrites, w => w.ValueName.EndsWith("UserOverride") && Equals(w.Value, 1));
        });
    }

    [Theory]
    [InlineData("Core", 22631, false)]
    [InlineData("Professional", 18363, false)]
    [InlineData("Professional", 19044, true)]
    [InlineData("IoTEnterpriseS", 26100, true)]
    public void DocumentedPolicies_RespectEditionAndBuild(string edition, int build, bool supported)
    {
        var profile = new HardwareProfile(null!, new(), null!, null!, new(),
            new OsInfo("Windows", "", build, edition, "64-bit", "", "", false, false, build >= 22000),
            new PowerAndChassisInfo(false, false, 100, false, true),
            new ProcessDiagnosticInfo(0, new(), new()), new RestorePointStatus(false, false, ""));
        var policy = DocumentedWindowsTweaks.GetTweaks().Single(t => t.Id == "doc_search_cloud");
        Assert.Equal(supported, policy.CustomCompatibilityCheck!(profile).IsCompatible);
        var enterpriseOnly = DocumentedWindowsTweaks.GetTweaks().Single(t => t.Id == "doc_cloud_consumer");
        Assert.Equal(supported && edition.Contains("Enterprise"), enterpriseOnly.CustomCompatibilityCheck!(profile).IsCompatible);
    }

    [Fact]
    public void ActiveCatalog_HasNoDuplicateRegistryWrites()
    {
        var duplicates = TweakCatalog.GetAllTweaks().Where(t => t.AuditBlockReason == null)
            .SelectMany(t => t.RegistryWrites.Select(w => new { t.Id, w.Target, w.Value }))
            .GroupBy(x => (x.Target, x.Value))
            .Where(g => g.Select(x => x.Id).Distinct().Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => x.Id))}");
        Assert.Empty(duplicates);
    }

    [Fact]
    public void ActiveCatalog_HasNoUnresolvedOpposingRegistryWrites()
    {
        var active = TweakCatalog.GetAllTweaks().Where(t => t.AuditBlockReason == null).ToList();
        var overlaps = active.SelectMany(t => t.RegistryWrites.Select(w => new { Tweak = t, Write = w }))
            .GroupBy(x => x.Write.Target).Where(g => g.Select(x => x.Tweak.Id).Distinct().Count() > 1);
        foreach (var group in overlaps)
            foreach (var a in group)
                foreach (var b in group.Where(b => b.Tweak.Id != a.Tweak.Id))
                    Assert.Contains(b.Tweak.Id, a.Tweak.ConflictsWith);
    }

    [Fact]
    public async Task StringDefinition_DoesNotTreatMalformedDwordAsApplied()
    {
        string path = @"Software\SilkThread.Tests\" + Guid.NewGuid().ToString("N");
        try
        {
            Assert.True(SilkThread.Core.SystemOperations.RegistryOperations.SetDword("HKCU", path, "Probe", 0));
            var definition = TweakBuilder.CreateRegistryString("test_kind", "Test", "Test", "Test", "Test", "Test",
                TweakCategory.WindowsPerformance, RiskLevel.Low, RecommendationLevel.Optional, "HKCU", path, "Probe", "0");
            Assert.Equal(TweakStatus.NotApplied, await definition.CheckCurrentStateAsync());
            Assert.True(SilkThread.Core.SystemOperations.RegistryOperations.SetString("HKCU", path, "Probe", "0"));
            Assert.Equal(TweakStatus.Applied, await definition.CheckCurrentStateAsync());
        }
        finally { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false); }
    }

    [Fact]
    public void AuditWithdrawals_AllResolveToDefinitionsForHistoricalRollback()
    {
        var catalog = TweakCatalog.GetAllTweaks().ToDictionary(t => t.Id);
        foreach (var (id, reason) in TweakAuditPolicy.Withdrawals)
        {
            Assert.True(catalog.ContainsKey(id), id);
            Assert.Equal(reason, catalog[id].AuditBlockReason);
            Assert.NotNull(catalog[id].RollbackAsync);
        }
    }

    [Theory]
    [InlineData("win_menu_show_delay", "MenuShowDelay")]
    [InlineData("win_window_animations_disable", "MinAnimate")]
    [InlineData("input_sticky_keys_prompt_disable", "Flags")]
    public void TextRegistryValues_UseStringPayload(string id, string name)
    {
        var tweak = TweakCatalog.GetAllTweaks().Single(t => t.Id == id);
        Assert.IsType<string>(tweak.RegistryWrites.Single(w => w.ValueName == name).Value);
    }
}
