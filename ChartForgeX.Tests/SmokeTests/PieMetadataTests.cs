using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Verify = Xunit.Assert;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PieAndDonutSvgExposeSliceMetadata() {
        foreach (var donut in new[] { false, true }) {
            var chart = Chart.Create().WithSize(640, 420).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
                .WithPieSliceLabelContent(ChartPieSliceLabelContent.Percent).WithXLabels("Passed", "Failed");
            if (donut) chart.AddDonut("Results", Points(75, 25)); else chart.AddPie("Results", Points(75, 25));
            var prepared = PreparedFamily(chart);
            var groups = FamilyGroups(prepared, "radial-point");
            Verify.Equal(new[] { "0", "1" }, groups.Select(group => group.Metadata["data-cfx-point"]));
            Verify.Equal(new[] { "Passed", "Failed" }, groups.Select(group => group.Metadata["data-cfx-label"]));
            Verify.Equal(new[] { 75d, 25 }, groups.Select(group => FamilyNumber(group, "data-cfx-value")));
            Verify.Equal(new[] { .75, .25 }, groups.Select(group => FamilyNumber(group, "data-cfx-percent")));
            var slices = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role == (donut ? "donut-slice" : "pie-slice")).ToArray();
            Verify.Equal(2, slices.Length);
            Verify.Equal(new[] { "series-0-point-0", "series-0-point-1" }, slices.Select(slice => slice.Id));
            Verify.Equal(.75, slices[0].Sweep / (Math.PI * 2), 10);
            Verify.Equal(donut, slices[0].Inner > 0);
            Verify.Equal(new[] { "75%", "25%" }, FamilyLabels(prepared, "data-label").Select(FamilyContent));
            Verify.Equal(new[] { "75", "25" }, FamilyLabels(prepared, "legend-value").Select(FamilyContent));
            Verify.Equal(new[] { "75%", "25%" }, FamilyLabels(prepared, "legend-percentage").Select(FamilyContent));
            var svg = XDocument.Parse(prepared.ToSvg());
            for (var index = 0; index < groups.Length; index++) {
                var source = groups[index]; var sliceId = slices[index].Id;
                var exportedMark = Verify.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-source-id") == sliceId &&
                    (string?)element.Attribute("data-cfx-role") == slices[index].Role);
                var exportedSource = Verify.Single(exportedMark.Ancestors(), element => (string?)element.Attribute("data-cfx-role") == "radial-point");
                Verify.Equal(source.Metadata["data-cfx-percent"], (string?)exportedSource.Attribute("data-cfx-percent"));
                Verify.Equal(source.Metadata["data-cfx-source-points"], (string?)exportedSource.Attribute("data-cfx-source-points"));
                Verify.Contains(prepared.Regions, region => region.Id == sliceId && region.Bounds.Width > 0);
            }
            if (donut) Verify.Single(FamilyLabels(prepared, "donut-total-label"));
            Verify.NotEmpty(prepared.ToPng());
        }

        var positionedLegend = Chart.Create().WithLegendPosition(ChartLegendPosition.TopRight)
            .WithXLabels("Passed", "Failed", "Skipped").AddDonut("Results", Points(70, 20, 10));
        var positioned = PreparedFamily(positionedLegend);
        Verify.True(positioned.Regions.Where(region => region.Role == "legend").Max(region => region.Bounds.Bottom) <
            positioned.Scene.Nodes.OfType<VisualSceneSlice>().First().Cy);
        Verify.Equal(new[] { "70", "20", "10" }, FamilyLabels(positioned, "legend-value").Select(FamilyContent));
        Verify.Equal(new[] { "70%", "20%", "10%" }, FamilyLabels(positioned, "legend-percentage").Select(FamilyContent));
        Verify.NotEmpty(positioned.ToPng());
    }
}
