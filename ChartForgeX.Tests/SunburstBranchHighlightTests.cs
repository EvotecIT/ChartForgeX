using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SunburstBranchHighlightTests {
    [Fact]
    public void RootBranchFactsUseIdsAndSurviveReorderingZeroAndCollapsedGeometry() {
        var items = new[] {
            new ChartHierarchyItem("center", "Repeated"), new ChartHierarchyItem("a", "Repeated", "center"),
            new ChartHierarchyItem("leaf", "Repeated", "a", 1), new ChartHierarchyItem("zero", "Repeated", "a", 0),
            new ChartHierarchyItem("tiny", "Repeated", "a", double.Epsilon), new ChartHierarchyItem("b", "Repeated", "center", 1)
        };
        var first = Chart.Create().AddSunburst("Work", items);
        var prepared = SunburstHierarchyTests.Prepare(first);
        var nodes = SunburstHierarchyTests.Nodes(prepared);
        var reordered = SunburstHierarchyTests.Nodes(SunburstHierarchyTests.Prepare(Chart.Create().AddSunburst("Work", items.AsEnumerable().Reverse())));
        Assert.Null(nodes["center"].Attribute("data-cfx-branch-root"));
        foreach (var id in new[] { "a", "leaf", "zero", "tiny", "b" }) {
            var expected = id == "b" ? "b" : "a";
            Assert.Equal(expected, nodes[id].Attribute("data-cfx-branch-root")?.Value);
            Assert.Equal(expected, reordered[id].Attribute("data-cfx-branch-root")?.Value);
        }
        Assert.Equal("zero", nodes["zero"].Attribute("data-cfx-geometry-status")?.Value);
        Assert.Equal("precision-collapse", nodes["tiny"].Attribute("data-cfx-geometry-status")?.Value);
        Assert.Empty(nodes["zero"].Elements()); Assert.Empty(nodes["tiny"].Elements());
        first.ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal);
        Assert.Equal("a", SunburstHierarchyTests.Nodes(prepared)["leaf"].Attribute("data-cfx-branch-root")?.Value);
        var only = SunburstHierarchyTests.Nodes(SunburstHierarchyTests.Prepare(Chart.Create().AddSunburst("Only", new[] { new ChartHierarchyItem("only", "Only", value: 1) })));
        Assert.Null(only["only"].Attribute("data-cfx-branch-root"));
    }

    [Fact]
    public void SparseHighlightOptionsReachAllHtmlSurfacesAndDoNotChangeNativeExports() {
        var chart = Chart.Create().WithSize(360, 300).AddSunburst("Work", new[] {
            new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("child", "Child", "root", 1)
        });
        var svg = chart.ToSvg(); var png = chart.ToPng();
        Assert.Equal(HtmlChartHighlightMode.Default, new HtmlChartInteractionOptions().Highlight.Mode);
        Assert.Equal(HtmlChartHighlightMode.Default, new HtmlInteractiveDashboardOptions().Highlight.Mode);
        var defaultRoot = RootSections(chart.ToInteractiveHtmlPage()).Single();
        Assert.DoesNotContain("data-cfx-highlight-", defaultRoot, StringComparison.Ordinal);
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            void Configure(HtmlChartInteractionOptions options) => ConfigureHighlight(options.Highlight);
            var surfaces = new[] {
                chart.ToInteractiveHtmlPage(Configure), chart.ToInteractiveHtmlFragment(Configure), chart.ToInteractiveHtmlFragmentWithoutAssets(Configure),
                chart.ToInteractiveHtmlPage(options => { Configure(options); options.ExternalAssets = new HtmlAssetReferences("assets/"); }),
                new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => ConfigureHighlight(options.Highlight))
            };
            foreach (var html in surfaces) foreach (var root in RootSections(html)) {
                Assert.Contains("data-cfx-highlight-mode=\"root-branch\"", root, StringComparison.Ordinal);
                Assert.Contains("data-cfx-highlight-item-stroke-width=\"2.5\"", root, StringComparison.Ordinal);
                Assert.Contains("data-cfx-highlight-branch-opacity=\"0.7\"", root, StringComparison.Ordinal);
                Assert.Contains("data-cfx-highlight-other-item-opacity=\"0\"", root, StringComparison.Ordinal);
                Assert.Contains("data-cfx-highlight-other-branch-stroke-width=\"0\"", root, StringComparison.Ordinal);
            }
        } finally { CultureInfo.CurrentCulture = culture; }
        Assert.Equal(svg, chart.ToSvg()); Assert.Equal(png, chart.ToPng());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void HighlightStyleRejectsNonFiniteAndNegativeValues(double value) {
        var style = new HtmlChartHighlightStyle();
        Assert.Throws<ArgumentOutOfRangeException>(() => style.Opacity = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => style.StrokeWidth = value);
    }

    [Fact]
    public void HighlightBoundsAndInvalidModesFailAtThePublicBoundary() {
        var style = new HtmlChartHighlightStyle { Opacity = 0, StrokeWidth = 0 };
        style.Opacity = 1; style.StrokeWidth = double.MaxValue;
        style.Opacity = null; style.StrokeWidth = null; style.Stroke = null;
        Assert.Throws<ArgumentOutOfRangeException>(() => style.Opacity = 1.01);
        var chart = Chart.Create().AddLine("Readings", ChartPoints.FromValues(1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ToInteractiveHtmlPage(options => options.Highlight.Mode = (HtmlChartHighlightMode)123));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ToInteractiveHtmlFragment(options => options.Highlight.Mode = (HtmlChartHighlightMode)123));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ToInteractiveHtmlFragmentWithoutAssets(options => options.Highlight.Mode = (HtmlChartHighlightMode)123));
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { chart }.ToInteractiveHtmlDashboardPage(options => options.Highlight.Mode = (HtmlChartHighlightMode)123));
    }

    private static IEnumerable<string> RootSections(string html) => Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>").Cast<Match>().Select(match => match.Value);
    private static void ConfigureHighlight(HtmlChartHighlightOptions options) {
        options.Mode = HtmlChartHighlightMode.RootBranch;
        options.HighlightedItem.Stroke = ChartColor.FromRgb(20, 30, 40);
        options.HighlightedItem.StrokeWidth = 2.5;
        options.HighlightedBranch.Opacity = .7;
        options.UnhighlightedItem.Opacity = 0;
        options.UnhighlightedBranch.StrokeWidth = 0;
    }
}
