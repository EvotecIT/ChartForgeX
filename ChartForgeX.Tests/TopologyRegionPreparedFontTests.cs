using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyRegionPreparedFontTests {
    [Fact]
    public void FittingReplaysTheActualPrimaryAndFallbackGlyphRootsAfterRegistrationChanges() {
        const string primary = "CFX Region Primary"; const string fallback = "CFX Region Fallback";
        try {
            FontRegistry.Register(primary, OpenTypeTestFonts.NameKeyed(includePrivateUse: false));
            FontRegistry.Register(fallback, OpenTypeTestFonts.NameKeyed());
            var chart = TopologyChart.Create().WithViewport(860, 500, 20).WithLayout(TopologyLayoutMode.Regions)
                .WithTheme(VisualTheme.Graphite().WithFontFamily(primary + ", " + fallback))
                .AddAutoGroup("r", "HOx").AddAutoNode("a", "H\uEFFF", groupId: "r").AddAutoNode("b", "HO", groupId: "r").AddEdge("link", "a", "b", "HOx");
            var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
            var text = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Text.Lines.Count == 1 && node.Text.Lines[0].Text == "H\uEFFF").Text;
            var roots = text.Lines[0].Glyphs.Select(glyph => glyph.Face.Root).ToArray();
            Assert.Equal(2, roots.Distinct().Count());
            FontRegistry.Register(primary, OpenTypeTestFonts.NameKeyed());
            FontRegistry.Register(fallback, OpenTypeTestFonts.NameKeyed(includePrivateUse: false));
            var fitted = prepared.WithOutputSize(prepared.Width / 2, prepared.Height / 2);
            var replay = fitted.Visual.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Text.Lines.Count == 1 && node.Text.Lines[0].Text == "H\uEFFF").Text;
            Assert.Equal(text.Face, replay.Face);
            Assert.Equal(text.Size / 2, replay.Size, 6);
            Assert.Equal(roots.Length, replay.Lines[0].Glyphs.Count);
            for (var i = 0; i < roots.Length; i++) Assert.Same(roots[i], replay.Lines[0].Glyphs[i].Face.Root);
            Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
            var fresh = chart.Prepare().Visual.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Text.Lines.Count == 1 && node.Text.Lines[0].Text == "H\uEFFF").Text;
            Assert.Single(fresh.Lines[0].Glyphs.Select(glyph => glyph.Face.Root).Distinct());
        } finally { FontRegistry.Clear(); }
    }
}
