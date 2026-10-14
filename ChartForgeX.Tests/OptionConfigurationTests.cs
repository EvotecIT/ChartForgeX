using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects transactional callbacks and detached ownership at the mutable model boundary.</summary>
public sealed class OptionConfigurationTests {
    [Theory]
    [InlineData("bubble")]
    [InlineData("box-plot")]
    [InlineData("chord")]
    [InlineData("funnel")]
    [InlineData("gauge")]
    [InlineData("pyramid")]
    [InlineData("range-bar")]
    [InlineData("sankey")]
    [InlineData("sunburst")]
    [InlineData("treemap")]
    [InlineData("waterfall")]
    [InlineData("map-color")]
    [InlineData("heatmap-color")]
    [InlineData("labels")]
    [InlineData("accessibility")]
    [InlineData("line-style")]
    [InlineData("bar-style")]
    [InlineData("grid-style")]
    public void ChartCallbacksRollBackExceptionsAndDetachRetainedCandidates(string family) {
        var chart = Chart.Create();
        switch (family) {
            case "bubble": Verify(() => chart.Options.Bubble, action => chart.ConfigureBubble(action), (options, value) => options.MinimumRadius = value, options => options.MinimumRadius, 8d, 9d); break;
            case "box-plot": Verify(() => chart.Options.BoxPlot, action => chart.ConfigureBoxPlot(action), (options, value) => options.Orientation = value, options => options.Orientation, ChartOrientation.Horizontal, ChartOrientation.Vertical); break;
            case "chord": Verify(() => chart.Options.Chord, action => chart.ConfigureChord(action), (options, value) => options.NodeGapDegrees = value, options => options.NodeGapDegrees, 5d, 7d); break;
            case "funnel": Verify(() => chart.Options.Funnel, action => chart.ConfigureFunnel(action), (options, value) => options.Orientation = value, options => options.Orientation, ChartOrientation.Horizontal, ChartOrientation.Vertical); break;
            case "gauge": Verify(() => chart.Options.Gauge, action => chart.ConfigureGauge(action), (options, value) => options.Caption = value, options => options.Caption, "Observed", "Retained mutation"); break;
            case "pyramid": Verify(() => chart.Options.Pyramid, action => chart.ConfigurePyramid(action), (options, value) => options.Reversed = value, options => options.Reversed, true, false); break;
            case "range-bar": Verify(() => chart.Options.RangeBar, action => chart.ConfigureRangeBar(action), (options, value) => options.Orientation = value, options => options.Orientation, ChartOrientation.Horizontal, ChartOrientation.Vertical); break;
            case "sankey": Verify(() => chart.Options.Sankey, action => chart.ConfigureSankey(action), (options, value) => options.NodeWidth = value, options => options.NodeWidth, 14d, 18d); break;
            case "sunburst": Verify(() => chart.Options.Sunburst, action => chart.ConfigureSunburst(action), (options, value) => options.CornerRadius = value, options => options.CornerRadius, 3d, 5d); break;
            case "treemap": Verify(() => chart.Options.Treemap, action => chart.ConfigureTreemap(action), (options, value) => options.Gap = value, options => options.Gap, 4d, 6d); break;
            case "waterfall": Verify(() => chart.Options.Waterfall, action => chart.ConfigureWaterfall(action), (options, value) => options.Orientation = value, options => options.Orientation, ChartOrientation.Horizontal, ChartOrientation.Vertical); break;
            case "map-color": Verify(() => chart.Options.MapColorDimension, action => chart.ConfigureMapColorDimension(action), (options, value) => options.Name = value, options => options.Name, "Population", "Retained mutation"); break;
            case "heatmap-color": Verify(() => chart.Options.HeatmapColorDimension, action => chart.ConfigureHeatmapColorDimension(action), (options, value) => options.Name = value, options => options.Name, "Temperature", "Retained mutation"); break;
            case "labels": Verify(() => chart.Options.Labels, action => chart.ConfigureLabels(action), (options, value) => options.Now = value, options => options.Now, "Jetzt", "Retained mutation"); break;
            case "accessibility": Verify(() => chart.Accessibility, action => chart.ConfigureAccessibility(action), (options, value) => options.Description = value, options => options.Description, "Observed trend", "Retained mutation"); break;
            case "line-style": Verify(() => chart.Options.LineVisualStyle, action => chart.ConfigureLineVisualStyle(action), (options, value) => options.HaloOpacity = value, options => options.HaloOpacity, .2, .3); break;
            case "bar-style": Verify(() => chart.Options.BarVisualStyle, action => chart.ConfigureBarVisualStyle(action), (options, value) => options.CornerRadius = value, options => options.CornerRadius, 3d, 5d); break;
            case "grid-style": Verify(() => chart.Options.GridLineStyle, action => chart.ConfigureGridStyle(action), (options, value) => options.Dash = value, options => options.Dash, 3d, 5d); break;
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AxisCallbacksPreserveAuthoredPolicyAndDetachExplicitLabels(int axisIndex) {
        var chart = Chart.Create();
        ChartAxis Current() => axisIndex == 0 ? chart.Options.XAxis : axisIndex == 1 ? chart.Options.YAxis : chart.Options.SecondaryYAxis;
        void Configure(Action<ChartAxis> action) {
            if (axisIndex == 0) chart.ConfigureXAxis(action);
            else if (axisIndex == 1) chart.ConfigureYAxis(action);
            else chart.ConfigureSecondaryYAxis(action);
        }
        Verify(Current, Configure, (axis, value) => axis.TickCount = value, axis => axis.TickCount, 8, 12);
        Current().Scale = ChartScaleKind.Linear;
        Current().Labels.Add(new ChartAxisLabel(1, "First"));
        var previous = Current(); ChartAxis? retained = null;
        Assert.Throws<InvalidOperationException>(() => Configure(axis => { axis.Labels.Clear(); axis.Scale = ChartScaleKind.Time; throw new InvalidOperationException(); }));
        Assert.Single(Current().Labels);
        Configure(axis => { retained = axis; axis.Labels.Add(new ChartAxisLabel(2, "Second")); });
        retained!.Labels.Clear(); previous.Labels.Clear();
        Assert.Equal(new[] { "First", "Second" }, Current().Labels.Select(label => label.Text));
        Current().UseTimeScaleByDefault();
        Assert.Equal(ChartScaleKind.Linear, Current().Scale);
    }

    [Fact]
    public void NestedFamilyCollectionsAndStylesDoNotEscapeTheirCallback() {
        var chart = Chart.Create();
        chart.Options.Gauge.Bands.Add(new ChartGaugeBand(0, 50, ChartSeriesState.Success));
        ChartGaugeOptions? gauge = null;
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureGauge(options => { options.Bands.Clear(); throw new InvalidOperationException(); }));
        chart.ConfigureGauge(options => { gauge = options; options.Bands.Add(new ChartGaugeBand(50, 100, ChartSeriesState.Warning)); });
        gauge!.Bands.Clear();
        Assert.Equal(2, chart.Options.Gauge.Bands.Count);

        ChartSunburstOptions? sunburst = null;
        chart.ConfigureSunburst(options => { sunburst = options; options.ColorDimension.Name = "Cost"; options.SecondaryLabelStyle.FontSize = 14; });
        sunburst!.ColorDimension.Name = "Other"; sunburst.SecondaryLabelStyle.FontSize = 18;
        Assert.Equal("Cost", chart.Options.Sunburst.ColorDimension.Name);
        Assert.Equal(14, chart.Options.Sunburst.SecondaryLabelStyle.FontSize);
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureSunburst(options => { options.SecondaryLabelStyle.FontSize = 21; throw new InvalidOperationException(); }));
        Assert.Equal(14, chart.Options.Sunburst.SecondaryLabelStyle.FontSize);
        ChartTreemapOptions? treemap = null;
        chart.ConfigureTreemap(options => { treemap = options; options.ColorDimension.Name = "Revenue"; });
        treemap!.ColorDimension.Name = "Other";
        Assert.Equal("Revenue", chart.Options.Treemap.ColorDimension.Name);
    }

