using SilkThread.App.ViewModels;
using SilkThread.Core.Models;
using SilkThread.Core.PresetManager;
using SilkThread.Core.TweakDatabase;
using Xunit;

namespace SilkThread.Tests;

public class PresetReviewOrderingTests
{
    private static PresetReviewViewModel MixedReview()
    {
        var definitions = TweakCatalog.GetAllTweaks().GroupBy(t => t.Category).Take(2).Select(g => g.Take(2).ToList()).ToList();
        var evaluated = new EvaluatedPreset { Preset = new TweakPreset { Name = "Mixed" } };
        foreach (var category in definitions)
        {
            for (int i = 0; i < category.Count; i++)
                evaluated.Tweaks.Add(new PresetTweakEvaluation
                {
                    TweakId = category[i].Id, Tweak = category[i], IsSelected = true,
                    Compatibility = i == 0 ? CompatibilityResult.Compatible : CompatibilityResult.Incompatible("Unsupported hardware")
                });
        }
        return new PresetReviewViewModel(evaluated, null!);
    }

    [Fact]
    public void DisplayedGroups_PlaceEveryIncompatibleBeforeAnyCompatible()
    {
        var vm = MixedReview();
        var displayed = vm.CategoryGroups.SelectMany(g => g.Items).ToList();
        Assert.Equal(4, displayed.Count);
        Assert.All(displayed.Take(2), t => Assert.True(t.IsIncompatible));
        Assert.All(displayed.Skip(2), t => Assert.True(t.IsCompatible));
        Assert.All(displayed.Take(2), t => Assert.Equal("Unsupported hardware", t.IncompatibilityReason));
    }

    [Fact]
    public void IncompatibleRows_CannotBeSelectedEvenBySelectAll()
    {
        var vm = MixedReview();
        vm.SelectAll();
        foreach (var row in vm.Tweaks.Where(t => t.IsIncompatible)) row.IsSelected = true;
        Assert.All(vm.Tweaks.Where(t => t.IsIncompatible), t => Assert.False(t.IsSelected));
        Assert.Equal(2, vm.SelectedCount);
        vm.DeselectAll();
        Assert.False(vm.CanApply);
    }
}
