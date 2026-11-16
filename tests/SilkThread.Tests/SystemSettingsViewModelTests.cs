using System.Reflection;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.Models;
using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class SystemSettingsViewModelTests
{
    private sealed class Localization : ILocalizationService
    {
        public string CurrentLanguage { get; private set; } = "ru";
        public event EventHandler? LanguageChanged;
        public void SetLanguage(string lang) { CurrentLanguage = lang; LanguageChanged?.Invoke(this, EventArgs.Empty); }
        public static Dictionary<string, string> Strings(string lang) => (Dictionary<string, string>)typeof(LocalizationService)
            .GetField(lang == "ru" ? "RuStrings" : "EnStrings", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        public string this[string key] => GetString(key);
        public string GetString(string key) => Strings(CurrentLanguage).GetValueOrDefault(key, key);
        public string Format(string key, params object[] args) => string.Format(this[key], args);
        public string GetCategoryName(TweakCategory value) => value.ToString();
        public string GetCategoryDescription(TweakCategory value) => value.ToString();
        public string GetRiskName(RiskLevel value) => value.ToString();
        public string GetRecommendationName(RecommendationLevel value) => value.ToString();
        public string GetStatusName(TweakStatus value) => value.ToString();
    }
    private sealed class Memory : IMemoryConfigurationReader
    {
        public Task<MemoryConfiguration> ReadAsync(CancellationToken token = default) => Task.FromResult(new MemoryConfiguration(
            new(new(16UL << 30, 8UL << 30, 12UL << 30, 32UL << 30, 14UL << 30), null), new(new(false), null),
            new(new[] { new PageFileSetting(@"C:\pagefile.sys", 1024, 4096) }, null),
            new(new[] { new PageFileUsage(@"C:\pagefile.sys", 2048, 0, 50, false) }, null),
            new(new[] { new PageFileVolume("C:", 100UL << 30, 50UL << 30) }, null), new(new(7, null, null, null, null), null),
            new(DumpRequirementKind.WorkloadDependent, null), PageFilePathComparison.PathsMatch, DateTimeOffset.UtcNow));
    }
    private sealed class Pages : IPageFileService
    {
        public ConfigurationChange? Pending;
        public PageFilePlan? Reviewed;
        public Task<PageFileReview> PreviewAsync(PageFilePlan plan) { Reviewed = plan; return Task.FromResult(new PageFileReview(plan, "{}", "boot", Array.Empty<string>())); }
        public Task<PageFileResult> ApplyAsync(PageFileReview review) => throw new NotSupportedException();
        public Task<PageFileRestoreReview?> PreviewRestoreAsync(int? id = null) => Task.FromResult<PageFileRestoreReview?>(null);
        public Task<PageFileResult> RestoreAsync(PageFileRestoreReview review, bool acknowledgeConflict = false) => throw new NotSupportedException();
        public Task<ConfigurationChange?> CheckPendingAsync() => Task.FromResult(Pending);
    }
    private sealed class Power : IPowerConfigurationReader, IPowerSchemeService
    {
        public Task<PowerSchemeCatalog> ReadSchemesAsync(CancellationToken token = default)
        {
            var id = Guid.Parse(PowerOperations.BalancedGuid);
            return Task.FromResult(new PowerSchemeCatalog(new[] { new PowerSchemeInfo(id, "Balanced", null) }, id, null, null, DateTimeOffset.UtcNow));
        }
        public Task<PowerSettingValues> ReadSettingAsync(Guid a, Guid b, Guid c, CancellationToken token = default) => throw new NotSupportedException();
        public Task<PowerSchemeChange?> GetPendingAsync() => Task.FromResult<PowerSchemeChange?>(null);
        public Task<PowerSchemePreview> PreviewAsync(Guid target) => throw new NotSupportedException();
        public Task<PowerSchemeResult> ApplyAsync(PowerSchemePreview preview) => throw new NotSupportedException();
        public Task<PowerSchemeResult> RestoreAsync(int id) => throw new NotSupportedException();
    }
    private sealed class Diagnostics : ISystemDiagnosticsService
    {
        public Task<SystemDiagnostics> ReadAsync(CancellationToken token = default) => Task.FromResult(new SystemDiagnostics(
            new(new(new[] { new CpuSetInfo(1, 0, 0, 0, 0, false) }, new[] { new ProcessorGroupInfo(0, 1) }), null),
            new(new(Array.Empty<DeviceDriverInfo>(), null), null), new(new(null, null, null, null), null),
            new(5, "", "", SystemDiagnosticsService.ParseBcd("")), false, DateTimeOffset.UtcNow));
    }
    private static SystemSettingsViewModel Create(Localization loc, Pages pages)
    {
        var power = new Power();
        return new(loc, null!, power, power, new Memory(), pages, new Diagnostics());
    }

    [Fact]
    public async Task RefreshLoadsEditorWithoutInventingDraft_AndPreservesUnknownPowerState()
    {
        var loc = new Localization(); var vm = Create(loc, new Pages());
        await Task.WhenAll(vm.RefreshPowerAsync(), vm.RefreshMemoryAsync(), vm.RefreshDiagnosticsAsync());
        Assert.False(vm.HasMemoryDraft); Assert.False(vm.HasSelectedChanges); Assert.True(vm.CanEditMemory);
        Assert.Equal(2, Assert.Single(vm.PageFileEditors).Mode);
        Assert.Contains(loc["Memory_Unknown"], vm.SupplyReport);
        Assert.DoesNotContain(loc["Memory_Unknown"] + "%", vm.SupplyReport);
        Assert.Contains(loc["System_BcdUnavailable"], vm.BcdReport);
        Assert.Contains(loc["System_Standard"], vm.GetSystemReport());
    }

    [Theory]
    [InlineData("-1")] [InlineData("1.5")] [InlineData("1,5")] [InlineData("+64")] [InlineData(" 64")]
    public async Task EditorRejectsNonWholeOrSignedSizesBeforeServicePreview(string input)
    {
        var loc = new Localization(); var pages = new Pages(); var vm = Create(loc, pages); await vm.RefreshMemoryAsync();
        Assert.Single(vm.PageFileEditors).Initial = input;
        Assert.Null(await vm.ReviewPageFilesAsync()); Assert.Null(pages.Reviewed);
        Assert.Equal(loc["Page_Validation_InvalidSize"], vm.PageFileStatus);
        Assert.False(vm.IsMemoryBusy);
    }

    [Fact]
    public async Task DraftSurvivesLanguageSwitchAndRemainsAccessibleFromDiagnostics()
    {
        var loc = new Localization(); var vm = Create(loc, new Pages()); await vm.RefreshMemoryAsync();
        var editor = Assert.Single(vm.PageFileEditors); editor.Initial = "32"; editor.Maximum = "64";
        vm.ActiveTab = 1; Assert.True(vm.HasSelectedChanges);
        loc.SetLanguage("en");
        Assert.True(vm.HasMemoryDraft); Assert.Equal("32", editor.Initial); Assert.Equal(2, editor.Mode);
        Assert.Equal(loc["Page_Custom"], editor.Modes[2]);
        vm.ActiveTab = 2; Assert.False(vm.HasSelectedChanges);
        Assert.True(vm.HasPendingDrafts); Assert.Contains(vm.MemoryTabTitle, vm.DraftSummary);
        vm.SelectDraftForReview(); Assert.Equal(1, vm.ActiveTab);
        vm.ActiveTab = 1; Assert.True(vm.HasSelectedChanges);
        vm.DiscardMemoryDraft(); Assert.False(vm.HasMemoryDraft); Assert.False(vm.HasPendingDrafts);
    }

    [Fact]
    public async Task PendingChangeDisablesEditorAndReportsPostBootVerificationFailure()
    {
        var loc = new Localization(); var pages = new Pages { Pending = new(1, PageFileService.Resource, "{}", "{}", null,
            "SavedRestartRequired", "old-boot", "PostBootVerificationPending") };
        var vm = Create(loc, pages); await vm.RefreshMemoryAsync();
        Assert.False(vm.CanEditMemory); Assert.False(vm.CanEditManual);
        Assert.Equal(loc["Page_Result_PostBootVerificationPending"], vm.PageFileStatus);
    }

    [Fact]
    public void NewLocalizedKeysHaveParityAndCompatibleFormatting()
    {
        var ru = Localization.Strings("ru"); var en = Localization.Strings("en");
        var keys = ru.Keys.Where(k => k.StartsWith("System_") || k.StartsWith("Page_") || k.StartsWith("Memory_")).Order().ToArray();
        Assert.Equal(keys, en.Keys.Where(k => k.StartsWith("System_") || k.StartsWith("Page_") || k.StartsWith("Memory_")).Order().ToArray());
        foreach (var key in keys)
        {
            static string[] Placeholders(string value) => System.Text.RegularExpressions.Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).Order().ToArray();
            Assert.Equal(Placeholders(ru[key]), Placeholders(en[key]));
        }
    }
}
