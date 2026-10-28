using System;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static partial class Program {
    private static void NativeHierarchySection() {
        var chart = Chart.Create().WithSize(520, 360).WithLegend(false).AddTreemap("Work", new[] {
            new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("team", "Team", "root"),
            new ChartHierarchyItem("api", "API", "team", 4), new ChartHierarchyItem("other", "Other", "root", 6)
        });
        var renderer = new HtmlInteractiveChartRenderer();
        var section = renderer.PrepareHierarchySection(chart, new ChartHierarchyView("team"), options => {
            options.Interaction.Enable(ChartInteractionFeatures.HierarchyNavigation | ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        });
        PackageAssertions.Require(section.PreparedVisual.HierarchyView!.RootId == "team"
            && section.NarrowPreparedVisual!.HierarchyView!.RootId == "team"
            && section.Html.Contains("data-cfx-hierarchy-control", StringComparison.Ordinal)
            && HtmlInteractiveChartRenderer.BuildInteractionScript().Contains("setHierarchyView", StringComparison.Ordinal),
            "Packed HTML hierarchy section lost its native view or managed navigation capability.");
        PackageAssertions.Png(section.PreparedVisual.ToPng());
        var sunburst = Chart.Create().WithSize(520, 360).WithLegend(false).AddSunburst("Work", chart.Series[0].HierarchyItems)
            .ConfigureSunburst(options => { options.SectorGap = 6; options.MinimumLabelFontSize = 9; });
        var spaced = renderer.PrepareHierarchySection(sunburst, new ChartHierarchyView("team"), options => {
            options.Interaction.Enable(ChartInteractionFeatures.HierarchyNavigation);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        });
        PackageAssertions.Require(spaced.Html.Contains("data-cfx-sector-gap=\"6\"", StringComparison.Ordinal)
            && spaced.PreparedVisual.HierarchyView!.RootId == "team" && spaced.NarrowPreparedVisual!.HierarchyView!.RootId == "team",
            "Packed adaptive Sunburst section lost its gap or committed native view.");
        PackageAssertions.Png(spaced.PreparedVisual.ToPng());
        var script = HtmlInteractiveChartRenderer.BuildInteractionScript();
        // The packaged contract is the public controller API, not an optimizer's private function names.
        PackageAssertions.Require(script.Contains("setHierarchyView", StringComparison.Ordinal)
            && script.Contains("connectNative", StringComparison.Ordinal)
            && script.Contains("editAnnotations", StringComparison.Ordinal)
            && script.Contains("prefers-reduced-motion", StringComparison.Ordinal),
            "Packed interaction asset omitted a native hierarchy/editor/reduced-motion lifetime owner.");
        // Physical transaction, preview and animation acceptance remain in the maintained browser gate.

    }
}
