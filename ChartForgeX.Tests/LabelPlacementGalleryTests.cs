using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LabelPlacementGalleryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DenseGalleryPanelsUseOneCompletedSceneForBothExports(bool dark) {
        foreach (var (_, chart) in LabelPlacementExamples.Charts(dark)) {
            var request = VisualExportRequest.ForChart(chart);
            var prepared = chart.Prepare(request.Context);
            Assert.Equal(prepared.ToSvg(), chart.ToSvg());
            Assert.True(prepared.Scene.Nodes.OfType<VisualSceneText>().Count() > 2);
            Assert.NotEmpty(prepared.Regions);
            var raster = chart.ToRgbaImage();
            Assert.Equal(chart.Options.Size.Width * chart.Options.PngOutputScale, raster.Width);
            Assert.Equal(chart.Options.Size.Height * chart.Options.PngOutputScale, raster.Height);
            Assert.NotEmpty(chart.ToPng());
        }
        var grid = LabelPlacementExamples.Scorecard(dark);
        Assert.Contains(XDocument.Parse(grid.ToSvg()).Descendants(), element => element.Name.LocalName == "text");
        Assert.NotEmpty(grid.ToPng());
        var topology = LabelPlacementExamples.Topology(dark);
        var options = new TopologyRenderOptions { IncludeLegend = false };
        var report = ChartLabelScene.Inspect(topology.ToSvg(options), new FontSpec { Family = topology.Theme!.FontFamily });
        Assert.True(report.Visible > 2);
        Assert.Equal(0, report.LabelLabel); Assert.Equal(0, report.LabelMark);
        Assert.True(report.Contained >= topology.Nodes.Count * 2);
        Assert.NotEmpty(topology.ToPng(options));
    }

    [Fact]
    public void NativePngBulletMarksOccupyTheirPreparedSemanticRows() {
        var chart = LabelPlacementExamples.Charts(false).Single(panel => panel.Name == "bullet").Chart.WithPngOutputScale(2);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bullet-value").ToArray();
        var rows = prepared.Regions.Where(region => region.Role == "bullet-row").ToArray();
        Assert.Equal(chart.Series.Count, marks.Length); Assert.Equal(marks.Length, rows.Length);
        var image = chart.ToRgbaImage();
        for (var index = 0; index < marks.Length; index++) {
            var mark = marks[index]; var row = rows[index].Bounds;
            Assert.InRange(mark.Bounds.Left, row.Left, row.Right); Assert.InRange(mark.Bounds.Right, row.Left, row.Right);
            Assert.InRange(mark.Bounds.Top, row.Top, row.Bottom); Assert.InRange(mark.Bounds.Bottom, row.Top, row.Bottom);
            var x = (int)Math.Round((mark.Bounds.Left + mark.Bounds.Width / 2) * 2);
            var y = (int)Math.Round((mark.Bounds.Top + mark.Bounds.Height / 2) * 2);
            var offset = (y * image.Width + x) * 4; var color = mark.Fill!.Value;
            Assert.True(Math.Abs(image.Pixels[offset] - color.R) + Math.Abs(image.Pixels[offset + 1] - color.G) + Math.Abs(image.Pixels[offset + 2] - color.B) < 100);
        }
    }

    [Fact]
    public void FunnelStagesRetainValuesAndRatiosBesideTheirNativeMarks() {
        var chart = LabelPlacementExamples.Charts(false).Single(panel => panel.Name == "funnel").Chart;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var stages = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "funnel-stage").ToArray();
        Assert.Equal(chart.Series[0].Points.Count, stages.Length);
        Assert.Equal(stages.Length, prepared.Scene.Nodes.Count(node => node.Role == "funnel-segment"));
        Assert.Equal(stages.Length, prepared.Regions.Count(region => region.Role == "funnel-stage"));
        for (var index = 0; index < stages.Length; index++) {
            Assert.Equal(chart.Series[0].Points[index].Y, double.Parse(stages[index].Metadata["data-cfx-value"], System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(index == 0 ? "false" : "true", stages[index].Metadata["data-cfx-dropoff-defined"]);
            Assert.Equal("true", stages[index].Metadata["data-cfx-retention-defined"]);
        }
        Assert.Equal(stages.Length - 1, prepared.Scene.Nodes.OfType<VisualSceneText>().Count(node => node.Role == "funnel-ratio"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void DroppedLabelsRetainEverySourceValueAndAssociatedMarkAccessibility() {
        var chart = Chart.Create().WithSize(260, 180).WithDataLabels().WithLegend(false)
            .AddScatter("Measurements", Enumerable.Range(0, 80).Select(index => new Primitives.ChartPoint(index % 5, index % 7)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        Assert.True(prepared.Scene.Nodes.Count(node => node.Role == "data-label") < 80);
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point").ToArray();
        Assert.Equal(80, points.Length); Assert.Equal(80, prepared.Regions.Count(region => region.Role == "point"));
        foreach (var point in points) {
            Assert.NotEmpty(point.Metadata["data-cfx-label"]);
            Assert.Contains(point.Metadata["data-cfx-label"], point.Metadata["aria-label"]);
            var region = Assert.Single(prepared.Regions, item => item.Id == point.Id);
            Assert.Equal(point.Metadata["aria-label"], region.Label);
        }
    }
}
