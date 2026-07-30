using System;
using System.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Stories;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualStoriesRevealDeclaredOutcomesAcrossPortableFormats() {
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
                    new ChartForgeX.Raster.RgbaImage(1, 1, new byte[] { 34, 197, 94, 255 }),
                    "Resolved vector preview",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"1\" height=\"1\" fill=\"#22c55e\"/></svg>"));
        story.Outcome("visible-result", "The result is visible", "result");

        var transcript = story.ToTranscript();
        var svg = story.ToSvg("portable");
        var html = story.ToHtmlPage();
        var png = story.ToPng();
        var options = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(2)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(4);
        var gif = story.ToGif(options);
        var apng = story.ToApng(options);

        Assert(transcript.Contains("Outcomes:", StringComparison.Ordinal) &&
               transcript.Contains("The result is visible", StringComparison.Ordinal),
            "Visual-story transcripts should preserve promised outcomes.");
        Assert(svg.Contains("data-cfx-story=\"visual\"", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-scene=\"source\"", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-scene=\"result\"", StringComparison.Ordinal) &&
               svg.Contains("@media (prefers-reduced-motion:reduce)", StringComparison.Ordinal) &&
               svg.Contains("cfx-story-scene-last", StringComparison.Ordinal) &&
               svg.Contains("-motion-scene-0", StringComparison.Ordinal) &&
               svg.Contains("0%{opacity:1}50%{opacity:1}51.2%{opacity:0}", StringComparison.Ordinal) &&
               svg.Contains("50%{opacity:0}51.2%{opacity:1}", StringComparison.Ordinal) &&
               !svg.Contains("cfx-story-seed-", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-role=\"story-vector-media\"", StringComparison.Ordinal) &&
               svg.Contains("data:image/svg+xml;base64,", StringComparison.Ordinal) &&
               !svg.Contains("<script", StringComparison.OrdinalIgnoreCase),
            "Visual-story SVG should be self-contained, script-free, animated, and completed under reduced motion.");
        Assert(html.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase) &&
               html.Contains("chartforgex-visual-story", StringComparison.Ordinal),
            "Visual stories should render complete responsive HTML pages.");
        Assert(png.Length > 8 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71,
            "Visual-story PNG should render the completed scene.");
        Assert(gif.Length > 8 && gif[0] == (byte)'G' && gif[1] == (byte)'I' && gif[2] == (byte)'F',
            "Visual stories should export animated GIF.");
        Assert(apng.Length > 128 && apng[0] == 137 && apng[1] == 80 && apng[2] == 78 && apng[3] == 71,
            "Visual stories should export animated PNG.");

        var longTransition = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(2)
            .WithTransition(1)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(4);
        var firstAnimatedFrame = GifReader.Decode(story.ToGif(longTransition));
        var noTransition = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(2)
            .WithTransition(0)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(4);
        var expectedFirstFrame = GifReader.Decode(story.ToGif(noTransition));
        Assert(firstAnimatedFrame.Pixels.SequenceEqual(expectedFirstFrame.Pixels),
            "A transition longer than its scene should still begin with the complete current scene.");
    }

    private static void VisualStoriesRejectUnrevealedOutcomesAndInvalidSyntaxSpans() {
        var noOutcome = VisualStory.Create("Missing outcome").WithSize(480, 320);
        noOutcome.Scene("only", "Only scene").Panel("text", new VisualStoryTextSurface("Nothing promised"));
        AssertThrows<InvalidOperationException>(() => noOutcome.ToSvg(), "Visual stories should require a declared outcome.");

        var hiddenOutcome = VisualStory.Create("Hidden outcome").WithSize(480, 320);
        hiddenOutcome.Scene("start", "Start").Panel("result", new VisualStoryTextSurface("visible early"));
        hiddenOutcome.Scene("end", "End").Panel("summary", new VisualStoryTextSurface("missing result"));
        hiddenOutcome.Outcome("result", "Visible result", "result");
        AssertThrows<InvalidOperationException>(() => hiddenOutcome.ToPng(), "Completed scenes should reveal every promised outcome.");

        var source = StorySourceText.Create("😀 value");
        AssertThrows<ArgumentException>(() => source.AddSpan(1, 1, StorySyntaxKind.Variable), "Syntax spans should not split surrogate pairs.");
        source.AddSpan(3, 5, StorySyntaxKind.Variable);
        AssertThrows<ArgumentException>(() => source.AddSpan(2, 2, StorySyntaxKind.Keyword), "Syntax spans should be ordered and non-overlapping.");

        var unicodeStory = VisualStory.Create("Unicode clipping").WithSize(480, 320);
        unicodeStory.Scene("result", "Completed")
            .Panel("result", new VisualStorySourceSurface(
                StorySourceText.Create("😀😀😀😀😀😀😀😀😀😀 result", "text"),
                "Unicode source"));
        unicodeStory.Outcome("unicode", "Unicode source remains renderable", "result");
        var unicodePng = unicodeStory.ToPng();
        Assert(unicodePng.Length > 8 && unicodePng[0] == 137,
            "Source clipping should preserve complete Unicode text elements.");

        var crowded = VisualStory.Create("Crowded stack").WithSize(480, 320);
        var crowdedScene = crowded.Scene("result", "Completed", 0.25, VisualStorySceneLayout.Stacked);
        for (var index = 0; index < 4; index++) {
            crowdedScene.Panel(
                "panel-" + index,
                new VisualStoryTextSurface("value"),
                "Panel " + index);
        }
        crowded.Outcome("visible", "A result is visible", "panel-3");
        AssertThrows<InvalidOperationException>(
            () => crowded.ToPng(),
            "Stories should reject stacked layouts that cannot provide a positive panel content area.");

        var lightStory = VisualStory.Create("Light documentation")
            .WithSize(480, 320)
            .WithTheme(VisualStoryTheme.Light());
        lightStory.Scene("result", "Completed", 0.25)
            .Panel("result", new VisualStoryTextSurface("ready", emphasized: true));
        lightStory.Outcome("ready", "The result is visible", "result");
        var lightHtml = lightStory.ToHtmlPage();
        Assert(lightHtml.Contains("color-scheme:light", StringComparison.Ordinal) &&
               lightHtml.Contains("background:#E9EEF5", StringComparison.Ordinal) &&
               lightHtml.Contains("linear-gradient(180deg", StringComparison.Ordinal) &&
               lightHtml.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal) &&
               lightHtml.Contains("overflow:visible", StringComparison.Ordinal) &&
               lightHtml.Contains("@media print", StringComparison.Ordinal) &&
               lightHtml.Contains("width:min(480px,100%)", StringComparison.Ordinal),
            "Complete visual-story pages should honor light themes and the configured story width.");
    }
}
