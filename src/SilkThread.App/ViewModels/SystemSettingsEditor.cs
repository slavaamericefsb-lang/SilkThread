using CommunityToolkit.Mvvm.ComponentModel;
using SilkThread.App.Services;
using SilkThread.Core.SystemOperations;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SilkThread.App.ViewModels;

public sealed class PageFileVolumeEditor : ObservableObject
{
    private readonly ILocalizationService _loc;
    private int _mode;
    private string _initial = "1024", _maximum = "4096";
    public string Root { get; }
    public string Path => Root.TrimEnd('\\') + @"\pagefile.sys";
    public int Mode { get => _mode; set { if (value is >= 0 and <= 2 && SetProperty(ref _mode, value)) { OnPropertyChanged(nameof(IsCustom)); Changed?.Invoke(); } } }
    public bool IsCustom => Mode == 2;
    public string Initial { get => _initial; set { if (SetProperty(ref _initial, value)) Changed?.Invoke(); } }
    public string Maximum { get => _maximum; set { if (SetProperty(ref _maximum, value)) Changed?.Invoke(); } }
    public string[] Modes => new[] { _loc["Page_None"], _loc["Memory_SystemSize"], _loc["Page_Custom"] };
    public string InitialLabel => _loc["Page_Initial"];
    public string MaximumLabel => _loc["Page_Maximum"];
    public void NotifyLanguage() { OnPropertyChanged(nameof(Modes)); OnPropertyChanged(nameof(Mode)); OnPropertyChanged(nameof(InitialLabel)); OnPropertyChanged(nameof(MaximumLabel)); }
    public event Action? Changed;
    public PageFileVolumeEditor(string root, ILocalizationService loc)
    {
        Root = root; _loc = loc;
    }
}

public partial class SystemSettingsViewModel
{
    private readonly IPageFileService _pageFiles;
    private ConfigurationChange? _pagePending;
    private bool _automaticPageFile = true, _allowRemoval, _hasMemoryDraft;
    private string _pageFileStatus = "";
    public ObservableCollection<PageFileVolumeEditor> PageFileEditors { get; } = new();
    public bool AutomaticPageFile { get => _automaticPageFile; set { if (SetProperty(ref _automaticPageFile, value)) { MarkMemoryDraft(); OnPropertyChanged(nameof(CanEditManual)); } } }
    public bool AllowRemoval { get => _allowRemoval; set { if (SetProperty(ref _allowRemoval, value)) MarkMemoryDraft(); } }
    public bool HasMemoryDraft { get => _hasMemoryDraft; private set { if (SetProperty(ref _hasMemoryDraft, value)) { NotifyDraftState(); OnPropertyChanged(nameof(InspectorText)); } } }
    public bool CanEditMemory => !IsMemoryBusy && _pagePending == null;
    public bool CanEditManual => CanEditMemory && !AutomaticPageFile;
    public string PageFileStatus { get => _pageFileStatus; private set { if (SetProperty(ref _pageFileStatus, value)) { OnPropertyChanged(nameof(OperationStatus)); OnPropertyChanged(nameof(HasOperationStatus)); } } }
    public string AutomaticLabel => _loc["Page_Automatic"];
    public string EnabledLabel => _loc["Memory_On"];
    public string DisabledLabel => _loc["Memory_Off"];
    public string RemovalLabel => _loc["Page_Removal"];
    public string PageReviewLabel => _loc["Page_Review"];
    public string PageRestoreLabel => _loc["Page_Restore"];
    public string PageRiskLabel => _loc["Page_Risk"];
    private void MarkMemoryDraft() { HasMemoryDraft = true; OnPropertyChanged(nameof(InspectorText)); }

