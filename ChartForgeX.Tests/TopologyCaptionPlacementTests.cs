using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyCaptionPlacementTests {
    [Fact]
    public void WrappedCaptionKeepsItsLinesTogetherWithoutAnInterlineLeader() {
        const string family = "CFX Caption Placement";
        var font = OpenTypeTestFonts.NameKeyed(weight: 700);
        // Keep the frozen outline fixture and enlarge its ascent to reproduce a normal installed font's line metrics:
        // 13.2px at size 11 leaves less than the placement service's 2px gap between the authored 14px baselines.
        var tables = (font[4] << 8) | font[5];
        for (var i = 0; i < tables; i++) {
            var record = 12 + i * 16;
            if (System.Text.Encoding.ASCII.GetString(font, record, 4) != "hhea") continue;
            var at = (font[record + 8] << 24) | (font[record + 9] << 16) | (font[record + 10] << 8) | font[record + 11];
            font[at + 4] = 1000 >> 8; font[at + 5] = 1000 & 0xff;
        }
        FontRegistry.Register(family, font, weight: 700);
        try {
            var theme = TopologyTheme.Light(); theme.FontFamily = family;
            var chart = TopologyChart.Create().WithTheme(theme).AddNode("a", "HO\nOH", 100, 100, width: 64, height: 40);
            var svg = XDocument.Parse(chart.ToSvg(new TopologyRenderOptions {
                NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeLegend = false
            }));
            var caption = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "topology-node-label");
            var lines = caption.Elements().Where(e => e.Name.LocalName == "text").ToArray();
            Assert.Equal(new[] { "HO", "OH" }, lines.Select(e => e.Value));
            Assert.Equal(14, (double)lines[1].Attribute("y")! - (double)lines[0].Attribute("y")!, 3);
            Assert.All(lines, line => { Assert.Null(line.Attribute("transform")); Assert.NotEqual("none", (string?)line.Attribute("display")); });
            Assert.DoesNotContain(caption.Parent!.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "label-leader");
        } finally {
            FontRegistry.Clear();
        }
    }
}
