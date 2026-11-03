using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ChartXZoomTests {
    private static readonly ChartColor Blue = new(20, 60, 220);

    [Fact]
    public void LevelsAreFiniteIncreasingDetachedValuesAndInvalidNavigationFailsExplicitly() {
        var values = new[] { 1d, 2, 4 }; var levels = new ChartXZoomLevels(values); values[1] = 9;
        Assert.Equal(new[] { 1d, 2, 4 }, levels);
        Assert.Throws<ArgumentException>(() => new ChartXZoomLevels());
        Assert.Throws<ArgumentException>(() => new ChartXZoomLevels(2));
        Assert.Throws<ArgumentException>(() => new ChartXZoomLevels(1, 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartXZoomLevels(1, double.PositiveInfinity));
        var bank = Bare().WithXAxisBounds(0, 10).AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }).PrepareXZoom(Context(), levels);
        Assert.Throws<ArgumentOutOfRangeException>(() => bank.At(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => bank.At(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => bank.At(1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => bank.At(1, -.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => bank.At(1, 1.1));
        var unsupported = Bare().AddLine("Smooth", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) });
        unsupported.Series[0].WithInterpolation(ChartInterpolation.Smooth);
        Assert.Throws<NotSupportedException>(() => unsupported.PrepareXZoom(Context(), levels));
    }

    [Theory]
    [InlineData(false, false, ChartStepPosition.Start)]
    [InlineData(false, true, ChartStepPosition.Start)]
    [InlineData(true, false, ChartStepPosition.Start)]
    [InlineData(true, true, ChartStepPosition.Start)]
    [InlineData(true, false, ChartStepPosition.Middle)]
    [InlineData(true, true, ChartStepPosition.End)]
    public void ContainedPanRetainsNativeCrossingsGapsAndSourceIdentities(bool step, bool reversed, ChartStepPosition position) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var points = new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) };
        if (step) chart.AddStepLine("Observed", points, Blue); else chart.AddLine("Observed", points, Blue);
        chart.Series[0].Markers.Enabled = false;
        if (step) chart.Series[0].WithInterpolation(ChartInterpolation.Step, position);
        chart.Options.XAxis.Reversed = reversed;
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2, 4));
        foreach (var pan in new[] { 0d, .173, .5, .899, 1 }) {
            var prepared = bank.At(1, pan); var projection = prepared.XProjection!;
            Assert.Equal(5 * pan, projection.Minimum, 10); Assert.Equal(5 * pan + 5, projection.Maximum, 10);
            Assert.Equal(reversed, projection.Reversed); Assert.Equal(bank.PlotBounds, projection.PlotBounds);
            Assert.Equal(ChartOrientation.Vertical, prepared.CartesianOrientation);
            Assert.NotNull(prepared.YProjection);
            Assert.Equal(0, prepared.YProjection.Minimum); Assert.Equal(10, prepared.YProjection.Maximum);
            foreach (var sourceIndex in new[] { 0, 1 }) {
                var region = prepared.Regions.Single(item => item.Id == "series-0-point-" + sourceIndex);
                Assert.Equal(projection.Project(points[sourceIndex].X), region.Bounds.Left + region.Bounds.Width / 2, 8);
                Assert.Equal(prepared.YProjection.Project(points[sourceIndex].Y), region.Bounds.Top + region.Bounds.Height / 2, 8);
            }
            var ordinary = chart.Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(projection.Minimum, projection.Maximum)));
            Assert.Equal(ordinary.ToPng(), prepared.ToPng());
            var facts = PointFacts(prepared);
            Assert.Equal(new[] { "0", "1" }, facts.Select(node => (string?)node.Attribute("data-cfx-source-point")));
            Assert.Contains(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "line" && path.Commands.Count >= 2);
        }
        chart.Series[0].Points[1] = new ChartPoint(10, 8, breakBefore: true);
        chart.WithDataLabels();
        var disconnected = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2)).At(1, .5);
        Assert.Equal(2, disconnected.Scene.Nodes.OfType<VisualScenePath>().Single(path => path.Role == "line").Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.DoesNotContain(disconnected.ToRgba().Pixels.Chunk(4), pixel => pixel[2] > pixel[0] + 60 && pixel[2] > pixel[1] + 60);
        Assert.DoesNotContain(disconnected.Scene.Nodes, node => node.Role == "data-label");
        Assert.Equal(2, PointFacts(disconnected).Length);
    }

    [Fact]
    public void FinalBankPlotOwnsAutomaticMarkerPaddingAndFixedPrimaryAndSecondaryY() {
        var chart = Chart.Create().WithLegend(false).WithXAxis("Sample").WithYAxis("Primary")
            .AddLine("Primary", new[] { new ChartPoint(0, -100), new ChartPoint(5, 4), new ChartPoint(10, 100) })
            .AddStepLine("Secondary", new[] { new ChartPoint(0, 1), new ChartPoint(5, 10), new ChartPoint(10, 1000) });
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        chart.Options.XAxis.LabelAngle = 45;
        chart.Options.XAxis.LabelFormatter = value => "Anchored " + value.ToString("G4", System.Globalization.CultureInfo.InvariantCulture);
        foreach (var series in chart.Series) series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 11; markers.StrokeWidth = 2; });
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2, 4));
        var full = bank.At(0); var fullPoints = full.Regions.Where(item => item.Role == "point").ToDictionary(item => item.Id, item => item.Bounds);
        Assert.True(bank.FullViewport.Minimum < 0); Assert.True(bank.FullViewport.Maximum > 10);
        Assert.All(fullPoints.Values, bounds => Assert.True(LabelPlacementService.Contains(bank.PlotBounds, bounds)));
        var primary = AxisLabels(full, "axis-y-label"); var secondary = AxisLabels(full, "axis-secondary-y-label");
        for (var level = 0; level < bank.Levels.Count; level++) {
            var view = bank.At(level, .63);
            Assert.Equal(bank.PlotBounds, view.XProjection!.PlotBounds);
            Assert.Equal(primary, AxisLabels(view, "axis-y-label")); Assert.Equal(secondary, AxisLabels(view, "axis-secondary-y-label"));
            Assert.All(view.Regions.Where(item => item.Role == "point"), point => Assert.Equal(fullPoints[point.Id].Top, point.Bounds.Top));
            Assert.All(view.Scene.Nodes.OfType<VisualSceneEllipse>().Where(item => item.Role == "marker"), marker => Assert.Equal(11, marker.Rx));
            Assert.All(bank.Bank[level].Layers.SelectMany(layer => layer.Scene.Nodes).OfType<VisualSceneText>(), text => Assert.NotNull(text.Text.Face.Font));
        }
        Assert.Null(chart.Options.XAxis.Minimum); Assert.Null(chart.Options.XAxis.Maximum); Assert.Null(chart.Options.YAxis.Minimum);
    }

    [Fact]
    public void NativePreparationIsDetachedAndAtNeverInvokesFormattersOrRetainsTheModel() {
        var chart = Chart.Create().WithLegend(false).WithDataLabels().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 5), new ChartPoint(10, 8) }, Blue);
        var xCalls = new Dictionary<double, int>(); var valueCalls = 0;
        chart.Options.XAxis.LabelFormatter = value => { xCalls[value] = xCalls.GetValueOrDefault(value) + 1; return "X " + value; };
        chart.Options.ValueFormatter = value => { valueCalls++; return "Value " + value; };
        var ordinaryBefore = chart.Prepare(Context()).ToSvg();
        xCalls.Clear(); valueCalls = 0;
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2, 4));
        Assert.All(xCalls.Values, count => Assert.Equal(1, count));
        var capturedValueCalls = valueCalls; var capturedXCalls = xCalls.Count;
        var snapshot = bank.At(1, .5); var svg = snapshot.ToSvg(); var png = snapshot.ToPng();
        chart.ClearSeries(); chart.Options.XAxis.Reversed = true; chart.WithTitle("Changed");
        chart.Options.XAxis.LabelFormatter = _ => throw new InvalidOperationException("Detached bank used the axis callback.");
        chart.Options.ValueFormatter = _ => throw new InvalidOperationException("Detached bank used the data callback.");
        Assert.Equal(svg, bank.At(1, .5).ToSvg()); Assert.Equal(png, bank.At(1, .5).ToPng());
        Assert.Equal(capturedValueCalls, valueCalls); Assert.Equal(capturedXCalls, xCalls.Count);
        Assert.NotEqual(ordinaryBefore, svg);
        Assert.All(bank.Bank, level => {
            Assert.True(level.Statistics.Nodes > 0); Assert.True(level.Statistics.PathCommands > 0);
            Assert.True(level.Statistics.Glyphs > 0); Assert.Equal(level.Ticks.Count, level.Statistics.Ticks);
            Assert.True(level.Statistics.NativeSvgUtf8Bytes > 0);
        });
    }

    [Fact]
    public void SourceCaptionsAndReferenceFactsSurviveWithExplicitOffWindowReasons() {
        var chart = Bare().WithDataLabels().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(1, 5), new ChartPoint(5, 5), new ChartPoint(9, 5) }, Blue)
            .AddVerticalLine(1, "X reference")
            .AddHorizontalLine(2, "Y reference")
            .AddPlotRegion(new(new ChartColor(220, 180, 40), xMinimum: 1, xMaximum: 4, yMinimum: 1, yMaximum: 3, label: "X region", showLabel: true))
            .AddPlotRegion(new(new ChartColor(100, 180, 40), yMinimum: 7, yMaximum: 9, label: "Y region", showLabel: true));
        chart.Series[0].Markers.Enabled = false;
        chart.Series[0].WithPointLabel(0, "Left").WithPointLabel(1, "Middle").WithPointLabel(2, "Right");
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2));
        var view = bank.At(1, .5); var svg = XDocument.Parse(view.ToSvg());
        Assert.Equal(3, PointFacts(view).Length);
        Assert.Equal(2, view.Regions.Count(region => region.Role == "annotation"));
        Assert.Equal(2, view.Regions.Count(region => region.Role == "plot-region"));
        var captions = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "x-zoom-caption").ToArray();
        Assert.Contains(captions, node => (string?)node.Attribute("data-cfx-label") == "Left" && (string?)node.Attribute("data-cfx-label-visibility") == "anchor-outside-window");
        Assert.Contains(captions, node => (string?)node.Attribute("data-cfx-label") == "Right" && (string?)node.Attribute("data-cfx-label-visibility") == "anchor-outside-window");
        Assert.Contains(view.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label" && node.Text.Lines.Any(line => line.Text == "Middle"));
        Assert.Contains(captions, node => (string?)node.Attribute("data-cfx-label") == "X reference" && (string?)node.Attribute("data-cfx-label-visibility") == "anchor-outside-window");
        Assert.Equal(bank.At(1).Regions.Single(region => region.Id == "annotation-1").Bounds, view.Regions.Single(region => region.Id == "annotation-1").Bounds);
        Assert.Equal(bank.At(1).Regions.Single(region => region.Id == "plot-region-1").Bounds, view.Regions.Single(region => region.Id == "plot-region-1").Bounds);
    }

    [Fact]
    public void FixedReferenceCaptionUsesCoreIntervalsToAvoidMovingNativeInk() {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 8), new ChartPoint(10, 2) }, Blue)
            .AddHorizontalLine(5, "Fixed reference");
        chart.Series[0].Markers.Enabled = false;
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2));
        var level = bank.Bank[1];
        var caption = level.Layers.Select(layer => layer.Caption).Single(item => item?.Id == "annotation-0-label")!;
        Assert.Null(caption.FittingReason);
        var intervals = caption.Hidden.Where(item => item.Reason.StartsWith("moving-mark-clearance:", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(intervals);
        var interval = intervals[0];
        var pan = -(interval.Minimum + interval.Maximum) / 2 / level.Travel;
        var blocked = bank.At(1, pan);
        Assert.DoesNotContain(blocked.Scene.Nodes, node => node.Role == "annotation-label");
        Assert.Contains(blocked.Diagnostics, diagnostic => diagnostic.Code == "cartesian.x-zoom-caption-hidden" && diagnostic.Message.Contains("moving-mark-clearance:", StringComparison.Ordinal));
        Assert.Single(blocked.Regions, region => region.Role == "annotation");
        Assert.Contains(new[] { bank.At(1, 0), bank.At(1, 1) }, view => view.Scene.Nodes.Any(node => node.Role == "annotation-label"));
    }

    [Fact]
    public void MagnificationPreservesMarkerContoursPointPaintHatchesAndGuideDashes() {
        var green = new ChartColor(20, 160, 60); var red = new ChartColor(180, 40, 20);
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddStepLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 5), new ChartPoint(10, 8) }, Blue)
            .AddVerticalLine(5, "Reference");
        chart.Series[0].ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 6; markers.Shape = ChartMarkerShape.Diamond; markers.Stroke = red; markers.StrokeWidth = 3; });
        chart.Series[0].WithPointColor(1, green).WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2, 4));
        foreach (var level in bank.Bank) {
            var nodes = level.Layers.SelectMany(layer => layer.Scene.Nodes).ToArray();
            var markers = nodes.OfType<VisualScenePath>().Where(node => node.Role == "marker").ToArray();
            Assert.Equal(3, markers.Length); Assert.Equal(new[] { Blue, green, Blue }, markers.Select(marker => marker.Fill!.Value));
            Assert.All(markers, marker => { Assert.Equal(red, marker.Stroke); Assert.Equal(3, marker.StrokeWidth);
                Assert.Equal(12, marker.Commands.Max(command => command.X) - marker.Commands.Min(command => command.X), 9); });
            Assert.Contains(nodes, node => node.Role == "marker-pattern");
            var guide = Assert.Single(nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
            Assert.Equal(VisualStrokeCap.Butt, guide.Cap); Assert.NotNull(guide.Dash);
            Assert.Equal(bank.Bank[0].Layers.SelectMany(layer => layer.Scene.Nodes).OfType<VisualScenePath>().Single(node => node.Role == "annotation-line").Dash, guide.Dash);
        }
        var middle = bank.At(1, .5);
        Assert.Contains(middle.ToRgba().Pixels.Chunk(4), pixel => pixel[1] > pixel[0] + 60 && pixel[1] > pixel[2] + 40);
    }

    [Fact]
    public void ClearanceIntervalsDoNotInventInkAcrossDisconnectedContours() {
        var shape = new LabelMarkShape(new[] { new List<ChartPoint> { new(0, 0), new(1, 0) }, new List<ChartPoint> { new(9, 0), new(10, 0) } }, false, .2);
        var intervals = shape.HorizontalTranslations(new ChartRect(4, -.2, 2, .4), "native-ink").ToArray();
        Assert.Equal(2, intervals.Length);
        Assert.DoesNotContain(intervals, interval => interval.Minimum <= 0 && interval.Maximum >= 0);
        Assert.Contains(intervals, interval => interval.Minimum < -4 && interval.Maximum > -4);
    }

    [Fact]
    public void ExtremeFiniteDomainsUseStableContainedWindowsAndUnrepresentableLevelsFail() {
        var chart = Bare().WithXAxisBounds(-1e308, 1e308).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(-1e308, 2), new ChartPoint(1e308, 8) });
        chart.Series[0].Markers.Enabled = false;
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2));
        var middle = bank.At(1, .5).XProjection!;
        Assert.Equal(-5e307, middle.Minimum); Assert.Equal(5e307, middle.Maximum);
        Assert.Equal(bank.PlotBounds.Left + bank.PlotBounds.Width / 2, middle.Project(0), 9);
        var narrow = Bare().WithXAxisBounds(1e15, 1e15 + 1).AddLine("Narrow", new[] { new ChartPoint(1e15, 2), new ChartPoint(1e15 + 1, 8) });
        narrow.Series[0].Markers.Enabled = false;
        Assert.Throws<ArgumentOutOfRangeException>(() => narrow.PrepareXZoom(Context(), new ChartXZoomLevels(1, 100)));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void NonpresetPanRejectsOffWindowSourceCaptionsEvenWhenTheirPaintWouldFit(bool step, bool reversed, bool markers) {
        const double pan = .173;
        var minimum = pan * 5; var maximum = minimum + 5;
        var chart = Bare().WithDataLabels().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var observations = new[] { (X: minimum - .005, Y: 2d, Label: "Outside lower"),
            (X: minimum, Y: 4d, Label: "Boundary lower"), (X: maximum + .005, Y: 6d, Label: "Outside upper"),
            (X: maximum, Y: 8d, Label: "Boundary upper") };
        for (var i = 0; i < observations.Length; i++) {
            var point = observations[i];
            if (step) chart.AddStepLine(point.Label, new[] { new ChartPoint(point.X, point.Y) });
            else chart.AddLine(point.Label, new[] { new ChartPoint(point.X, point.Y) });
            var series = chart.Series[i];
            series.WithPointLabel(0, point.Label);
            series.Markers.Enabled = markers;
            var right = (i < 2) != reversed;
            series.DataLabelPlacement = right ? ChartDataLabelPlacement.Right : ChartDataLabelPlacement.Left;
        }
        chart.Options.XAxis.Reversed = reversed;
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2));
        var view = bank.At(1, pan);
        Assert.Equal(minimum, view.XProjection!.Minimum); Assert.Equal(maximum, view.XProjection.Maximum);
        Assert.Equal(4, PointFacts(view).Length);
        var captions = XDocument.Parse(view.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "x-zoom-caption").ToArray();
        for (var i = 0; i < observations.Length; i++) {
            var caption = Assert.Single(captions, node => (string?)node.Attribute("data-cfx-label") == observations[i].Label);
            Assert.Equal("series-" + i + "-point-0", (string?)caption.Attribute("data-cfx-label-for"));
        }
        foreach (var text in new[] { "Outside lower", "Outside upper" }) {
            Assert.Contains(captions, node => (string?)node.Attribute("data-cfx-label") == text
                && (string?)node.Attribute("data-cfx-label-visibility") == "anchor-outside-window");
            Assert.DoesNotContain(view.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label" && node.Text.Lines.Any(line => line.Text == text));
        }
        foreach (var text in new[] { "Boundary lower", "Boundary upper" })
            Assert.Contains(view.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label" && node.Text.Lines.Any(line => line.Text == text));
    }

    private static Chart Bare() => Chart.Create().WithAxes(false).WithGrid(false).WithLegend(false);
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(420, 280)), VisualTheme.Graphite(),
        frame: new VisualFrame(showLegend: false), font: FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));
    private static XElement[] PointFacts(PreparedVisual visual) => XDocument.Parse(visual.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
    private static string?[] AxisLabels(PreparedVisual visual, string role) => visual.Regions.Where(region => region.Role == role).Select(region => region.Label).ToArray();
}
