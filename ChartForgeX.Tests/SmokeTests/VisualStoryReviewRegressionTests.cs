using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualStoryReviewRegressionsRemainBoundedAndDeterministic() {
        StorySourceText.Create("\n\u0301value").AddSpan(0, 1, StorySyntaxKind.Keyword);
        Assert(TerminalTextWidth.Elements("\n\u0301").Count() == 2,
            "Grapheme controls should break before following combining marks.");

        foreach (var conditionalAttribute in new[] { "systemLanguage=\"pl\"", "requiredFeatures=\"feature\"", "requiredExtensions=\"extension\"" }) {
            AssertThrows<ArgumentException>(
                () => new VisualStoryMediaSurface(
                    new RgbaImage(1, 1, new byte[4]),
                    "Conditional vector",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\"><switch " + conditionalAttribute + "><rect width=\"1\" height=\"1\"/></switch></svg>"),
                "Vector media should reject locale- or capability-dependent SVG conditional content.");
        }

        var longTabbedStory = VisualStory.Create("Bounded tab expansion")
            .WithSize(480, 320);
        longTabbedStory.Scene("result", "Completed")
            .Panel(
                "result",
                new VisualStorySourceSurface(
                    StorySourceText.Create(new string('x', 1024 * 1024) + "\tremaining", "text")));
        longTabbedStory.Outcome("ready", "Ready", "result");
        Assert(longTabbedStory.ToPng().Length > 200,
            "Source rendering should stop tab expansion after the visible prefix of a large line.");

        var gifPixelCount = 480L * 320;
        var oneFrameGifBudget = AnimatedRasterMemoryBudget.EncoderRetainedBytes(
            480,
            320,
            1,
            AnimatedRasterFormat.Gif);
        Assert(oneFrameGifBudget >= gifPixelCount * 3,
            "GIF memory estimates should reserve the retained frame plus simultaneous previous/current optimization arrays.");
    }
}
