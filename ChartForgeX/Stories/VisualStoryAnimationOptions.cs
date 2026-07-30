using System;

namespace ChartForgeX.Stories;

/// <summary>Configures animated raster export for a generic visual story.</summary>
public sealed class VisualStoryAnimationOptions {
    /// <summary>Gets the requested frame rate.</summary>
    public int FramesPerSecond { get; private set; } = 6;

    /// <summary>Gets whether the animation repeats.</summary>
    public bool Loop { get; private set; } = true;

    /// <summary>Gets the completed-state hold time.</summary>
    public double EndHoldSeconds { get; private set; } = 1.5;

    /// <summary>Gets the raster output density multiplier.</summary>
    public int OutputScale { get; private set; } = 1;

    /// <summary>Gets the maximum retained frame count.</summary>
    public int MaximumFrames { get; private set; } = 240;

    /// <summary>Gets the cross-fade duration between scenes.</summary>
    public double TransitionSeconds { get; private set; } = 0.24;

    /// <summary>Creates options with documentation and Discord-friendly defaults.</summary>
    public static VisualStoryAnimationOptions Create() => new();

    /// <summary>Sets the requested frame rate.</summary>
    public VisualStoryAnimationOptions WithFramesPerSecond(int framesPerSecond) {
        if (framesPerSecond < 2 || framesPerSecond > 30) throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        FramesPerSecond = framesPerSecond;
        return this;
    }

    /// <summary>Configures whether the animation repeats.</summary>
    public VisualStoryAnimationOptions WithLoop(bool loop = true) {
        Loop = loop;
        return this;
    }

    /// <summary>Sets how long the completed scene remains visible.</summary>
    public VisualStoryAnimationOptions WithEndHold(double seconds) {
        FiniteRange(seconds, 0, 10, nameof(seconds));
        EndHoldSeconds = seconds;
        return this;
    }

    /// <summary>Sets the raster output density multiplier.</summary>
    public VisualStoryAnimationOptions WithOutputScale(int scale) {
        if (scale < 1 || scale > 4) throw new ArgumentOutOfRangeException(nameof(scale));
        OutputScale = scale;
        return this;
    }

    /// <summary>Sets the maximum retained frame count.</summary>
    public VisualStoryAnimationOptions WithMaximumFrames(int maximumFrames) {
        if (maximumFrames < 2 || maximumFrames > 600) throw new ArgumentOutOfRangeException(nameof(maximumFrames));
        MaximumFrames = maximumFrames;
        return this;
    }

    /// <summary>Sets the cross-fade duration between scenes.</summary>
    public VisualStoryAnimationOptions WithTransition(double seconds) {
        FiniteRange(seconds, 0, 1, nameof(seconds));
        TransitionSeconds = seconds;
        return this;
    }

    private static void FiniteRange(double value, double minimum, double maximum, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }
}
