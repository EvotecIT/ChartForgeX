using System.Xml.Linq;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TreemapValueCaptionTests {
    [Fact]
    public void ValueVisibilityPreservesLabelsSemanticValuesAndPreparedSnapshots() {
        var chart = Chart.Create().WithDataLabels().AddTreemap("Allocation", new[] {
            new ChartHierarchyItem("root", "All"),
            new ChartHierarchyItem("alpha", "Alpha", "root", 7),
            new ChartHierarchyItem("beta", "Beta", "root", 3)
        });
        Assert.True(chart.Options.Treemap.ShowValues);
        var frozen = TreemapHierarchyTests.Prepare(chart);
        var originalSvg = frozen.ToSvg(); var originalPng = frozen.ToPng();
        Assert.Contains("All · 10", Captions(originalSvg));
        Assert.Contains("Alpha7", Captions(originalSvg));
        chart.Options.Treemap.ShowValues = false;
        var hidden = TreemapHierarchyTests.Prepare(chart);
        var labels = Captions(hidden.ToSvg());
        Assert.Equal(new[] { "All", "Alpha", "Beta" }, labels.OrderBy(label => label));
        Assert.Contains(hidden.Regions, region => region.Label == "Alpha: 7");
        var alpha = XDocument.Parse(hidden.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-target-id") == "alpha");
        Assert.Equal("7", (string?)alpha.Attribute("data-cfx-value"));
        Assert.Equal("7", (string?)alpha.Attribute("data-cfx-formatted-value"));
        Assert.Equal("root", (string?)alpha.Attribute("data-cfx-parent"));
        Assert.Equal(7, chart.Series[0].HierarchyItems[1].Value);
        Assert.NotEqual(originalPng, hidden.ToPng());
        Assert.Equal(originalSvg, frozen.ToSvg()); Assert.Equal(originalPng, frozen.ToPng());
        chart.Options.Treemap.ShowValues = true;
        Assert.Equal(originalSvg, TreemapHierarchyTests.Prepare(chart).ToSvg());
        chart.WithDataLabels(false);
        Assert.Empty(Captions(TreemapHierarchyTests.Prepare(chart).ToSvg()));
    }

    internal static string[] Captions(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") is "treemap-label" or "treemap-group-label")
        .Select(element => element.Value).ToArray();
}
