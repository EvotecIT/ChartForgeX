using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Core;

/// <summary>
/// Describes a dependency-free small-multiple report made from multiple charts.
/// </summary>
public sealed class ChartGrid {
    private readonly List<Chart> _charts = new();
    private readonly List<ChartGridPanelSpan> _panelSpans = new();
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private int _columns = 2;
    private int _gap = 18;
    private int _padding = 24;
    private int _pngOutputScale = 1;
    private ChartSize? _panelSize;
    private ChartGridPanelFit _panelFit = ChartGridPanelFit.Contain;
    private ChartTheme? _theme;

    /// <summary>
    /// Gets or sets the report title.
    /// </summary>
    public string Title {
        get => _title;
        set => _title = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the report subtitle.
    /// </summary>
    public string Subtitle {
        get => _subtitle;
        set => _subtitle = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the preferred number of columns on wide viewports.
    /// </summary>
    public int Columns {
        get => _columns;
        set {
            ChartPrimitiveGuards.Positive(value, nameof(value));
            _columns = value;
        }
    }

    /// <summary>
    /// Gets or sets the gap between charts in pixels.
    /// </summary>
    public int Gap {
        get => _gap;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than or equal to zero.");
            _gap = value;
        }
    }

    /// <summary>
    /// Gets or sets the outer padding around exported grid content in pixels.
    /// </summary>
    public int Padding {
        get => _padding;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than or equal to zero.");
            _padding = value;
        }
    }

    /// <summary>
    /// Gets or sets the output pixel multiplier used by composed PNG grid exports.
    /// </summary>
    public int PngOutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four.");
            _pngOutputScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional fixed panel size used by composed SVG and PNG grid exports.
    /// </summary>
    public ChartSize? PanelSize {
        get => _panelSize;
        set {
            if (value.HasValue && (value.Value.Width <= 0 || value.Value.Height <= 0)) throw new ArgumentOutOfRangeException(nameof(value), "Grid panel size must have positive dimensions.");
            _panelSize = value;
        }
    }

    /// <summary>
    /// Gets or sets how charts fit inside fixed-size grid panels.
    /// </summary>
    public ChartGridPanelFit PanelFit {
        get => _panelFit;
        set {
            if (!Enum.IsDefined(typeof(ChartGridPanelFit), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown chart grid panel fit.");
            _panelFit = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional theme used by the grid background and heading.
    /// </summary>
    public ChartTheme? Theme {
        get => _theme;
        set => _theme = value;
    }

    /// <summary>
    /// Gets the text style used for the grid title.
    /// </summary>
    public ChartTextStyle TitleStyle { get; } = new();

    /// <summary>
    /// Gets the text style used for the grid subtitle.
    /// </summary>
    public ChartTextStyle SubtitleStyle { get; } = new();

    /// <summary>
    /// Gets the charts included in the grid.
    /// </summary>
    public IReadOnlyList<Chart> Charts => _charts;

    /// <summary>
    /// Gets the panel spans for charts included in the grid.
    /// </summary>
    public IReadOnlyList<ChartGridPanelSpan> PanelSpans => _panelSpans;

    /// <summary>
    /// Creates a new chart grid.
    /// </summary>
    /// <returns>A new chart grid instance.</returns>
    public static ChartGrid Create() => new();

    /// <summary>
    /// Sets the report title.
    /// </summary>
    /// <param name="title">The report title.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>
    /// Sets the report subtitle.
    /// </summary>
    /// <param name="subtitle">The report subtitle.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return this; }

    /// <summary>
    /// Configures grid title text styling.
    /// </summary>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithTitleStyle(Action<ChartTextStyle> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(TitleStyle);
        return this;
    }

    /// <summary>
    /// Configures grid subtitle text styling.
    /// </summary>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithSubtitleStyle(Action<ChartTextStyle> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(SubtitleStyle);
        return this;
    }

    /// <summary>
    /// Sets the preferred number of columns on wide viewports.
    /// </summary>
    /// <param name="columns">The preferred column count.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithColumns(int columns) { Columns = columns; return this; }

    /// <summary>
    /// Sets the gap between charts in pixels.
    /// </summary>
    /// <param name="gap">The gap in pixels.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithGap(int gap) { Gap = gap; return this; }

    /// <summary>
    /// Sets the outer padding around exported grid content in pixels.
    /// </summary>
    /// <param name="padding">The padding in pixels.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithPadding(int padding) { Padding = padding; return this; }

    /// <summary>
    /// Sets the output pixel multiplier used by composed PNG grid exports.
    /// </summary>
    /// <param name="scale">The output scale from one to four. Higher values emit larger PNG dimensions with the same logical grid layout.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithPngOutputScale(int scale) { PngOutputScale = scale; return this; }

    /// <summary>
    /// Sets the output pixel multiplier used by composed PNG grid exports using a named density preset.
    /// </summary>
    /// <param name="scale">The output density preset.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithPngOutputScale(ChartPngOutputScale scale) => WithPngOutputScale((int)scale);

    /// <summary>
    /// Sets a fixed panel size for composed SVG and PNG grid exports.
    /// </summary>
    /// <param name="width">The panel width in pixels.</param>
    /// <param name="height">The panel height in pixels.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithPanelSize(int width, int height) { PanelSize = new ChartSize(width, height); return this; }

    /// <summary>
    /// Sets how charts fit inside fixed-size grid panels.
    /// </summary>
    /// <param name="fit">The panel fit behavior.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithPanelFit(ChartGridPanelFit fit) { PanelFit = fit; return this; }

    /// <summary>
    /// Clears the fixed panel size so composed exports use the largest chart dimensions.
    /// </summary>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithAutomaticPanelSize() { PanelSize = null; return this; }

    /// <summary>
    /// Sets the theme used by the grid background and heading.
    /// </summary>
    /// <param name="theme">The grid theme.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithTheme(ChartTheme theme) { Theme = theme ?? throw new ArgumentNullException(nameof(theme)); return this; }

    /// <summary>
    /// Clears the grid theme so renderers use the first chart theme.
    /// </summary>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithAutomaticTheme() { _theme = null; return this; }

    /// <summary>
    /// Adds a chart to the grid.
    /// </summary>
    /// <param name="chart">The chart to add.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid Add(Chart chart) => Add(chart, 1, 1);

    /// <summary>
    /// Adds a chart to the grid with a panel span.
    /// </summary>
    /// <param name="chart">The chart to add.</param>
    /// <param name="columnSpan">The number of columns occupied by the chart panel.</param>
    /// <param name="rowSpan">The number of rows occupied by the chart panel.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid Add(Chart chart, int columnSpan, int rowSpan = 1) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        _charts.Add(chart);
        _panelSpans.Add(new ChartGridPanelSpan(columnSpan, rowSpan));
        return this;
    }

    /// <summary>
    /// Sets the panel span for an existing chart in the grid.
    /// </summary>
    /// <param name="chartIndex">The zero-based chart index.</param>
    /// <param name="columnSpan">The number of columns occupied by the chart panel.</param>
    /// <param name="rowSpan">The number of rows occupied by the chart panel.</param>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithPanelSpan(int chartIndex, int columnSpan, int rowSpan = 1) {
        if (chartIndex < 0 || chartIndex >= _charts.Count) throw new ArgumentOutOfRangeException(nameof(chartIndex), chartIndex, "Chart index must reference an existing grid chart.");
        _panelSpans[chartIndex] = new ChartGridPanelSpan(columnSpan, rowSpan);
        return this;
    }

