using System.Collections.Generic;
using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one named series of points in a chart.
/// </summary>
public sealed class ChartSeries {
    private double _strokeWidth = 3;
    private ChartAxisSide _yAxis = ChartAxisSide.Primary;
    private ChartDataLabelPlacement? _dataLabelPlacement;
    private ChartFillPattern _fillPattern = ChartFillPattern.None;

    /// <summary>
    /// Gets the display name shown in legends.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the series rendering kind.
    /// </summary>
    public ChartSeriesKind Kind { get; }

    /// <summary>
    /// Gets the ordered data points in the series.
    /// </summary>
    public List<ChartPoint> Points { get; } = new();

    /// <summary>
    /// Gets or sets the series color. When null, the chart theme palette is used.
    /// </summary>
    public ChartColor? Color { get; set; }

    /// <summary>
    /// Gets optional point-level colors. Null entries fall back to the series color or theme palette.
    /// </summary>
    public List<ChartColor?> PointColors { get; } = new();

    /// <summary>
    /// Gets or sets the texture overlay applied to filled marks in this series.
    /// </summary>
    public ChartFillPattern FillPattern {
        get => _fillPattern;
        set {
            if (!Enum.IsDefined(typeof(ChartFillPattern), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown fill pattern.");
            _fillPattern = value;
        }
    }

    /// <summary>
    /// Gets optional point-level fill patterns. Null entries fall back to the series fill pattern.
    /// </summary>
    public List<ChartFillPattern?> PointFillPatterns { get; } = new();

    /// <summary>
    /// Gets optional point-level numeric values for specialized renderers that need per-point metadata.
    /// </summary>
    public List<double?> PointValues { get; } = new();

    /// <summary>
    /// Gets or sets the full heatmap column span for masked matrix rows.
    /// </summary>
    internal int? HeatmapColumnCount { get; set; }

    /// <summary>
    /// Gets optional point-level data labels. Null entries use the formatted point value.
    /// </summary>
    public List<string?> PointLabels { get; } = new();

    /// <summary>
    /// Gets optional point-level data-label styles. Null entries fall back to the series or chart data-label style.
    /// </summary>
    public List<ChartTextStyle?> PointDataLabelStyles { get; } = new();

    /// <summary>
    /// Gets optional point-level pie/donut slice offset ratios. Zero entries use the default slice position.
    /// </summary>
    public List<double> PointSliceOffsets { get; } = new();

    /// <summary>
    /// Gets radial arc layer definitions for layered radial charts.
    /// </summary>
    public List<ChartRadialLayer> RadialLayers { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether capable renderers should smooth connected line segments.
    /// </summary>
    public bool Smooth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the series should appear in the chart legend.
    /// </summary>
    public bool ShowInLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional semantic role emitted by capable renderers.
    /// </summary>
    public string? SemanticRole { get; set; }

    /// <summary>
    /// Gets or sets a series-specific data-label override. When null, the chart-level setting is used.
    /// </summary>
    public bool? ShowDataLabels { get; set; }

    /// <summary>
    /// Gets or sets the series-specific data-label placement. When null, the chart-level placement is used.
    /// </summary>
    public ChartDataLabelPlacement? DataLabelPlacement {
        get => _dataLabelPlacement;
        set {
            if (value.HasValue && !Enum.IsDefined(typeof(ChartDataLabelPlacement), value.Value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown data-label placement.");
            _dataLabelPlacement = value;
        }
    }

    /// <summary>
    /// Gets the series-specific data-label style. When empty, the chart-level data-label style is used.
    /// </summary>
    public ChartTextStyle DataLabelStyle { get; } = new();

    /// <summary>
    /// Gets or sets the vertical axis used for cartesian rendering.
    /// </summary>
    public ChartAxisSide YAxis {
        get => _yAxis;
        set {
            if (!Enum.IsDefined(typeof(ChartAxisSide), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown y-axis side.");
            _yAxis = value;
        }
    }

    /// <summary>
    /// Gets or sets the stroke width for line and area series.
    /// </summary>
    public double StrokeWidth {
        get => _strokeWidth;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Stroke width must be greater than zero.");
            _strokeWidth = value;
        }
    }

    /// <summary>
    /// Sets the series color. Pass null to use the chart theme palette.
    /// </summary>
    /// <param name="color">The series color, or null to use the theme palette.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithColor(ChartColor? color) {
        Color = color;
        return this;
    }

    /// <summary>
    /// Clears the series color so the chart theme palette is used.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UseThemeColor() {
        Color = null;
        return this;
    }

    /// <summary>
    /// Sets the color for one point. Pass null to use the series color or theme palette.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <param name="color">The point color, or null to use the series color.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointColor(int pointIndex, ChartColor? color) {
        ValidatePointIndex(pointIndex);
        while (PointColors.Count <= pointIndex) PointColors.Add(null);
        PointColors[pointIndex] = color;
        return this;
    }

    /// <summary>
    /// Sets the color for one point from hex notation.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <param name="hex">The point color in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointColor(int pointIndex, string hex) => WithPointColor(pointIndex, ChartColor.FromHex(hex));

    /// <summary>
    /// Sets the same color for a contiguous point range.
    /// </summary>
    /// <param name="startPointIndex">The zero-based first point index.</param>
    /// <param name="count">The number of points to color.</param>
    /// <param name="color">The point color, or null to use the series color.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointColorRange(int startPointIndex, int count, ChartColor? color) {
        ValidatePointIndex(startPointIndex);
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Point color range count must be greater than zero.");
        ValidatePointIndex(startPointIndex + count - 1);
        for (var i = 0; i < count; i++) WithPointColor(startPointIndex + i, color);
        return this;
    }

    /// <summary>
    /// Sets the same color for a contiguous point range from hex notation.
    /// </summary>
    /// <param name="startPointIndex">The zero-based first point index.</param>
    /// <param name="count">The number of points to color.</param>
    /// <param name="hex">The point color in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointColorRange(int startPointIndex, int count, string hex) => WithPointColorRange(startPointIndex, count, ChartColor.FromHex(hex));

    /// <summary>
    /// Clears the color for one point so the series color or theme palette is used.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UseSeriesColor(int pointIndex) {
        ValidatePointIndex(pointIndex);
        if (pointIndex < PointColors.Count) PointColors[pointIndex] = null;
        return this;
    }

    /// <summary>
    /// Sets the texture overlay applied to filled marks in this series.
    /// </summary>
    /// <param name="pattern">The fill pattern.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithFillPattern(ChartFillPattern pattern) {
        FillPattern = pattern;
        return this;
    }

