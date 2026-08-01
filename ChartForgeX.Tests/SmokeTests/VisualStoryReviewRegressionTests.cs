using System;
using System.Globalization;
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
        Assert(TerminalTextWidth.Elements("\u0600👩‍💻").Count() == 1 &&
               TerminalTextWidth.Measure("\u0600👩‍💻") == 2,
            "Extended pictographic sequences should remain joined after a Prepend scalar.");
        Assert(TerminalTextWidth.Elements("🇦\u0301🇧").Count() == 2,
            "An Extend scalar should reset regional-indicator pairing before the next indicator.");
        StorySourceText.Create("🇦\u0301🇧").AddSpan(0, 3, StorySyntaxKind.Variable);
        var unmatchedJoiner = "👩‍\u0301‍💻";
        Assert(TerminalTextWidth.Elements(unmatchedJoiner).Count() == 2,
            "An unmatched pictographic ZWJ must reset GB11 state before a later ZWJ.");
        StorySourceText.Create(unmatchedJoiner).AddSpan(0, 5, StorySyntaxKind.Variable);
        var longGrapheme = "x" + new string('\u0301', 65536);
        StorySourceText.Create(longGrapheme).AddSpan(0, longGrapheme.Length, StorySyntaxKind.Variable);

        foreach (var conditionalAttribute in new[] { "systemLanguage=\"pl\"", "requiredFeatures=\"feature\"", "requiredExtensions=\"extension\"" }) {
            AssertThrows<ArgumentException>(
                () => new VisualStoryMediaSurface(
                    new RgbaImage(1, 1, new byte[4]),
                    "Conditional vector",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><switch " + conditionalAttribute + "><rect width=\"1\" height=\"1\"/></switch></svg>"),
                "Vector media should reject locale- or capability-dependent SVG conditional content.");
        }
        foreach (var conditionalCss in new[] { "@media (prefers-color-scheme:dark){rect{fill:black}}", "@supports (display:grid){rect{fill:black}}" }) {
            AssertThrows<ArgumentException>(
                () => new VisualStoryMediaSurface(
                    new RgbaImage(1, 1, new byte[4]),
                    "Conditional vector",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><style>" + conditionalCss + "</style><rect width=\"1\" height=\"1\"/></svg>"),
                "Vector media should reject environment-dependent CSS at-rules.");
        }
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Conditional vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><style media=\"(prefers-color-scheme: dark)\">rect{fill:black}</style><rect width=\"1\" height=\"1\"/></svg>"),
            "Vector media should reject environment-dependent style media attributes.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Nested style vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><style>@im<style/>port \"https://example.invalid/x.css\";</style></svg>"),
            "Vector media should reject nested style elements instead of losing the outer stylesheet buffer.");
        var staticCssIdentifiers = new VisualStoryMediaSurface(
            new RgbaImage(1, 1, new byte[4]),
            "Static CSS identifiers",
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><style>.animation-status,.transition-note,.behavior-label{fill:red}</style><rect class=\"animation-status\" width=\"1\" height=\"1\"/></svg>");
        Assert(staticCssIdentifiers.Svg.Length > 0,
            "Static SVG class names containing active-property words should remain valid.");
        foreach (var activeProperty in new[] { "animation-name:spin", "transition:fill 1s", "-webkit-animation:spin 1s", "anim/**/ation:spin 1s" }) {
            AssertThrows<ArgumentException>(
                () => new VisualStoryMediaSurface(
                    new RgbaImage(1, 1, new byte[4]),
                    "Active CSS property",
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><style>rect{" + activeProperty + "}</style><rect width=\"1\" height=\"1\"/></svg>"),
                "Vector media should reject active CSS properties after token and comment normalization.");
        }
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Quoted CSS comment opener",
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><style>.x{content:\"/*\";animation:p 1s infinite}@keyframes p{to{opacity:0}}</style><rect class=\"x\"/></svg>"),
            "CSS comment stripping should preserve quoted text and still detect following active declarations.");

        var boundedOutcomeStory = VisualStory.Create("Bounded outcome").WithSize(480, 320);
        boundedOutcomeStory.Scene("valid", "Completed")
            .Panel("valid", new VisualStoryTextSurface("ready"));
        AssertThrows<ArgumentOutOfRangeException>(
            () => boundedOutcomeStory.Outcome("oversized", new string('x', 513), "valid"),
            "Outcome labels should be bounded before raster renderers aggregate badge text.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => VisualStory.Create(new string('x', VisualStorySurface.MaximumHeadingLength + 1)),
            "Story headings should be bounded before raster measurement.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => VisualStory.Create("Bounded heading").Scene("scene", new string('x', VisualStorySurface.MaximumHeadingLength + 1)),
            "Scene headings should be bounded before raster measurement.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => VisualStory.Create("Bounded heading").Scene("scene", "Scene").Panel(
                "panel",
                new VisualStoryTextSurface("ready"),
                new string('x', VisualStorySurface.MaximumHeadingLength + 1)),
            "Panel headings should be bounded before raster measurement.");

        var endpointScenes = VisualStory.Create("Endpoint scenes").WithSize(480, 320);
        endpointScenes.Scene("first", "First", 0.25)
            .Panel("first-result", new VisualStoryTextSurface("first"));
        endpointScenes.Scene("last", "Last", 0.25)
            .Panel("last-result", new VisualStoryTextSurface("last"));
        endpointScenes.Outcome("ready", "Ready", "last-result");
        AssertThrows<InvalidOperationException>(
            () => endpointScenes.ToGif(
                VisualStoryAnimationOptions.Create()
                    .WithFramesPerSecond(2)
                    .WithEndHold(0)
                    .WithMaximumFrames(2)),
            "Raster stories should reject endpoint sampling when residual timing makes the completed scene effectively invisible.");
        Assert(endpointScenes.ToGif(
                VisualStoryAnimationOptions.Create()
                    .WithFramesPerSecond(4)
                    .WithEndHold(0)
                    .WithMaximumFrames(2)).Length > 8,
            "Raster stories should retain short endpoint scenes when both receive their requested visible duration.");

        var joinedText = StorySourceText.Create("a\u200Db");
        AssertThrows<ArgumentException>(
            () => joinedText.AddSpan(0, 1, StorySyntaxKind.Variable),
            "Syntax spans should keep a ZWJ attached to the preceding grapheme even outside emoji sequences.");
        joinedText.AddSpan(0, 2, StorySyntaxKind.Variable);
        var nonJoiningText = StorySourceText.Create("a\u200Cb");
        AssertThrows<ArgumentException>(
            () => nonJoiningText.AddSpan(0, 1, StorySyntaxKind.Variable),
            "Syntax spans should keep a ZWNJ attached to the preceding grapheme.");
        nonJoiningText.AddSpan(0, 2, StorySyntaxKind.Variable);

        foreach (var environmentPaint in new[] {
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\" color-scheme=\"light dark\"><rect fill=\"CanvasText\"/></svg>",
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><style>rect{fill:Can\\76 asText}</style><rect/></svg>"
        }) {
            AssertThrows<ArgumentException>(
                () => new VisualStoryMediaSurface(new RgbaImage(1, 1, new byte[4]), "System color", environmentPaint),
                "Vector media should reject environment-dependent color schemes and escaped system colors.");
        }
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "External CSS image",
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\" style=\"background-image:im/**/age-set('https://example.invalid/a.png' 1x)\"><rect/></svg>"),
            "Vector media should reject CSS image-source functions, including string-valued external references after comment normalization.");

        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(100, 100, new byte[100 * 100 * 4]),
                "Mismatched vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 9\"><rect width=\"16\" height=\"9\"/></svg>"),
            "Raster and vector media representations should reject incompatible intrinsic aspect ratios.");
        var matchingVector = new VisualStoryMediaSurface(
            new RgbaImage(100, 50, new byte[100 * 50 * 4]),
            "Matching vector",
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"100\"><rect width=\"200\" height=\"100\"/></svg>");
        Assert(matchingVector.Svg.Length > 0,
            "Vector media should accept explicit intrinsic dimensions with the raster aspect ratio.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => new VisualStoryTextSurface(new string('x', 1024 * 1024 + 1)),
            "Text surfaces should reject payloads that exceed their bounded raster-layout contract.");

        foreach (var separator in new[] { '\r', '\n', '\u000B', '\u000C', '\u0085', '\u2028', '\u2029' }) {
            AssertThrows<ArgumentException>(
                () => VisualStory.Create("before" + separator + "after"),
                "Single-line story fields should reject every semantic Unicode line separator.");
        }

        var revealStory = TerminalStory.Create()
            .WithTiming(0, 200, 0)
            .WithFinalPrompt(false)
            .Output("ready")
            .OpenTab("ubuntu", "Ubuntu", TerminalDialect.Bash, "~", TerminalTheme.Ubuntu(), transitionSeconds: 0)
            .Table(TerminalTable.Create().WithColumns("State").AddRow("complete"))
            .SelectTab("main", transitionSeconds: 0);
        var revealLayout = TerminalStoryLayout.Build(revealStory);
        var mainRevealEnd = revealLayout.Lines
            .Where(line => line.TabId == "main")
            .Max(line => line.StartSeconds + line.DurationSeconds);
        var ubuntuRevealEnd = revealLayout.Lines
            .Where(line => line.TabId == "ubuntu")
            .Max(line => line.StartSeconds + line.DurationSeconds);
        Assert(revealLayout.Transitions[0].StartSeconds >= mainRevealEnd &&
               revealLayout.Transitions[1].StartSeconds >= ubuntuRevealEnd,
            "Tab switches should wait for output and table reveals even when line and transition delays are zero.");
        var zeroTransitionSvg = revealStory.ToSvg();
        var firstTransition = revealLayout.Transitions[0];
        var epsilon = Math.Max(0.000001, revealLayout.DurationSeconds * 0.00000002);
        var beforePercentage = Math.Max(0, firstTransition.StartSeconds - epsilon) / revealLayout.DurationSeconds * 100;
        var startPercentage = firstTransition.StartSeconds / revealLayout.DurationSeconds * 100;
        var beforeToken = beforePercentage.ToString("0.######", CultureInfo.InvariantCulture) + "%{opacity:1}";
        var startToken = startPercentage.ToString("0.######", CultureInfo.InvariantCulture) + "%{opacity:0}";
        Assert(zeroTransitionSvg.Contains(beforeToken + startToken, StringComparison.Ordinal),
            "Zero-duration SVG tab switches should retain the old state immediately before the scheduled instant and jump at the instant.");

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

        var tallTerminal = TerminalStory.Create()
            .WithWidth(1800)
            .WithTypography(24, 40)
            .WithTiming(0, 200, 0)
            .WithFinalPrompt(false);
        for (var index = 0; index < 120; index++) {
            tallTerminal.Output("line " + index.ToString(CultureInfo.InvariantCulture));
        }
        var nestedTerminalStory = VisualStory.Create("Nested terminal budget").WithSize(1400, 788);
        nestedTerminalStory.Scene("result", "Completed", 0.25)
            .Panel("result", new VisualStoryTerminalSurface(tallTerminal));
        nestedTerminalStory.Outcome("ready", "Ready", "result");
        AssertThrows<InvalidOperationException>(
            () => nestedTerminalStory.ToGif(
                VisualStoryAnimationOptions.Create()
                    .WithFramesPerSecond(4)
                    .WithEndHold(0)
                    .WithOutputScale(2)
                    .WithMaximumFrames(2)),
            "Animated-story memory gates should include fitted terminal supersampling and output buffers before rendering scenes.");
    }
}
