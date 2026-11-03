using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartXZoomSourceAdmissionTests {
    private static readonly ChartColor Blue = new(20, 60, 220);

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void SourceAdmissionRejectsOutsideMarkerInkAndCaptionsButKeepsInclusiveBoundaries(bool step, bool reversed, bool ulp) {
        var chart = Bare().WithDataLabels();
        chart.Options.XAxis.Reversed = reversed;
        var xs = new[] { ulp ? Math.BitDecrement(3) : 2.99, 3, 7, ulp ? Math.BitIncrement(7) : 7.01 };
        var labels = new[] { "Outside lower", "L", "R", "Outside upper" };
        for (var i = 0; i < xs.Length; i++) {
            if (step) chart.AddStepLine(labels[i], new[] { new ChartPoint(xs[i], 2 + i * 2, true) }, Blue);
            else chart.AddLine(labels[i], new[] { new ChartPoint(xs[i], 2 + i * 2, true) }, Blue);
            chart.Series[i].ConfigureMarkers(marker => { marker.Enabled = true; marker.Radius = 4; });
            chart.Series[i].WithPointLabel(0, labels[i]);
            chart.Series[i].DataLabelPlacement = (i < 2) != reversed ? ChartDataLabelPlacement.Right : ChartDataLabelPlacement.Left;
        }
        var context = Context();
        var bank = chart.PrepareXZoom(context, new ChartXZoomLevels(1, 2.5));
        var view = bank.At(1, .5);
        Assert.Equal(3, view.XProjection!.Minimum); Assert.Equal(7, view.XProjection.Maximum);
        Assert.Equal(4, view.Regions.Count(region => region.Role == "point"));
        Assert.Equal(Enumerable.Range(0, 4).Select(i => "series-" + i + "-point-0"), view.Regions.Where(region => region.Role == "point").Select(region => region.Id));
        var ordinary = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(3, 7)));
        Assert.Equal(2, ordinary.Scene.Nodes.Count(node => node.Role == "marker"));
        Assert.Equal(2, view.Scene.Nodes.Count(node => node.Role == "marker"));
        Assert.Equal(new[] { "L", "R" }, view.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label")
            .Select(node => node.Text.Lines.Single().Text));
        foreach (var text in new[] { labels[0], labels[3] })
            Assert.Contains(Facts(view, "x-zoom-caption"), node => (string?)node.Attribute("data-cfx-label") == text
                && (string?)node.Attribute("data-cfx-label-visibility") == "anchor-outside-window");
        var pixels = view.ToPng();
        foreach (var i in new[] { 0, 3 }) { chart.Series[i].Markers.Enabled = false; chart.Series[i].WithPointLabel(0, ""); }
        Assert.Equal(pixels, chart.PrepareXZoom(context, new ChartXZoomLevels(1, 2.5)).At(1, .5).ToPng());
        chart.WithDataLabels(false);
        Assert.Equal(chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(3, 7))).ToPng(),
            chart.PrepareXZoom(context, new ChartXZoomLevels(1, 2.5)).At(1, .5).ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaptionUsesSourceAnchorWhenOutsideValueRoundsToVisiblePixels(bool reversed) {
        var chart = Bare().WithDataLabels().AddLine("Observed", new[] { new ChartPoint(Math.BitDecrement(5 * .173), 5) }, Blue);
        chart.Options.XAxis.Reversed = reversed; chart.Series[0].Markers.Enabled = false;
        chart.Series[0].WithPointLabel(0, "Outside");
        chart.Series[0].DataLabelPlacement = reversed ? ChartDataLabelPlacement.Left : ChartDataLabelPlacement.Right;
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2)); var view = bank.At(1, .173);
        Assert.Single(Facts(view, "point"));
        Assert.DoesNotContain(view.Scene.Nodes, node => node.Role == "data-label");
        Assert.Contains(Facts(view, "x-zoom-caption"), node => (string?)node.Attribute("data-cfx-label") == "Outside"
            && (string?)node.Attribute("data-cfx-label-visibility") == "anchor-outside-window");
        Assert.Contains(bank.At(0).Scene.Nodes, node => node.Role == "data-label");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void XTickGridAndCaptionKeepRawSourceEligibilityAndFullTickFacts(bool reversed) {
        var chart = Bare().WithAxes().WithGrid().AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }, Blue);
        chart.ConfigureGridStyle(grid => { grid.ShowVerticalLines = true; grid.ShowHorizontalLines = false; });
        chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Visible = false; chart.Series[0].Markers.Enabled = false;
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(Math.BitDecrement(5 * .173), "Outside"));
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2)); var view = bank.At(1, .173);
        Assert.Single(view.Regions, region => region.Role == "axis-x-label");
        Assert.DoesNotContain(view.Scene.Nodes, node => node.Role is "axis-x-label" or "grid-x");
        Assert.Contains(Facts(view, "x-zoom-caption"), node => (string?)node.Attribute("data-cfx-label") == "Outside"
            && (string?)node.Attribute("data-cfx-label-visibility") == "anchor-outside-window");
        Assert.Single(bank.At(0).Scene.Nodes, node => node.Role == "grid-x");
        Assert.Single(bank.At(0).Scene.Nodes, node => node.Role == "axis-x-label");
    }

    [Fact]
    public void SuppressedOutsideMarkerCannotHideAFixedCaptionThroughItsClearanceIntervals() {
        var chart = Bare().AddLine("Observed", new[] { new ChartPoint(2.99, 5) }, Blue).AddHorizontalLine(5, "Fixed");
        chart.Series[0].ConfigureMarkers(marker => { marker.Enabled = true; marker.Radius = 20; });
        var view = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2.5)).At(1, .5);
        Assert.Single(view.Regions, region => region.Role == "point");
        Assert.Single(view.Scene.Nodes, node => node.Role == "annotation-label");
        var png = view.ToPng(); chart.Series[0].Markers.Enabled = false;
        Assert.Equal(png, chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2.5)).At(1, .5).ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonpresetPanMatchesOrdinarySourceMarkerAdmissionAndRetainsCrossingsGapsAndCallbacks(bool reversed) {
        const double pan = .173;
        var minimum = 5 * pan; var maximum = minimum + 5;
        var chart = Bare().WithDataLabels(); chart.Options.XAxis.Reversed = reversed;
        chart.AddStepLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8),
            new ChartPoint(minimum - .005, 3, true), new ChartPoint(maximum + .005, 7, true),
            new ChartPoint(minimum, 4, true), new ChartPoint(maximum, 6, true) }, Blue);
        chart.Series[0].ConfigureMarkers(marker => { marker.Enabled = true; marker.Radius = 4; });
        for (var i = 0; i < 6; i++) chart.Series[0].WithPointLabel(i, "");
        var calls = 0; chart.Options.ValueFormatter = value => { calls++; return value.ToString(); };
        chart.Options.XAxis.LabelFormatter = value => { calls++; return value.ToString(); };
        var context = Context(); var bank = chart.PrepareXZoom(context, new ChartXZoomLevels(1, 2)); var after = calls;
        var view = bank.At(1, pan);
        Assert.Equal(6, Facts(view, "point").Length);
        Assert.Equal(new[] { "0", "1", "2", "3", "4", "5" }, Facts(view, "point").Select(node => (string?)node.Attribute("data-cfx-source-point")));
        Assert.Equal(5, view.Scene.Nodes.OfType<VisualScenePath>().Single(path => path.Role == "line").Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Equal(2, view.Scene.Nodes.Count(node => node.Role == "marker"));
        var png = view.ToPng(); Assert.Equal(view.ToSvg(), bank.At(1, pan).ToSvg()); Assert.Equal(png, bank.At(1, pan).ToPng());
        Assert.Equal(after, calls);
        var ordinary = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(minimum, maximum)));
        Assert.Equal(ordinary.Scene.Nodes.Count(node => node.Role == "marker"), view.Scene.Nodes.Count(node => node.Role == "marker"));
        Assert.Equal(ordinary.ToPng(), png);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawReferenceLineAndBandAdmissionKeepsFactsAndRejectsOutsideAndTouchingPaint(bool reversed) {
        var chart = Bare(); chart.Options.XAxis.Reversed = reversed;
        chart.AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }, Blue); chart.Series[0].Markers.Enabled = false;
        var context = Context(); var control = chart.PrepareXZoom(context, new ChartXZoomLevels(1, 2.5)).At(1, .5).ToPng();
        chart.AddVerticalLine(Math.BitDecrement(3), "Outside lower").AddVerticalLine(Math.BitIncrement(7), "Outside upper")
            .AddVerticalBand(1, 3, "Touch lower").AddVerticalBand(7, 9, "Touch upper");
        var view = chart.PrepareXZoom(context, new ChartXZoomLevels(1, 2.5)).At(1, .5);
        Assert.Equal(4, Facts(view, "annotation").Length); Assert.Equal(4, view.Regions.Count(region => region.Role == "annotation"));
        Assert.DoesNotContain(view.Scene.Nodes, node => node.Role is "annotation-line" or "annotation-band" or "annotation-label" or "annotation-label-backplate");
        Assert.Equal(control, view.ToPng());
        foreach (var annotation in chart.Annotations.ToArray()) chart.RemoveAnnotation(annotation);
        chart.AddVerticalLine(3).AddVerticalLine(7).AddVerticalBand(1, 4).AddVerticalBand(6, 9);
        view = chart.PrepareXZoom(context, new ChartXZoomLevels(1, 2.5)).At(1, .5);
        Assert.Equal(2, view.Scene.Nodes.Count(node => node.Role == "annotation-line"));
        Assert.Equal(2, view.Scene.Nodes.Count(node => node.Role == "annotation-band"));
        Assert.Equal(chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(3, 7))).ToPng(), view.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PartialXRegionsKeepTheirNativeIntersectionAndAnchoredCaptionWhileTouchingRegionsKeepOnlyFacts(bool reversed) {
        var chart = Bare().AddLine("Observed", new[] { new ChartPoint(0, 5), new ChartPoint(10, 5) }, Blue);
        chart.Series[0].Markers.Enabled = false; chart.Options.XAxis.Reversed = reversed;
        chart.AddPlotRegion(new(Blue, xMinimum: 1, xMaximum: 3, yMinimum: 1, yMaximum: 3, label: "Touch", opacity: 1))
            .AddPlotRegion(new(new ChartColor(20, 150, 60), xMinimum: 2, xMaximum: 6, yMinimum: 1, yMaximum: 3, label: "Partial", opacity: 1))
            .AddPlotRegion(new(new ChartColor(190, 150, 20), yMinimum: 7, yMaximum: 9, label: "Fixed", opacity: 1));
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2.5)); var view = bank.At(1, .5);
        Assert.Equal(3, Facts(view, "plot-region").Length);
        Assert.Equal(2, view.Scene.Nodes.Count(node => node.Role == "plot-region-fill"));
        var partial = view.Regions.Single(region => region.Id == "plot-region-1");
        Assert.Equal(Math.Min(view.XProjection!.Project(3), view.XProjection.Project(6)), partial.Bounds.Left, 8);
        Assert.Equal(Math.Abs(view.XProjection.Project(6) - view.XProjection.Project(3)), partial.Bounds.Width, 8);
        Assert.Contains(view.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "plot-region-label" && node.Text.Lines.Single().Text == "Partial");
        Assert.Contains(Facts(view, "x-zoom-caption"), node => (string?)node.Attribute("data-cfx-label") == "Touch"
            && (string?)node.Attribute("data-cfx-label-visibility") == "range-outside-window");
        Assert.Equal(bank.At(1).Regions.Single(region => region.Id == "plot-region-2").Bounds,
            view.Regions.Single(region => region.Id == "plot-region-2").Bounds);
        var png = view.ToPng(); chart.PlotRegions.RemoveAt(0);
        Assert.Equal(png, chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2.5)).At(1, .5).ToPng());
    }

    private static Chart Bare() => Chart.Create().WithAxes(false).WithGrid(false).WithLegend(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(420, 280)), VisualTheme.Graphite(),
        frame: new VisualFrame(showLegend: false), font: FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));
    private static XElement[] Facts(PreparedVisual visual, string role) => XDocument.Parse(visual.ToSvg()).Descendants()
        .Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
}
