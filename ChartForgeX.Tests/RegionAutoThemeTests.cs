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
