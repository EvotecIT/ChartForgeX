using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;

internal static class StoryReplayExamples {
    internal static void Write(string output) {
        foreach (var format in new[] { VisualStoryFormat.Widescreen, VisualStoryFormat.Square, VisualStoryFormat.Portrait }) {
            var name = "session-replay-" + format.ToString().ToLowerInvariant();
            var prepared = Create(format).Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(2), TimeSpan.Zero, 1));
            var animation = prepared.ToAnimatedSvg(new VisualStoryFrameOptions(4));
            File.WriteAllText(Path.Combine(output, name + ".svg"), animation);
            File.WriteAllText(Path.Combine(output, name + ".html"), new HtmlMotionPlayerRenderer().RenderPage(animation, prepared.Title));
            File.WriteAllText(Path.Combine(output, name + ".txt"), prepared.ToTranscript());
            File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
            File.WriteAllBytes(Path.Combine(output, name + "-history.png"), prepared.ToPng(TimeSpan.FromSeconds(4)));
            using var gif = File.Create(Path.Combine(output, name + ".gif"));
            prepared.WriteAnimation(gif, RasterAnimationFormat.Gif, new VisualStoryFrameOptions(6));
            using var apng = File.Create(Path.Combine(output, name + ".apng"));
            prepared.WriteAnimation(apng, RasterAnimationFormat.Apng, new VisualStoryFrameOptions(6));
        }
    }

    internal static VisualStory Create(VisualStoryFormat format) {
        var recording = StoryReplay.Create(TimeSpan.FromSeconds(90), workingDirectory: "~/demo", title: "PowerShell · validation")
            .Marker(TimeSpan.Zero, "Run the validation")
            .Command(TimeSpan.FromSeconds(2), "./Test-Project.ps1")
            .Output(TimeSpan.FromSeconds(3), string.Join("\n", Enumerable.Range(1, 24).Select(index => "PASS  check " + index.ToString("00"))), TerminalTextTone.Success)
            .Output(TimeSpan.FromSeconds(40), "Packaging 0%", TerminalTextTone.Muted)
            .ReplaceLine(TimeSpan.FromSeconds(48), "Packaging 50%", TerminalTextTone.Accent)
            .ReplaceLine(TimeSpan.FromSeconds(60), "Packaging complete", TerminalTextTone.Success)
            .Marker(TimeSpan.FromSeconds(62), "Preview the exported images")
            .OpenTab(TimeSpan.FromSeconds(62), "preview", "Bash · preview", TerminalDialect.Bash, "~/out")
            .Command(TimeSpan.FromSeconds(63), "ls previews")
            .Output(TimeSpan.FromSeconds(64), "landscape.png  square.png\nportrait.png", TerminalTextTone.Accent)
            .SelectTab(TimeSpan.FromSeconds(75), "main")
            .Marker(TimeSpan.FromSeconds(76), "Return to the completed report")
            .Clear(TimeSpan.FromSeconds(76))
            .ChangeDirectory(TimeSpan.FromSeconds(77), "~/out")
            .Command(TimeSpan.FromSeconds(78), "Get-Item report.html")
            .Output(TimeSpan.FromSeconds(79), "report.html\n24 checks passed", TerminalTextTone.Success);
        var replay = recording.CompressPauses(TimeSpan.FromSeconds(1.5))
            .Explain(TimeSpan.FromSeconds(5), "Idle waits shortened; original times retained.");
        var story = VisualStory.Create("Replay the result.")
            .WithDescription("Resolved demonstration events: scroll, update progress, switch sessions and keep the original timestamps. Commands are not executed.")
            .WithTheme(VisualStoryTheme.GraphiteDark()).WithFormat(format, format == VisualStoryFormat.Portrait ? 480 : 720);
        story.Scene("session", "A complete session, at a readable pace", replay.Duration.TotalSeconds)
            .Panel("terminal", new VisualStoryReplaySurface(replay, options: new VisualStoryTerminalOptions(20)));
        story.Outcome("report", "report.html · 24 checks passed", "terminal");
        return story;
    }
}
