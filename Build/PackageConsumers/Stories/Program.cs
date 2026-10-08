using System;
using ChartForgeX;
using ChartForgeX.Motion;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
        PackageAssertions.Owner(typeof(VisualStory), "ChartForgeX.Stories");
        PackageAssertions.Owner(typeof(TerminalStory), "ChartForgeX.Stories");
        PackageAssertions.Owner(typeof(VisualMotionTimeline), "ChartForgeX.Stories");
        PackageAssertions.Owner(typeof(TopologyMotionOptions), "ChartForgeX.Stories");
        PackageAssertions.References(typeof(VisualStory).Assembly, "ChartForgeX");
        var image = new RgbaImage(2, 1, new byte[] { 20, 80, 120, 255, 80, 30, 90, 0 });
        var stillGif = image.ToGif();
        PackageAssertions.Gif(stillGif);
        var decoded = RasterImageDecoder.Decode(stillGif);
        PackageAssertions.Require(decoded.Width == 2 && decoded.Height == 1 && decoded.Pixels[7] == 0,
            "Single-frame GIF lost size or transparent pixels.");

        var terminal = TerminalStory.Create().WithTitle("Packed terminal").WithWidth(480).WithPngOutputScale(1)
            .WithTiming(0, 100, 0).WithFinalPrompt(false).Command("status", 0.1).Output("Ready");
        PackageAssertions.Require(PackageAssertions.Contains(terminal.ToTranscript(), "Ready"), "Terminal transcript is missing.");
        PackageAssertions.Png(terminal.ToPng());
        var terminalArtifact = terminal.ToVisualArtifact();
        PackageAssertions.Require(ReferenceEquals(terminal, terminalArtifact.Model)
            && PackageAssertions.Contains(terminalArtifact.ToSvg(), "Ready"), "Completed terminal artifact lost its model or transcript.");
        var terminalOptions = TerminalStoryAnimationOptions.Create().WithFramesPerSecond(2).WithEndHold(0).WithLoop(false);
        PackageAssertions.Gif(terminal.ToGif(terminalOptions));
        PackageAssertions.Apng(terminal.ToApng(terminalOptions));

        var story = VisualStory.Create("Packed story").WithSize(480, 320);
        story.Scene("first", "Start", 0.5).Panel("text", new VisualStoryTextSurface("Ready"));
        story.Scene("second", "Finish", 0.5).Panel("image", new VisualStoryMediaSurface(image, "Two pixels"));
        story.Outcome("ready", "Ready", "image");
        PackageAssertions.Require(PackageAssertions.Contains(story.ToTranscript(), "Ready"), "Story transcript is missing.");
        PackageAssertions.Png(story.ToPng());
        var options = VisualStoryAnimationOptions.Create().WithFramesPerSecond(2).WithEndHold(0).WithTransition(0).WithLoop(false);
        PackageAssertions.Gif(story.ToGif(options));
        PackageAssertions.Apng(story.ToApng(options));

        var topology = PackageAssertions.Topology();
        var motion = topology.WithMotion(TopologyMotionOptions.RoutePulseForEdges("api-db")
            .WithDuration(1).WithFrameRate(2).WithFrameLimit(4));
        PackageAssertions.Require(PackageAssertions.Contains(motion.ToSvg(), "<svg"), "Topology motion SVG failed.");
        PackageAssertions.Png(motion.ToPng());
        PackageAssertions.Gif(motion.ToGif());
        PackageAssertions.Apng(motion.ToApng());
        PackageAssertions.Payload("ChartForgeX", "ChartForgeX.Stories");
        Console.WriteLine("Stories package boundaries, terminal/story exports, static GIF and topology motion passed.");
    }
}
