using System;
using PowerForge.Fluent;

namespace ChartForgeX.Stories;

/// <summary>Configures animated raster export for a generic visual story.</summary>
[Fluent]
public sealed class VisualStoryAnimationOptions {
    internal const double DefaultTransitionSeconds = 0.24;
    internal const double DefaultEndHoldSeconds = 1.5;

    private int _framesPerSecond = 6;
    private double _endHoldSeconds = DefaultEndHoldSeconds;
    private int _outputScale = 1;
    private int _maximumFrames = 240;
    private double _transitionSeconds = DefaultTransitionSeconds;

    /// <summary>Gets or sets the requested frame rate, from 2 to 30.</summary>
    public int FramesPerSecond {
        get => _framesPerSecond;
        set {
            if (value < 2 || value > 30) throw new ArgumentOutOfRangeException(nameof(value));
            _framesPerSecond = value;
        }
    }

    /// <summary>Gets or sets whether the animation repeats.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Gets or sets how long the completed scene remains visible, from 0 to 10 seconds.</summary>
    public double EndHoldSeconds {
        get => _endHoldSeconds;
        set {
            FiniteRange(value, 0, 10, nameof(value));
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

    /// <summary>Gets or sets the maximum sampled frame count, from 2 to 600; export produces frames one at a time.</summary>
    public int MaximumFrames {
        get => _maximumFrames;
        set {
            if (value < 2 || value > 600) throw new ArgumentOutOfRangeException(nameof(value));
            _maximumFrames = value;
        }
    }

    /// <summary>Gets or sets the cross-fade duration between scenes, from 0 to 1 second.</summary>
    public double TransitionSeconds {
        get => _transitionSeconds;
        set {
            FiniteRange(value, 0, 1, nameof(value));
            _transitionSeconds = value;
        }
    }

    /// <summary>Creates options with documentation and Discord-friendly defaults.</summary>
    public static VisualStoryAnimationOptions Create() => new();

    private static void FiniteRange(double value, double minimum, double maximum, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }

    internal VisualStoryPlaybackOptions Playback() => new(TimeSpan.FromTicks((long)Math.Round(EndHoldSeconds * TimeSpan.TicksPerSecond)),
        TimeSpan.FromTicks((long)Math.Round(TransitionSeconds * TimeSpan.TicksPerSecond)), Loop ? 0 : 1);
    internal VisualStoryFrameOptions Sampling() => new(FramesPerSecond, OutputScale, MaximumFrames);
}
