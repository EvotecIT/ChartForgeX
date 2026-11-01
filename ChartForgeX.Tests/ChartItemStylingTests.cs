using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartItemStylingTests {
    [Fact]
    public void GeneralItemStrokeFitsAutomaticMarkerBoundsAndAppliesOpacityOnce() {
        var ink = ChartColor.FromArgb(128, 220, 20, 80);
        var series = ChartSeries.CreateScatter(new ChartSeriesId("samples"), "Samples", new[] {
            new ChartPoint(1, 10), new ChartPoint(3, 30)
        }).WithSemanticRole("sample-glyph").WithItemStyler<ChartPoint>(item => item.Index == 1
            ? new ChartItemStyle(stroke: new ChartStrokeStyle(width: 40, opacity: .5, dash: new[] { 6d, 3d })) : null);
        series.ConfigureMarkers(options => { options.Radius = 4; options.Stroke = ink; });
        var chart = Chart.Create().WithSize(440, 320).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithAxes(false).WithGrid(false).AddSeries(series);
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true;
        var prepared = Prepare(chart);
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "sample-glyph");
        Assert.Equal(40, mark.StrokeWidth);
        Assert.Equal(ink.WithAlpha(64), mark.Stroke);
        Assert.Equal(new[] { 6d, 3d }, mark.Dash);
        var contour = new ChartPath(mark.Commands).Bounds();
        var painted = new ChartRect(contour.X - 20, contour.Y - 20, contour.Width + 40, contour.Height + 40);
        var plot = prepared.XProjection!.PlotBounds;
        Assert.True(LabelPlacementService.Contains(new ChartRect(plot.X - 1e-6, plot.Y - 1e-6, plot.Width + 2e-6, plot.Height + 2e-6), painted));
        Assert.Contains(prepared.Regions, region => region.Role == "point" && LabelPlacementService.Contains(region.Bounds, painted));
        var svg = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element =>
            (string?)element.Attribute("data-cfx-role") == "sample-glyph" && (string?)element.Attribute("stroke-width") == "40");
        Assert.Equal(ink.WithAlpha(64).ToCss(), (string?)svg.Attribute("stroke"));
        Assert.True(ContainsColor(prepared.ToPng(), ink.WithAlpha(64)), "Native PNG must retain the once-multiplied outline alpha.");
    }

    [Fact]
    public void DatumCallbackOverridesOrdinalPaintOnceAndExportsDetachedSvgAndNativePixels() {
        var calls = new List<(double Value, string Series, int Index, bool Highlight)>();
        var pink = ChartColor.FromHex("#F20B83");
        var series = ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders", new[] {
            new ChartPoint(2, 4), new ChartPoint(6, 8).WithMarkOptions(new ChartMarkOptions(selected: true))
        }).WithColor(ChartColor.Black).WithPointColor(1, ChartColor.White)
            .WithItemStyler<ChartPoint>(context => {
                calls.Add((context.Datum.Y, context.Series.Id.Value, context.Index, context.IsHighlighted));
                return context.Datum.Y >= 8 ? new ChartItemStyle(color: pink,
                    stroke: new ChartStrokeStyle(ChartColor.Black, 3, dash: new[] { 5d, 2d })) : null;
            });
        var chart = Bare().AddSeries(series);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal(new[] { (4d, "orders", 0, false), (8d, "orders", 1, true) }, calls);
        var target = XDocument.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-point") == "1")
            .SelectMany(node => node.Descendants()).First(node => (string?)node.Attribute("fill") == "#F20B83");
        Assert.Equal("5 2", (string?)target.Attribute("stroke-dasharray"));
        Assert.Equal("3", (string?)target.Attribute("stroke-width"));
        Assert.True(ContainsColor(png, pink), "The callback's fill must reach actual native PNG pixels.");
        series.WithItemStyler<ChartPoint>(_ => throw new InvalidOperationException("changed"));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(2, calls.Count);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Equal(ChartColor.White, series.PointColors[1]); Assert.Equal(ChartColor.Black, series.Color);
    }

    [Fact]
    public void TypedBubbleContextRetainsSizeAndMarkerOverridesAreImmutable() {
        var markers = new ChartMarkerOptions { Shape = ChartMarkerShape.Diamond, Stroke = ChartColor.Black, StrokeWidth = 2 };
        var style = new ChartItemStyle(color: ChartColor.FromHex("#0BBC84"), markers: markers);
        markers.Shape = ChartMarkerShape.Circle;
        style.Markers!.Shape = ChartMarkerShape.Square;
        var series = ChartSeries.CreateBubble(new ChartSeriesId("batches"), "Batches", new[] { new ChartBubble(5, 5, 42) })
            .WithItemStyler<ChartBubble>(context => {
                Assert.Equal(42, context.Datum.Size); Assert.Equal(0, context.Index);
                Assert.Equal(ChartSeriesKind.Bubble, context.Series.Kind); return style;
            });
        var prepared = Prepare(Bare().AddSeries(series));
        var marker = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "bubble");
        Assert.Equal(4, marker.Commands.Count);
        Assert.True(ContainsColor(prepared.ToPng(), ChartColor.FromHex("#0BBC84")));
        Assert.Equal(ChartMarkerShape.Circle, series.Markers.Shape);
    }

    [Fact]
    public void HierarchyStylerReceivesAuthoredItemAndRejectsTheNumericProjectionType() {
        var seen = new List<string>();
        var series = ChartSeries.CreateTreemap(new ChartSeriesId("work"), "Work", new[] {
            new ChartHierarchyItem("a", "Operations", value: 4), new ChartHierarchyItem("b", "Delivery", value: 6)
        }).WithItemStyler<ChartHierarchyItem>(context => {
            seen.Add(context.Datum.Id);
            return new ChartItemStyle(color: context.Datum.Id == "b" ? ChartColor.FromHex("#F20B83") : ChartColor.Black);
        });
        Assert.Throws<ArgumentException>(() => series.WithItemStyler<ChartPoint>(_ => null));
        var prepared = Prepare(Bare().AddSeries(series));
        Assert.Equal(new[] { "a", "b" }, seen);
        Assert.True(ContainsColor(prepared.ToPng(), ChartColor.FromHex("#F20B83")));
    }

    [Fact]
    public void ClearedStylerRestoresOrdinalSugarAndExceptionDoesNotInstallResolvedPaint() {
        var series = ChartSeries.CreateBar(new ChartSeriesId("bars"), "Bars", new[] { new ChartPoint(2, 4), new ChartPoint(6, 8) })
            .WithPointColor(0, ChartColor.Black);
        var chart = Bare().AddSeries(series); var before = chart.ToSvg();
        series.WithItemStyler<ChartPoint>(context => context.Index == 0 ? new ChartItemStyle(color: ChartColor.White)
            : throw new InvalidOperationException("formatter failed"));
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Equal(ChartColor.Black, series.PointColors[0]);
        series.WithItemStyler<ChartPoint>(null);
        Assert.Equal(before, chart.ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    public void TypedNodeAndRelationshipStylersCoexistWithoutBorrowingEachOthersOrdinals(ChartSeriesKind kind) {
        var nodes = new[] { new ChartNode("a", "Alpha"), new ChartNode("b", "Beta") };
        var id = new ChartSeriesId("relationships");
        var series = kind == ChartSeriesKind.Tree ? ChartSeries.CreateTree(id, "Relationships", nodes, new[] { new ChartTreeLink("a", "b") })
            : kind == ChartSeriesKind.Sankey ? ChartSeries.CreateSankey(id, "Relationships", nodes, new[] { new ChartFlowLink("ab", "a", "b", 4) })
            : ChartSeries.CreateChord(id, "Relationships", nodes, new[] { new ChartFlowLink("ab", "a", "b", 4) });
        var nodeCalls = 0; var linkCalls = 0; var pink = ChartColor.FromHex("#F20B83");
        series.WithNodeState("a", ChartSeriesState.Warning);
        series.WithItemStyler<ChartNode>(item => {
            nodeCalls++; Assert.Equal(item.Index == 0 ? ChartSeriesState.Warning : ChartSeriesState.None, item.State);
            return new ChartItemStyle(color: ChartColor.Black);
        });
        ChartItemStyle LinkStyle() { linkCalls++; return new ChartItemStyle(color: pink, stroke: new ChartStrokeStyle(pink, 3, dash: new[] { 5d, 3d })); }
        if (kind == ChartSeriesKind.Tree)
            series.WithItemStyler<ChartTreeLink>(item => { Assert.Equal("b", item.Datum.ChildId); return LinkStyle(); });
        else series.WithItemStyler<ChartFlowLink>(item => { Assert.Equal("ab", item.Datum.Id); Assert.Equal(4, item.Datum.Value); return LinkStyle(); });
        var prepared = Prepare(Bare().AddSeries(series));
        Assert.Equal(2, nodeCalls); Assert.Equal(1, linkCalls);
        var linkRole = kind == ChartSeriesKind.Tree ? "tree-link-path" : kind == ChartSeriesKind.Sankey ? "sankey-ribbon" : "chord-ribbon";
        var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == linkRole);
        Assert.Equal(pink, path.Stroke); Assert.Equal(3, path.StrokeWidth); Assert.Equal(new[] { 5d, 3d }, path.Dash);
        Assert.True(ContainsColor(prepared.ToPng(), pink));
        Assert.Equal(2, nodeCalls); Assert.Equal(1, linkCalls);
        Assert.Empty(series.PointColors);
    }

    private static bool ContainsColor(byte[] png, ChartColor color) {
        var pixels = RasterImageDecoder.Decode(png).Pixels;
        for (var offset = 0; offset < pixels.Length; offset += 4)
            if (pixels[offset] == color.R && pixels[offset + 1] == color.G && pixels[offset + 2] == color.B && pixels[offset + 3] == color.A) return true;
        return false;
    }

    [Fact]
    public void NativeReflowRetainsTypedItemPaintAndHiddenStyleChangesAlterSourceIdentity() {
        var id = new ChartSeriesId("readings"); var calls = 0; var hiddenWidth = 2d;
        var series = ChartSeries.CreateLine(id, "Readings", new[] {
            new ChartPoint(2, 4), new ChartPoint(5, 6), new ChartPoint(8, 5)
        }).WithObservationIds(new[] { new ChartObservationId("a"), new ChartObservationId("b"), new ChartObservationId("c") })
            .WithItemStyler<ChartPoint>(item => {
                calls++;
                return new ChartItemStyle(stroke: new ChartStrokeStyle(ChartColor.Black, item.Index == 0 ? hiddenWidth : 4,
                    dash: new[] { 5d, 2d }), markers: new ChartMarkerOptions { Shape = ChartMarkerShape.Square, Radius = 8, Enabled = true });
            });
        var chart = Bare().AddSeries(series);
        var view = new ChartVisibilityView(new[] { ChartVisibilityTarget.Observation(id, new ChartObservationId("a")) }, ChartVisibilityLayout.Reflow);
        var context = VisualExportRequest.ForChart(chart).Context;
        var first = chart.PrepareVisibility(context, view);
        Assert.Equal(3, calls);
        var markers = first.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "marker").ToArray();
        Assert.Equal(2, markers.Length);
        Assert.All(markers, mark => { Assert.Equal(4, mark.StrokeWidth); Assert.Equal(new[] { 5d, 2d }, mark.Dash); });
        hiddenWidth = 6;
        var second = chart.PrepareVisibility(context, view);
        Assert.Equal(6, calls);
        Assert.NotEqual(first.Scene.VisibilityCapture!.SourceKey, second.Scene.VisibilityCapture!.SourceKey);
        series.WithItemStyler<ChartPoint>(_ => throw new InvalidOperationException("Must not be retained"));
        var retained = first.Scene.VisibilityCapture.ReflowSource!.CreateNativeView(view).Prepare(context);
        Assert.Equal(6, calls);
        Assert.NotEmpty(retained.ToPng());
    }
    private static Chart Bare() {
        var chart = Chart.Create().WithSize(440, 320).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithAxes(false).WithGrid(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true;
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
