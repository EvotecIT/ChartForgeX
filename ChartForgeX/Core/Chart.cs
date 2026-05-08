using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Core;

/// <summary>
/// Represents a renderer-independent chart definition.
/// </summary>
public sealed partial class Chart {
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _xAxisTitle = string.Empty;
    private string _yAxisTitle = string.Empty;
    private string _secondaryYAxisTitle = string.Empty;

    /// <summary>
    /// Gets or sets the chart title.
    /// </summary>
    public string Title {
        get => _title;
        set => _title = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the chart subtitle.
    /// </summary>
    public string Subtitle {
        get => _subtitle;
        set => _subtitle = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the x-axis title.
    /// </summary>
    public string XAxisTitle {
        get => _xAxisTitle;
        set => _xAxisTitle = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the y-axis title.
    /// </summary>
    public string YAxisTitle {
        get => _yAxisTitle;
        set => _yAxisTitle = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the secondary y-axis title.
    /// </summary>
    public string SecondaryYAxisTitle {
        get => _secondaryYAxisTitle;
        set => _secondaryYAxisTitle = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the chart options.
    /// </summary>
    public ChartOptions Options { get; } = new();

    /// <summary>
    /// Gets the chart series.
    /// </summary>
    public List<ChartSeries> Series { get; } = new();

    /// <summary>
    /// Gets the chart annotations.
    /// </summary>
    public List<ChartAnnotation> Annotations { get; } = new();

    /// <summary>
    /// Creates a new chart.
    /// </summary>
    /// <returns>A new chart instance.</returns>
    public static Chart Create() => new();

    /// <summary>
    /// Sets the chart title.
    /// </summary>
    /// <param name="title">The chart title.</param>
    /// <returns>The current chart.</returns>
    public Chart WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>
    /// Sets the chart subtitle.
    /// </summary>
    /// <param name="subtitle">The chart subtitle.</param>
    /// <returns>The current chart.</returns>
    public Chart WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return this; }

    /// <summary>
    /// Sets the x-axis title.
    /// </summary>
    /// <param name="title">The x-axis title.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxis(string title) { XAxisTitle = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>
    /// Sets the y-axis title.
    /// </summary>
    /// <param name="title">The y-axis title.</param>
    /// <returns>The current chart.</returns>
    public Chart WithYAxis(string title) { YAxisTitle = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>
    /// Sets the secondary y-axis title and optional tick formatter.
    /// </summary>
    /// <param name="title">The secondary y-axis title.</param>
    /// <param name="formatter">An optional formatter for secondary y-axis ticks.</param>
    /// <returns>The current chart.</returns>
    public Chart WithSecondaryYAxis(string title, Func<double, string>? formatter = null) {
        SecondaryYAxisTitle = title ?? throw new ArgumentNullException(nameof(title));
        Options.SecondaryYAxisValueFormatter = formatter;
        return this;
    }

    /// <summary>
    /// Sets the rendered chart size.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns>The current chart.</returns>
    public Chart WithSize(int width, int height) {
        Options.Size = new ChartSize(width, height);
        return this;
    }

    /// <summary>
    /// Sets the chart padding around the plot area.
    /// </summary>
    /// <param name="left">The left padding in pixels.</param>
    /// <param name="top">The top padding in pixels.</param>
    /// <param name="right">The right padding in pixels.</param>
    /// <param name="bottom">The bottom padding in pixels.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPadding(double left, double top, double right, double bottom) {
        Options.Padding = new ChartPadding(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// Sets the chart theme.
    /// </summary>
    /// <param name="theme">The theme to use.</param>
    /// <returns>The current chart.</returns>
    public Chart WithTheme(ChartTheme theme) { Options.Theme = theme ?? throw new ArgumentNullException(nameof(theme)); return this; }

    /// <summary>
    /// Sets the CSS font-family used by vector and HTML renderers.
    /// </summary>
    /// <param name="fontFamily">The CSS font-family stack.</param>
    /// <returns>The current chart.</returns>
    public Chart WithFontFamily(string fontFamily) { Options.Theme.FontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily)); return this; }

    /// <summary>
    /// Sets the preferred TrueType font file or TrueType collection used by the PNG renderer.
    /// </summary>
    /// <param name="fontPath">The path to a .ttf or .ttc font file. Pass null to use automatic PNG font discovery.</param>
    /// <param name="collectionIndex">The optional zero-based face index for .ttc font collections.</param>
    /// <param name="faceName">The optional family, subfamily, full, or PostScript face name to select.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPngFont(string? fontPath, int? collectionIndex = null, string? faceName = null) {
        Options.PngFontPath = fontPath;
        Options.PngFontCollectionIndex = collectionIndex;
        Options.PngFontFaceName = faceName;
        return this;
    }

    /// <summary>
    /// Sets the preferred PNG font and selects a face by family, subfamily, full, or PostScript name.
    /// </summary>
    /// <param name="fontPath">The path to a .ttf or .ttc font file.</param>
    /// <param name="faceName">The face name to select.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPngFont(string? fontPath, string? faceName) => WithPngFont(fontPath, null, faceName);

    /// <summary>
    /// Sets the internal supersampling scale used by the PNG renderer.
    /// </summary>
    /// <param name="scale">The supersampling scale from one to four. Higher values improve edges at the cost of render time and memory.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPngSupersampling(int scale) {
        Options.PngSupersamplingScale = scale;
        return this;
    }

    /// <summary>
    /// Sets the output pixel multiplier used by the PNG renderer.
    /// </summary>
    /// <param name="scale">The output scale from one to four. Higher values emit larger PNG dimensions with the same logical chart layout.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPngOutputScale(int scale) {
        Options.PngOutputScale = scale;
        return this;
    }

    /// <summary>
    /// Sets the output pixel multiplier used by the PNG renderer using a named density preset.
    /// </summary>
    /// <param name="scale">The output density preset.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPngOutputScale(ChartPngOutputScale scale) => WithPngOutputScale((int)scale);

    /// <summary>
    /// Sets explicit cartesian x-axis bounds.
    /// </summary>
    /// <param name="minimum">The minimum x-axis value.</param>
    /// <param name="maximum">The maximum x-axis value.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxisBounds(double minimum, double maximum) {
        Options.SetXAxisBounds(minimum, maximum);
        return this;
    }

