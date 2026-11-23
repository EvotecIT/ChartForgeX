using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void StagePresentationContracts() {
        var series = new ChartSeries(new ChartSeriesId("funnel"), "Intake", ChartSeriesKind.Funnel,
            new[] { new ChartPoint(1, 100), new ChartPoint(2, 40), new ChartPoint(3, 0) })
            .PointStrokeColor(0, ChartColor.FromHex("#764CA6"))
            .FunnelConnectorStyle(0, new ChartFunnelConnectorStyle(fillOpacity: .5, outlineStroke: new ChartStrokeStyle(width: 2, dash: new[] { 4d, 2d })));
        var chart = Chart.Create().Size(640, 480).DataLabels().XLabels("Received", "Approved", "Completed").AddSeries(series)
            .Funnel(options => { options.LabelContent = ChartStageLabelContent.Separate; options.StageOutline = new ChartStrokeStyle(width: 2); });
        var document = XDocument.Parse(chart.ToSvg());
        PackageAssertions.Require(document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "funnel-stage") == 3,
            "Packed stage presentation lost a zero source stage.");
        PackageAssertions.Require(document.Descendants().Any(node => node.Attribute("stroke-dasharray") != null), "Packed connector outline lost its dash policy.");
        PackageAssertions.Raster(chart.ToPng());
        var pyramid = Chart.Create().DataLabels().XLabels("Services", "Platform").AddPyramid("Allocation", new[] { new ChartPoint(1, 3), new ChartPoint(2, 2) })
            .Pyramid(options => { options.LabelContent = ChartStageLabelContent.Separate; options.PartitionOutline = new ChartStrokeStyle(width: 2); });
        PackageAssertions.Require(PackageAssertions.Contains(pyramid.ToSvg(), "pyramid-category-label"), "Packed independent Pyramid captions are missing."); PackageAssertions.Raster(pyramid.ToPng());
    }
}
