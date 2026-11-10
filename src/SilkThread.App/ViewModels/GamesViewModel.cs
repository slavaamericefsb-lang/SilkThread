using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.Core.GameProfiles;

namespace SilkThread.App.ViewModels;

public partial class GameCardViewModel : ObservableObject
{
    public GameProfile Profile { get; }

    public string Id => Profile.Id;
    public string Title => Profile.Title;
    public string ShortTitle => Profile.ShortTitle;
    public string Genre => Profile.Genre;
    public GameGenreCategory GenreCategory => Profile.GenreCategory;
    public string EngineBadge => Profile.EngineBadge;
    public string Launcher => Profile.Launcher;
    public string AccentColorHex => Profile.AccentColorHex;
    public string IconGlyph => Profile.IconGlyph;
    public string Description => Profile.Description;
    public string LaunchOptions => Profile.LaunchOptions;
    public IReadOnlyList<LaunchArgItem> LaunchArgs => Profile.LaunchArgs;
    public IReadOnlyList<VideoSettingItem> VideoSettings => Profile.VideoSettings;
    public IReadOnlyList<ConfigFileInfo> ConfigFiles => Profile.ConfigFiles;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isProfileApplied;

    [ObservableProperty]
    private string _statusBadge = "Готов";

    public GameCardViewModel(GameProfile profile, bool isApplied)
    {
        Profile = profile;
        _isProfileApplied = isApplied;
        _statusBadge = isApplied ? "Активен ✓" : "Готов";
    }
}

public partial class GamesViewModel : ObservableObject
{
    private readonly IGameProfileService _profileService;
    private readonly ILocalizationService _loc;

    public ObservableCollection<GameCardViewModel> AllGames { get; } = new();
    public ObservableCollection<GameCardViewModel> FilteredGames { get; } = new();

    [ObservableProperty]
    private GameCardViewModel? _selectedGame;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private GameGenreCategory _currentFilter = GameGenreCategory.All;

    [ObservableProperty]
    private bool _isCopied;

    [ObservableProperty]
    private string _copyConfirmationText = "Скопировано!";

    [ObservableProperty]
    private bool _isConfigCopied;

    [ObservableProperty]
    private string _configCopiedNotification = "Путь скопирован!";

    [ObservableProperty]
    private bool _isApplyingProfile;

    [ObservableProperty]
    private string _systemProfileStatusText = string.Empty;

    [ObservableProperty]
    private string _appliedProfilesSummary = string.Empty;

    // Filter toggle helpers
    public bool IsFilterAll => CurrentFilter == GameGenreCategory.All;
    public bool IsFilterShooters => CurrentFilter == GameGenreCategory.Shooter;
    public bool IsFilterMoba => CurrentFilter == GameGenreCategory.Moba;
    public bool IsFilterBattleRoyale => CurrentFilter == GameGenreCategory.BattleRoyale;

    // Localized Strings
    public string PageTitle => _loc["Games_Title"];
    public string PageSubtitle => _loc["Games_Subtitle"];
    public string StatTotalBadge => _loc["Games_Stat_Total"];
    public string StatReadyBadge => _loc["Games_Stat_Ready"];
    public string FilterAllLabel => _loc["Games_Filter_All"];
    public string FilterShootersLabel => _loc["Games_Filter_Shooters"];
    public string FilterMobaLabel => _loc["Games_Filter_Moba"];
    public string FilterBattleRoyaleLabel => _loc["Games_Filter_BattleRoyale"];
    public string SearchPlaceholder => _loc["Games_Search_Placeholder"];
    public string ConfigureLabel => _loc["Games_Card_Configure"];
    public string Block1Title => _loc["Games_Block1_Title"];
    public string Block1Subtitle => _loc["Games_Block1_Subtitle"];
    public string CopyBtnText => _loc["Games_Copy_Btn"];
    public string CopiedBadge => _loc["Games_Copied_Badge"];
    public string BreakdownTitle => _loc["Games_Breakdown_Title"];
    public string Block2Title => _loc["Games_Block2_Title"];
    public string Block2Subtitle => _loc["Games_Block2_Subtitle"];
    public string Block3Title => _loc["Games_Block3_Title"];
    public string Block3Subtitle => _loc["Games_Block3_Subtitle"];
    public string ApplyProfileBtnText => _loc["Games_ApplyProfile_Btn"];
    public string RevertProfileBtnText => _loc["Games_RevertProfile_Btn"];
    public string CopyPathBtnText => _loc["Games_CopyPath_Btn"];
    public string NoResultsText => _loc["Games_NoResults"];

    public GamesViewModel(IGameProfileService profileService, ILocalizationService loc)
    {
        _profileService = profileService;
        _loc = loc;

        InitializeProfiles();

        _loc.LanguageChanged += (s, e) =>
        {
            OnPropertyChanged(string.Empty);
            UpdateAppliedSummary();
            UpdateStatusText();
        };
    }

