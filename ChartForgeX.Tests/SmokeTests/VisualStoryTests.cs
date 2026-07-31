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
               svg.Contains("cfx-story-scene-last", StringComparison.Ordinal) &&
               svg.Contains("-motion-scene-0", StringComparison.Ordinal) &&
               svg.Contains(".cfx-story-scene-0{opacity:0;animation:", StringComparison.Ordinal) &&
               svg.Contains(".cfx-story-scene-1{opacity:1;animation:", StringComparison.Ordinal) &&
               svg.Contains("0%{opacity:1}0.5%{opacity:1}12.5%{opacity:0}100%{opacity:0}", StringComparison.Ordinal) &&
               svg.Contains("0%{opacity:0}0.5%{opacity:0}12.5%{opacity:1}100%{opacity:1}", StringComparison.Ordinal) &&
               svg.Contains("animation:", StringComparison.Ordinal) &&
               svg.Contains(" 2s linear infinite both", StringComparison.Ordinal) &&
               svg.Contains("mix-blend-mode:plus-lighter", StringComparison.Ordinal) &&
               svg.Contains("isolation:isolate", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-motion-duration=\"2\"", StringComparison.Ordinal) &&
               !svg.Contains("animation-timing-function:steps(1,end)", StringComparison.Ordinal) &&
               !svg.Contains("cfx-story-seed-", StringComparison.Ordinal) &&
               svg.Contains("data-cfx-role=\"story-vector-media\"", StringComparison.Ordinal) &&
               svg.Contains("data:image/svg+xml;base64,", StringComparison.Ordinal) &&
               !svg.Contains("<script", StringComparison.OrdinalIgnoreCase),
            "Visual-story SVG should be self-contained, script-free, animated, and completed under reduced motion.");
        var embeddedPngStart = svg.IndexOf("data:image/png;base64,", StringComparison.Ordinal);
        Assert(embeddedPngStart >= 0, "Visual-story SVG should contain a raster scene base.");
        embeddedPngStart += "data:image/png;base64,".Length;
        var embeddedPngEnd = svg.IndexOf('"', embeddedPngStart);
        var embeddedScene = PngReader.Decode(Convert.FromBase64String(svg.Substring(
            embeddedPngStart,
            embeddedPngEnd - embeddedPngStart)));
        Assert(CountNearColorInRect(
                embeddedScene.Pixels,
                embeddedScene.Width,
                0,
                0,
                embeddedScene.Width,
                embeddedScene.Height,
                255,
                0,
                0,
                0) == 0,
            "SVG vector media should replace its raster representation instead of being layered over it.");
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
        var combiningSource = StorySourceText.Create("e\u0301 value");
        AssertThrows<ArgumentException>(
            () => combiningSource.AddSpan(0, 1, StorySyntaxKind.Keyword),
            "Syntax spans should not split a base character from its combining marks.");
        combiningSource.AddSpan(0, 2, StorySyntaxKind.Keyword);
        var joinedEmojiSource = StorySourceText.Create("👩‍💻 value");
        AssertThrows<ArgumentException>(
            () => joinedEmojiSource.AddSpan(0, 2, StorySyntaxKind.Type),
            "Syntax spans should not split emoji ZWJ sequences.");
        joinedEmojiSource.AddSpan(0, 5, StorySyntaxKind.Type);

        var whitespaceSource = StorySourceText.Create("  indented source  ", "text");
        var whitespaceSurface = new VisualStorySourceSurface(whitespaceSource);
        Assert(string.Equals(
                whitespaceSurface.AccessibleText,
                "Language: text" + Environment.NewLine + whitespaceSource.Text,
                StringComparison.Ordinal),
            "Captionless source accessibility text should declare its language and preserve exact source whitespace.");
        whitespaceSource.WithLanguage("powershell");
        Assert(whitespaceSurface.AccessibleText.StartsWith("Language: powershell" + Environment.NewLine, StringComparison.Ordinal),
            "Source accessibility text should reflect language metadata mutations visible in the retained source presentation.");

        AssertThrows<ArgumentException>(
            () => VisualStory.Create("First line\nSecond line"),
            "Story titles should reject line breaks because renderers present them as one-line headings.");
        var headingStory = VisualStory.Create("Single-line headings").WithSize(480, 320);
        AssertThrows<ArgumentException>(
            () => headingStory.Scene("invalid", "First line\nSecond line"),
            "Scene titles should reject line breaks because renderers present them as one-line headings.");
        var headingScene = headingStory.Scene("valid", "Completed");
        AssertThrows<ArgumentException>(
            () => headingScene.Panel("invalid", new VisualStoryTextSurface("content"), "First line\nSecond line"),
            "Panel titles should reject line breaks because renderers present them as one-line headings.");
        AssertThrows<ArgumentException>(
            () => headingStory.Outcome("invalid", "First line\nSecond line", "valid"),
            "Outcome labels should reject line breaks because renderers present them as one-line badges.");

        var accessibleTerminal = TerminalStory.Create()
            .WithFinalPrompt(false)
            .Command("Get-Widget")
            .Output("Widget is ready");
        var terminalSurface = new VisualStoryTerminalSurface(accessibleTerminal, "Console output");
        Assert(terminalSurface.AccessibleText.Contains("Console output", StringComparison.Ordinal) &&
               terminalSurface.AccessibleText.Contains(accessibleTerminal.Prompt() + "Get-Widget", StringComparison.Ordinal) &&
               terminalSurface.AccessibleText.Contains("Widget is ready", StringComparison.Ordinal),
            "Terminal accessibility text should combine its optional heading with the deterministic command and output transcript.");
        accessibleTerminal.Output("Mutation remains accessible");
        Assert(terminalSurface.AccessibleText.Contains("Mutation remains accessible", StringComparison.Ordinal),
            "Terminal accessibility text should reflect mutations visible in the retained terminal presentation.");

        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Invalid vector",
                "<not-svg/>"),
            "Vector media should reject malformed or non-SVG replacements before suppressing the raster fallback.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Animated vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><animate attributeName=\"opacity\" values=\"0;1\"/></svg>"),
            "Vector media should reject SMIL animation that cannot follow the parent story timeline.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Stylesheet instruction",
                "<?xml-stylesheet href=\"data:text/css,rect%7Bfill:red%7D\"?><svg xmlns=\"http://www.w3.org/2000/svg\"><rect/></svg>"),
            "Vector media should reject processing instructions that could load active styling before the SVG root.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Styled vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><style>@keyframes pulse{to{opacity:0}}</style><rect/></svg>"),
            "Vector media should reject CSS animation that cannot follow reduced-motion and print rules.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Escaped external stylesheet",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><style>@\\69 mport \"https://example.invalid/a.css\";</style><rect/></svg>"),
            "Vector media should decode CSS escapes before screening active or external styles.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                default(RgbaImage),
                "Missing raster"),
            "Media surfaces should reject default RGBA values before rendering.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Scripted vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"><rect/></svg>"),
            "Vector media should reject event handlers before embedding SVG content.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Remote vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"https://example.invalid/image.png\"/></svg>"),
            "Vector media should reject external resource references so story output stays deterministic and self-contained.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "External base vector",
                "<svg xmlns=\"http://www.w3.org/2000/svg\" xml:base=\"https://example.invalid/media.svg\"><use href=\"#shape\"/></svg>"),
            "Vector media should reject xml:base attributes that can turn fragment references into external resources.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Remote presentation resource",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect filter=\"url(https://example.invalid/filter.svg#f)\"/></svg>"),
            "Vector media should reject external functional IRIs in SVG presentation attributes.");
        var animatedDataImage = ApngWriter.WriteRgba(
            new[] {
                new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }),
                new RgbaImage(1, 1, new byte[] { 0, 0, 255, 255 })
            },
            10,
            loop: true);
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Nested animated raster",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"data:image/png;base64," +
                Convert.ToBase64String(animatedDataImage) +
                "\"/></svg>"),
            "Vector media should reject nested APNG data images that cannot follow the parent story timeline.");
        AssertThrows<ArgumentException>(
            () => new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[4]),
                "Nested GIF",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==\"/></svg>"),
            "Vector media should reject GIF data images even when the supplied GIF has only one frame.");
        var staticPng = PngWriter.WriteRgba(new RgbaImage(1, 1, new byte[] { 0, 255, 0, 255 }));
        var staticDataImage = new VisualStoryMediaSurface(
            new RgbaImage(1, 1, new byte[4]),
            "Nested static raster",
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"data:image/png;base64," +
            Convert.ToBase64String(staticPng) +
            "\"/></svg>");
        Assert(staticDataImage.Svg.Contains("data:image/png;base64,", StringComparison.Ordinal),
            "Vector media should retain valid static PNG data images.");
        var staticStyledVector = new VisualStoryMediaSurface(
            new RgbaImage(1, 1, new byte[4]),
            "Static styled vector",
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><defs><linearGradient id=\"g\"><stop offset=\"0\"/></linearGradient></defs><style>.shape{fill:url(#g)}</style><rect class=\"shape\" stroke=\"url(#g)\"/></svg>");
        Assert(staticStyledVector.Svg.Contains("fill:url(#g)", StringComparison.Ordinal) &&
               staticStyledVector.Svg.Contains("stroke=\"url(#g)\"", StringComparison.Ordinal),
            "Vector media should retain static CSS and local presentation fragment resources exactly.");

        var tabColumn = 0;
        Assert(PngVisualStoryRenderer.ExpandSourceTabs("\tvalue\t", ref tabColumn) == "    value   " &&
               tabColumn == 12,
            "Raster source layout should expand tabs to deterministic four-column stops.");

        var invalidXmlStory = VisualStory.Create("XML-safe transcript").WithSize(480, 320);
        invalidXmlStory.Scene("result", "Completed")
            .Panel("result", new VisualStorySourceSurface(StorySourceText.Create("before\u000Cafter", "text")));
        invalidXmlStory.Outcome("safe", "The source remains available", "result");
        var invalidXmlSvg = invalidXmlStory.ToSvg();
        XDocument.Parse(invalidXmlSvg);
        Assert(invalidXmlSvg.Contains("before\uFFFDafter", StringComparison.Ordinal),
            "SVG text should replace XML-invalid controls while retaining the surrounding accessible transcript.");

        var normalizedIdentifiers = VisualStory.Create("Normalized identifiers").WithSize(480, 320);
        normalizedIdentifiers.Scene("result", "Completed")
            .Panel("output", new VisualStoryTextSurface("ready"));
        AssertThrows<ArgumentException>(
            () => normalizedIdentifiers.Scene(" result ", "Duplicate"),
            "Whitespace-equivalent scene identifiers should be rejected.");
        AssertThrows<ArgumentException>(
            () => normalizedIdentifiers.Scenes[0].Panel(" output ", new VisualStoryTextSurface("duplicate")),
            "Whitespace-equivalent panel identifiers should be rejected.");
        normalizedIdentifiers.Outcome("ready", "Ready", "output");
        AssertThrows<ArgumentException>(
            () => normalizedIdentifiers.Outcome(" ready ", "Duplicate", "output"),
            "Whitespace-equivalent outcome identifiers should be rejected.");

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

        var sourceTooShort = VisualStory.Create("Crowded source stack").WithSize(480, 480);
        var sourceScene = sourceTooShort.Scene("result", "Completed", 0.25, VisualStorySceneLayout.Stacked);
        for (var index = 0; index < 4; index++) {
            sourceScene.Panel(
                "source-" + index,
                new VisualStorySourceSurface(StorySourceText.Create("Write-Output ready", "powershell")),
                "Source " + index);
        }
        sourceTooShort.Outcome("visible", "Source is visible", "source-3");
        AssertThrows<InvalidOperationException>(
            () => sourceTooShort.ToPng(),
            "Stories should reject positive source-panel content areas that are too short to render one source line.");

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

        var retainedScenes = VisualStory.Create("Retained-scene memory").WithSize(1500, 1000);
        for (var index = 0; index < 24; index++) {
            retainedScenes.Scene("scene-" + index, "Scene " + index, 0.25)
                .Panel("result", new VisualStoryTextSurface("ready"));
        }
        retainedScenes.Outcome("ready", "Ready", "result");
        var constrainedAnimation = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(4)
            .WithEndHold(0)
            .WithMaximumFrames(30);
        AssertThrows<InvalidOperationException>(
            () => retainedScenes.ToGif(constrainedAnimation),
            "Animated visual-story memory limits should include cached scene images.");

        var encoderBuffers = VisualStory.Create("Encoder memory").WithSize(3840, 2160);
        encoderBuffers.Scene("first", "First", 2.8)
            .Panel("result", new VisualStoryTextSurface("first"));
        encoderBuffers.Scene("second", "Second", 2.8)
            .Panel("result", new VisualStoryTextSurface("second"));
        encoderBuffers.Outcome("ready", "Ready", "result");
        AssertThrows<InvalidOperationException>(
            () => encoderBuffers.ToApng(
                VisualStoryAnimationOptions.Create()
                    .WithFramesPerSecond(4)
                    .WithEndHold(0)
                    .WithMaximumFrames(30)),
            "Animated visual-story memory limits should include concurrent streamed APNG working buffers before scenes are allocated.");
        var oneFrameApngBudget = AnimatedRasterMemoryBudget.EncoderRetainedBytes(
            480,
            320,
            1,
            AnimatedRasterFormat.Apng);
        var fourFrameApngBudget = AnimatedRasterMemoryBudget.EncoderRetainedBytes(
            480,
            320,
            4,
            AnimatedRasterFormat.Apng);
        Assert(fourFrameApngBudget > oneFrameApngBudget,
            "APNG memory estimates should include the accumulated encoded stream and final output materialization.");
        var oneFrameGifBudget = AnimatedRasterMemoryBudget.EncoderRetainedBytes(
            480,
            320,
            1,
            AnimatedRasterFormat.Gif);
        var fourFrameGifBudget = AnimatedRasterMemoryBudget.EncoderRetainedBytes(
            480,
            320,
            4,
            AnimatedRasterFormat.Gif);
        Assert(fourFrameGifBudget == oneFrameGifBudget + 480L * 320 * 3,
            "GIF memory estimates should include retained indexed frames and bounded per-frame compression buffers.");
        Assert(
            AnimatedRasterMemoryBudget.MaximumStreamedGifBytes(fourFrameGifBudget) <
            AnimatedRasterMemoryBudget.MaximumStreamedGifBytes(oneFrameGifBudget),
            "GIF output bounds should reserve space for both encoded chunks and the returned array.");
        var tinyGifFrames = AnimatedRasterFrames.Create(
            new[] {
                new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }),
                new RgbaImage(1, 1, new byte[] { 0, 0, 255, 255 })
            },
            10,
            true,
            "GIF");
        AssertThrows<InvalidOperationException>(
            () => AnimatedRasterEncoder.EncodeBoundedGif(tinyGifFrames, 10),
            "GIF encoding should enforce its retained-memory output ceiling while writing.");

        var sharedTranscriptSource = new VisualStorySourceSurface(
            StorySourceText.Create(new string('x', 1024 * 1024), "text"));
        var oversizedTranscript = VisualStory.Create("Bounded transcript").WithSize(480, 320);
        for (var index = 0; index < 24; index++) {
            oversizedTranscript.Scene("scene-" + index, "Scene " + index, 0.25)
                .Panel("source", sharedTranscriptSource);
        }
        oversizedTranscript.Outcome("source", "Source remains available", "source");
        AssertThrows<InvalidOperationException>(
            () => oversizedTranscript.ToTranscript(),
            "Visual-story transcripts should reject aggregate accessible content before allocating the output builder.");

        var skippedScene = VisualStory.Create("Every scene sampled").WithSize(480, 320);
        for (var index = 0; index < 3; index++) {
            skippedScene.Scene("scene-" + index, "Scene " + index, 0.25)
                .Panel("result", new VisualStoryTextSurface("state " + index));
        }
        skippedScene.Outcome("ready", "Ready", "result");
        AssertThrows<InvalidOperationException>(
            () => skippedScene.ToGif(VisualStoryAnimationOptions.Create().WithFramesPerSecond(2)),
            "Raster visual stories should reject a frame interval that can skip valid scenes.");

        var nearlyHiddenScene = VisualStory.Create("Every scene visible").WithSize(480, 320);
        nearlyHiddenScene.Scene("first", "First", 0.251)
            .Panel("result", new VisualStoryTextSurface("first"));
        nearlyHiddenScene.Scene("middle", "Middle", 0.25)
            .Panel("result", new VisualStoryTextSurface("middle"));
        nearlyHiddenScene.Scene("last", "Last", 0.25)
            .Panel("result", new VisualStoryTextSurface("last"));
        nearlyHiddenScene.Outcome("ready", "Ready", "result");
        AssertThrows<InvalidOperationException>(
            () => nearlyHiddenScene.ToGif(
                VisualStoryAnimationOptions.Create()
                    .WithFramesPerSecond(2)
                    .WithEndHold(0)),
            "Raster visual stories should reject scenes sampled only at effectively invisible transition opacity.");

        var endpointScenes = VisualStory.Create("Endpoint scenes").WithSize(480, 320);
        endpointScenes.Scene("first", "First", 0.25)
            .Panel("first-result", new VisualStoryTextSurface("first"));
        endpointScenes.Scene("last", "Last", 0.25)
            .Panel("last-result", new VisualStoryTextSurface("last"));
        endpointScenes.Outcome("ready", "Ready", "last-result");
        var endpointGif = endpointScenes.ToGif(
            VisualStoryAnimationOptions.Create()
                .WithFramesPerSecond(2)
                .WithEndHold(0)
                .WithMaximumFrames(2));
        Assert(endpointGif.Length > 8,
            "Raster visual stories should allow short first and last scenes sampled at timeline endpoints.");

        var singleShortScene = VisualStory.Create("One short scene").WithSize(480, 320);
        singleShortScene.Scene("only", "Only", 0.25)
            .Panel("result", new VisualStoryTextSurface("ready"));
        singleShortScene.Outcome("ready", "Ready", "result");
        Assert(singleShortScene.ToGif(
                VisualStoryAnimationOptions.Create()
                    .WithFramesPerSecond(2)
                    .WithEndHold(0)
                    .WithMaximumFrames(2)).Length > 8,
            "A single short scene should be sampled at both timeline endpoints.");

        var embeddedCharacters = 0L;
        AssertThrows<InvalidOperationException>(
            () => {
                for (var index = 0; index < 24; index++) {
                    embeddedCharacters = SvgVisualStoryRenderer.ReserveEmbeddedMedia(
                        embeddedCharacters,
                        3L * 1024 * 1024,
                        "scene-" + index);
                }
            },
            "Self-contained SVG stories should bound aggregate embedded media across the maximum scene count.");

        var denseText = new string('x', 8192);
        var denseSource = StorySourceText.Create(denseText, "text");
        for (var index = 0; index < 4096; index++) {
            denseSource.AddSpan(index * 2, 1, index % 2 == 0 ? StorySyntaxKind.Keyword : StorySyntaxKind.Variable);
        }
        var denseStory = VisualStory.Create("Dense syntax").WithSize(480, 320);
        denseStory.Scene("result", "Completed")
            .Panel("result", new VisualStorySourceSurface(denseSource));
        denseStory.Outcome("ready", "Ready", "result");
        Assert(denseStory.ToPng().Length > 8,
            "Source rendering should remain bounded for the maximum supported syntax-span count.");

        var narrowSource = VisualStory.Create("Narrow source").WithSize(480, 320);
        var narrowScene = narrowSource.Scene("result", "Completed", layout: VisualStorySceneLayout.Split);
        narrowScene.Panel("source", new VisualStorySourceSurface(StorySourceText.Create("x", "text")), weight: 0.12);
        narrowScene.Panel("result", new VisualStoryTextSurface("ready"), weight: 1);
        narrowSource.Outcome("ready", "Ready", "result");
        AssertThrows<InvalidOperationException>(
            () => narrowSource.ToPng(),
            "Source panels should reject positive but too-narrow content areas that cannot draw a source line.");

        var narrowText = VisualStory.Create("Narrow text").WithSize(480, 320);
        var narrowTextScene = narrowText.Scene("result", "Completed", layout: VisualStorySceneLayout.Split);
        narrowTextScene.Panel("text", new VisualStoryTextSurface("W", emphasized: true), weight: 0.2);
        narrowTextScene.Panel("result", new VisualStoryTextSurface("ready"), weight: 1);
        narrowText.Outcome("ready", "Ready", "result");
        AssertThrows<InvalidOperationException>(
            () => narrowText.ToPng(),
            "Text panels should reject positive but too-narrow content areas that cannot contain a rendered glyph.");

        var narrowTerminal = VisualStory.Create("Narrow terminal").WithSize(480, 320);
        var narrowTerminalScene = narrowTerminal.Scene("result", "Completed", layout: VisualStorySceneLayout.Split);
        narrowTerminalScene.Panel(
            "result",
            new VisualStoryTextSurface("ready"),
            weight: 10);
        narrowTerminalScene.Panel(
            "terminal",
            new VisualStoryTerminalSurface(
                TerminalStory.Create()
                    .WithFinalPrompt(false)
                    .Command("Get-Widget")
                    .Output("ready")),
            weight: 1.2);
        narrowTerminal.Outcome("ready", "Terminal output is readable", "terminal");
        AssertThrows<InvalidOperationException>(
            () => narrowTerminal.ToPng(),
            "Terminal panels should reject positive but unreadably narrow content areas.");

        var narrowMedia = VisualStory.Create("Narrow media").WithSize(480, 320);
        var narrowMediaScene = narrowMedia.Scene("result", "Completed", layout: VisualStorySceneLayout.Split);
        narrowMediaScene.Panel(
            "text",
            new VisualStoryTextSurface("ready"),
            weight: 10);
        narrowMediaScene.Panel(
            "result",
            new VisualStoryMediaSurface(
                new RgbaImage(1, 1, new byte[] { 64, 128, 255, 255 }),
                "Generated chart"),
            weight: 1.2);
        narrowMedia.Outcome("chart", "The generated chart is visible", "result");
        AssertThrows<InvalidOperationException>(
            () => narrowMedia.ToPng(),
            "Outcome media panels should reject positive but unrecognizably narrow content areas.");

        var truncatedTheme = VisualStoryTheme.PremiumDark();
        truncatedTheme.Syntax.Plain = ChartColor.FromRgb(255, 0, 128);
        var truncatedStory = VisualStory.Create("Visible truncation")
            .WithSize(480, 320)
            .WithTheme(truncatedTheme);
        truncatedStory.Scene("result", "Completed")
            .Panel(
                "result",
                new VisualStorySourceSurface(
                    StorySourceText.Create(string.Join("\n", Enumerable.Repeat("x", 100)), "text")));
        truncatedStory.Outcome("ready", "Ready", "result");
        var truncatedPixels = ReadPngRgba(truncatedStory.ToPng(), out var truncatedWidth, out _);
        var truncatedBounds = VisualStoryLayout.PanelContent(
            truncatedStory.Scenes[0].Panels[0],
            VisualStoryLayout.Panels(truncatedStory, truncatedStory.Scenes[0])[0]);
        var truncationBounds = FindNearColorBounds(
            truncatedPixels,
            truncatedWidth,
            255,
            0,
            128,
            32);
        Assert(truncationBounds.Right >= truncatedBounds.X + truncatedBounds.Width - 28,
            "Vertically truncated source panels should draw a visible ellipsis on the final rendered line.");

        var denseLinesStory = VisualStory.Create("Bounded source lines")
            .WithSize(480, 320)
            .WithTheme(truncatedTheme);
        denseLinesStory.Scene("result", "Completed")
            .Panel(
                "result",
                new VisualStorySourceSurface(
                    StorySourceText.Create(string.Concat(Enumerable.Repeat("x\n", 1024 * 1024)), "text")));
        denseLinesStory.Outcome("ready", "Ready", "result");
        Assert(denseLinesStory.ToPng().Length > 200,
            "Source rendering should retain only the visible prefix of a large newline-delimited value.");

        var horizontalTheme = VisualStoryTheme.PremiumDark();
        horizontalTheme.Syntax.Plain = ChartColor.FromRgb(255, 0, 128);
        horizontalTheme.Syntax.Keyword = ChartColor.FromRgb(0, 255, 0);
        horizontalTheme.Syntax.Type = ChartColor.FromRgb(0, 128, 255);
        var horizontalSource = StorySourceText.Create(new string('W', 37) + "tail", "text")
            .AddSpan(0, 37, StorySyntaxKind.Keyword)
            .AddSpan(37, 4, StorySyntaxKind.Type);
        var horizontalStory = VisualStory.Create("Run-boundary truncation")
            .WithSize(480, 320)
            .WithTheme(horizontalTheme);
        horizontalStory.Scene("result", "Completed")
            .Panel("result", new VisualStorySourceSurface(horizontalSource));
        horizontalStory.Outcome("ready", "Ready", "result");
        var horizontalPixels = ReadPngRgba(horizontalStory.ToPng(), out var horizontalWidth, out _);
        var horizontalBounds = VisualStoryLayout.PanelContent(
            horizontalStory.Scenes[0].Panels[0],
            VisualStoryLayout.Panels(horizontalStory, horizontalStory.Scenes[0])[0]);
        var horizontalMarker = FindNearColorBounds(
            horizontalPixels,
            horizontalWidth,
            255,
            0,
            128,
            32);
        Assert(horizontalMarker.Right >= horizontalBounds.X + horizontalBounds.Width - 20,
            "Horizontally truncated syntax runs should reserve a visible line-level ellipsis.");
    }

    private static void VisualStoryRasterLayoutStaysBoundedAtEveryDensity() {
        var theme = VisualStoryTheme.PremiumDark();
        theme.Muted = ChartColor.FromRgb(255, 0, 128);
        var split = VisualStory.Create("Bounded panel titles")
            .WithSize(480, 320)
            .WithTheme(theme);
        var splitScene = split.Scene("result", "Completed", 0.25, VisualStorySceneLayout.Split);
        splitScene.Panel("left", new VisualStoryTextSurface("left"), new string('W', 80));
        splitScene.Panel("right", new VisualStoryTextSurface("right"), "Right panel");
        split.Outcome("visible", "Right panel is visible", "right");
        var splitPixels = ReadPngRgba(split.ToPng(), out var splitWidth, out _);
        var splitBounds = VisualStoryLayout.Panels(split, splitScene);
        var titleGapX = (int)Math.Ceiling(splitBounds[0].X + splitBounds[0].Width);
        var titleGapWidth = Math.Max(1, (int)Math.Floor(splitBounds[1].X) - titleGapX);
        var titleGapInk = CountNearColorInRect(
            splitPixels,
            splitWidth,
            titleGapX,
            (int)(splitBounds[0].Y + VisualStoryLayout.PanelPadding),
            titleGapWidth,
            22,
            255,
            0,
            128,
            16);
        Assert(titleGapInk == 0, "Raster visual-story panel titles should not bleed into the adjacent panel gap.");

        var stacked = VisualStory.Create("Bounded wrapped text")
            .WithSize(480, 320)
            .WithTheme(theme);
        var stackedScene = stacked.Scene("result", "Completed", 0.25, VisualStorySceneLayout.Stacked);
        stackedScene.Panel("first", new VisualStoryTextSurface(string.Join(" ", Enumerable.Repeat("overflow", 80))));
        stackedScene.Panel("second", new VisualStoryTextSurface("ready"));
        stacked.Outcome("visible", "Ready is visible", "second");
        var stackedPixels = ReadPngRgba(stacked.ToPng(), out var stackedWidth, out _);
        var stackedBounds = VisualStoryLayout.Panels(stacked, stackedScene);
        var textGapY = (int)Math.Ceiling(stackedBounds[0].Y + stackedBounds[0].Height);
        var textGapHeight = Math.Max(1, (int)Math.Floor(stackedBounds[1].Y) - textGapY);
        var textGapInk = CountNearColorInRect(
            stackedPixels,
            stackedWidth,
            (int)(stackedBounds[0].X + VisualStoryLayout.PanelPadding),
            textGapY,
            (int)(stackedBounds[0].Width - VisualStoryLayout.PanelPadding * 2),
            textGapHeight,
            255,
            0,
            128,
            16);
        Assert(textGapInk == 0, "Wrapped raster story text should remain inside the available panel height.");

        var normalOptions = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(4)
            .WithTransition(0)
            .WithEndHold(0)
            .WithLoop(false)
            .WithMaximumFrames(2);
        var highDensityOptions = VisualStoryAnimationOptions.Create()
            .WithFramesPerSecond(4)
            .WithTransition(0)
            .WithEndHold(0)
            .WithLoop(false)
            .WithOutputScale(2)
            .WithMaximumFrames(2);
        var normal = GifReader.Decode(split.ToGif(normalOptions));
        var highDensity = GifReader.Decode(split.ToGif(highDensityOptions));
        var stretched = ImageComposition.Create(highDensity.Width, highDensity.Height, ChartColor.Transparent)
            .DrawImage(normal, 0, 0, highDensity.Width, highDensity.Height, VisualCanvasImageFit.Stretch)
            .ToImage();
        Assert(highDensity.Width == normal.Width * 2 && highDensity.Height == normal.Height * 2,
            "Animated visual-story output scale should multiply the rendered frame dimensions.");
        Assert(!highDensity.Pixels.SequenceEqual(stretched.Pixels),
            "Animated visual-story output scale should render at the requested density instead of stretching one-times frames.");

        var terminal = TerminalStory.Create()
            .WithWidth(480)
            .WithPngOutputScale(1)
            .WithTiming(0, 200, 0)
            .WithFinalPrompt(false)
            .Command("Get-Process | Sort-Object CPU", 0.05)
            .Output("ready", TerminalTextTone.Success);
        var terminalRenderer = new PngTerminalStoryRenderer();
        var terminalNormal = PngReader.Decode(terminalRenderer.Render(terminal, 1));
        var terminalHighDensity = PngReader.Decode(terminalRenderer.Render(terminal, 4));
        var stretchedTerminal = ImageComposition.Create(
                terminalHighDensity.Width,
                terminalHighDensity.Height,
                ChartColor.Transparent)
            .DrawImage(
                terminalNormal,
                0,
                0,
                terminalHighDensity.Width,
                terminalHighDensity.Height,
                VisualCanvasImageFit.Stretch)
            .ToImage();
        Assert(!terminalHighDensity.Pixels.SequenceEqual(stretchedTerminal.Pixels),
            "Terminal panels should support native story output density instead of stretched terminal text.");

        var terminalStory = VisualStory.Create("Terminal density")
            .WithSize(480, 320);
        terminalStory.Scene("result", "Completed", 0.25)
            .Panel("terminal", new VisualStoryTerminalSurface(terminal, "A completed terminal command"));
        terminalStory.Outcome("visible", "The terminal is visible", "terminal");
        var terminalStoryFrame = GifReader.Decode(terminalStory.ToGif(highDensityOptions.WithOutputScale(4)));
        Assert(terminalStoryFrame.Width == terminalStory.Width * 4 &&
               terminalStoryFrame.Height == terminalStory.Height * 4,
            "Animated visual stories should propagate their requested density through terminal panels.");

        var longTerminal = TerminalStory.Create()
            .WithWidth(960)
            .WithPngOutputScale(4)
            .WithTiming(0, 200, 0)
            .WithFinalPrompt(false)
            .Output(string.Join(Environment.NewLine, Enumerable.Repeat("completed line", 105)));
        var fittedTerminalStory = VisualStory.Create("Fitted terminal density")
            .WithSize(480, 320);
        fittedTerminalStory.Scene("result", "Completed", 0.25)
            .Panel("terminal", new VisualStoryTerminalSurface(longTerminal, "A long completed transcript"));
        fittedTerminalStory.Outcome("visible", "The terminal is visible", "terminal");
        var fittedTerminalGif = fittedTerminalStory.ToGif(
            VisualStoryAnimationOptions.Create()
                .WithFramesPerSecond(4)
                .WithTransition(0)
                .WithEndHold(0)
                .WithOutputScale(4)
                .WithMaximumFrames(2));
        Assert(fittedTerminalGif.Length > 8,
            "Nested terminal canvases should render only at the density needed by their fitted story panel.");

        var enlargedTerminal = terminalRenderer.RenderFitted(terminal, 700, 300, 4);
        Assert(enlargedTerminal.Width > terminal.Width * 4,
            "Enlarged terminal panels should render at their fitted destination density instead of stretching a four-times source.");

        var outgoing = new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 });
        var incoming = new RgbaImage(1, 1, new byte[] { 0, 0, 0, 0 });
        var transparentFade = VisualStoryAnimatedRasterRenderer.CrossFade(
            outgoing,
            incoming,
            0.5);
        Assert(transparentFade.Pixels[3] >= 126 && transparentFade.Pixels[3] <= 129,
            "Raster story cross-fades should reduce outgoing alpha when the incoming scene is transparent.");
        Assert(transparentFade.Pixels[0] >= 254 &&
               transparentFade.Pixels[1] == 0 &&
               transparentFade.Pixels[2] == 0,
            "Raster story cross-fades should interpolate transparent colors in premultiplied-alpha space without dark fringes.");
        var opaqueFade = VisualStoryAnimatedRasterRenderer.CrossFade(
            outgoing,
            new RgbaImage(1, 1, new byte[] { 0, 0, 255, 255 }),
            0.5);
        Assert(opaqueFade.Pixels[3] == 255 &&
               opaqueFade.Pixels[0] >= 126 && opaqueFade.Pixels[0] <= 129 &&
               opaqueFade.Pixels[2] >= 126 && opaqueFade.Pixels[2] <= 129,
            "Raster story cross-fades should linearly interpolate opaque scene colors without reducing opacity.");
    }
}