    /// <summary>
    /// Clears the series fill pattern.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UseSolidFill() {
        FillPattern = ChartFillPattern.None;
        return this;
    }

    /// <summary>
    /// Sets the texture overlay for one point. Pass null to use the series fill pattern.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <param name="pattern">The point fill pattern, or null to use the series fill pattern.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointFillPattern(int pointIndex, ChartFillPattern? pattern) {
        ValidatePointIndex(pointIndex);
        if (pattern.HasValue && !Enum.IsDefined(typeof(ChartFillPattern), pattern.Value)) throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown fill pattern.");
        while (PointFillPatterns.Count <= pointIndex) PointFillPatterns.Add(null);
        PointFillPatterns[pointIndex] = pattern;
        return this;
    }

    /// <summary>
    /// Clears the texture overlay for one point so the series fill pattern is used.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UseSeriesFillPattern(int pointIndex) {
        ValidatePointIndex(pointIndex);
        if (pointIndex < PointFillPatterns.Count) PointFillPatterns[pointIndex] = null;
        return this;
    }

    /// <summary>
    /// Sets the data label for one point.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <param name="label">The data label text.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointLabel(int pointIndex, string label) {
        ValidatePointIndex(pointIndex);
        if (label == null) throw new ArgumentNullException(nameof(label));
        while (PointLabels.Count <= pointIndex) PointLabels.Add(null);
        PointLabels[pointIndex] = label;
        return this;
    }

    /// <summary>
    /// Clears the data label for one point so the formatted point value is used.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UseFormattedPointLabel(int pointIndex) {
        ValidatePointIndex(pointIndex);
        if (pointIndex < PointLabels.Count) PointLabels[pointIndex] = null;
        return this;
    }

    /// <summary>
    /// Sets the stroke width for line and area series.
    /// </summary>
    /// <param name="width">The stroke width in pixels.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithStrokeWidth(double width) {
        StrokeWidth = width;
        return this;
    }

    /// <summary>
    /// Sets whether capable renderers should smooth connected line segments.
    /// </summary>
    /// <param name="smooth">True to smooth connected segments; otherwise false.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithSmooth(bool smooth = true) {
        Smooth = smooth;
        return this;
    }

    /// <summary>
    /// Sets whether the series should appear in the chart legend.
    /// </summary>
    /// <param name="visible">True to show the series in legends; otherwise false.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithLegendEntry(bool visible = true) {
        ShowInLegend = visible;
        return this;
    }

    /// <summary>
    /// Sets an optional semantic role emitted by capable renderers.
    /// </summary>
    /// <param name="role">The semantic role, or null to use the renderer default.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithSemanticRole(string? role) {
        SemanticRole = role;
        return this;
    }

    /// <summary>
    /// Sets a series-specific data-label visibility override.
    /// </summary>
    /// <param name="visible">A value indicating whether labels should render for this series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithDataLabels(bool visible = true) {
        ShowDataLabels = visible;
        return this;
    }

    /// <summary>
    /// Clears the series-specific data-label visibility override and uses the chart-level setting.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UseChartDataLabels() {
        ShowDataLabels = null;
        return this;
    }

    /// <summary>
    /// Sets a series-specific data-label placement.
    /// </summary>
    /// <param name="placement">The preferred placement.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithDataLabelPlacement(ChartDataLabelPlacement placement) {
        DataLabelPlacement = placement;
        return this;
    }

    /// <summary>
    /// Clears the series-specific data-label placement and uses the chart-level placement.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UseChartDataLabelPlacement() {
        DataLabelPlacement = null;
        return this;
    }

    /// <summary>
    /// Configures series-specific data-label styling.
    /// </summary>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithDataLabelStyle(Action<ChartTextStyle> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(DataLabelStyle);
        return this;
    }

    /// <summary>
    /// Configures data-label styling for one point.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointDataLabelStyle(int pointIndex, Action<ChartTextStyle> configure) {
        ValidatePointIndex(pointIndex);
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        while (PointDataLabelStyles.Count <= pointIndex) PointDataLabelStyles.Add(null);
        var style = PointDataLabelStyles[pointIndex] ?? new ChartTextStyle();
        configure(style);
        PointDataLabelStyles[pointIndex] = style;
        return this;
    }

    /// <summary>
    /// Clears point-specific data-label styling for one point.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UseSeriesDataLabelStyle(int pointIndex) {
        ValidatePointIndex(pointIndex);
        if (pointIndex < PointDataLabelStyles.Count) PointDataLabelStyles[pointIndex] = null;
        return this;
    }

    /// <summary>
    /// Sets the pie/donut slice offset ratio for one point.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <param name="ratio">The offset ratio from zero to 0.35 of the outer radius.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointSliceOffset(int pointIndex, double ratio) {
        ValidatePointIndex(pointIndex);
        ChartGuards.Finite(ratio, nameof(ratio));
        if (ratio < 0 || ratio > 0.35) throw new ArgumentOutOfRangeException(nameof(ratio), ratio, "Slice offset ratio must be between zero and 0.35.");
        while (PointSliceOffsets.Count <= pointIndex) PointSliceOffsets.Add(0);
        PointSliceOffsets[pointIndex] = ratio;
        return this;
    }

    /// <summary>
    /// Clears the pie/donut slice offset ratio for one point.
    /// </summary>
    /// <param name="pointIndex">The zero-based point index.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UseDefaultSliceOffset(int pointIndex) {
        ValidatePointIndex(pointIndex);
        if (pointIndex < PointSliceOffsets.Count) PointSliceOffsets[pointIndex] = 0;
        return this;
    }

    /// <summary>
    /// Assigns the series to the primary left y-axis.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UsePrimaryYAxis() {
        YAxis = ChartAxisSide.Primary;
        return this;
    }

    /// <summary>
    /// Assigns the series to the secondary right y-axis.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UseSecondaryYAxis() {
        YAxis = ChartAxisSide.Secondary;
        return this;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartSeries"/> class.
    /// </summary>
    /// <param name="name">The display name shown in legends.</param>
    /// <param name="kind">The series rendering kind.</param>
    /// <param name="points">The ordered data points.</param>
    public ChartSeries(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        if (!Enum.IsDefined(typeof(ChartSeriesKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown series kind.");
        Kind = kind;
        Points.AddRange(ChartGuards.Points(points, nameof(points)));
    }

    private void ValidatePointIndex(int pointIndex) {
        var count = LogicalPointCount;
        if (pointIndex < 0 || pointIndex >= count) throw new ArgumentOutOfRangeException(nameof(pointIndex), pointIndex, "Point index must refer to an existing point.");
    }

    private int LogicalPointCount {
        get {
            var tupleSize = Kind == ChartSeriesKind.Bubble ||
                Kind == ChartSeriesKind.RangeBand ||
                Kind == ChartSeriesKind.RangeArea ||
                Kind == ChartSeriesKind.RangeBar ||
                Kind == ChartSeriesKind.Dumbbell
                    ? 2
                    : Kind == ChartSeriesKind.ErrorBar
                        ? 3
                        : Kind == ChartSeriesKind.Candlestick || Kind == ChartSeriesKind.Ohlc
                            ? 4
                            : Kind == ChartSeriesKind.BoxPlot
                                ? 5
                                : 1;
            return Points.Count / tupleSize;
        }
    }
}
