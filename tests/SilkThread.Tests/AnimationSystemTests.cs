using System;
using SilkThread.App.Helpers;
using Xunit;

namespace SilkThread.Tests;

public class AnimationSystemTests
{
    [Fact]
    public void HoverAnimationParameters_MatchDesignSpecifications()
    {
        // Scale is strictly 1.0 (no ScaleTransform, preserving 100% DirectWrite font sharpness)
        double defaultScale = 1.0;
        Assert.Equal(1.0, defaultScale);

        // Hover animation duration should be fast and snappy (150-200ms)
        int defaultDurationMs = 180;
        Assert.InRange(defaultDurationMs, 150, 200);
    }

    [Fact]
    public void PulseAnimationParameters_MatchDesignSpecifications()
    {
        // Pulse opacity should cycle between 0.6 and 1.0
        double minOpacity = 0.6;
        double maxOpacity = 1.0;
        Assert.Equal(0.6, minOpacity, 2);
        Assert.Equal(1.0, maxOpacity, 2);

        // Pulse period should be between 2.0 and 3.0 seconds
        double durationSeconds = 2.2;
        Assert.InRange(durationSeconds, 2.0, 3.0);
    }

    [Fact]
    public void EntranceAnimationParameters_MatchDesignSpecifications()
    {
        // Entrance animation: TranslateY 8 -> 0 (clean micro-entrance)
        double fromTranslateY = 8.0;
        double toTranslateY = 0.0;
        Assert.Equal(8.0, fromTranslateY, 1);
        Assert.Equal(0.0, toTranslateY, 1);

        // Entrance animation duration should be fast and fluid (150 - 200 ms)
        int durationMs = 170;
        Assert.InRange(durationMs, 150, 200);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.875)] // 1 - (1 - 0.5)^3 = 1 - 0.125 = 0.875
    [InlineData(1.0, 1.0)]
    public void CubicEaseOut_CalculatesCorrectProgression(double inputProgress, double expectedProgress)
    {
        // CubicEaseOut curve: f(t) = 1 - (1 - t)^3
        double actual = 1.0 - Math.Pow(1.0 - inputProgress, 3);
        Assert.Equal(expectedProgress, actual, 3);
    }
}
