using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>The compact visual presentation of a sparkline sample sequence.</summary>
public enum SparklineStyle {
    /// <summary>A connected or segmented trend line.</summary>
    Line,
    /// <summary>A trend line with filled area beneath each connected segment.</summary>
    Area,
    /// <summary>Individual vertical bars at the original sample positions.</summary>
    Bars
}

/// <summary>A compact chart over the shared Cartesian compiler, with explicit missing-observation semantics.</summary>
public sealed class Sparkline : IVisualRenderable {
    private SparklineStyle _style;

    /// <summary>Creates a compact visual from detached sample data.</summary>
    public Sparkline(SparklineData data) { Data = data ?? throw new ArgumentNullException(nameof(data)); }

    /// <summary>Gets the immutable samples and resolved bounds.</summary>
    public SparklineData Data { get; }
    /// <summary>Gets or sets the compact mark style.</summary>
    public SparklineStyle Style {
        get => _style;
        set {
            if (!Enum.IsDefined(typeof(SparklineStyle), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _style = value;
        }
    }
    /// <summary>Gets or sets the optional series colour; null uses the shared theme.</summary>
    public ChartColor? Color { get; set; }
    /// <summary>Gets or sets whether line and area segments use the shared curve interpolation.</summary>
    public bool Smooth { get; set; }
    /// <summary>Gets or sets the shared numeric display policy, compact by default.</summary>
    public ChartValueFormat ValueFormat { get => _valueFormat; set => _valueFormat = value ?? throw new ArgumentNullException(nameof(value)); }
    private ChartValueFormat _valueFormat = ChartValueFormat.Compact();

    /// <summary>Sets the mark style and optional curve interpolation.</summary>
    public Sparkline WithStyle(SparklineStyle style, bool smooth = false) { Style = style; Smooth = smooth; return this; }
    /// <summary>Sets the optional series colour.</summary>
    public Sparkline WithColor(ChartColor? color) { Color = color; return this; }
    /// <summary>Sets the shared numeric display policy.</summary>
    public Sparkline WithValueFormat(ChartValueFormat format) { ValueFormat = format; return this; }

    /// <summary>Prepares compact marks through the same compiler and frame contract as a full Cartesian chart.</summary>
    public PreparedVisual Prepare(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (Style == SparklineStyle.Bars && Smooth) throw new InvalidOperationException("Bar sparklines do not use curve interpolation.");
        var chart = Chart.Create().WithSparkline().WithValueFormat(ValueFormat);
        chart.Options.XAxis.WithBounds(Data.Values.Count == 1 ? 0 : 1, Data.Values.Count == 1 ? 2 : Data.Values.Count);
        chart.Options.YAxis.WithBounds(Data.Minimum, Data.Maximum);
        chart.Options.LineMarkerMode = Data.Values[Data.Values.Count - 1].HasValue ? ChartLineMarkerMode.Last : ChartLineMarkerMode.None;
        var kind = Style == SparklineStyle.Bars ? ChartSeriesKind.Bar : Style == SparklineStyle.Area ? ChartSeriesKind.Area : ChartSeriesKind.Line;
        var series = new ChartSeries("Trend", kind, Data.ToPoints()) { Color = Color, Smooth = Smooth, ShowInLegend = false };
        chart.Series.Add(series);
        return chart.Prepare(context);
    }
}
