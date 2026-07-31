using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using ChartForgeX.Themes;

internal static class VisualStoryExamples {
    public static void Write(string output) {
        WriteFiveLineChart(output);
        WriteApiStory(output);
        WriteImageStory(output);
    }

    private static void WriteFiveLineChart(string output) {
        var chart = FiveLineChartExample.Create();
        chart.SavePng(Path.Combine(output, "weekly-builds.png"));
        var console = TerminalStory.Create()
            .WithTitle("dotnet run — five-line-chart")
            .WithDialect(TerminalDialect.CSharp)
            .WithWidth(900)
            .WithFinalPrompt(false)
            .Command("dotnet run")
            .Output("Saved weekly-builds.png", TerminalTextTone.Success);
        var story = VisualStory.Create("Create a chart in five lines")
            .WithDescription("Source, execution transcript, and the generated chart stay together.")
            .WithSize(1200, 675);
        story.Scene("source", "Start with five lines", 2.8)
            .Panel("source", new VisualStorySourceSurface(StorySourceText.Create(FiveLineChartExample.Source, "csharp"), "C# source"));
        story.Scene("run", "Run the example", 2.2, VisualStorySceneLayout.Split)
            .Panel("source", new VisualStorySourceSurface(StorySourceText.Create(FiveLineChartExample.Source, "csharp"), "C# source"), weight: 1.2)
            .Panel("console", new VisualStoryTerminalSurface(console, "dotnet run reports Saved weekly-builds.png"), "Console", weight: 0.8);
        story.Scene("result", "See what the code created", 3.2, VisualStorySceneLayout.Split)
            .Panel("source", new VisualStorySourceSurface(StorySourceText.Create(FiveLineChartExample.Source, "csharp"), "C# source"), weight: 0.9)
            .Panel("chart", new VisualStoryMediaSurface(chart.ToPng(), "Weekly builds chart rising from 12 on Monday to 31 on Friday.", chart.ToSvg()), "Generated chart", weight: 1.1);
        story.Outcome("chart-created", "weekly-builds.png is visible", "chart");
        WriteAll(story, output, "chart-in-five-lines-story");
    }

    private static void WriteApiStory(string output) {
        const string request = "GET /api/projects/chartforgex\nAccept: application/json";
        const string response = "{\n  \"name\": \"ChartForgeX\",\n  \"status\": \"ready\",\n  \"formats\": [\"svg\", \"png\", \"gif\"]\n}";
        var story = VisualStory.Create("Explain an API in one visual story")
            .WithDescription("The request, response, and useful result are presented as one resolved narrative.")
            .WithSize(960, 540);
        story.Scene("request", "Send the request", 2)
            .Panel("request", new VisualStorySourceSurface(StorySourceText.Create(request, "http"), "HTTP request"));
        story.Scene("response", "Inspect the response", 2, VisualStorySceneLayout.Split)
            .Panel("request", new VisualStorySourceSurface(StorySourceText.Create(request, "http"), "HTTP request"))
            .Panel("response", new VisualStorySourceSurface(StorySourceText.Create(response, "json"), "JSON response"));
        story.Scene("outcome", "Use the returned capability", 2.4, VisualStorySceneLayout.Split)
            .Panel("response", new VisualStorySourceSurface(StorySourceText.Create(response, "json"), "JSON response"))
            .Panel("formats", new VisualStoryTextSurface("SVG · PNG · GIF", emphasized: true), "Supported formats");
        story.Outcome("formats-returned", "Supported formats are visible", "formats");
        WriteAll(story, output, "api-request-response-story");
    }

    private static void WriteImageStory(string output) {
        var before = ImageComposition.Create(640, 360, ChartColor.FromHex("#253044"))
            .FillRoundedRectangle(60, 58, 520, 244, 26, ChartColor.FromHex("#42526B"))
            .DrawText(60, 150, 520, "before", 40, ChartColor.FromHex("#C7D1DE"), alignment: TextAlignment.Center, emphasized: true)
            .ToImage();
        var after = ImageComposition.Create(640, 360, ChartColor.FromHex("#071625"))
            .FillRoundedRectangle(60, 58, 520, 244, 26, ChartColor.FromHex("#0D3551"))
            .StrokeRoundedRectangle(60, 58, 520, 244, 26, ChartColor.FromHex("#5ED7F2"), 3)
            .DrawText(60, 132, 520, "after", 44, ChartColor.FromHex("#F1F5FB"), alignment: TextAlignment.Center, emphasized: true)
            .DrawText(60, 198, 520, "clean · sharp · ready", 20, ChartColor.FromHex("#55D6A9"), alignment: TextAlignment.Center)
            .ToImage();
        var story = VisualStory.Create("Show an image transformation")
            .WithDescription("Generic media surfaces make before-and-after demos first-class.")
            .WithSize(960, 540);
        story.Scene("before", "Begin with the source image", 2)
            .Panel("before", new VisualStoryMediaSurface(before, "Plain source image labeled before"));
        story.Scene("compare", "Apply the transformation", 2.4, VisualStorySceneLayout.Split)
            .Panel("before", new VisualStoryMediaSurface(before, "Plain source image labeled before"), "Before")
            .Panel("after", new VisualStoryMediaSurface(after, "Polished transformed image labeled after"), "After");
        story.Outcome("image-transformed", "The transformed image is visible", "after");
        WriteAll(story, output, "image-before-after-story");
    }

    private static void WriteAll(VisualStory story, string output, string name) {
        story.SaveSvg(Path.Combine(output, name + ".svg"));
        File.WriteAllText(Path.Combine(output, name + ".html"), story.ToHtmlPage());
        story.SavePng(Path.Combine(output, name + ".png"));
        story.SaveTranscript(Path.Combine(output, name + ".txt"));
        var animation = VisualStoryAnimationOptions.Create().WithFramesPerSecond(6).WithEndHold(1.5);
        story.SaveGif(Path.Combine(output, name + ".gif"), animation);
        story.SaveApng(Path.Combine(output, name + ".apng"), animation);
    }
}

internal static class FiveLineChartExample {
    internal static readonly string Source = ReadSource();

    internal static Chart Create() {
        var chart = Chart.Create()
            .WithTitle("Weekly builds")
            .WithSubtitle("A real outcome, not a console promise")
            .WithTheme(ChartTheme.ReportDark())
            .WithSize(900, 500)
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri");
        chart.AddSmoothArea(
            "Builds",
            new[] {
                new ChartPoint(1, 12),
                new ChartPoint(2, 18),
                new ChartPoint(3, 15),
                new ChartPoint(4, 24),
                new ChartPoint(5, 31)
            });
        return chart;
    }

    private static string ReadSource() {
        using var stream = typeof(FiveLineChartExample).Assembly
            .GetManifestResourceStream("ChartForgeX.Examples.FiveLineChart.cs")
            ?? throw new InvalidOperationException("The compiled five-line chart snippet is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
