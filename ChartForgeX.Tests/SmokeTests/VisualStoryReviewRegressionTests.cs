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
        Assert(TerminalTextWidth.Elements("\u0600🇦🇧").Count() == 1 &&
               TerminalTextWidth.Measure("\u0600🇦🇧") == 2,
            "Regional indicators should remain paired after a Prepend scalar.");

        foreach (var conditionalAttribute in new[] { "systemLanguage=\"pl\"", "requiredFeatures=\"feature\"", "requiredExtensions=\"extension\"" }) {
            AssertThrows<ArgumentException>(
                () => new VisualStoryMediaSurface(
                    new RgbaImage(1, 1, new byte[4]),
                    "Conditional vector",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\"><switch " + conditionalAttribute + "><rect width=\"1\" height=\"1\"/></switch></svg>"),
                "Vector media should reject locale- or capability-dependent SVG conditional content.");
        }
        foreach (var conditionalCss in new[] { "@media (prefers-color-scheme:dark){rect{fill:black}}", "@supports (display:grid){rect{fill:black}}" }) {
            AssertThrows<ArgumentException>(
                () => new VisualStoryMediaSurface(
                    new RgbaImage(1, 1, new byte[4]),
                    "Conditional vector",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\"><style>" + conditionalCss + "</style><rect width=\"1\" height=\"1\"/></svg>"),
                "Vector media should reject environment-dependent CSS at-rules.");
        }
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Conditional vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><style media=\"(prefers-color-scheme: dark)\">rect{fill:black}</style><rect width=\"1\" height=\"1\"/></svg>"),
            "Vector media should reject environment-dependent style media attributes.");

        var joinedText = StorySourceText.Create("a\u200Db");
        AssertThrows<ArgumentException>(
            () => joinedText.AddSpan(0, 1, StorySyntaxKind.Variable),
            "Syntax spans should keep a ZWJ attached to the preceding grapheme even outside emoji sequences.");
        joinedText.AddSpan(0, 2, StorySyntaxKind.Variable);

        var denseSource = StorySourceText.Create(new string('x', 1024 * 1024) + new string('y', 4096));
        for (var index = 0; index < 4096; index++) {
            denseSource.AddSpan(1024 * 1024 + index, 1, StorySyntaxKind.Variable);
        }
        denseSource.Validate();
        Assert(denseSource.Spans.Count == 4096,
            "Syntax-span boundary validation should advance linearly through the source instead of rescanning its prefix per span.");

        var invalidScalarStory = VisualStory.Create("Broken\0\uD800 title").WithSize(480, 320);
        invalidScalarStory.Scene("result", "Completed")
            .Panel("result", new VisualStoryTextSurface("Visible result"));
        invalidScalarStory.Outcome("ready", "Ready", "result");
        var invalidScalarHtml = invalidScalarStory.ToHtmlFragment();
        Assert(!invalidScalarHtml.Contains('\0') &&
               !invalidScalarHtml.Contains('\uD800') &&
               invalidScalarHtml.Contains('\uFFFD'),
            "HTML story output should replace NUL and unpaired surrogate input with the Unicode replacement character.");

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