    public void DiscardMemoryDraft() { LoadMemoryEditor(); PageFileStatus = ""; }
    private void LoadMemoryEditor()
    {
        _automaticPageFile = _memorySnapshot?.AutomaticMode.Value?.Enabled ?? true;
        _allowRemoval = false;
        PageFileEditors.Clear();
        foreach (var volume in _memorySnapshot?.Volumes.Value ?? Array.Empty<PageFileVolume>())
        {
            var editor = new PageFileVolumeEditor(volume.Root, _loc);
            var setting = _memorySnapshot?.ConfiguredFiles.Value?.FirstOrDefault(f => string.Equals(f.Path, editor.Path, StringComparison.OrdinalIgnoreCase));
            if (setting != null)
            {
                editor.Mode = setting.InitialMb == 0 && setting.MaximumMb == 0 ? 1 : 2;
                editor.Initial = setting.InitialMb.ToString(CultureInfo.InvariantCulture); editor.Maximum = setting.MaximumMb.ToString(CultureInfo.InvariantCulture);
            }
            editor.Changed += MarkMemoryDraft; PageFileEditors.Add(editor);
        }
        HasMemoryDraft = false;
        OnPropertyChanged(nameof(AutomaticPageFile)); OnPropertyChanged(nameof(AllowRemoval)); OnPropertyChanged(nameof(CanEditManual));
        OnPropertyChanged(nameof(InspectorText));
    }

    public async Task<PageFileReview?> ReviewPageFilesAsync()
    {
        if (IsMemoryBusy) return null;
        IsMemoryBusy = true;
        try
        {
            var files = new List<PageFileSetting>();
            if (!AutomaticPageFile) foreach (var editor in PageFileEditors.Where(e => e.Mode != 0))
            {
                uint initial = 0, maximum = 0;
                if (editor.Mode == 2 && (!uint.TryParse(editor.Initial, NumberStyles.None, CultureInfo.InvariantCulture, out initial)
                    || !uint.TryParse(editor.Maximum, NumberStyles.None, CultureInfo.InvariantCulture, out maximum)))
                    throw new InvalidOperationException("InvalidSize");
                files.Add(new(editor.Path, initial, maximum));
            }
            // Never silently discard unusual pre-existing paths when building a manual plan.
            if (!AutomaticPageFile) foreach (var existing in _memorySnapshot?.ConfiguredFiles.Value ?? Array.Empty<PageFileSetting>())
                if (!PageFileEditors.Any(e => string.Equals(e.Path, existing.Path, StringComparison.OrdinalIgnoreCase))) files.Add(existing);
            return await _pageFiles.PreviewAsync(new(AutomaticPageFile, files.ToArray(), AllowRemoval));
        }
        catch (Exception ex)
        {
            App.Log($"[Page file preview]: {ex}");
            var code = ex.Message is "MissingValidationData" or "RemovalAcknowledgement" or "InvalidPath" or "InvalidSize" or "VolumeUnavailable" or "InsufficientSpace" or "CommitCapacity" ? ex.Message : "Failed";
            PageFileStatus = _loc["Page_Validation_" + code]; return null;
        }
        finally { IsMemoryBusy = false; }
    }

    public async Task ApplyPageFilesAsync(PageFileReview review)
    {
        if (IsMemoryBusy) return;
        IsMemoryBusy = true;
        PageFileResult result;
        try { result = await _pageFiles.ApplyAsync(review); }
        catch (Exception ex) { App.Log($"[Page file apply]: {ex}"); result = new("Failed"); }
        finally { IsMemoryBusy = false; }
        HasMemoryDraft = false; await RefreshMemoryAsync(); PageFileStatus = _loc["Page_Result_" + result.Status];
    }

    public Task<PageFileRestoreReview?> PreviewPageFileRestoreAsync() => _pageFiles.PreviewRestoreAsync();
    public async Task RestorePageFilesAsync(PageFileRestoreReview review, bool acknowledgeConflict)
    {
        if (IsMemoryBusy) return;
        IsMemoryBusy = true;
        PageFileResult result;
        try { result = await _pageFiles.RestoreAsync(review, acknowledgeConflict); }
        catch (Exception ex) { App.Log($"[Page file restore]: {ex}"); result = new("Failed"); }
        finally { IsMemoryBusy = false; }
        HasMemoryDraft = false; await RefreshMemoryAsync(); PageFileStatus = _loc["Page_Result_" + result.Status];
    }
}
