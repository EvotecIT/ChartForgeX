using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Dense exports share rectangular definitions while retaining each drawing and its clip scope.</summary>
public sealed class SharedSvgClipTests {
    [Fact]
    public void RepeatedAndNestedClipsKeepScopedReferencesAndDistinctGeometry() {
        var builder = new VisualSceneBuilder(new VisualSize(80, 60), FontSpec.SystemSans());
        var outer = new ChartRect(10, 10, 60, 40);
        for (var index = 0; index < 960; index++) {
            using (builder.PushClip(outer)) {
                builder.Rect(new ChartRect(0, 0, 80, 60), ChartColor.Black, role: "cell");
                if (index == 0) {
                    using (builder.PushClip(new ChartRect(20, 20, 20, 20)))
                        builder.Rect(new ChartRect(0, 0, 80, 60), ChartColor.White, role: "nested-cell");
                }
            }
        }
        var scene = builder.Build();
        var first = XDocument.Parse(VisualSceneSvgRenderer.Render(scene, idPrefix: "first"));
        var second = XDocument.Parse(VisualSceneSvgRenderer.Render(scene, idPrefix: "second"));
        foreach (var document in new[] { first, second }) {
            var clips = document.Descendants().Where(element => element.Name.LocalName == "clipPath").ToArray();
            Assert.Equal(2, clips.Length);
            var identities = clips.Select(element => (string)element.Attribute("id")!).ToHashSet();
            var references = document.Descendants().Attributes("clip-path").Select(attribute => attribute.Value[5..^1]).ToArray();
            Assert.Equal(961, references.Length);
            Assert.All(references, reference => Assert.Contains(reference, identities));
            Assert.Equal(960, document.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "cell"));
        }
        Assert.Empty(first.Descendants().Attributes("id").Select(attribute => attribute.Value)
            .Intersect(second.Descendants().Attributes("id").Select(attribute => attribute.Value)));
    }
}
