using System;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Enables host-accepted progress editing without changing a source chart from browser paint.</summary>
public sealed class HtmlChartProgressEditingOptions {
    private double _stepRatio = .01;
    /// <summary>Gets or sets whether identified progress rows expose editing gestures when connected to native preparation. Default: false.</summary>
    public bool Enabled { get; set; }
    /// <summary>Gets or sets the fraction of each row's own domain used for arrow steps and pointer snapping. Page keys use ten steps.</summary>
    public double StepRatio {
        get => _stepRatio;
        set { if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); _stepRatio = value; }
    }
}
