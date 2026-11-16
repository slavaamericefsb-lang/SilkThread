using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.GameProfiles;
using Xunit;

namespace SilkThread.Tests;

public class GamesViewModelRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProfileCompletion_UpdatesOriginalGameAfterSelectionChanges(bool revert)
    {
        var service = new DeferredProfiles();
        var vm = new GamesViewModel(service, LocalizationService.Instance);
        var original = vm.AllGames[0];
        var other = vm.AllGames[1];
        original.IsProfileApplied = revert;
        var operation = revert ? vm.RevertSystemProfileAsync() : vm.ApplySystemProfileAsync();
        Assert.Equal(original.Id, service.RequestedId);
        vm.SelectGame(other);
        service.Completion.SetResult(true);
        await operation;
        Assert.Equal(!revert, original.IsProfileApplied);
        Assert.False(other.IsProfileApplied);
        Assert.False(vm.IsApplyingProfile);
    }

    [Fact]
    public async Task ProfileCompletion_SurvivesFilterWithNoResults()
    {
        var service = new DeferredProfiles();
        var vm = new GamesViewModel(service, LocalizationService.Instance);
        var original = vm.SelectedGame!;
        var operation = vm.ApplySystemProfileAsync();
        vm.SearchQuery = "no-game-can-match-this-query";
        Assert.Null(vm.SelectedGame);
        Assert.False(original.IsSelected);
        service.Completion.SetResult(true);
        await operation;
        Assert.True(original.IsProfileApplied);
        Assert.Equal(string.Empty, vm.SystemProfileStatusText);
    }

    private sealed class DeferredProfiles : IGameProfileService
    {
        public TaskCompletionSource<bool> Completion { get; } = new();
        public string? RequestedId { get; private set; }
        public IReadOnlyList<GameProfile> GetAllProfiles() => GameProfileCatalog.GetAllProfiles();
        public GameProfile? GetProfileById(string id) => GetAllProfiles().FirstOrDefault(x => x.Id == id);
        public bool IsProfileApplied(string id) => false;
        public Task<bool> ApplyProfileAsync(string id) { RequestedId = id; return Completion.Task; }
        public Task<bool> RevertProfileAsync(string id) { RequestedId = id; return Completion.Task; }
    }
}

