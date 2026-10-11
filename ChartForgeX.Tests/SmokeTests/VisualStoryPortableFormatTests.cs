using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    internal static void VisualStoriesRevealDeclaredOutcomesAcrossPortableFormats() {
        var source = StorySourceText.Create("Write-Output \"ready\"", "powershell")
            .AddSpan(0, 12, StorySyntaxKind.Command)
            .AddSpan(13, 7, StorySyntaxKind.String);
        var story = VisualStory.Create("Portable story")
            .WithDescription("A resolved source-to-result presentation.")
            .WithSize(480, 320);
        story.Scene("source", "Run the example", 0.25)
            .Panel("code", new VisualStorySourceSurface(source, "PowerShell source"));
        story.Scene("result", "See the result", 0.25, VisualStorySceneLayout.Split)
            .Panel("code", new VisualStorySourceSurface(source, "PowerShell source"))
            .Panel("result", new VisualStoryTextSurface("ready", emphasized: true))
            .Panel(
                "vector",
                new VisualStoryMediaSurface(
                    new ChartForgeX.Raster.RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }),
                    "Resolved vector preview",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><rect width=\"1\" height=\"1\" fill=\"none\"/></svg>"));
        story.Outcome("visible-result", "The result is visible", "result");

        var transcript = story.ToTranscript();
        var svg = story.ToSvg("portable");
        var html = story.ToHtmlPage();
        var png = story.ToPng();
        var options = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(4)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(4);
        var gif = story.ToGif(options);
        var apng = story.ToApng(options);

        Assert(transcript.Contains("Outcomes:", StringComparison.Ordinal) &&
               transcript.Contains("The result is visible", StringComparison.Ordinal) &&
               transcript.Contains("PowerShell source", StringComparison.Ordinal) &&
               transcript.Contains("Write-Output \"ready\"", StringComparison.Ordinal),
            "Visual-story transcripts should preserve promised outcomes, source captions, and source text.");
        Assert(svg.Contains("data-cfx-story=\"visual\"", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-scene=\"source\"", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-scene=\"result\"", StringComparison.Ordinal) &&
               svg.Contains("@media (prefers-reduced-motion:reduce)", StringComparison.Ordinal) &&
               svg.Contains("cfx-story-frame-last", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-motion-duration=\"2\"", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-motion-plays=\"0\"", StringComparison.Ordinal) &&
               svg.Contains("steps(1,end) infinite both", StringComparison.Ordinal) &&
               svg.Contains("data:image/svg+xml;base64,", StringComparison.Ordinal) &&
               !svg.Contains("<script", StringComparison.OrdinalIgnoreCase),
            "Visual-story SVG should use the prepared sampled clock and show its completed poster under reduced motion.");
        var nativePoster = XDocument.Parse(story.Prepare().ToSvg());
        var vector = nativePoster.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "story-vector-media");
        Assert(vector.Name.LocalName == "image" && vector.Attribute("href")!.Value.StartsWith("data:image/svg+xml;base64,", StringComparison.Ordinal),
            "SVG media should retain its resolved vector representation without layering the raster fallback over it.");
        Assert(html.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase) &&
               html.Contains("chartforgex-visual-story", StringComparison.Ordinal),
            "Visual stories should render complete responsive HTML pages.");
        Assert(png.Length > 8 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71,
            "Visual-story PNG should render the completed scene.");
        Assert(gif.Length > 8 && gif[0] == (byte)'G' && gif[1] == (byte)'I' && gif[2] == (byte)'F',
            "Visual stories should export animated GIF.");
        Assert(apng.Length > 128 && apng[0] == 137 && apng[1] == 80 && apng[2] == 78 && apng[3] == 71,
            "Visual stories should export animated PNG.");
        Assert(ReadImageDescriptors(gif).Length == 2 && ReadApngFrameControls(apng).Length == 2,
            "Animated story frame quantization should retain the completed endpoint without adding a full extra frame interval.");

        var longTransition = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(4)
            .WithTransition(1)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(4);
        var firstAnimatedFrame = GifReader.Decode(story.ToGif(longTransition));
        var noTransition = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(4)
            .WithTransition(0)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(4);
        var expectedFirstFrame = GifReader.Decode(story.ToGif(noTransition));
        Assert(firstAnimatedFrame.Pixels.SequenceEqual(expectedFirstFrame.Pixels),
            "A transition longer than its scene should still begin with the complete current scene.");
    }

}
