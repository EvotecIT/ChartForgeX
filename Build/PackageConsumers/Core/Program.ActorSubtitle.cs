using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Topology;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

internal static partial class Program {
    private static void NativeActorSubtitleContracts() {
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(PackageAssertions.OutlineFontFamily)).WithId("package-actor-caption").WithViewport(700, 520, 24)
            .WithLayout(TopologyLayoutMode.Layered)
            .AddAutoNode("author", "Release coordinator", width: 180, height: 110, subtitle: "primary actor")
            .AddAutoNode("publish", "Publish documents", width: 180, height: 80)
            .AddEdge("request", "author", "publish", "requests", routing: TopologyEdgeRouting.Straight);
        var actor = chart.Nodes[0]; actor.Shape = TopologyNodeShape.Actor; actor.ShowStatusBadge = false;
        actor.PreserveDisplayModeSize = true; actor.DisplayMode = TopologyNodeDisplayMode.Card;
        actor.TextStyle = new TextStyleOverride { FontSize = 16, FontWeight = "500" };
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var document = XDocument.Parse(svg);
        PackageAssertions.Require(document.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "topology-node-subtitle"),
            "Packed Core Actor omitted its native subtitle owner.");
        var captions = document.Descendants().Where(node => node.Attribute("data-cfx-caption") != null).ToArray();
        foreach (var caption in captions) {
            var glyphs = caption.Descendants().Where(node => node.Name.LocalName == "use" && node.Ancestors().Any(parent => parent.Attribute("data-cfx-native-text-ink") != null)).ToArray();
            PackageAssertions.Require(glyphs.Length != 0 && glyphs.All(glyph => document.Descendants().Any(target =>
                "#" + (string?)target.Attribute("id") == (string?)glyph.Attribute("href")))
                && glyphs.Any(glyph => document.Descendants().Any(target =>
                    "#" + (string?)target.Attribute("id") == (string?)glyph.Attribute("href") && target.DescendantsAndSelf().Any(path =>
                        path.Name.LocalName == "path" && !string.IsNullOrEmpty((string?)path.Attribute("d"))))),
                "Packed Actor contour captions have no resolved native glyph ink.");
            var captionWords = (string)caption.Attribute("data-cfx-caption")!;
            if (captionWords == "Release coordinator" || captionWords == "primary actor") {
                // These finite Latin fixtures have one glyph per character. Only their
                // authored spaces may resolve to an empty outline.
                PackageAssertions.Require(glyphs.Length == captionWords.Length, "Packed Actor lost a finite caption glyph.");
                for (var index = 0; index < glyphs.Length; index++) {
                    var target = document.Descendants().Single(path => "#" + (string?)path.Attribute("id") == (string?)glyphs[index].Attribute("href"));
                    var painted = target.DescendantsAndSelf().Any(path => path.Name.LocalName == "path" && !string.IsNullOrEmpty((string?)path.Attribute("d")));
                    PackageAssertions.Require(painted != char.IsWhiteSpace(captionWords[index]), "Packed Actor lost a non-whitespace glyph or fabricated space ink.");
                }
            }
        }
        var words = string.Join(" ", document.Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value)
            .Concat(captions.Select(node => (string)node.Attribute("data-cfx-caption")!)));
        PackageAssertions.Require(words.Contains("primary actor") && words.Contains("coordinator"), "Packed Actor lost complete native caption words.");
        PackageAssertions.Require(prepared.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "author").Subtitle == "primary actor",
            "Packed Actor subtitle was not retained as a source fact.");
        actor.Subtitle = "changed"; actor.TextStyle.FontSize = 60; chart.Edges.Clear(); chart.Nodes.Clear();
        PackageAssertions.Require(prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png), "Packed Actor exports retained a mutable model or typography dependency.");
        PackageAssertions.Png(png);
    }
}