    /// <summary>
    /// Clears explicit cartesian x-axis bounds so renderers can infer them from data.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithAutomaticXAxisBounds() {
        Options.ClearXAxisBounds();
        return this;
    }

    /// <summary>
    /// Sets explicit cartesian y-axis bounds.
    /// </summary>
    /// <param name="minimum">The minimum y-axis value.</param>
    /// <param name="maximum">The maximum y-axis value.</param>
    /// <returns>The current chart.</returns>
    public Chart WithYAxisBounds(double minimum, double maximum) {
        Options.SetYAxisBounds(minimum, maximum);
        return this;
    }

    /// <summary>
    /// Clears explicit cartesian y-axis bounds so renderers can infer them from data.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithAutomaticYAxisBounds() {
        Options.ClearYAxisBounds();
        return this;
    }

    /// <summary>
    /// Sets explicit secondary cartesian y-axis bounds.
    /// </summary>
    /// <param name="minimum">The minimum secondary y-axis value.</param>
    /// <param name="maximum">The maximum secondary y-axis value.</param>
    /// <returns>The current chart.</returns>
    public Chart WithSecondaryYAxisBounds(double minimum, double maximum) {
        Options.SetSecondaryYAxisBounds(minimum, maximum);
        return this;
    }

    /// <summary>
    /// Clears explicit secondary cartesian y-axis bounds so renderers can infer them from data.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithAutomaticSecondaryYAxisBounds() {
        Options.ClearSecondaryYAxisBounds();
        return this;
    }

    /// <summary>
    /// Sets whether the rendered background should be transparent.
    /// </summary>
    /// <param name="transparent">True to render a transparent background; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithTransparentBackground(bool transparent = true) { Options.TransparentBackground = transparent; return this; }

    /// <summary>
    /// Sets whether the legend should be rendered.
    /// </summary>
    /// <param name="visible">True to render the legend; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithLegend(bool visible = true) { Options.ShowLegend = visible; return this; }

    /// <summary>
    /// Sets whether capable legends should render one entry per visual point instead of one entry per series.
    /// </summary>
    /// <param name="visible">True to render point-level legend entries; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPointLegend(bool visible = true) { Options.ShowPointLegend = visible; return this; }

    /// <summary>
    /// Sets whether the chart title and subtitle should be rendered.
    /// </summary>
    /// <param name="visible">True to render the header; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeader(bool visible = true) { Options.ShowHeader = visible; return this; }

    /// <summary>
    /// Sets whether the outer card surface should be rendered.
    /// </summary>
    /// <param name="visible">True to render the card; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCard(bool visible = true) { Options.ShowCard = visible; return this; }

    /// <summary>
    /// Sets whether the plot background surface should be rendered.
    /// </summary>
    /// <param name="visible">True to render the plot background; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPlotBackground(bool visible = true) { Options.ShowPlotBackground = visible; return this; }

    /// <summary>
    /// Sets the preferred number of ticks per axis.
    /// </summary>
    /// <param name="count">The preferred tick count.</param>
    /// <returns>The current chart.</returns>
    public Chart WithTickCount(int count) {
        if (count < 2) throw new ArgumentOutOfRangeException(nameof(count), count, "Tick count must be at least two.");
        Options.TickCount = count;
        return this;
    }

    /// <summary>
    /// Sets how aggressively explicit x-axis labels should be reduced when space is limited.
    /// </summary>
    /// <param name="density">The x-axis label density mode.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxisLabelDensity(ChartLabelDensity density) { Options.XAxisLabelDensity = density; return this; }

    /// <summary>
    /// Sets the x-axis label rotation angle in degrees for capable renderers.
    /// </summary>
    /// <param name="degrees">The label angle in degrees. Values are clamped by renderers when needed.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxisLabelAngle(double degrees) { ChartGuards.Finite(degrees, nameof(degrees)); Options.XAxisLabelAngle = degrees; return this; }

    /// <summary>
    /// Sets the optional Gantt current-date marker.
    /// </summary>
    /// <param name="today">The current date marker. Pass null to hide the marker.</param>
    /// <returns>The current chart.</returns>
    public Chart WithGanttToday(DateTime? today) { Options.GanttToday = today?.ToOADate(); return this; }

    /// <summary>
    /// Sets the optional Gantt current-position marker for numeric schedules.
    /// </summary>
    /// <param name="value">The current schedule value. Pass null to hide the marker.</param>
    /// <returns>The current chart.</returns>
    public Chart WithGanttToday(double? value) { Options.GanttToday = value; return this; }

    /// <summary>
    /// Sets whether point and bar values should be rendered as data labels.
    /// </summary>
    /// <param name="visible">True to render data labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabels(bool visible = true) { Options.ShowDataLabels = visible; return this; }

    /// <summary>
    /// Sets the preferred data-label placement for capable renderers.
    /// </summary>
    /// <param name="placement">The preferred placement.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelPlacement(ChartDataLabelPlacement placement) { Options.DataLabelPlacement = placement; return this; }

    /// <summary>
    /// Sets how multiple bar series are arranged within each category.
    /// </summary>
    /// <param name="mode">The bar arrangement mode.</param>
    /// <returns>The current chart.</returns>
    public Chart WithBarMode(ChartBarMode mode) { Options.BarMode = mode; return this; }

    /// <summary>
    /// Arranges multiple bar series side by side within each category.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithGroupedBars() { Options.BarMode = ChartBarMode.Grouped; return this; }

    /// <summary>
    /// Arranges multiple bar series as stacked segments within each category.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithStackedBars() { Options.BarMode = ChartBarMode.Stacked; return this; }

    /// <summary>
    /// Arranges multiple horizontal bar series as stacked segments within each category.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithStackedHorizontalBars() => WithStackedBars();

    /// <summary>
    /// Sets whether stacked bar totals should be rendered above each category.
    /// </summary>
    /// <param name="visible">True to render stacked bar totals; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithStackTotals(bool visible = true) { Options.ShowStackTotals = visible; return this; }

    /// <summary>
    /// Sets how heatmap values are converted into cell colors.
    /// </summary>
    /// <param name="scale">The heatmap color scale.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapScale(ChartHeatmapScale scale) { Options.HeatmapScale = scale; return this; }

    /// <summary>
    /// Sets a formatter used for y-axis ticks, data labels, stack totals, and donut totals.
    /// </summary>
    /// <param name="formatter">The value formatter. Pass null to restore the default compact formatter.</param>
    /// <returns>The current chart.</returns>
    public Chart WithValueFormatter(Func<double, string>? formatter) { Options.ValueFormatter = formatter; return this; }

    /// <summary>
    /// Sets a formatter used for generated numeric x-axis tick labels.
    /// </summary>
    /// <param name="formatter">The x-axis value formatter. Pass null to restore the default compact formatter.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxisValueFormatter(Func<double, string>? formatter) { Options.XAxisValueFormatter = formatter; return this; }

    /// <summary>
    /// Configures the chart as a compact sparkline for inline dashboard and table use.
    /// </summary>
    /// <param name="enabled">True to render as a sparkline; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithSparkline(bool enabled = true) {
        Options.IsSparkline = enabled;
        Options.ShowHeader = !enabled;
        Options.ShowLegend = !enabled;
        Options.ShowGrid = !enabled;
        Options.ShowAxes = !enabled;
        Options.ShowXAxis = !enabled;
        Options.ShowYAxis = !enabled;
        Options.ShowAxisLines = !enabled;
        Options.ShowCard = !enabled;
        Options.ShowPlotBackground = !enabled;
        if (enabled) Options.Padding = new ChartPadding(8, 8, 8, 8);
        return this;
    }

    /// <summary>
    /// Sets x-axis labels for one-based x values.
    /// </summary>
    /// <param name="labels">The labels to place at x values 1 through N.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXLabels(params string[] labels) {
        if (labels == null) throw new ArgumentNullException(nameof(labels));
        Options.XAxisLabels.Clear();
        for (var i = 0; i < labels.Length; i++) Options.XAxisLabels.Add(new ChartAxisLabel(i + 1, labels[i] ?? throw new ArgumentNullException(nameof(labels))));
        return this;
    }

    /// <summary>
    /// Sets explicit x-axis labels.
    /// </summary>
    /// <param name="labels">The labels to render at numeric x values.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXLabels(IEnumerable<ChartAxisLabel> labels) {
        if (labels == null) throw new ArgumentNullException(nameof(labels));
        Options.XAxisLabels.Clear();
        var materialized = labels.ToArray();
        foreach (var label in materialized) {
            ChartGuards.Finite(label.Value, nameof(labels));
            if (label.Text == null) throw new ArgumentException("Axis label text must not be null.", nameof(labels));
        }

        Options.XAxisLabels.AddRange(materialized);
        return this;
    }

    /// <summary>
    /// Sets x-axis labels from date/time values.
    /// </summary>
    /// <param name="dates">The date/time values to label.</param>
    /// <param name="format">The date/time format string used for labels.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXDateLabels(IEnumerable<DateTime> dates, string format = "MMM d") {
        if (dates == null) throw new ArgumentNullException(nameof(dates));
        if (format == null) throw new ArgumentNullException(nameof(format));
        Options.XAxisLabels.Clear();
        foreach (var date in dates) Options.XAxisLabels.Add(new ChartAxisLabel(date, date.ToString(format, CultureInfo.InvariantCulture)));
        return this;
    }

    /// <summary>
    /// Adds a line series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddLine(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Line, points, color);

    /// <summary>
    /// Adds a smoothed line series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddSmoothLine(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Line, points, color, true);

    /// <summary>
    /// Adds a stepped line series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddStepLine(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.StepLine, points, color);

    /// <summary>
    /// Adds an area series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddArea(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Area, points, color);

    /// <summary>
    /// Adds a stepped area series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddStepArea(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.StepArea, points, color);

    /// <summary>
    /// Adds a smoothed area series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddSmoothArea(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Area, points, color, true);

    /// <summary>
    /// Adds an area series stacked on top of earlier stacked area series at matching x values.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddStackedArea(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.StackedArea, points, color);

    /// <summary>
    /// Adds a smoothed area series stacked on top of earlier stacked area series at matching x values.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddSmoothStackedArea(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.StackedArea, points, color, true);

    /// <summary>
    /// Adds a scatter series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddScatter(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Scatter, points, color);

    /// <summary>
    /// Adds a bar series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBar(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Bar, points, color);

    /// <summary>
    /// Adds a lollipop series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The series data points.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddLollipop(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Lollipop, points, color);

    /// <summary>
    /// Adds a horizontal bar series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The category/value points. The x values identify categories and the y values set bar length.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHorizontalBar(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.HorizontalBar, points, color);

    /// <summary>
    /// Adds a single-value gauge series.
    /// </summary>
    /// <param name="name">The gauge label.</param>
    /// <param name="value">The gauge value.</param>
    /// <param name="min">The minimum gauge value.</param>
    /// <param name="max">The maximum gauge value.</param>
    /// <param name="color">An optional gauge accent color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddGauge(string name, double value, double min = 0, double max = 100, ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        ChartGuards.Finite(min, nameof(min));
        ChartGuards.Finite(max, nameof(max));
        if (max <= min) throw new ArgumentOutOfRangeException(nameof(max), max, "Gauge maximum must be greater than minimum.");
        Series.Add(new ChartSeries(name, ChartSeriesKind.Gauge, new[] { new ChartPoint(min, value), new ChartPoint(max, value) }) { Color = color });
        return this;
    }

    /// <summary>
    /// Adds a bullet series showing a value, target marker, and qualitative range bands.
    /// </summary>
    /// <param name="name">The bullet row label.</param>
    /// <param name="value">The measured value.</param>
    /// <param name="target">The target marker value.</param>
    /// <param name="min">The minimum scale value.</param>
    /// <param name="max">The maximum scale value.</param>
    /// <param name="rangeEnds">Optional qualitative range end values. Values are clamped to the scale.</param>
    /// <param name="color">An optional value accent color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBullet(string name, double value, double target, double min = 0, double max = 100, IEnumerable<double>? rangeEnds = null, ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        ChartGuards.Finite(target, nameof(target));
        ChartGuards.Finite(min, nameof(min));
        ChartGuards.Finite(max, nameof(max));
        if (max <= min) throw new ArgumentOutOfRangeException(nameof(max), max, "Bullet maximum must be greater than minimum.");
        var points = new List<ChartPoint> {
            new(min, value),
            new(max, target)
        };
        if (rangeEnds != null) {
            foreach (var rangeEnd in rangeEnds) {
                ChartGuards.Finite(rangeEnd, nameof(rangeEnds));
                points.Add(new ChartPoint(rangeEnd, rangeEnd));
            }
        }

        Series.Add(new ChartSeries(name, ChartSeriesKind.Bullet, points) { Color = color });
        return this;
    }

    /// <summary>
    /// Adds a waterfall series where each point y-value represents a positive or negative change.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The waterfall steps. The x values identify categories and the y values set cumulative changes.</param>
    /// <param name="color">An optional positive change color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddWaterfall(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Waterfall, points, color);

    /// <summary>
    /// Adds a radar series for comparing values across radial categories.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The category/value points. The x values identify radial categories and the y values set distance from center.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRadar(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Radar, points, color);

    /// <summary>
    /// Adds a funnel series for descending stage or conversion values.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The stage/value points. The x values identify stages and the y values set segment width.</param>
    /// <param name="color">An optional base color. When null, the theme palette colors the segments.</param>
    /// <returns>The current chart.</returns>
    public Chart AddFunnel(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Funnel, points, color);

    /// <summary>
    /// Adds a timeline range item with date/time bounds.
    /// </summary>
    /// <param name="name">The timeline item label.</param>
    /// <param name="start">The range start date/time.</param>
    /// <param name="end">The range end date/time.</param>
    /// <param name="color">An optional item color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTimelineItem(string name, DateTime start, DateTime end, ChartColor? color = null) => AddTimelineRange(name, start.ToOADate(), end.ToOADate(), color);

    /// <summary>
    /// Adds a timeline range item with numeric bounds.
    /// </summary>
    /// <param name="name">The timeline item label.</param>
    /// <param name="start">The range start value.</param>
    /// <param name="end">The range end value.</param>
    /// <param name="color">An optional item color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTimelineRange(string name, double start, double end, ChartColor? color = null) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), end, "Timeline end must be greater than or equal to start.");
        Series.Add(new ChartSeries(name, ChartSeriesKind.Timeline, new[] { new ChartPoint(start, end) }) { Color = color });
        return this;
    }

    /// <summary>
    /// Adds a pie series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The slice values. The x values are used for optional slice labels.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPie(string name, IEnumerable<ChartPoint> points) => Add(name, ChartSeriesKind.Pie, points, null);

    /// <summary>
    /// Adds a donut series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The slice values. The x values are used for optional slice labels.</param>
    /// <returns>The current chart.</returns>
    public Chart AddDonut(string name, IEnumerable<ChartPoint> points) => Add(name, ChartSeriesKind.Donut, points, null);

    /// <summary>
    /// Adds a horizontal line annotation at a y-axis value.
    /// </summary>
    /// <param name="value">The y-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHorizontalLine(double value, string label = "", ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        Annotations.Add(new ChartAnnotation(ChartAnnotationKind.HorizontalLine, value, null, label, color ?? Options.Theme.Axis, 1));
        return this;
    }

    /// <summary>
    /// Adds a vertical line annotation at an x-axis value.
    /// </summary>
    /// <param name="value">The x-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddVerticalLine(double value, string label = "", ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        Annotations.Add(new ChartAnnotation(ChartAnnotationKind.VerticalLine, value, null, label, color ?? Options.Theme.Axis, 1));
        return this;
    }

    /// <summary>
    /// Adds a horizontal band annotation between two y-axis values.
    /// </summary>
    /// <param name="start">The first y-axis value.</param>
    /// <param name="end">The second y-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <param name="opacity">The band opacity.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHorizontalBand(double start, double end, string label = "", ChartColor? color = null, double opacity = 0.14) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        Annotations.Add(new ChartAnnotation(ChartAnnotationKind.HorizontalBand, start, end, label, color ?? Options.Theme.Axis, opacity));
        return this;
    }

    /// <summary>
    /// Adds a vertical band annotation between two x-axis values.
    /// </summary>
    /// <param name="start">The first x-axis value.</param>
    /// <param name="end">The second x-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <param name="opacity">The band opacity.</param>
    /// <returns>The current chart.</returns>
    public Chart AddVerticalBand(double start, double end, string label = "", ChartColor? color = null, double opacity = 0.14) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        Annotations.Add(new ChartAnnotation(ChartAnnotationKind.VerticalBand, start, end, label, color ?? Options.Theme.Axis, opacity));
        return this;
    }

    private Chart Add(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points, ChartColor? color, bool smooth = false) {
        Series.Add(new ChartSeries(name ?? throw new ArgumentNullException(nameof(name)), kind, ChartGuards.Points(points, nameof(points))) { Color = color, Smooth = smooth });
        return this;
    }
}