    private void InitializeProfiles()
    {
        AllGames.Clear();
        var rawProfiles = _profileService.GetAllProfiles();

        foreach (var profile in rawProfiles)
        {
            bool isApplied = _profileService.IsProfileApplied(profile.Id);
            var item = new GameCardViewModel(profile, isApplied);
            AllGames.Add(item);
        }

        ApplyFilter();

        if (FilteredGames.Count > 0)
        {
            SelectGame(FilteredGames[0]);
        }

        UpdateAppliedSummary();
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnCurrentFilterChanged(GameGenreCategory value)
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterShooters));
        OnPropertyChanged(nameof(IsFilterMoba));
        OnPropertyChanged(nameof(IsFilterBattleRoyale));
        ApplyFilter();
    }

    [RelayCommand]
    public void SetFilterCategory(string categoryName)
    {
        CurrentFilter = categoryName.ToLowerInvariant() switch
        {
            "shooter" or "shooters" => GameGenreCategory.Shooter,
            "moba" => GameGenreCategory.Moba,
            "battleroyale" or "br" => GameGenreCategory.BattleRoyale,
            _ => GameGenreCategory.All
        };
    }

    [RelayCommand]
    public void SelectGame(GameCardViewModel? game)
    {
        if (game == null) return;

        foreach (var g in AllGames)
        {
            g.IsSelected = (g.Id == game.Id);
        }

        SelectedGame = game;
        UpdateStatusText();
    }

    private void ApplyFilter()
    {
        FilteredGames.Clear();

        var query = SearchQuery?.Trim() ?? string.Empty;

        foreach (var game in AllGames)
        {
            // Category check
            if (CurrentFilter != GameGenreCategory.All && game.GenreCategory != CurrentFilter)
            {
                continue;
            }

            // Search query check
            if (!string.IsNullOrEmpty(query))
            {
                bool matchesTitle = game.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchesShort = game.ShortTitle.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchesEngine = game.EngineBadge.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchesLauncher = game.Launcher.Contains(query, StringComparison.OrdinalIgnoreCase);

                if (!matchesTitle && !matchesShort && !matchesEngine && !matchesLauncher)
                {
                    continue;
                }
            }

            FilteredGames.Add(game);
        }

        // If currently selected game is not in filtered list, select first available
        if (SelectedGame == null || !FilteredGames.Contains(SelectedGame))
        {
            if (FilteredGames.Count > 0)
            {
                SelectGame(FilteredGames[0]);
            }
            else
            {
                SelectedGame = null;
            }
        }
    }

    [RelayCommand]
    public async Task CopyLaunchOptionsAsync()
    {
        if (SelectedGame == null) return;

        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(SelectedGame.LaunchOptions);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        catch (Exception ex)
        {
            App.Log($"[CopyLaunchOptions error]: {ex.Message}");
        }

        CopyConfirmationText = _loc["Games_Copied_Badge"];
        IsCopied = true;
        await Task.Delay(2500);
        IsCopied = false;
    }

    [RelayCommand]
    public async Task CopyConfigPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(path);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        catch (Exception ex)
        {
            App.Log($"[CopyConfigPath error]: {ex.Message}");
        }

        ConfigCopiedNotification = _loc.CurrentLanguage == "ru" ? "Путь скопирован! ✓" : "Path copied! ✓";
        IsConfigCopied = true;
        await Task.Delay(2000);
        IsConfigCopied = false;
    }

    [RelayCommand]
    public async Task ApplySystemProfileAsync()
    {
        if (SelectedGame == null) return;

        IsApplyingProfile = true;
        try
        {
            bool success = await _profileService.ApplyProfileAsync(SelectedGame.Id);
            if (success)
            {
                SelectedGame.IsProfileApplied = true;
                SelectedGame.StatusBadge = _loc.CurrentLanguage == "ru" ? "Активен ✓" : "Active ✓";
                UpdateAppliedSummary();
                UpdateStatusText();
            }
        }
        catch (Exception ex)
        {
            App.Log($"[ApplySystemProfile exception]: {ex.Message}");
        }
        finally
        {
            IsApplyingProfile = false;
        }
    }

    [RelayCommand]
    public async Task RevertSystemProfileAsync()
    {
        if (SelectedGame == null) return;

        IsApplyingProfile = true;
        try
        {
            bool success = await _profileService.RevertProfileAsync(SelectedGame.Id);
            if (success)
            {
                SelectedGame.IsProfileApplied = false;
                SelectedGame.StatusBadge = _loc.CurrentLanguage == "ru" ? "Готов" : "Ready";
                UpdateAppliedSummary();
                UpdateStatusText();
            }
        }
        catch (Exception ex)
        {
            App.Log($"[RevertSystemProfile exception]: {ex.Message}");
        }
        finally
        {
            IsApplyingProfile = false;
        }
    }

    private void UpdateAppliedSummary()
    {
        int applied = AllGames.Count(g => g.IsProfileApplied);
        int total = AllGames.Count;
        AppliedProfilesSummary = string.Format(_loc["Games_Stat_Applied"], applied, total);
    }

    private void UpdateStatusText()
    {
        if (SelectedGame == null)
        {
            SystemProfileStatusText = string.Empty;
            return;
        }

        SystemProfileStatusText = SelectedGame.IsProfileApplied
            ? _loc["Games_Status_Active"]
            : _loc["Games_Status_Ready"];
    }
}
