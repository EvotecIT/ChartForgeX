using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SharedVocabularyTests {
    [Fact]
    public void AutoModeRendersAsLightInStaticOutput() {
        Chart Line(ThemeMode mode) => Chart.Create().Size(480, 300).ThemeMode(mode)
            .AddLine("Orders", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5), new ChartPoint(3, 4) });

        Assert.Equal(ThemeMode.Auto, Chart.Create().Options.ThemeMode);
        Assert.Equal(Line(ThemeMode.Light).ToSvg(), Line(ThemeMode.Auto).ToSvg());
        Assert.Equal(Line(ThemeMode.Light).ToPng(), Line(ThemeMode.Auto).ToPng());
        Assert.NotEqual(Line(ThemeMode.Dark).ToSvg(), Line(ThemeMode.Auto).ToSvg());
        Assert.Equal(ThemeMode.Light, new VisualRenderContext(themeMode: ThemeMode.Auto).ThemeMode);
    }

    [Fact]
    public void TopologyTonesAreStoredByWireName() {
        var chart = TopologyEdgePresentationTests.Sites();
        var node = chart.Nodes[0];
        node.Tone = Tone.Danger;
        Assert.Equal("danger", node.Metadata["chartforgex.region.tone"]);
        node.Tone = Tone.Named("Maintenance");
        Assert.Equal("maintenance", node.Metadata["chartforgex.region.tone"]);
        Assert.Equal(Tone.Named("maintenance"), node.Tone);
        node.Tone = Tone.None;
        Assert.False(node.Metadata.ContainsKey("chartforgex.region.tone"));

        chart.AddEdgeChip("link", "lag 4m", Tone.Named("accent"));
        var restored = VisualArtifactInterchangeEnvelope.FromJson(chart.Prepare().ToInterchangeEnvelope().ToJson()).ToTopologyChart();
        Assert.Equal(Tone.Named("accent"), restored.Edges.Single(edge => edge.Id == "link").Chips.Single().Tone);
    }

    [Fact]
    public void NamedTonesPaintTheirThemeRoleAndUnknownNamesPaintNeutral() {
        var theme = VisualTheme.Graphite();
        var resolved = theme.ResolvePresentation(ThemeMode.Light);
        var colors = theme.Resolve(ThemeMode.Light);
        Assert.Equal(colors.Accent, VisualBlockRendering.StatusColor(resolved, Tone.Named("accent")));
        Assert.Equal(colors.Status.Maintenance.Fill, VisualBlockRendering.StatusColor(resolved, Tone.Named("maintenance")));
        Assert.Equal(resolved.MutedText, VisualBlockRendering.StatusColor(resolved, Tone.Named("brand")));
        Assert.Equal(resolved.MutedText, VisualBlockRendering.StatusColor(resolved, Tone.Muted));

        var chart = TopologyEdgePresentationTests.Sites();
        chart.Edges.Single(edge => edge.Id == "link").Chips.Add(new TopologyEdgeChip { Text = "brand", Tone = Tone.Named("brand") });
        chart.Edges.Single(edge => edge.Id == "link").Chips.Add(new TopologyEdgeChip { Text = "accent", Tone = Tone.Named("accent") });
        var svg = XDocument.Parse(chart.Prepare().ToSvg());
        string ChipOutline(string text) => (string)svg.Descendants().First(element => (string?)element.Attribute("data-chip-text") == text)
            .Descendants().First(element => element.Name.LocalName == "rect").Attribute("stroke")!;
        Assert.Equal(colors.Accent.ToCss(), ChipOutline("accent"), ignoreCase: true);
        Assert.Equal(colors.Status.Neutral.Fill.ToCss(), ChipOutline("brand"), ignoreCase: true);
    }

    [Fact]
    public void UnknownIconKeyDrawsTheDefaultMarkAndNoneDrawsNothing() {
        string IconOf(Icon icon) {
            var svg = MetricCard.Create().Metric("Load", "42%").Icon(icon).Size(360, 190).ToSvg("icon");
            var marks = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "visual-icon").ToList();
            return marks.Count == 0 ? "" : (string)marks.Single().Attribute("data-cfx-icon")!;
        }

        Assert.Equal("flame", IconOf(Icons.Flame));
        Assert.Equal("lightning", IconOf(Icon.Parse("LIGHTNING")));
        Assert.Equal("default", IconOf(Icon.Named("tabler:server")));
        Assert.Equal("default", IconOf(Icon.Named("rocket")));
        Assert.Equal("", IconOf(Icon.None));
        Assert.Equal(Icons.All.Count, Icons.All.Select(icon => IconOf(icon)).Distinct().Count());
        Assert.DoesNotContain("default", Icons.All.Select(icon => IconOf(icon)));
    }
}
