using ChartForgeX;
using ChartForgeX.Core;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PolarAreaHonorsGridVisibility() {
        var compact = PolarAreaSample()
            .WithGrid(false)
            .ToSvg();

        Assert(compact.Contains("data-cfx-role=\"polar-area-segment\"", System.StringComparison.Ordinal), "Polar-area segments should still render when grid is disabled.");
        Assert(!compact.Contains("data-cfx-role=\"polar-area-ring\"", System.StringComparison.Ordinal), "Polar-area reference rings should hide when grid is disabled.");

        var defaultSvg = PolarAreaSample().ToSvg();
        Assert(defaultSvg.Contains("data-cfx-role=\"polar-area-ring\"", System.StringComparison.Ordinal), "Polar-area reference rings should render by default.");
        Assert(PolarAreaSample().WithGrid(false).ToPng().Length > 64, "Compact polar-area options should render valid PNG output.");
        var positionedLegend = PolarAreaSample().WithLegendPosition(ChartLegendPosition.BottomRight);
        var positionedSvg = positionedLegend.ToSvg();
        var prepared = PreparedFamily(positionedLegend);
        Assert(prepared.Regions.Count(region => region.Role == "legend") == 4, "Polar-area legends should retain all four categories.");
        Assert(prepared.Regions.Where(region => region.Role == "legend").Min(region => region.Bounds.Top) > prepared.Scene.Nodes.OfType<VisualSceneSlice>().First().Cy,
            "Polar-area legends configured at the bottom should occupy the lower frame.");
        Assert(positionedLegend.ToPng().Length > 64, "Polar-area positioned legends should render valid PNG output.");
    }

    private static Chart PolarAreaSample() => Chart.Create()
        .WithSize(520, 340)
        .WithXLabels("Coverage", "Policy", "Alerts", "Response")
        .AddPolarArea("Control share", Points(92, 74, 88, 96));
}
