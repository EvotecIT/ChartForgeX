using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The immutable shared outline policy reaches native Sankey and Chord paint without changing weighted geometry.</summary>
public sealed class FlowDashTests {
    [Theory]
    [InlineData(ChartSeriesKind.Sankey)] [InlineData(ChartSeriesKind.Chord)]
    public void OutlineDashPhaseOpacityAndSnapshotsUseOneNativePolicyInBothFlowFamilies(ChartSeriesKind kind) {
        var chart = Bare(kind); var original = SankeyFlowColorTests.Prepare(chart); var series = chart.Series[0]; var dash = new[] { 5d, 3d };
        series.WithFlowStyle("small", new ChartFlowStyle(stroke: ChartColor.FromRgb(255, 0, 0), strokeWidth: 1,
            outlineStroke: new ChartStrokeStyle(ChartColor.Black, 4, .5, dash), strokeDashOffset: -2));
        dash[0] = 100; var prepared = SankeyFlowColorTests.Prepare(chart);
        var ribbon = Ribbons(prepared, kind)[1];
        Assert.Equal(new[] { 5d, 3d }, ribbon.Dash); Assert.Equal(-2, ribbon.DashOffset); Assert.Equal(4, ribbon.StrokeWidth);
        Assert.Equal(ChartColor.FromRgba(0, 0, 0, 128), ribbon.Stroke); Assert.Equal(Ribbons(original, kind)[1].Commands, ribbon.Commands);
        Assert.Null(Ribbons(prepared, kind)[0].Stroke); Assert.Null(Ribbons(prepared, kind)[0].Dash);
        var element = XDocument.Parse(prepared.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-target-id") == "small")
            .Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == Role(kind));
        Assert.Equal("5 3", (string?)element.Attribute("stroke-dasharray")); Assert.Equal("-2", (string?)element.Attribute("stroke-dashoffset"));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.WithFlowStyle("small", new ChartFlowStyle(outlineStroke: new ChartStrokeStyle(width: 0)));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Null(Ribbons(SankeyFlowColorTests.Prepare(chart), kind)[1].Stroke);
        ScatterBubbleColorRenderingTests.Capture(prepared, "flow-dash-" + kind.ToString().ToLowerInvariant());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)] [InlineData(ChartSeriesKind.Chord)]
    public void NativeRasterPreservesActualDashGapsAndDefaultStylePreservesLegacyOutput(ChartSeriesKind kind) {
        var chart = Bare(kind); var original = SankeyFlowColorTests.Prepare(chart);
        chart.Series[0].WithFlowStyle("small", default(ChartFlowStyle));
        Assert.Equal(original.ToSvg(), SankeyFlowColorTests.Prepare(chart).ToSvg());
        Assert.Equal(original.ToPng(), SankeyFlowColorTests.Prepare(chart).ToPng());
        chart.Series[0].WithFlowStyle("small", new ChartFlowStyle(outlineStroke: new ChartStrokeStyle(ChartColor.Black, 4, dash: new[] { 8d, 8d })));
        var dashed = Ink(SankeyFlowColorTests.Prepare(chart).ToPng(new VisualRenderOptions(supersampling: 1)));
        chart.Series[0].WithFlowStyle("small", new ChartFlowStyle(outlineStroke: new ChartStrokeStyle(ChartColor.Black, 4)));
        var solid = Ink(SankeyFlowColorTests.Prepare(chart).ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.True(dashed > 0 && dashed < solid * .85, "Both native exporters must preserve real gaps in the ribbon outline.");
    }

    [Theory]
    [InlineData(double.NaN)] [InlineData(double.NegativeInfinity)] [InlineData(double.PositiveInfinity)]
    public void NonFiniteDashPhaseIsRejectedAtThePublicBoundary(double invalid) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowStyle(strokeDashOffset: invalid));

    private static Chart Bare(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(600, 400).WithHeader(false).WithLegend(false).WithDataLabels(false);
        var nodes = new[] { new ChartNode("r", "Received"), new ChartNode("a", "Accepted") };
        var flows = new[] { new ChartFlowLink("large", "r", "a", 6), new ChartFlowLink("small", "r", "a", 4) };
        if (kind == ChartSeriesKind.Sankey) chart.AddSankey("Requests", nodes, flows); else chart.AddChord("Requests", nodes, flows);
        chart.Series[0].Color = ChartColor.Transparent;
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true;
        return chart;
    }
    private static double Ink(byte[] png) => RasterImageDecoder.Decode(png).Pixels.Where((_, index) => index % 4 == 3).Sum(alpha => (double)alpha);
    private static string Role(ChartSeriesKind kind) => kind == ChartSeriesKind.Sankey ? "sankey-ribbon" : "chord-ribbon";
    private static VisualScenePath[] Ribbons(PreparedVisual prepared, ChartSeriesKind kind) => prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == Role(kind)).ToArray();
}
