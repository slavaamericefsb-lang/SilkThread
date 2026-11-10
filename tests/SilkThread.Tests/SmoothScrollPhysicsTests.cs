using System;
using SilkThread.App.Helpers;
using Xunit;

namespace SilkThread.Tests;

public class SmoothScrollPhysicsTests
{
    [Fact]
    public void DefaultPhysicsConstants_MatchDesignSpecifications()
    {
        Assert.Equal(0.88, SmoothScroll.DefaultDecelerationFriction, 2);
        Assert.Equal(0.14, SmoothScroll.DefaultScrollMultiplier, 2);
        Assert.Equal(0.1, SmoothScroll.StopVelocityThreshold, 2);
        Assert.Equal(250.0, SmoothScroll.MaxVelocity, 1);
    }

    [Theory]
    [InlineData(-120, 0.14, 16.8)]    // 1 standard notch down -> positive downward velocity
    [InlineData(120, 0.14, -16.8)]   // 1 standard notch up -> negative upward velocity
    [InlineData(-240, 0.14, 33.6)]   // 2 fast notches down
    public void VelocityAccumulation_ComputesCorrectImpulse(int wheelDelta, double multiplier, double expectedVelocity)
    {
        // Wheel down (wheelDelta < 0) should produce positive offset velocity (downward scroll)
        double delta = -wheelDelta;
        double velocity = delta * multiplier;

        Assert.Equal(expectedVelocity, velocity, 2);
    }

    [Fact]
    public void DecelerationLoop_StopsCleanlyUnderThreshold()
    {
        double velocity = 16.8;
        double currentOffset = 0;
        double friction = SmoothScroll.DefaultDecelerationFriction;
        int tickCount = 0;

        while (Math.Abs(velocity) >= SmoothScroll.StopVelocityThreshold)
        {
            currentOffset += velocity;
            velocity *= friction;
            tickCount++;

            // Safety assert against infinite loop
            Assert.True(tickCount < 100, "Physics loop exceeded maximum expected tick count");
        }

        // Velocity should have decayed below threshold
        Assert.True(Math.Abs(velocity) < SmoothScroll.StopVelocityThreshold);
        // Total duration at 60fps should be roughly 35-45 frames (~0.6-0.7s)
        Assert.InRange(tickCount, 30, 45);
        // Total offset should be close to V0 / (1 - friction) = 16.8 / 0.12 = 140px
        Assert.InRange(currentOffset, 120.0, 145.0);
    }

    [Fact]
    public void BoundsClamping_GracefullyKillsVelocityAtTopAndBottom()
    {
        double scrollableHeight = 500.0;
        double currentOffset = 10.0;
        double velocity = -25.0; // Fast scroll towards top

        // Simulate tick that crosses top edge
        currentOffset += velocity; // -15.0
        if (currentOffset <= 0)
        {
            currentOffset = 0;
            velocity = 0;
        }

        Assert.Equal(0.0, currentOffset);
        Assert.Equal(0.0, velocity);

        // Simulate tick that crosses bottom edge
        currentOffset = 490.0;
        velocity = 30.0; // Fast scroll towards bottom

        currentOffset += velocity; // 520.0
        if (currentOffset >= scrollableHeight)
        {
            currentOffset = scrollableHeight;
            velocity = 0;
        }

        Assert.Equal(500.0, currentOffset);
        Assert.Equal(0.0, velocity);
    }

    [Fact]
    public void VelocityClamping_RespectsMaxVelocity()
    {
        double extremeImpulse = 1000.0 * SmoothScroll.DefaultScrollMultiplier; // 140.0
        double accumulatedVelocity = 200.0 + extremeImpulse; // 340.0

        double clamped = Math.Clamp(accumulatedVelocity, -SmoothScroll.MaxVelocity, SmoothScroll.MaxVelocity);

        Assert.Equal(SmoothScroll.MaxVelocity, clamped);
    }

    [Fact]
    public void OppositeDirectionImpulse_ReversesMomentumSmoothly()
    {
        double velocity = 20.0; // Moving down
        double reverseImpulse = -120 * SmoothScroll.DefaultScrollMultiplier; // -16.8 (user flicked wheel up)

        velocity += reverseImpulse;

        Assert.Equal(3.2, velocity, 2); // Velocity naturally braked from 20 to 3.2
    }
}
