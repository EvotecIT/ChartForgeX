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
    internal static void VisualStoryRasterLayoutStaysBoundedAtEveryDensity() {
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

        AssertExactAnimatedRasterDuration();
    }
}
