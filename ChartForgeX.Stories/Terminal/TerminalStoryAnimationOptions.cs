using System;
using PowerForge.Fluent;

namespace ChartForgeX.Terminal;

/// <summary>
/// Configures animated raster export for a terminal story.
/// </summary>
[Fluent]
public sealed class TerminalStoryAnimationOptions {
    private int _framesPerSecond = 10;
    private double _endHoldSeconds = 1.2;
    private int _outputScale = 1;
    private int _maximumFrames = 240;

    /// <summary>Gets or sets the maximum sampling frame rate, from 2 to 30. GIF and APNG round the regular interval up to a whole centisecond.</summary>
    /// <remarks>
    /// GIF and APNG sample at regular intervals rounded up to whole centiseconds, keeping that cadence at or below the requested rate.
    /// The first completed sample may occur up to one interval after the logical story end. The last frame can use a shorter residual hold delay.
    /// </remarks>
    public int FramesPerSecond {
        get => _framesPerSecond;
        set {
            if (value < 2 || value > 30) throw new ArgumentOutOfRangeException(nameof(value), "Animated terminal stories support 2 to 30 frames per second.");
            _framesPerSecond = value;
        }
    }

    /// <summary>Gets or sets whether the animation repeats.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Gets or sets the completed-state hold time (0 to 10 seconds) before the animation repeats or ends, measured from the first completed sample.</summary>
    /// <remarks>
    /// The hold is rounded to the nearest centisecond, with a minimum of one centisecond for the completed frame, including when the hold is zero.
    /// The animation duration includes the regular sampling interval needed to reach that frame.
    /// </remarks>
    public double EndHoldSeconds {
        get => _endHoldSeconds;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 10) throw new ArgumentOutOfRangeException(nameof(value));
            _endHoldSeconds = value;
        }
    }

    /// <summary>Gets or sets the raster output density multiplier, from 1 to 4.</summary>
    public int OutputScale {
        get => _outputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value));
            _outputScale = value;
        }
    }

    /// <summary>Gets or sets the frame budget used to bound memory and file size, from 2 to 600.</summary>
    public int MaximumFrames {
        get => _maximumFrames;
        set {
            if (value < 2 || value > 600) throw new ArgumentOutOfRangeException(nameof(value), "Animated terminal stories support a frame budget from 2 to 600.");
            _maximumFrames = value;
        }
    }

    /// <summary>Creates animation options with Discord-friendly defaults.</summary>
    public static TerminalStoryAnimationOptions Create() => new();

    /// <summary>Sets the maximum sampling frame rate.</summary>
    public TerminalStoryAnimationOptions WithFramesPerSecond(int framesPerSecond) {
        FramesPerSecond = framesPerSecond;
        return this;
    }

    /// <summary>Configures whether the animation repeats.</summary>
    public TerminalStoryAnimationOptions WithLoop(bool loop = true) {
        Loop = loop;
        return this;
    }

    /// <summary>Sets how long the completed state remains visible from the first completed sample.</summary>
    public TerminalStoryAnimationOptions WithEndHold(double seconds) {
        EndHoldSeconds = seconds;
        return this;
    }

    /// <summary>Sets the raster output density multiplier.</summary>
    public TerminalStoryAnimationOptions WithOutputScale(int scale) {
        OutputScale = scale;
        return this;
    }

    /// <summary>Sets the frame budget used to bound memory and file size.</summary>
    public TerminalStoryAnimationOptions WithMaximumFrames(int maximumFrames) {
        MaximumFrames = maximumFrames;
        return this;
    }
}
