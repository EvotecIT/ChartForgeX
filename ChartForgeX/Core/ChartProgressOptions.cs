using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Progress bar and ring presentation. Configure it through <see cref="ChartOptions.Progress"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartProgressOptions {
    internal ChartProgressOptions Clone() => (ChartProgressOptions)MemberwiseClone();
    private double _progressMaximum = 100;

    private double _progressBarThicknessRatio = 0.30;

    private double _progressTrackOpacity = 0.24;

    private double _radialProgressRadiusScale = 1.0;

    private double _radialProgressStrokeScale = 1.0;


    /// <summary>
    /// Gets or sets the maximum value used by progress-bar charts.
    /// </summary>
    public double Max {
        get => _progressMaximum;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress maximum must be greater than zero.");
            _progressMaximum = value;
        }
    }


    /// <summary>
    /// Gets or sets a value indicating whether numeric value labels are rendered for progress-bar rows.
    /// </summary>
    public bool ShowValues { get; set; } = true;


    /// <summary>
    /// Gets or sets a value indicating whether progress-bar rows render slider handles. Off by default: a static
    /// progress bar is a measure, and a knob reads as an input control. An interactive chart with progress editing
    /// enabled shows them, because there the bar is an input.
    /// </summary>
    public bool ShowHandles { get; set; }


    /// <summary>
    /// Gets or sets the progress-bar thickness as a ratio of each row height.
    /// </summary>
    public double BarThicknessRatio {
        get => _progressBarThicknessRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.16 || value > 0.72) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress-bar thickness ratio must be between 0.16 and 0.72.");
            _progressBarThicknessRatio = value;
        }
    }


    /// <summary>
    /// Gets or sets the opacity used by progress-bar tracks.
    /// </summary>
    public double TrackOpacity {
        get => _progressTrackOpacity;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress track opacity must be between zero and one.");
            _progressTrackOpacity = value;
        }
    }


    /// <summary>
    /// Gets or sets a value indicating whether progress-ring charts render the center average and series label.
    /// </summary>
    public bool ShowRingCenterLabel { get; set; } = true;


    /// <summary>
    /// Gets or sets the relative outer radius used by progress rings and layered radial progress charts.
    /// </summary>
    public double RadialRadiusScale {
        get => _radialProgressRadiusScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.65 || value > 1.35) throw new ArgumentOutOfRangeException(nameof(value), value, "Radial-progress radius scale must be between 0.65 and 1.35.");
            _radialProgressRadiusScale = value;
        }
    }


    /// <summary>
    /// Gets or sets the relative stroke thickness used by progress rings and layered radial progress charts.
    /// </summary>
    public double RadialStrokeScale {
        get => _radialProgressStrokeScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.55 || value > 1.8) throw new ArgumentOutOfRangeException(nameof(value), value, "Radial-progress stroke scale must be between 0.55 and 1.8.");
            _radialProgressStrokeScale = value;
        }
    }
}
