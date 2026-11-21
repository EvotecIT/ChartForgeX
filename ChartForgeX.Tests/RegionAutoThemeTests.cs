using System.Text.RegularExpressions;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RegionAutoThemeTests {
    private static TopologyChart Regions() {
        var chart = TopologyChart.Create().WithViewport(860, 500, 20).WithLayout(TopologyLayoutMode.Regions)
            .AddAutoGroup("r", "Region").AddAutoNode("muted", "Muted", groupId: "r").AddAutoNode("accent", "Accent", groupId: "r")
            .AddAutoNode("unknown", "Unknown", groupId: "r").AddEdge("link", "muted", "accent");
        chart.Nodes.Single(node => node.Id == "muted").Tone = Tone.Muted;
        chart.Nodes.Single(node => node.Id == "accent").Tone = Tone.Named("accent");
        chart.Nodes.Single(node => node.Id == "unknown").Tone = Tone.Named("no-such-role");
        return chart;
    }

    // tone-neutral carries a stylesheet stroke; muted and recognised named rings keep the colour the SVG paints.
    [Fact]
    public void MutedAndNamedRegionRingsKeepTheirOwnClass() {
        var html = new TopologyHtmlRenderer().RenderFragment(Regions());
        string Tone(string id) => Regex.Match(html, "<g[^>]*class=\"cfx-node (tone-[a-z]+)[^\"]*\"[^>]*data-node-id=\"" + id + "\"").Groups[1].Value;
        Assert.Equal("tone-muted", Tone("muted"));
        Assert.Equal("tone-named", Tone("accent"));
        Assert.Equal("tone-neutral", Tone("unknown"));
    }

    // Print keeps the light drawing whatever the viewer's colour scheme: its SVG holds no Auto properties.
    [Fact]
    public void AutoRegionPrintDrawingPaintsLiteralLightColours() {
        var html = new TopologyHtmlRenderer().RenderFragment(Regions());
        var start = html.IndexOf("class=\"cfx-region-print\"", StringComparison.Ordinal);
        Assert.True(start > 0);
        var print = html.Substring(start, html.IndexOf("</svg>", start, StringComparison.Ordinal) - start);
        Assert.DoesNotContain("--cfx-auto-", print, StringComparison.Ordinal);
        Assert.Contains("--cfx-auto-", html.Substring(0, start), StringComparison.Ordinal);
    }
}

public sealed class AutoThemeBindingTests {
    // Region ring and edge rules read the Auto role properties, so a host's shared status roles reach them.
    [Fact]
    public void AutoRegionStatusPropertiesFollowTheSharedRoles() {
        var chart = TopologyChart.Create().WithViewport(860, 500, 20).WithLayout(TopologyLayoutMode.Regions)
            .AddAutoGroup("r", "Region").AddAutoNode("a", "A", groupId: "r");
        var css = TopologyHtmlRenderer.BuildAutoThemeStyle(chart)!;
        Assert.Contains("--cfx-good:var(--cfx-auto-color-outcome-pass-fill,", css, StringComparison.Ordinal);
        Assert.DoesNotMatch("--cfx-good:#", css);
    }

    // A derived colour (story syntax) equal to a theme colour takes it over, so its own dark value applies.
    [Fact]
    public void DerivedColoursTakeOverAnEqualThemeColour() {
        var theme = ChartForgeX.Themes.VisualTheme.Graphite();
        var light = theme.Resolve(ThemeMode.Light).Foreground;
        var auto = ChartForgeX.Themes.AutoThemeStyle.ForHtml(theme, ThemeMode.Auto, null)!
            .WithColor("story-syntax-0", light, ChartForgeX.Primitives.ChartColor.FromHex("#e5e7eb"));
        Assert.Contains("--cfx-auto-story-syntax-0", auto.Variables.Apply("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect fill=\"" + light.ToCss() + "\"/></svg>"), StringComparison.Ordinal);
        Assert.Contains("--cfx-auto-story-syntax-0:#E5E7EB", auto.StyleSheet, StringComparison.Ordinal);
    }

    // Panels with their own themes paint in their own colour scope; an Auto panel with the grid's theme follows the viewer.
    [Fact]
    public void AutoGridPanelsWithTheGridThemeFollowTheViewer() {
        var chart = ChartForgeX.Core.Chart.Create().WithSize(320, 200).AddLine("Requests", new[] { new ChartPoint(1, 5), new ChartPoint(2, 10) });
        var grid = ChartForgeX.Core.ChartGrid.Create().Add(chart);
        grid.UsePanelThemes = true;
        var html = new ChartForgeX.Html.HtmlChartGridRenderer().RenderFragment(grid, "g");
        Assert.Contains("var(--cfx-auto-color-chart-series-1", html, StringComparison.Ordinal);
        Assert.Null(chart.Options.SvgColorVariables);
    }
}
