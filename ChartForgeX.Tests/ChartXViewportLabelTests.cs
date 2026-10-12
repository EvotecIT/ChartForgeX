using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartXViewportLabelTests {
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void OffWindowLabelsCannotMoveIntoThePlotButInclusiveBoundaryLabelsRemain(bool step, bool reversed, bool markers) {
        var chart = Chart.Create().WithSize(420, 280).WithAxes(false).WithGrid(false).WithLegend(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).WithDataLabels();
        var points = new[] {
            new ChartPoint(2.99, 2), new ChartPoint(3, 4, true), new ChartPoint(5, 5, true),
            new ChartPoint(7, 6, true), new ChartPoint(7.01, 8, true)
        };
        if (step) chart.AddStepLine("Observed", points); else chart.AddLine("Observed", points);
        var series = chart.Series[0];
        series.ConfigureMarkers(options => { options.Enabled = markers; options.Radius = 4; });
        var captions = new[] { "HiddenLeft", "L", "M", "R", "HiddenRight" };
        for (var index = 0; index < captions.Length; index++) series.WithPointLabel(index, captions[index]);
        chart.Options.XAxis.Reversed = reversed;
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context, new ChartXViewport(3, 7));
        var painted = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label")
            .Select(node => node.Text.Lines.Single().Text).ToArray();
        Assert.Equal(new[] { "L", "M", "R" }, painted);

        var facts = XDocument.Parse(prepared.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "0", "1", "2", "3", "4" }, facts.Select(node => (string?)node.Attribute("data-cfx-source-point")));
        Assert.Equal(captions, facts.Select(node => (string?)node.Attribute("data-cfx-label")));
        Assert.Equal(5, prepared.Regions.Count(region => region.Role == "point"));

        var raster = prepared.ToPng();
        series.WithPointLabel(0, "").WithPointLabel(4, "");
        Assert.Equal(raster, chart.Prepare(context, new ChartXViewport(3, 7)).ToPng());
        series.WithPointLabel(0, captions[0]).WithPointLabel(4, captions[4]);
        var ordinary = chart.Prepare(context).Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Role == "data-label").Select(node => node.Text.Lines.Single().Text);
        Assert.Contains("HiddenLeft", ordinary);
        Assert.Contains("HiddenRight", ordinary);
        Assert.Equal(captions, series.PointLabels);
    }
}