    /// <summary>
    /// Applies one shared cartesian y-axis range to all compatible charts in the grid.
    /// </summary>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithSharedYAxis() {
        if (_charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart before sharing y-axis bounds.");

        var primaryMinimum = double.PositiveInfinity;
        var primaryMaximum = double.NegativeInfinity;
        var secondaryMinimum = double.PositiveInfinity;
        var secondaryMaximum = double.NegativeInfinity;
        var primaryCompatible = new List<Chart>();
        var secondaryCompatible = new List<Chart>();
        foreach (var chart in _charts) {
            ChartGuards.RenderCompatibility(chart);
            if (UsesPrimaryCartesianYAxis(chart)) {
                var range = ChartRange.FromChart(chart, false);
                if (range.MinY < primaryMinimum) primaryMinimum = range.MinY;
                if (range.MaxY > primaryMaximum) primaryMaximum = range.MaxY;
                primaryCompatible.Add(chart);
            }

            if (UsesSecondaryCartesianYAxis(chart)) {
                var primaryRange = ChartRange.FromChart(chart, false);
                var secondaryRange = ChartRange.FromSecondaryYAxis(chart, primaryRange, false);
                if (secondaryRange.MinY < secondaryMinimum) secondaryMinimum = secondaryRange.MinY;
                if (secondaryRange.MaxY > secondaryMaximum) secondaryMaximum = secondaryRange.MaxY;
                secondaryCompatible.Add(chart);
            }
        }

        if (primaryCompatible.Count == 0 && secondaryCompatible.Count == 0) throw new InvalidOperationException("Shared y-axis bounds require at least one cartesian chart.");
        if (primaryCompatible.Count > 0) {
            if (Math.Abs(primaryMaximum - primaryMinimum) < double.Epsilon) primaryMaximum = primaryMinimum + 1;
            foreach (var chart in primaryCompatible) chart.WithYAxisBounds(primaryMinimum, primaryMaximum);
        }

        if (secondaryCompatible.Count > 0) {
            if (Math.Abs(secondaryMaximum - secondaryMinimum) < double.Epsilon) secondaryMaximum = secondaryMinimum + 1;
            foreach (var chart in secondaryCompatible) chart.WithSecondaryYAxisBounds(secondaryMinimum, secondaryMaximum);
        }

        return this;
    }

