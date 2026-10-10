using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;

internal static class StoryAppearanceExamples {
    internal static void Write(string output) {
        foreach (var light in new[] { false, true }) {
            foreach (var appearance in new[] { "macos", "windows", "linux" }) {
                var theme = appearance == "macos" ? VisualStoryTheme.MacOS(light)
                    : appearance == "windows" ? VisualStoryTheme.Windows(light) : VisualStoryTheme.Linux(light);
                var prepared = Create(theme).Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(1), TimeSpan.Zero, 0));
                var name = "desktop-" + appearance + (light ? "-light" : "-dark");
                var svg = prepared.ToAnimatedSvg(new VisualStoryFrameOptions(4));
                File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
                File.WriteAllText(Path.Combine(output, name + ".html"), new HtmlMotionPlayerRenderer().RenderPage(svg, prepared.Title));
                File.WriteAllBytes(Path.Combine(output, name + "-writing.png"), prepared.ToPng(TimeSpan.FromSeconds(2)));
                File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
                using var gif = File.Create(Path.Combine(output, name + ".gif"));
                prepared.WriteAnimation(gif, RasterAnimationFormat.Gif, new VisualStoryFrameOptions(4));
            }
        }
    }

    private static VisualStory Create(VisualStoryTheme theme) {
        const string text = "$name = 'Stories'\nWrite-Output \"Hello, $name!\"";
        var source = StorySourceText.Create(text, "powershell")
            .AddSpan(0, 5, StorySyntaxKind.Variable).AddSpan(6, 1, StorySyntaxKind.Operator)
            .AddSpan(8, 9, StorySyntaxKind.String).AddSpan(18, 12, StorySyntaxKind.Command)
            .AddSpan(31, 8, StorySyntaxKind.String).AddSpan(39, 5, StorySyntaxKind.Variable).AddSpan(44, 2, StorySyntaxKind.String);
        var editor = StorySourceTimeline.Create(StorySourceText.Create("", "powershell")).Type(source, TimeSpan.FromSeconds(3));
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(3), title: "PowerShell · captured output")
            .Command(TimeSpan.FromSeconds(.3), "./hello.ps1").Output(TimeSpan.FromSeconds(1), "Hello, Stories!");
        var story = VisualStory.Create("Make your code a story").WithTheme(theme).WithFormat(VisualStoryFormat.Square, 720)
            .WithDescription("Resolved source and authored example output. Displayed commands are not executed.");
        story.Scene("write", "01  Write with syntax colors", 3)
            .Panel("code", new VisualStorySourceSurface(editor, options: new VisualStorySourceOptions("hello.ps1", 22)));
        story.Scene("read", "02  Read the completed script", 1)
            .Panel("code", new VisualStorySourceSurface(source, options: new VisualStorySourceOptions("hello.ps1", 22)));
        story.Scene("replay", "03  Replay captured output", 3)
            .Panel("terminal", new VisualStoryReplaySurface(replay, options: new VisualStoryTerminalOptions(22)));
        story.Outcome("visible", "The captured output is visible", "terminal");
        return story;
    }
}
