using ChartForgeX.Mermaid;
using ChartForgeX.Stories;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class ConcurrentRenderDeterminismTests {
    [Fact]
    public async Task SameContentExportsIdenticalBytesWhileFontCachesAreRebuilt() {
        var expected = Render();
        using var stop = new CancellationTokenSource();
        // Clearing the registry rebuilds resolved stacks and loaded files; many weights evict cached face views,
        // so concurrent renders receive new instances of the same faces part way through preparation.
        var churn = Task.Run(() => {
            for (var weight = 100; !stop.IsCancellationRequested; weight = weight >= 900 ? 101 : weight + 7) {
                FontRegistry.Clear();
                for (var i = 0; i < 40; i++) TypographyFontResolver.ResolveFace(BrowserProofFonts.Family, weight + i, false);
            }
        });
        Rendered[] actual;
        try { actual = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(Render))); }
        finally { stop.Cancel(); await churn; }
        Assert.All(actual, rendered => {
            Assert.Equal(expected.Svg, rendered.Svg);
            Assert.Equal(expected.Png, rendered.Png);
            Assert.Equal(expected.Gif, rendered.Gif);
        });
    }

    private static Rendered Render() {
        var artifact = MermaidRenderer.Render(MermaidClassErCaptionTests.Source("er"), MermaidClassErCaptionTests.Options(true, 390)).Artifact!;
        var story = VisualStory.Create("One GIF clock").WithSize(480, 320);
        story.Scene("result", "Result", 9).Panel("result", new VisualStoryTextSurface("Ready"));
        story.Outcome("ready", "Ready", "result");
        var gif = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(1), TimeSpan.Zero, 1)).ToGif(new VisualStoryFrameOptions(6));
        return new Rendered(artifact.ToSvg(), artifact.ToPng(), gif);
    }

    private sealed record Rendered(string Svg, byte[] Png, byte[] Gif);
}
