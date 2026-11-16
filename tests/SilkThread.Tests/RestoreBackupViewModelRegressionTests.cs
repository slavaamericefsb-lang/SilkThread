using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.Models;
using SilkThread.Core.RestorePointManager;
using Xunit;

namespace SilkThread.Tests;

public class RestoreBackupViewModelRegressionTests
{
    [Theory]
    [InlineData("create")]
    [InlineData("delete")]
    [InlineData("enable")]
    public async Task OperationExceptions_AreVisibleAndReleaseBusyState(string operation)
    {
        var vm = new RestoreBackupViewModel(new FailingRestorePoints(), null!, LocalizationService.Instance);
        if (operation == "create") await vm.CreateRestorePointAsync();
        else if (operation == "enable") await vm.EnableProtectionAsync();
        else await vm.DeleteRestorePointAsync(new RestorePointItem(1, "test", DateTime.UtcNow, "test"));
        Assert.False(vm.IsBusy);
        Assert.Contains("simulated failure", vm.OperationMessage);
    }

    [Fact]
    public async Task AccessDeniedIsNotReportedAsEmptyAndCachedPointsSurvive()
    {
        var source = new FailingRestorePoints { Points = new() { new(42, "Снимок Windows", DateTime.Now, "test") } };
        var vm = new RestoreBackupViewModel(source, null!, LocalizationService.Instance);
        await vm.LoadRestorePointsAsync();
        Assert.Single(vm.RestorePoints);
        source.Points = null; source.ReadError = new UnauthorizedAccessException();
        await vm.LoadRestorePointsAsync();
        Assert.True(vm.HasListReadError); Assert.False(vm.HasNoPoints); Assert.Single(vm.RestorePoints);
        Assert.Equal("—", vm.RestorePointsCountDisplay);
        Assert.False(vm.IsBusy); Assert.NotEmpty(vm.OperationMessage!);
        source.Points = new(); await vm.LoadRestorePointsAsync();
        Assert.False(vm.HasListReadError); Assert.True(vm.HasNoPoints); Assert.Null(vm.OperationMessage);
        Assert.Equal("0", vm.RestorePointsCountDisplay);
    }

    [Fact]
    public async Task InitialReadFailureNeverInventsNoRestorePoints()
    {
        var vm = new RestoreBackupViewModel(new FailingRestorePoints(), null!, LocalizationService.Instance);
        Assert.False(vm.HasNoPoints);
        await vm.LoadRestorePointsAsync();
        Assert.True(vm.HasListReadError); Assert.False(vm.HasNoPoints);
    }

    [Theory]
    [InlineData("[]", 0)]
    [InlineData("{\"SequenceNumber\":12,\"Description\":\"Тест\",\"CreationTime\":\"20261115120000.000000+180\"}", 1)]
    [InlineData("[{\"SequenceNumber\":12,\"Description\":\"Тест\"},{\"SequenceNumber\":13,\"Description\":\"Другой\"}]", 2)]
    public void JsonFallbackReadsEmptySingleAndMultipleRecords(string json, int count)
    {
        var points = SilkThread.Core.RestorePointManager.RestorePointManager.ParseRestorePointsJson(json);
        Assert.Equal(count, points.Count);
        Assert.Equal(points.OrderByDescending(p => p.SequenceNumber), points);
    }

    [Theory]
    [InlineData("null")] [InlineData("garbage")] [InlineData("[{\"Description\":\"missing id\"}]")]
    public void FailedJsonReadIsNeverConvertedToEmptyList(string json)
        => Assert.ThrowsAny<Exception>(() => SilkThread.Core.RestorePointManager.RestorePointManager.ParseRestorePointsJson(json));

    [Fact]
    public void UnknownProtectionIsNotDisplayedAsDisabled()
    {
        var vm = new RestoreBackupViewModel(new FailingRestorePoints(), null!, LocalizationService.Instance);
        vm.Status = new(false, false, "Unavailable") { ProtectionStateKnown = false };
        Assert.False(vm.IsProtectionActive); Assert.False(vm.IsProtectionInactive);
    }

    private sealed class FailingRestorePoints : IRestorePointManager
    {
        public List<RestorePointItem>? Points;
        public Exception ReadError = new InvalidOperationException("simulated failure");
        public Task<RestorePointStatus> CheckStatusAsync() => throw new InvalidOperationException("simulated failure");
        public Task<List<RestorePointItem>> GetRestorePointsAsync() => Points == null ? throw ReadError : Task.FromResult(Points);
        public Task<(bool Success, string Message, long SequenceNumber)> CreateRestorePointAsync(string description) => throw new InvalidOperationException("simulated failure");
        public Task<(bool Success, string Message)> DeleteRestorePointAsync(long sequenceNumber) => throw new InvalidOperationException("simulated failure");
        public Task<(bool Success, string Message)> EnableProtectionAsync(string drive = "C:\\") => throw new InvalidOperationException("simulated failure");
    }
}
