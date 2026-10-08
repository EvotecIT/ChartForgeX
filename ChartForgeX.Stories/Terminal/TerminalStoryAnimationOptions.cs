using System;

namespace ChartForgeX.Terminal;

/// <summary>
/// Configures animated raster export for a terminal story.
/// </summary>
public sealed class TerminalStoryAnimationOptions {
    /// <summary>Gets the requested frame rate.</summary>
    public int FramesPerSecond { get; private set; } = 10;

    /// <summary>Gets whether the animation repeats.</summary>
    public bool Loop { get; private set; } = true;

    /// <summary>Gets the completed-state hold time before the animation repeats or ends.</summary>
    public double EndHoldSeconds { get; private set; } = 1.2;

    /// <summary>Gets the raster output density multiplier.</summary>
    public int OutputScale { get; private set; } = 1;

    /// <summary>Gets the maximum number of rendered frames.</summary>
    public int MaximumFrames { get; private set; } = 240;

    /// <summary>Creates animation options with Discord-friendly defaults.</summary>
    public static TerminalStoryAnimationOptions Create() => new();

    /// <summary>Sets the requested frame rate.</summary>
    public TerminalStoryAnimationOptions WithFramesPerSecond(int framesPerSecond) {
        if (framesPerSecond < 2 || framesPerSecond > 30) throw new ArgumentOutOfRangeException(nameof(framesPerSecond), "Animated terminal stories support 2 to 30 frames per second.");
        FramesPerSecond = framesPerSecond;
        return this;
    }

    /// <summary>Configures whether the animation repeats.</summary>
    public TerminalStoryAnimationOptions WithLoop(bool loop = true) {
        Loop = loop;
        return this;
    }

    /// <summary>Sets how long the completed state remains visible.</summary>
    public TerminalStoryAnimationOptions WithEndHold(double seconds) {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 10) throw new ArgumentOutOfRangeException(nameof(seconds));
        EndHoldSeconds = seconds;
        return this;
    }

    /// <summary>Sets the raster output density multiplier.</summary>
    public TerminalStoryAnimationOptions WithOutputScale(int scale) {
        if (scale < 1 || scale > 4) throw new ArgumentOutOfRangeException(nameof(scale));
        OutputScale = scale;
        return this;
    }

    /// <summary>Sets the frame budget used to bound memory and file size.</summary>
    public TerminalStoryAnimationOptions WithMaximumFrames(int maximumFrames) {
        if (maximumFrames < 2 || maximumFrames > 600) throw new ArgumentOutOfRangeException(nameof(maximumFrames), "Animated terminal stories support a frame budget from 2 to 600.");
        MaximumFrames = maximumFrames;
        return this;
    }
}
