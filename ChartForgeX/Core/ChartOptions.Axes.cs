using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the primary horizontal axis.</summary>
    public ChartAxis XAxis { get; } = new();

    /// <summary>Gets the primary vertical axis.</summary>
    public ChartAxis YAxis { get; } = new();

    /// <summary>Gets the secondary vertical axis.</summary>
    public ChartAxis SecondaryYAxis { get; } = new();

    /// <summary>Gets or sets a value indicating whether axes, labels, and titles are rendered.</summary>
    public bool ShowAxes { get; set; } = true;

    /// <summary>Gets or sets legacy x-axis visibility through <see cref="XAxis"/>.</summary>
    public bool ShowXAxis { get => XAxis.Visible; set => XAxis.Visible = value; }

    /// <summary>Gets or sets legacy y-axis visibility through <see cref="YAxis"/>.</summary>
    public bool ShowYAxis { get => YAxis.Visible; set => YAxis.Visible = value; }

    /// <summary>Gets or sets axis-rule visibility for every configured axis.</summary>
    public bool ShowAxisLines {
        get => XAxis.ShowLine && YAxis.ShowLine && SecondaryYAxis.ShowLine;
        set { XAxis.ShowLine = value; YAxis.ShowLine = value; SecondaryYAxis.ShowLine = value; }
    }

    /// <summary>Gets or sets the shared legacy tick count for every axis.</summary>
    public int TickCount {
        get => XAxis.TickCount;
        set { XAxis.TickCount = value; YAxis.TickCount = value; SecondaryYAxis.TickCount = value; }
    }

    /// <summary>Gets or sets x-axis label density.</summary>
    public ChartLabelDensity XAxisLabelDensity { get => XAxis.LabelDensity; set => XAxis.LabelDensity = value; }

    /// <summary>Gets or sets y-axis label density.</summary>
    public ChartLabelDensity YAxisLabelDensity { get => YAxis.LabelDensity; set => YAxis.LabelDensity = value; }

    /// <summary>Gets or sets x-axis label rotation.</summary>
    public double XAxisLabelAngle { get => XAxis.LabelAngle; set => XAxis.LabelAngle = value; }

    /// <summary>Gets or sets the explicit x-axis minimum.</summary>
    public double? XAxisMinimum { get => XAxis.Minimum; set => XAxis.Minimum = value; }

    /// <summary>Gets or sets the explicit x-axis maximum.</summary>
    public double? XAxisMaximum { get => XAxis.Maximum; set => XAxis.Maximum = value; }

    /// <summary>Gets or sets the explicit y-axis minimum.</summary>
    public double? YAxisMinimum { get => YAxis.Minimum; set => YAxis.Minimum = value; }

    /// <summary>Gets or sets the explicit y-axis maximum.</summary>
    public double? YAxisMaximum { get => YAxis.Maximum; set => YAxis.Maximum = value; }

    /// <summary>Gets or sets the explicit secondary y-axis minimum.</summary>
    public double? SecondaryYAxisMinimum { get => SecondaryYAxis.Minimum; set => SecondaryYAxis.Minimum = value; }

    /// <summary>Gets or sets the explicit secondary y-axis maximum.</summary>
    public double? SecondaryYAxisMaximum { get => SecondaryYAxis.Maximum; set => SecondaryYAxis.Maximum = value; }

    /// <summary>Gets or sets the secondary y-axis label formatter.</summary>
    public Func<double, string>? SecondaryYAxisValueFormatter { get => SecondaryYAxis.LabelFormatter; set => SecondaryYAxis.LabelFormatter = value; }

    /// <summary>Gets or sets the x-axis label formatter.</summary>
    public Func<double, string>? XAxisValueFormatter { get => XAxis.LabelFormatter; set => XAxis.LabelFormatter = value; }

    /// <summary>Gets explicit x-axis labels.</summary>
    public List<ChartAxisLabel> XAxisLabels => XAxis.Labels;

    internal void SetXAxisBounds(double minimum, double maximum) => XAxis.WithBounds(minimum, maximum);
    internal void ClearXAxisBounds() => XAxis.WithAutomaticBounds();
    internal void SetYAxisBounds(double minimum, double maximum) => YAxis.WithBounds(minimum, maximum);
    internal void ClearYAxisBounds() => YAxis.WithAutomaticBounds();
    internal void SetSecondaryYAxisBounds(double minimum, double maximum) => SecondaryYAxis.WithBounds(minimum, maximum);
    internal void ClearSecondaryYAxisBounds() => SecondaryYAxis.WithAutomaticBounds();
}
