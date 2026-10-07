using ChartForgeX;
using ChartForgeX.Core;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PieAndDonutSvgExposeSliceMetadata() {
        var pie = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithXLabels("Passed", "Failed")
            .AddPie("Results", Points(75, 25))
            .ToSvg();
        var pieSource = FamilyGroups(PreparedFamily(Chart.Create().WithXLabels("Passed", "Failed").AddPie("Results", Points(75, 25))), "radial-point")[0];
        Assert(pieSource.Metadata["data-cfx-point"] == "0" && pieSource.Metadata["data-cfx-label"] == "Passed" && pieSource.Metadata["data-cfx-value"] == "75" && pieSource.Metadata["data-cfx-percent"] == "0.75", "Pie slices should retain label, value, percent and source identity.");

        var donut = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithXLabels("Passed", "Failed")
            .AddDonut("Results", Points(75, 25))
            .ToSvg();
        var donutPrepared = PreparedFamily(Chart.Create().WithXLabels("Passed", "Failed").AddDonut("Results", Points(75, 25)));
        Assert(FamilyGroups(donutPrepared, "radial-point")[0].Metadata["data-cfx-value"] == "75" && donutPrepared.Scene.Nodes.OfType<VisualSceneSlice>().First().Inner > 0, "Donut slices should retain values and actual hole geometry.");
        Assert(donut.Contains("data-cfx-role=\"donut-total-label\"", System.StringComparison.Ordinal), "Donuts should expose center total role metadata.");

        var positionedLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithLegendPosition(ChartLegendPosition.TopRight)
            .WithXLabels("Passed", "Failed", "Skipped")
            .AddDonut("Results", Points(70, 20, 10));
        var positionedLegendSvg = positionedLegend.ToSvg();
        var prepared = PreparedFamily(positionedLegend);
        Assert(prepared.Regions.Where(region => region.Role == "legend").Max(region => region.Bounds.Bottom) < prepared.Scene.Nodes.OfType<VisualSceneSlice>().First().Cy, "Top slice legends should occupy the upper frame.");
        Assert(FamilyLabels(prepared, "legend-value").Any(label => FamilyContent(label) == "20%"), "Slice legends should retain per-slice percent labels.");
        Assert(positionedLegend.ToPng().Length > 64, "Positioned slice legends should render PNG output.");
    }
}
