using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AutoThemeHtmlTests {
    private static Chart Line(ThemeMode mode) => Chart.Create().Size(480, 300).ThemeMode(mode).Title("Orders")
        .AddLine("Orders", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5), new ChartPoint(3, 4) });

    private static TopologyChart Topology(ThemeMode mode) => TopologyChart.Create().Theme(VisualTheme.Graphite(), mode).Viewport(new TopologyViewport(640, 240, 20))
        .AddNode("a", "API", 80, 100).AddNode("b", "Worker", 340, 100).AddEdge("ab", "a", "b", "calls");

    public static IEnumerable<object[]> Renderers() {
        yield return new object[] { "chart fragment", (Func<ThemeMode, string>)(mode => new HtmlChartRenderer().RenderFragment(Line(mode))) };
        yield return new object[] { "chart page", (Func<ThemeMode, string>)(mode => new HtmlChartRenderer().RenderPage(Line(mode))) };
        yield return new object[] { "interactive fragment", (Func<ThemeMode, string>)(mode => new HtmlInteractiveChartRenderer().RenderFragment(Line(mode))) };
        yield return new object[] { "interactive section", (Func<ThemeMode, string>)(mode => {
            var section = new HtmlInteractiveChartRenderer().PrepareSection(Line(mode));
            return section.Html + section.AutoThemeStyleSheet;
        }) };
        yield return new object[] { "interactive page", (Func<ThemeMode, string>)(mode => new HtmlInteractiveChartRenderer().RenderPage(Line(mode))) };
        yield return new object[] { "responsive fragment", (Func<ThemeMode, string>)(mode => new HtmlResponsiveChartRenderer().RenderFragment(Line(mode))) };
        yield return new object[] { "chart grid page", (Func<ThemeMode, string>)(mode => new HtmlChartGridRenderer().RenderPage(ChartGrid.Create().ThemeMode(mode).Add(Line(mode)))) };
        yield return new object[] { "topology fragment", (Func<ThemeMode, string>)(mode => Topology(mode).ToHtmlFragment()) };
        yield return new object[] { "topology page", (Func<ThemeMode, string>)(mode => Topology(mode).ToHtmlPage()) };
        yield return new object[] { "interactive topology", (Func<ThemeMode, string>)(mode => new HtmlInteractiveTopologyRenderer().RenderPage(Topology(mode))) };
        yield return new object[] { "visual block page", (Func<ThemeMode, string>)(mode => new HtmlVisualBlockRenderer().RenderPage(
            MetricCard.Create().ThemeMode(mode).Metric("Load", "42%").Size(360, 190))) };
        yield return new object[] { "sequence diagram", (Func<ThemeMode, string>)(mode => {
            var sequence = SequenceArtifact.Create("handoff").AddParticipant("api", "API").AddParticipant("db", "Database").AddMessage("api", "db", "query");
            sequence.ThemeMode = mode;
            return sequence.ToInteractiveHtmlPage();
        }) };
    }

    [Theory]
    [MemberData(nameof(Renderers))]
    public void AutoHtmlFollowsTheViewerAndExplicitModesStayLiteral(string surface, Func<ThemeMode, string> render) {
        var auto = render(ThemeMode.Auto);
        var dark = VisualTheme.Graphite().Resolve(ThemeMode.Dark);
        Assert.True(auto.Contains("data-cfx-theme-mode=\"auto\"", StringComparison.Ordinal), surface + " marks its root as auto.");
        var media = auto.IndexOf("@media (prefers-color-scheme:dark){[data-cfx-auto-theme=\"cfx-auto-", StringComparison.Ordinal);
        Assert.True(media > 0, surface + " carries the scoped dark rules.");
        Assert.Contains("--cfx-auto-color-surface-card:var(--hfx-color-surface-card," + dark.ElevatedSurface.ToCss() + ")", auto.Substring(media));
        Assert.Contains("var(--cfx-auto-", auto);
        foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark }) {
            var literal = render(mode);
            Assert.DoesNotContain("cfx-auto", literal);
            Assert.DoesNotContain("data-cfx-auto-theme", literal);
        }
    }

    [Fact]
    public void HostColourVariablesWinOverAuto() {
        var chart = Line(ThemeMode.Auto).SvgColorVariables(SvgColorVariables.SharedRoles());
        Assert.DoesNotContain("cfx-auto", new HtmlChartRenderer().RenderFragment(chart));
        Assert.DoesNotContain("cfx-auto", new HtmlInteractiveChartRenderer().RenderFragment(chart));
        Assert.DoesNotContain("cfx-auto", Topology(ThemeMode.Auto).ToHtmlFragment(new TopologyRenderOptions { SvgColorVariables = SvgColorVariables.SharedRoles() }));
    }

    [Fact]
    public void AutoChromeLeavesTheInlineStyleSoTheDarkRuleCanApply() {
        var section = new HtmlInteractiveChartRenderer().PrepareSection(Line(ThemeMode.Auto));
        var style = Regex.Match(section.Html, "<section[^>]*\\sstyle=\"([^\"]*)\"").Groups[1].Value;
        Assert.DoesNotContain("--cfx-tooltip-surface", style);
        Assert.DoesNotContain("<style", section.Html);
        var css = section.AutoThemeStyleSheet!;
        Assert.Equal(css, HtmlInteractiveChartRenderer.AutoThemeStyleSheet(Line(ThemeMode.Auto)));
        var dark = VisualTheme.Graphite().Resolve(ThemeMode.Dark).Chrome.Surface.ToCss();
        Assert.Contains("--cfx-tooltip-surface:var(--cfx-surface-card,var(--hfx-color-surface," + dark + "))", css.Substring(css.IndexOf("prefers-color-scheme:dark", StringComparison.Ordinal)));
    }

    [Fact]
    public void ResponsiveRenderingRestoresTheCallerColourVariables() {
        var chart = Line(ThemeMode.Auto);
        var fragment = new HtmlResponsiveChartRenderer().RenderFragmentWithoutAssets(chart);
        Assert.Null(chart.Options.SvgColorVariables);
        var css = HtmlResponsiveChartRenderer.AutoThemeStylesheet(chart);
        Assert.NotNull(css);
        Assert.DoesNotContain("<style", fragment);
        var scope = Regex.Match(fragment, "data-cfx-auto-theme=\"(cfx-auto-[0-9a-f]+)\"").Groups[1].Value;
        Assert.StartsWith("[data-cfx-auto-theme=\"" + scope + "\"]{", css);
        Assert.Null(HtmlResponsiveChartRenderer.AutoThemeStylesheet(Line(ThemeMode.Light)));
    }

    [Fact]
    public void ScopesDifferByThemeAndAgreeForEqualThemes() {
        string Scope(Chart chart) => Regex.Match(new HtmlChartRenderer().RenderFragment(chart), "<div class=\"chartforgex-chart\" data-cfx-auto-theme=\"(cfx-auto-[0-9a-f]+)\"").Groups[1].Value;
        var brand = VisualTheme.Graphite().WithPalette(ThemeMode.Dark, ChartColor.FromHex("#FF00AA"));
        Assert.Equal(Scope(Line(ThemeMode.Auto)), Scope(Line(ThemeMode.Auto)));
        Assert.NotEqual(Scope(Line(ThemeMode.Auto)), Scope(Line(ThemeMode.Auto).Theme(brand)));
    }
}
