using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the Cartesian options migrated from the separate legacy export paths.</summary>
public sealed class V2CartesianMigrationTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void StatePaintAndDrawingPriority_PreserveExplicitOverridesAndSeriesIdentity(VisualThemeMode mode) {
        var chart = Chart.Create().AddScatter("Danger", Points(3)).AddScatter("Quiet", Points(4)).AddScatter("Warning", Points(5));
        chart.Series[0].StateRole = ChartSeriesState.Danger;
        chart.Series[1].StateRole = ChartSeriesState.Quiet;
        chart.Series[2].StateRole = ChartSeriesState.Warning;
        chart.Series[2].SemanticRole = "forecast";
        chart.Series[2].WithInteractionKey("forecast-data");
        var context = new VisualRenderContext(themeMode: mode);
        var colors = context.Theme.Resolve(mode);
        var scene = Compile(chart, context);
        var series = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "series").ToArray();
        Assert.Equal(new[] { "series-1", "series-2", "series-0" }, series.Select(group => group.Id));
        var marks = scene.Nodes.OfType<VisualSceneEllipse>().ToArray();
        Assert.Equal(colors.Status.Pass.Fill, marks[0].Fill);
        Assert.Equal(colors.Status.Medium.Fill, marks[1].Fill);
        Assert.Equal(colors.Status.Critical.Fill, marks[2].Fill);
        Assert.Equal("forecast", series[1].Metadata["data-cfx-semantic-role"]);
        Assert.Equal("forecast-data", series[1].Metadata["data-cfx-series-key"]);
        var explicitColor = ChartColor.FromHex("#9E78B4");
        chart.Series[0].Color = explicitColor;
        chart.Series[0].WithPointColor(0, ChartColor.FromHex("#175AA4"));
        var updated = Compile(chart, context);
        Assert.Equal(ChartColor.FromHex("#175AA4"), updated.Nodes.OfType<VisualSceneEllipse>().Last().Fill);
        Assert.Equal(explicitColor, VisualCartesianCompiler.LegendEntries(chart, colors)[0].Color);
    }

    [Theory]
    [InlineData(ChartBarStyle.Flat)]
    [InlineData(ChartBarStyle.Solid)]
    [InlineData(ChartBarStyle.SegmentedCapsule)]
    public void ExplicitBarTreatments_KeepGeometryPatternsAlphaAndDetachedStyles(ChartBarStyle treatment) {
        var color = ChartColor.FromRgba(60, 120, 180, 128);
        var chart = Chart.Create().AddBar("Observed", Points(6, -4), color).WithAxes(false);
        chart.Options.ShowGrid = false;
        chart.Options.BarStyle = treatment;
        chart.Options.BarVisualStyle.CornerRadius = 3;
        chart.Series[0].FillPattern = ChartFillPattern.Crosshatch;
        chart.Series[0].WithPointFillPattern(0, ChartFillPattern.None);
        var scene = Compile(chart);
        var bars = BarShapes(scene).ToArray();
        Assert.Equal(2, bars.Length);
        Assert.All(bars, bar => { Assert.True(bar.Bounds.Width > 0); Assert.True(bar.Bounds.Height > 0); Assert.Equal(3, bar.Radius); });
        Assert.Contains(scene.Nodes, node => node.Role == "bar-pattern");
        if (treatment == ChartBarStyle.Solid) {
            var gradients = scene.Nodes.OfType<VisualSceneGradient>().ToArray();
            Assert.Equal(2, gradients.Length);
            Assert.All(gradients.SelectMany(gradient => gradient.Stops), stop => Assert.InRange(stop.Color.A, (byte)0, (byte)128));
            Assert.NotEqual(gradients[0].Stops[0].Color, gradients[0].Stops[1].Color);
        } else Assert.All(bars, bar => Assert.InRange(bar.Fill!.Value.A, (byte)0, (byte)128));
        if (treatment == ChartBarStyle.SegmentedCapsule) {
            var caps = scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "bar-cap").ToArray();
            Assert.Equal(2, caps.Length);
            Assert.True(caps[0].Start.Y < bars[0].Bounds.Top + bars[0].Bounds.Height / 2);
            Assert.True(caps[1].Start.Y > bars[1].Bounds.Top + bars[1].Bounds.Height / 2);
            Assert.All(caps, cap => Assert.InRange(cap.Stroke!.Value.A, (byte)0, (byte)128));
        }
        var svg = VisualSceneSvgRenderer.Render(scene);
        chart.Options.BarVisualStyle.CapOpacity = .1;
        chart.Series[0].FillPattern = ChartFillPattern.None;
        Assert.Equal(svg, VisualSceneSvgRenderer.Render(scene));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitLineLayers_UseOnePathAndRetainAlphaAcrossLegacyThemes(bool graphite) {
        var chart = Chart.Create().AddLine("Observed", Points(3, 8, 4), ChartColor.FromRgba(50, 100, 150, 128));
        chart.WithTheme(graphite ? ChartTheme.GraphiteLight() : ChartTheme.Light());
        chart.Options.LineVisualStyle = ChartLineVisualStyle.Luminous();
        chart.Series[0].StrokeWidth = 4;
        var scene = Compile(chart);
        var paths = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role?.StartsWith("line", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(new[] { "line-ambient-halo", "line-halo", "line", "line-highlight" }, paths.Select(path => path.Role));
        Assert.Equal(4, paths[2].StrokeWidth);
        Assert.All(paths, path => { Assert.Equal(paths[0].Commands.Count, path.Commands.Count); Assert.InRange(path.Stroke!.Value.A, (byte)1, (byte)128); });
        chart.WithTheme(graphite ? ChartTheme.Light() : ChartTheme.GraphiteLight());
        Assert.Equal(VisualSceneSvgRenderer.Render(scene), VisualSceneSvgRenderer.Render(Compile(chart)));
    }

    [Fact]
    public void DirectLineStyleMutation_PreservesCoupledHaloDefaultsAcrossLegacyThemeSwitches() {
        var chart = Chart.Create().AddLine("Observed", Points(3, 8)).WithTheme(ChartTheme.Light());
        chart.Options.LineVisualStyle.HaloOpacity = .4;
        var scene = Compile(chart);
        Assert.Contains(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "line-halo" && path.StrokeWidth > 2);
        chart.WithTheme(ChartTheme.GraphiteLight());
        Assert.Equal(VisualSceneSvgRenderer.Render(scene), VisualSceneSvgRenderer.Render(Compile(chart)));
    }

    [Fact]
    public void SecondaryBarStacks_KeepSeparateRangesBaselinesAndFormattedTotals() {
        var chart = Chart.Create().AddBar("Primary A", Points(4, -3)).AddBar("Secondary A", Points(20, -15))
            .AddBar("Primary B", Points(6, -7)).AddBar("Secondary B", Points(30, -35)).WithAxes(false);
        chart.Series[1].UseSecondaryYAxis(); chart.Series[3].UseSecondaryYAxis();
        chart.Options.ShowGrid = false;
        chart.Options.BarMode = ChartBarMode.Stacked;
        chart.Options.ShowStackTotals = true;
        chart.Options.ValueFormatter = value => "total/value " + value.ToString(CultureInfo.InvariantCulture);
        var coordinates = ChartBarCoordinateMap.Create(chart);
        Assert.Equal(4, ChartBarStacking.BaseValue(chart, coordinates, 2, 0));
        Assert.Equal(20, ChartBarStacking.BaseValue(chart, coordinates, 3, 0));
        Assert.Equal(-3, ChartBarStacking.BaseValue(chart, coordinates, 2, 1));
        Assert.Equal(-15, ChartBarStacking.BaseValue(chart, coordinates, 3, 1));
        var range = ChartRange.FromChart(chart, coordinates);
        var secondary = ChartRange.FromSecondaryYAxis(chart, range);
        Assert.Equal(-10, range.MinY); Assert.Equal(-50, secondary.MinY);
        Assert.InRange(range.MaxY, 10, 12); Assert.InRange(secondary.MaxY, 50, 60);
        var scene = Compile(chart);
        var bars = BarShapes(scene).ToArray();
        Assert.Equal(bars[4].Bounds.Bottom, bars[0].Bounds.Top, 8);
        Assert.Equal(bars[6].Bounds.Bottom, bars[2].Bounds.Top, 8);
        Assert.True(bars[0].Bounds.Right < bars[2].Bounds.Left);
        var totals = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "stack-total").ToArray();
        Assert.Equal(4, totals.Length);
        Assert.Contains(totals, total => total.Metadata["data-cfx-axis"] == "primary" && total.Metadata["data-cfx-y"] == "10");
        Assert.Contains(totals, total => total.Metadata["data-cfx-axis"] == "secondary" && total.Metadata["data-cfx-y"] == "-50");
        Assert.All(scene.Regions.Where(region => region.Role == "stack-total"), region => Assert.StartsWith("total/value ", region.Label!));
    }

    [Fact]
    public void SecondaryStackedAreas_StackOnlyTheirOwnAxis_AndPreserveDisconnectedSegments() {
        var chart = Chart.Create().AddStackedArea("Primary", Points(100, 200)).AddStackedArea("Secondary A", Points(2, -3))
            .AddStackedArea("Secondary B", new[] { new ChartPoint(1, 4), new ChartPoint(2, -5, true), new ChartPoint(3, -2) }).WithAxes(false);
        chart.Series[1].UseSecondaryYAxis(); chart.Series[2].UseSecondaryYAxis();
        chart.Options.ShowGrid = false;
        var primary = ChartRange.FromChart(chart);
        var secondary = ChartRange.FromSecondaryYAxis(chart, primary);
        Assert.Equal(-8, secondary.MinY);
        Assert.InRange(secondary.MaxY, 6, 8);
        var plot = new ChartRect(70, 40, 490, 290);
        var mapper = new ChartMapper(plot, secondary, chart.Options.XAxis, chart.Options.SecondaryYAxis);
        var scene = Compile(chart);
        var paths = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "line").ToArray();
        Assert.Equal(mapper.Y(6), paths[2].Commands[0].Y, 8);
        Assert.Equal(2, paths[2].Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Equal(4, scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "area"));
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(45)]
    [InlineData(90)]
    public void RotatedExplicitTicks_UseMeasuredSharedSceneTransforms_AndAllKeepsEveryLabel(double angle) {
        var chart = Chart.Create().AddScatter("Observed", new[] { new ChartPoint(0, 1), new ChartPoint(2, 3) });
        chart.Options.YAxis.Visible = false;
        chart.Options.XAxis.WithBounds(0, 2);
        chart.Options.XAxis.Labels.AddRange(new[] { new ChartAxisLabel(0, "Zero"), new ChartAxisLabel(1, "One"), new ChartAxisLabel(2, "Two") });
        chart.Options.XAxis.LabelDensity = ChartLabelDensity.All;
        chart.Options.XAxis.LabelAngle = angle;
        var scene = Compile(chart, measured: true);
        Assert.Equal(3, scene.Nodes.OfType<VisualSceneText>().Count(text => text.Role == "axis-x-label"));
        var rotations = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Rotation.HasValue).ToArray();
        Assert.Equal(3, rotations.Length);
        Assert.All(rotations, group => Assert.Equal(angle, group.Rotation!.Value.Degrees));
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        Assert.Equal(3, svg.Descendants().Count(element => element.Attribute("transform")?.Value.StartsWith("rotate(", StringComparison.Ordinal) == true));
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(640, image.Width); Assert.Equal(400, image.Height);
    }

    [Fact]
    public void AllDensity_AllowsExplicitLabelOverlaps_WhileAutomaticDensityReducesThem() {
        var chart = Chart.Create().AddScatter("Observed", new[] { new ChartPoint(0, 1), new ChartPoint(11, 3) });
        chart.Options.YAxis.Visible = false;
        chart.Options.XAxis.WithBounds(0, 11);
        chart.Options.XAxis.Labels.AddRange(Enumerable.Range(0, 12).Select(index => new ChartAxisLabel(index, "A" + index)));
        chart.Options.XAxis.LabelDensity = ChartLabelDensity.All;
        var plot = new ChartRect(70, 40, 35, 200);
        Assert.Equal(12, Compile(chart, plot: plot).Nodes.Count(node => node.Role == "axis-x-label"));
        chart.Options.XAxis.LabelDensity = ChartLabelDensity.Auto;
        Assert.True(Compile(chart, plot: plot).Nodes.Count(node => node.Role == "axis-x-label") < 12);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Scatter)]
    public void PointLegends_RetainSourceIdsLabelsStylesAndExplicitColors(ChartSeriesKind kind) {
        var chart = kind == ChartSeriesKind.Bar ? Chart.Create().AddBar("Observed", Points(3, 5)) : Chart.Create().AddScatter("Observed", Points(3, 5));
        chart.Options.ShowPointLegend = true;
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(1, "First observation"));
        chart.Series[0].WithInteractionKey("observed-data").WithPointColor(0, ChartColor.FromHex("#AD376A")).WithPointFillPattern(0, ChartFillPattern.Crosshatch);
        chart.Series[0].StateRole = ChartSeriesState.Info;
        var colors = VisualTheme.Graphite().Resolve(VisualThemeMode.Light);
        var entries = VisualCartesianCompiler.LegendEntries(chart, colors);
        Assert.Equal(2, entries.Count);
        Assert.Equal("First observation", entries[0].Label); Assert.Equal("Item 2", entries[1].Label);
        Assert.Equal("series-0-point-0", entries[0].Id); Assert.Equal(ChartColor.FromHex("#AD376A"), entries[0].Color);
        Assert.Equal(ChartFillPattern.Crosshatch, entries[0].Pattern); Assert.Equal(ChartSeriesState.Info, entries[0].StateRole);
        Assert.Equal(kind, entries[0].Kind); Assert.Equal("observed-data", entries[0].SeriesKey);
        chart.Series[0].ShowInLegend = false;
        Assert.Empty(VisualCartesianCompiler.LegendEntries(chart, colors));
    }

    [Fact]
    public void HighlightedTicksAndAnnotationLayers_RetainFullSourceDescriptions() {
        var chart = Chart.Create().AddBar("Observed", Points(3, 6));
        var highlight = ChartColor.FromHex("#AF356A");
        chart.Options.XAxis.Labels.AddRange(new[] { new ChartAxisLabel(1, "First"), new ChartAxisLabel(2, "Second") });
        chart.WithHighlightedXAxisLabel(1, highlight).WithHighlightedXAxisRange(1, 2, highlight, label: "Full selected interval description");
        chart.Annotations.Add(new ChartAnnotation(ChartAnnotationKind.HorizontalLine, 4, null, "Complete target description", highlight, 1));
        var scene = Compile(chart, measured: true);
        Assert.Contains(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "axis-x-label" && text.Color.Equals(highlight));
        var bands = scene.Nodes.Select((node, index) => (node, index)).Where(item => item.node is VisualSceneRectangle && item.node.Role == "annotation-band").ToArray();
        var firstBar = scene.Nodes.Select((node, index) => (node, index)).First(item => item.node.Role == "bar").index;
        Assert.All(bands, band => Assert.True(band.index < firstBar));
        var lastBar = scene.Nodes.Select((node, index) => (node, index)).Last(item => item.node.Role == "bar").index;
        Assert.Contains(scene.Nodes.Select((node, index) => (node, index)), item => item.node is VisualScenePath && item.node.Role == "annotation-line" && item.index > lastBar);
        Assert.Contains(scene.Regions, region => region.Label == "Full selected interval description");
        Assert.Contains(scene.Regions, region => region.Label == "Complete target description");
    }

    [Fact]
    public void MarkerModesAndSparkline_KeepSourceSemanticsWhenVisibleMarkersAreSuppressed() {
        var chart = Chart.Create().AddLine("Observed", Points(3, 8, 4));
        Assert.Single(Compile(chart).Nodes.OfType<VisualSceneEllipse>());
        chart.Options.LineMarkerMode = ChartLineMarkerMode.All;
        Assert.Equal(3, Compile(chart).Nodes.OfType<VisualSceneEllipse>().Count());
        chart.Options.LineMarkerMode = ChartLineMarkerMode.None;
        var hidden = Compile(chart);
        Assert.Empty(hidden.Nodes.OfType<VisualSceneEllipse>());
        Assert.Equal(3, hidden.Regions.Count(region => region.Role == "point"));
        chart.Options.LineMarkerMode = ChartLineMarkerMode.All;
        chart.Options.IsSparkline = true;
        Assert.Empty(Compile(chart).Nodes.OfType<VisualSceneEllipse>());
        chart.Options.IsSparkline = false;
        chart.Series[0].MarkerRadius = 0;
        Assert.Empty(Compile(chart).Nodes.OfType<VisualSceneEllipse>());
    }

    private static IEnumerable<VisualSceneRectangle> BarShapes(VisualScene scene) => scene.Nodes
        .Select(node => node is VisualSceneGradient gradient ? gradient.Shape : node).OfType<VisualSceneRectangle>().Where(rect => rect.Role == "bar");

    private static VisualScene Compile(Chart chart, VisualRenderContext? context = null, bool measured = false, ChartRect? plot = null) {
        context ??= new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(640, 400), context.Font);
        if (measured) VisualCartesianCompiler.BuildInViewport(chart, context, builder, new ChartRect(30, 25, 580, 345));
        else VisualCartesianCompiler.Build(chart, context, builder, plot ?? new ChartRect(70, 40, 490, 290));
        return builder.Build();
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value));
}