    [Theory]
    [InlineData("markers")]
    [InlineData("radar")]
    [InlineData("difference")]
    [InlineData("financial")]
    [InlineData("color")]
    [InlineData("label")]
    public void SeriesCallbacksAreTransactionalAndRetainTheirOwnOptions(string family) {
        var chart = Chart.Create();
        var points = new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) };
        if (family == "radar") chart.AddRadar("Samples", points);
        else if (family == "difference") chart.AddDifferenceArea("Samples", new[] { new ChartDifferencePoint(0, 2, 1), new ChartDifferencePoint(1, 3, 2) });
        else if (family == "financial") chart.AddCandlestick("Samples", new[] { new ChartCandlestick(0, 2, 4, 1, 3) });
        else chart.AddScatter("Samples", points);
        var series = chart.Series[0];
        switch (family) {
            case "markers": Verify(() => series.Markers, action => series.ConfigureMarkers(action), (options, value) => options.Radius = value, options => options.Radius, (double?)7, 9); break;
            case "radar": Verify(() => series.Radar, action => series.ConfigureRadar(action), (options, value) => options.CurveTension = value, options => options.CurveTension, (double?).2, .4); break;
            case "difference": Verify(() => series.DifferenceArea, action => series.ConfigureDifferenceArea(action), (options, value) => options.FirstName = value, options => options.FirstName, "Before", "Other"); break;
            case "financial": Verify(() => series.Financial, action => series.ConfigureFinancial(action), (options, value) => options.Rising.Wick.StrokeWidth = value, options => options.Rising.Wick.StrokeWidth, (double?)3, 5); break;
            case "color": Verify(() => series.ColorDimension, action => series.ConfigureColorDimension(action), (options, value) => options.Name = value, options => options.Name, "Temperature", "Other"); break;
            case "label": Verify(() => series.DataLabelStyle, action => series.ConfigureDataLabelStyle(action), (options, value) => options.FontSize = value, options => options.FontSize, (double?)14, 18); break;
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
    }

    [Fact]
    public void PointStyleFailureDoesNotGrowTheOverrideCollectionAndSuccessDetachesTheCandidate() {
        var chart = Chart.Create().AddLine("Samples", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var series = chart.Series[0];
        Assert.Throws<InvalidOperationException>(() => series.ConfigurePointDataLabelStyle(1, style => { style.FontSize = 18; throw new InvalidOperationException(); }));
        Assert.Empty(series.PointDataLabelStyles);
        TextStyleOverride? retained = null;
        series.ConfigurePointDataLabelStyle(1, style => { retained = style; style.FontSize = 14; });
        retained!.FontSize = 21;
        Assert.Equal(14, series.PointDataLabelStyles[1]!.FontSize);
        Verify(() => series.PointDataLabelStyles[1]!, action => series.ConfigurePointDataLabelStyle(1, action), (style, value) => style.FontSize = value, style => style.FontSize, (double?)16, 22);
    }

    [Theory]
    [InlineData(ChartTextRole.Title)]
    [InlineData(ChartTextRole.Subtitle)]
    [InlineData(ChartTextRole.AxisTitle)]
    [InlineData(ChartTextRole.TickLabel)]
    [InlineData(ChartTextRole.Legend)]
    [InlineData(ChartTextRole.DataLabel)]
    public void EveryChartTextRoleAndItsConvenienceCallbackOwnsDetachedStyles(ChartTextRole role) {
        var chart = Chart.Create();
        Verify(() => chart.Options.GetTextStyle(role), action => chart.ConfigureTextStyle(role, action),
            (style, value) => style.FontSize = value, style => style.FontSize, (double?)14, 18);
        void Configure(Action<TextStyleOverride> action) {
            switch (role) {
                case ChartTextRole.Title: chart.ConfigureTitleStyle(action); break;
                case ChartTextRole.Subtitle: chart.ConfigureSubtitleStyle(action); break;
                case ChartTextRole.AxisTitle: chart.ConfigureAxisTitleStyle(action); break;
                case ChartTextRole.TickLabel: chart.ConfigureTickLabelStyle(action); break;
                case ChartTextRole.Legend: chart.ConfigureLegendStyle(action); break;
                case ChartTextRole.DataLabel: chart.ConfigureDataLabelStyle(action); break;
            }
        }
        Verify(() => chart.Options.GetTextStyle(role), Configure, (style, value) => style.FontSize = value, style => style.FontSize, (double?)16, 20);
    }

    [Fact]
    public void ChartGridTextCallbacksFollowTheSameOwnershipContract() {
        var grid = ChartGrid.Create();
        Verify(() => grid.TitleStyle, action => grid.ConfigureTitleStyle(action), (style, value) => style.FontSize = value, style => style.FontSize, (double?)14, 18);
        Verify(() => grid.SubtitleStyle, action => grid.ConfigureSubtitleStyle(action), (style, value) => style.FontSize = value, style => style.FontSize, (double?)12, 16);
    }

    private static void Verify<T, TValue>(Func<T> current, Action<Action<T>> configure,
        Action<T, TValue> write, Func<T, TValue> read, TValue changed, TValue retainedChange) where T : class {
        var previous = current(); var initial = read(previous);
        Assert.Throws<ArgumentNullException>(() => configure(null!));
        Assert.Throws<InvalidOperationException>(() => configure(candidate => { write(candidate, changed); throw new InvalidOperationException(); }));
        Assert.Same(previous, current()); Assert.Equal(initial, read(current()));
        T? retained = null;
        configure(candidate => { retained = candidate; write(candidate, changed); });
        Assert.NotSame(previous, current()); Assert.NotSame(retained, current());
        Assert.Equal(initial, read(previous)); Assert.Equal(changed, read(current()));
        write(retained!, retainedChange); write(previous, retainedChange);
        Assert.Equal(changed, read(current()));
    }
}
