using SilkThread.Core.Models;
using Xunit;

namespace SilkThread.Tests;

public class StatusProbeSchedulingTests
{
    private static SilkThread.Core.TweakEngine.TweakEngine Engine(Func<Task<TweakStatus>> check) => new(
        new[] { new TweakDefinition
        {
            Id = "probe", Title = "Probe", Description = "Probe", HowItWorks = "Probe",
            ProblemSolved = "Probe", ExpectedResult = "Probe", Category = TweakCategory.AdvancedTweaks,
            Risk = RiskLevel.Low, Recommendation = RecommendationLevel.Optional,
            CheckCurrentStateAsync = check,
            ApplyAsync = () => throw new NotSupportedException(),
            RollbackAsync = _ => throw new NotSupportedException()
        } }, null!, null!, null!);

    [Fact]
    public async Task StatusDelegate_DoesNotRunOnCallersSynchronizationContext()
    {
        var marker = new SynchronizationContext();
        SynchronizationContext? observed = marker;
        var engine = Engine(() => { observed = SynchronizationContext.Current; return Task.FromResult(TweakStatus.Applied); });
        var original = SynchronizationContext.Current;
        Task<TweakStatus> operation;
        try
        {
            SynchronizationContext.SetSynchronizationContext(marker);
            operation = engine.CheckTweakStatusAsync("probe");
        }
        finally { SynchronizationContext.SetSynchronizationContext(original); }
        Assert.Equal(TweakStatus.Applied, await operation);
        Assert.NotSame(marker, observed);
    }

    [Fact]
    public async Task StatusDelegate_BoundsConcurrentReadsAndReleasesWorkersAfterFailures()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedFour = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int active = 0, peak = 0;
        var engine = Engine(async () =>
        {
            int count = Interlocked.Increment(ref active);
            lock (release) peak = Math.Max(peak, count);
            if (count == 4) startedFour.TrySetResult();
            try { await release.Task; throw new InvalidOperationException("probe failed"); }
            finally { Interlocked.Decrement(ref active); }
        });
        var operations = Enumerable.Range(0, 12).Select(_ => engine.CheckTweakStatusAsync("probe")).ToArray();
        try { await startedFour.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { release.TrySetResult(); }
        var results = await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(4, peak);
        Assert.All(results, status => Assert.Equal(TweakStatus.Failed, status));
        Assert.Equal(0, active);
    }
}
