using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void HierarchySubtreeContracts() {
        foreach (var kind in new[] { ChartSeriesKind.Treemap, ChartSeriesKind.Sunburst }) {
            var chart = Chart.Create().WithSize(520, 360).WithLegend(false).WithDataLabels(false);
            var items = new[] { new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("team", "Team", "root"),
                new ChartHierarchyItem("api", "API", "team", 4), new ChartHierarchyItem("worker", "Worker", "team", 6),
                new ChartHierarchyItem("other", "Other", "root", 10) };
            if (kind == ChartSeriesKind.Treemap) chart.AddTreemap("Work", items);
            else chart.AddSunburst("Work", items).ConfigureSunburst(options => { options.SectorGap = 6; options.MinimumLabelFontSize = 9; options.CornerRadius = 4; });
            var context = DepthContext(chart, showLegend: false);
            var original = chart.Prepare(context); var view = chart.PrepareHierarchy(context, new ChartHierarchyView("team"));
            var xml = XDocument.Parse(view.ToSvg());
            PackageAssertions.Require(view.HierarchyView!.RootId == "team" && chart.Series[0].HierarchyItems.Count == 5
                && (string?)xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-node") == "team").Attribute("data-cfx-parent") == "root"
                && (string?)xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-node") == "other").Attribute("data-cfx-geometry-status") == "outside-view",
                "Packed hierarchy preparation lost source identities or painted an excluded branch.");
            PackageAssertions.Require(original.ToSvg() == chart.PrepareHierarchy(context, null).ToSvg(),
                "Packed hierarchy reset did not restore the full source scene.");
            if (kind == ChartSeriesKind.Sunburst)
                PackageAssertions.Require(xml.Descendants().Any(node => (string?)node.Attribute("data-cfx-node") == "team"
                    && (string?)node.Attribute("data-cfx-sector-gap") == "6" && (string?)node.Attribute("data-cfx-paint-inner-radius") == "0"),
                    "Packed Sunburst options did not use native inset geometry for the selected root.");
            PackageAssertions.Png(view.ToPng());
        }
    }
}
