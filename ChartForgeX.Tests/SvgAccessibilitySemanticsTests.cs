using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgAccessibilitySemanticsTests {
    [Fact]
    public void SceneLabelsFollowStaticAndExplorableSvgSemantics() {
        var metadata = new Dictionary<string, string> {
            ["aria-label"] = "Passkey: 800 sign-ins", ["data-cfx-label"] = "800",
            ["data-cfx-series"] = "0", ["data-cfx-point"] = "0", ["data-cfx-y"] = "800"
        };
        var scene = new VisualScene(new VisualSize(100, 60), new VisualSceneNode[] {
            new VisualSceneGroup("point", "passkey", null, metadata), new VisualSceneEndGroup(),
            new VisualSceneGroup("series", "scalar", null, new Dictionary<string, string> { ["data-cfx-value"] = "87" }), new VisualSceneEndGroup()
        }, Array.Empty<VisualDiagnostic>(), new[] { new VisualSemanticRegion("scalar", "series", new ChartRect(0, 0, 100, 60), "Covered: 87%") });
        var root = XDocument.Parse(VisualSceneSvgRenderer.Render(scene, "Sign-ins", "Weekly methods", "en", idPrefix: "hero")).Root!;
        Assert.Equal("img", (string?)root.Attribute("role"));
        Assert.Equal("hero-title", (string?)root.Attribute("aria-labelledby"));
        Assert.Equal("hero-description", (string?)root.Attribute("aria-describedby"));
        Assert.Equal("Sign-ins", root.Elements().Single(node => node.Name.LocalName == "title").Value);
        Assert.Equal("Weekly methods", root.Elements().Single(node => node.Name.LocalName == "desc").Value);
        var mark = Assert.Single(root.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.Null(mark.Attribute("aria-label"));
        Assert.Equal("Passkey: 800 sign-ins", (string?)mark.Attribute("data-cfx-description"));
        Assert.Equal("800", (string?)mark.Attribute("data-cfx-label"));
        Assert.Equal("800", (string?)mark.Attribute("data-cfx-y"));
        Assert.Equal("0", (string?)mark.Attribute("data-cfx-series"));
        Assert.Equal("0", (string?)mark.Attribute("data-cfx-point"));
        Assert.Equal("Passkey: 800 sign-ins", metadata["aria-label"]);

        var options = new VisualSvgOptions().WithExplorableSemantics(true)
            .WithPatternTiles().WithTextPresentation(VisualSvgTextPresentation.Browser);
        var explored = XDocument.Parse(VisualSceneSvgRenderer.Render(scene, "Sign-ins", options: options)).Root!;
        Assert.Equal("graphics-document", (string?)explored.Attribute("role"));
        var named = Assert.Single(explored.Descendants(), node => node.Attribute("data-cfx-point") != null);
        Assert.Equal("group", (string?)named.Attribute("role"));
        Assert.Equal("Passkey: 800 sign-ins", (string?)named.Attribute("aria-label"));
        var scalar = Assert.Single(explored.Descendants(), node => (string?)node.Attribute("data-cfx-source-id") == "scalar");
        Assert.Equal("group", (string?)scalar.Attribute("role"));
        Assert.Equal("Covered: 87%", (string?)scalar.Attribute("aria-label"));

        foreach (var role in new[] { "group", "generic", "none", "presentation" }) {
            var facts = new Dictionary<string, string>(metadata) { ["role"] = role };
            var semantic = Scene(facts);
            var element = XDocument.Parse(VisualSceneSvgRenderer.Render(semantic)).Descendants()
                .Single(node => node.Attribute("data-cfx-point") != null);
            Assert.Equal(role == "group" ? "Passkey: 800 sign-ins" : null, (string?)element.Attribute("aria-label"));
        }
        var linked = XDocument.Parse(VisualSceneSvgRenderer.Render(Scene(metadata, "https://example.com/sign-ins"))).Root!;
        Assert.Equal("graphics-document", (string?)linked.Attribute("role"));
        var link = Assert.Single(linked.Descendants(), node => node.Name.LocalName == "a");
        Assert.Equal("0", (string?)link.Attribute("tabindex"));
        Assert.Equal("Passkey: 800 sign-ins", (string?)link.Attribute("aria-label"));

        var hidden = XDocument.Parse(VisualSceneSvgRenderer.Render(scene, decorative: true, options: options)).Root!;
        Assert.Equal("true", (string?)hidden.Attribute("aria-hidden"));
        Assert.Empty(hidden.DescendantsAndSelf().Attributes("aria-label"));
        Assert.Equal("Passkey: 800 sign-ins", Assert.Single(hidden.Descendants().Attributes("data-cfx-description")).Value);

        VisualScene Scene(IReadOnlyDictionary<string, string> facts, string? href = null) =>
            new(new VisualSize(100, 60), new VisualSceneNode[] {
                new VisualSceneGroup("point", "passkey", null, facts, href: href), new VisualSceneEndGroup()
            }, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
    }
}