    /// <summary>
    /// Applies one shared cartesian x-axis range to all compatible charts in the grid.
    /// </summary>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithSharedXAxis() {
        if (_charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart before sharing x-axis bounds.");

        var minimum = double.PositiveInfinity;
        var maximum = double.NegativeInfinity;
        var compatible = new List<Chart>();
        foreach (var chart in _charts) {
            ChartGuards.RenderCompatibility(chart);
            if (!UsesCartesianXAxis(chart)) continue;
            var range = ChartRange.FromChart(chart, false);
            if (range.MinX < minimum) minimum = range.MinX;
            if (range.MaxX > maximum) maximum = range.MaxX;
            compatible.Add(chart);
        }

        if (compatible.Count == 0) throw new InvalidOperationException("Shared x-axis bounds require at least one cartesian chart.");
        if (Math.Abs(maximum - minimum) < double.Epsilon) maximum = minimum + 1;
        foreach (var chart in compatible) chart.WithXAxisBounds(minimum, maximum);
        return this;
    }

    /// <summary>
    /// Applies shared cartesian x-axis and y-axis ranges to all compatible charts in the grid.
    /// </summary>
    /// <returns>The current chart grid.</returns>
    public ChartGrid WithSharedAxes() {
        WithSharedXAxis();
        WithSharedYAxis();
        return this;
    }

    private static bool UsesCartesianXAxis(Chart chart) {
        if (chart.Series.Count == 0) return false;
        foreach (var series in chart.Series) {
            if (series.Kind == ChartSeriesKind.Heatmap ||
                series.Kind == ChartSeriesKind.HexbinHeatmap ||
                series.Kind == ChartSeriesKind.CalendarHeatmap ||
                series.Kind == ChartSeriesKind.DottedMap ||
                series.Kind == ChartSeriesKind.TileMap ||
                series.Kind == ChartSeriesKind.RegionMap ||
                series.Kind == ChartSeriesKind.Gauge ||
                series.Kind == ChartSeriesKind.Circle ||
                series.Kind == ChartSeriesKind.RadialBar ||
                series.Kind == ChartSeriesKind.LayeredRadial ||
                series.Kind == ChartSeriesKind.Bullet ||
                series.Kind == ChartSeriesKind.Waterfall ||
                series.Kind == ChartSeriesKind.Radar ||
                series.Kind == ChartSeriesKind.Funnel ||
                series.Kind == ChartSeriesKind.Treemap ||
                series.Kind == ChartSeriesKind.Timeline ||
                series.Kind == ChartSeriesKind.Gantt ||
                series.Kind == ChartSeriesKind.Sankey ||
                series.Kind == ChartSeriesKind.Tree ||
                series.Kind == ChartSeriesKind.Sunburst ||
                series.Kind == ChartSeriesKind.Pictorial ||
                series.Kind == ChartSeriesKind.ProgressBar ||
                series.Kind == ChartSeriesKind.WordCloud ||
                series.Kind == ChartSeriesKind.Pie ||
                series.Kind == ChartSeriesKind.Donut ||
                series.Kind == ChartSeriesKind.PolarArea) {
                return false;
            }
        }

        return true;
    }

    private static bool UsesPrimaryCartesianYAxis(Chart chart) {
        foreach (var series in chart.Series) {
            if (series.YAxis == ChartAxisSide.Primary && IsCartesianYAxisSeries(series)) return true;
        }

        return false;
    }

    private static bool UsesSecondaryCartesianYAxis(Chart chart) {
        foreach (var series in chart.Series) {
            if (series.YAxis == ChartAxisSide.Secondary && IsCartesianYAxisSeries(series)) return true;
        }

        return false;
    }

    private static bool IsCartesianYAxisSeries(ChartSeries series) {
        return series.Kind != ChartSeriesKind.HorizontalBar &&
            series.Kind != ChartSeriesKind.Heatmap &&
            series.Kind != ChartSeriesKind.HexbinHeatmap &&
            series.Kind != ChartSeriesKind.CalendarHeatmap &&
            series.Kind != ChartSeriesKind.DottedMap &&
            series.Kind != ChartSeriesKind.TileMap &&
            series.Kind != ChartSeriesKind.RegionMap &&
            series.Kind != ChartSeriesKind.Gauge &&
            series.Kind != ChartSeriesKind.Circle &&
            series.Kind != ChartSeriesKind.RadialBar &&
            series.Kind != ChartSeriesKind.LayeredRadial &&
            series.Kind != ChartSeriesKind.Bullet &&
            series.Kind != ChartSeriesKind.Waterfall &&
            series.Kind != ChartSeriesKind.Radar &&
            series.Kind != ChartSeriesKind.Funnel &&
            series.Kind != ChartSeriesKind.Treemap &&
            series.Kind != ChartSeriesKind.Timeline &&
            series.Kind != ChartSeriesKind.Gantt &&
            series.Kind != ChartSeriesKind.Sankey &&
            series.Kind != ChartSeriesKind.Tree &&
            series.Kind != ChartSeriesKind.Sunburst &&
            series.Kind != ChartSeriesKind.Pictorial &&
            series.Kind != ChartSeriesKind.ProgressBar &&
            series.Kind != ChartSeriesKind.WordCloud &&
            series.Kind != ChartSeriesKind.Pie &&
            series.Kind != ChartSeriesKind.Donut &&
            series.Kind != ChartSeriesKind.PolarArea;
    }
}
