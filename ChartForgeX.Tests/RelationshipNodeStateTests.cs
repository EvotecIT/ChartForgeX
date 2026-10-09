using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RelationshipNodeStateTests {
    [Theory]
    [InlineData(ChartSeriesKind.Tree, "tree-node", "tree-node-mark")]
    [InlineData(ChartSeriesKind.Sunburst, "sunburst-segment", "sunburst-segment-mark")]
    [InlineData(ChartSeriesKind.Sankey, "sankey-node", "sankey-node-mark")]
    [InlineData(ChartSeriesKind.Chord, "chord-node", "chord-node-mark")]
    public void SharedNodeStatePaintUsesAuthoredIdsAndExplicitColorsTakePrecedence(ChartSeriesKind kind, string groupRole, string markRole) {
        var nodes = new[] { new ChartNode("root", "Support"), new ChartNode("leaf", "Support") };
        var chart = kind switch {
            ChartSeriesKind.Tree => Chart.Create().AddTree("State", nodes, new[] { new ChartTreeLink("root", "leaf", 3) }),
            ChartSeriesKind.Sunburst => Chart.Create().AddSunburst("State", nodes, new[] { new ChartTreeLink("root", "leaf", 3) }),
            ChartSeriesKind.Sankey => Chart.Create().AddSankey("State", nodes, new[] { new ChartFlowLink("flow", "root", "leaf", 3) }),
            _ => Chart.Create().AddChord("State", nodes, new[] { new ChartFlowLink("flow", "root", "leaf", 3) })
        };
        chart.Series[0].WithNodeState("leaf", ChartSeriesState.Warning);
        var context = PreparedChordTests.Context();
        string Fill(PreparedVisual prepared) {
            var group = XDocument.Parse(prepared.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == groupRole && (string?)node.Attribute("data-cfx-target-id") == "leaf");
            Assert.Equal("Warning", (string?)group.Attribute("data-cfx-state"));
            return group.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == markRole).Attribute("fill")!.Value;
        }
        Assert.Equal(context.Theme.Resolve(context.ThemeMode).Status.Medium.Fill.ToCss(), Fill(chart.Prepare(context)));
        var authored = ChartColor.FromHex("#24567A"); chart.Series[0].WithPointColor(1, authored);
        Assert.Equal(authored.ToCss(), Fill(chart.Prepare(context)));
    }
    [Fact]
    public void SunburstRootNoneOverrideKeepsSeriesPaintProvenanceInHostVariables() {
        var chart = Chart.Create().AddSunburst("State", new[] { new ChartNode("root", "Root"), new ChartNode("leaf", "Leaf") },
            new[] { new ChartTreeLink("root", "leaf", 3) });
        chart.Series[0].StateRole = ChartSeriesState.Warning;
        chart.Series[0].WithNodeState("root", ChartSeriesState.None);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 460)));
        var blue = context.Theme.Resolve(context.ThemeMode).Palette[0];
        var variables = new SvgColorVariables().Add("--host-status", blue, SvgColorRole.Status)
            .Add("--host-series", blue, SvgColorRole.Series);
        var root = XDocument.Parse(chart.Prepare(context).ToSvg(new VisualSvgOptions("state", variables))).Descendants()
            .Single(node => (string?)node.Attribute("data-cfx-target-id") == "root");
        Assert.Equal("None", (string?)root.Attribute("data-cfx-state"));
        var fill = root.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "sunburst-segment-mark").Attribute("fill")!.Value;
        Assert.Contains("var(--host-series,", fill, StringComparison.Ordinal);
        Assert.DoesNotContain("var(--host-status,", fill, StringComparison.Ordinal);
    }

}
