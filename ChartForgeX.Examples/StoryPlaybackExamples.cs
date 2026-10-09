using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using ChartForgeX.Themes;

internal static class StoryPlaybackExamples {
    internal static void Write(string output) {
        foreach (var format in new[] { VisualStoryFormat.Widescreen, VisualStoryFormat.Square, VisualStoryFormat.Portrait }) {
            var name = "write-fix-reveal-" + format.ToString().ToLowerInvariant();
            var prepared = Create(format).Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(0.24), 1));
            var animatedSvg = prepared.ToAnimatedSvg(new VisualStoryFrameOptions(4));
            File.WriteAllText(Path.Combine(output, name + ".svg"), animatedSvg);
            File.WriteAllText(Path.Combine(output, name + "-poster.svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
            File.WriteAllText(Path.Combine(output, name + ".txt"), prepared.ToTranscript());
            File.WriteAllBytes(Path.Combine(output, name + "-write.png"), prepared.ToPng(TimeSpan.FromSeconds(3)));
            File.WriteAllBytes(Path.Combine(output, name + "-fix.png"), prepared.ToPng(TimeSpan.FromSeconds(10.75)));
            using (var apng = File.Create(Path.Combine(output, name + ".apng"))) prepared.WriteAnimation(apng, ChartForgeX.Raster.RasterAnimationFormat.Apng, new VisualStoryFrameOptions(6));
            using (var gif = File.Create(Path.Combine(output, name + ".gif"))) prepared.WriteAnimation(gif, ChartForgeX.Raster.RasterAnimationFormat.Gif, new VisualStoryFrameOptions(6));
            File.WriteAllText(Path.Combine(output, name + ".html"), new HtmlMotionPlayerRenderer().RenderPage(animatedSvg, prepared.Title));
        }
    }

    internal static VisualStory Create(VisualStoryFormat format) {
        const string wrong = "var chart = Chart.Create()\n    .WithTitle(\"Build results\")\n    .WithXLabels(\"Mon\", \"Tue\", \"Wed\")\n    .AddBars(\"Passed\", new[] {\n        new ChartPoint(1, 12),\n        new ChartPoint(2, 18),\n        new ChartPoint(3, 24)\n    });\nchart.SavePng(\"builds.png\");";
        var offset = wrong.IndexOf("AddBars", StringComparison.Ordinal);
        var correct = wrong.Replace("AddBars", "AddBar", StringComparison.Ordinal);
        var fontSize = format == VisualStoryFormat.Portrait ? 14 : 18;
        var editorOptions = new VisualStorySourceOptions("builds.cs", fontSize: fontSize);
        var write = StorySourceTimeline.Create(StorySourceText.Create(string.Empty, "csharp"))
            .Type(wrong, TimeSpan.FromSeconds(4)).Pause(TimeSpan.FromSeconds(2));
        var fix = StorySourceTimeline.Create(StorySourceText.Create(wrong, "csharp"))
            .Select(offset, 7, TimeSpan.FromSeconds(1))
            .Edit(offset, 7, StorySourceText.Create("AddBar", "csharp").AddSpan(0, 6, StorySyntaxKind.Command), TimeSpan.FromSeconds(1.5))
            .Pause(TimeSpan.FromSeconds(1.5));
        var failed = TerminalStory.Create().WithWidth(480).WithTheme(TerminalTheme.GraphiteDark()).WithTitle("dotnet run")
            .WithFinalPrompt(false).Command("dotnet run", 0.75)
            .Output("error CS1061: Chart has no\nmethod named 'AddBars'.", TerminalTextTone.Error);
        var succeeded = TerminalStory.Create().WithWidth(480).WithTheme(TerminalTheme.GraphiteDark()).WithTitle("dotnet run")
            .WithFinalPrompt(false).Command("dotnet run", 0.75).Output("Saved builds.png", TerminalTextTone.Success);
        var chart = Chart.Create().WithTitle("Build results").WithXLabels("Mon", "Tue", "Wed").WithTheme(ChartTheme.GraphiteDark())
            .WithSize(format == VisualStoryFormat.Portrait ? 412 : 1040, 500)
            .AddBar("Passed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18), new ChartPoint(3, 24) });
        var story = VisualStory.Create("Write. Fix. Reveal.").WithDescription("An authored source-editing demonstration with a resolved build-error transcript and a real chart outcome.")
            .WithTheme(VisualStoryTheme.GraphiteDark()).WithFormat(format, format == VisualStoryFormat.Portrait ? 480 : 720);
        story.Scene("write", "01  Write the example", 6).Panel("code", new VisualStorySourceSurface(write, "C# source", editorOptions));
        story.Scene("fail", "02  Read the build error", 3, VisualStorySceneLayout.Split)
            .Panel("code", new VisualStorySourceSurface(StorySourceText.Create(wrong, "csharp"), options: new VisualStorySourceOptions("builds.cs", fontSize: fontSize, highlightedLine: 4)))
            .Panel("console", new VisualStoryTerminalSurface(failed));
        story.Scene("fix", "03  Correct the method", 4).Panel("code", new VisualStorySourceSurface(fix, "Correct the method name", editorOptions));
        story.Scene("run", "04  Run the corrected source", 4, VisualStorySceneLayout.Split)
            .Panel("code", new VisualStorySourceSurface(StorySourceText.Create(correct, "csharp"), options: editorOptions))
            .Panel("console", new VisualStoryTerminalSurface(succeeded));
        story.Scene("reveal", "05  See the chart", 5).Panel("chart", new VisualStoryMediaSurface(chart.ToPng(), "Passed builds: Monday 12, Tuesday 18, Wednesday 24.", chart.ToSvg()));
        story.Outcome("chart", "builds.png", "chart");
        return story;
    }
}
