using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the primary horizontal axis.</summary>
    public ChartAxis XAxis {
        get => _xAxis;
        set => _xAxis = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartAxis _xAxis = new();

    /// <summary>Gets the primary vertical axis.</summary>
    public ChartAxis YAxis {
        get => _yAxis;
        set => _yAxis = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartAxis _yAxis = new();

    /// <summary>Gets the secondary horizontal value axis for horizontal Cartesian charts.</summary>
    public ChartAxis SecondaryXAxis {
        get => _secondaryXAxis;
        set => _secondaryXAxis = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartAxis _secondaryXAxis = new();

    /// <summary>Gets the secondary vertical axis.</summary>
    public ChartAxis SecondaryYAxis {
        get => _secondaryYAxis;
        set => _secondaryYAxis = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartAxis _secondaryYAxis = new();

    /// <summary>Gets or sets a value indicating whether axes, labels, and titles are rendered.</summary>
    public bool ShowAxes { get; set; } = true;

    /// <summary>Gets or sets axis-rule visibility for every configured axis.</summary>
    public bool ShowAxisLines {
        get => XAxis.ShowLine && YAxis.ShowLine && SecondaryXAxis.ShowLine && SecondaryYAxis.ShowLine;
        set { XAxis.ShowLine = value; YAxis.ShowLine = value; SecondaryXAxis.ShowLine = value; SecondaryYAxis.ShowLine = value; }
    }

    /// <summary>Gets or sets the shared legacy tick count for every axis.</summary>
    public int TickCount {
        get => XAxis.TickCount;
        set { XAxis.TickCount = value; YAxis.TickCount = value; SecondaryXAxis.TickCount = value; SecondaryYAxis.TickCount = value; }
    }
}
