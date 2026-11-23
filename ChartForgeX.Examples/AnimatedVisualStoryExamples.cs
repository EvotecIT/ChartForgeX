using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Motion;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static class AnimatedVisualStoryExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var theme = VisualTheme.Graphite();
        var activity = Chart.Create()
            .Title("Release activity")
            .Subtitle("A native chart can participate in the same story")
            .Theme(theme).ThemeMode(ThemeMode.Dark)
            .Size(800, 338)
            .Axes(false)
            .ShowGrid(false)
            .Legend(false)
            .XLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun")
            .AddSmoothArea("Releases", ChartPoints.FromValues(8, 11, 9, 16, 18, 23), ChartColor.FromRgb(56, 189, 248));

        var portfolio = ChartTable.Create()
            .Title("Active portfolio")
            .Subtitle("Exact values remain readable when motion is unavailable")
            .Theme(theme).ThemeMode(ThemeMode.Dark)
            .Size(800, 338)
            .Dense()
            .Columns("Project", "Purpose", "Signal")
            .AddRow("Identity Core", "Authentication and policy", "Healthy")
            .AddRow("Report Engine", "Portable visual reports", "Growing")
            .AddRow("Automation Kit", "Operational workflows", "Stable");

        var motion = VisualMotionTimeline.Create()
            .Reveal("title", durationSeconds: 0.65)
            .Fade("subtitle", delaySeconds: 0.12, durationSeconds: 0.5)
            .Cascade(new[] { "metric-projects", "metric-users", "metric-releases" }, initialDelaySeconds: 0.28, intervalSeconds: 0.1, durationSeconds: 0.55)
            .Rise("activity", delaySeconds: 0.66, durationSeconds: 0.65)
            .Rise("portfolio", delaySeconds: 0.78, durationSeconds: 0.65);

        var story = VisualGrid.Create()
            .Title("Engineering portfolio")
            .Subtitle("One reusable model for profile cards, release stories, status reports, and dashboards")
            .Theme(theme).ThemeMode(ThemeMode.Dark)
            .Columns(3)
            .PanelSize(250, 150)
            .Gap(16)
            .Padding(28)
            .FrameVisible()
            .PngOutputScale((int)pngOutputScale)
            .Add("metric-projects", Metric("Projects", "24", "Reusable libraries"))
            .Add("metric-users", Metric("Monthly users", "18.4K", "+12% this quarter"))
            .Add("metric-releases", Metric("Releases", "96", "Last 12 months"))
            .Add("activity", activity, columnSpan: 3, rowSpan: 2)
            .Add("portfolio", portfolio, columnSpan: 3, rowSpan: 2);
        var presentation = VisualMotionPresentation.Create(story, motion);

        File.WriteAllText(Path.Combine(output, "animated-engineering-portfolio-story.svg"), presentation.ToSvg());
        File.WriteAllText(Path.Combine(output, "animated-engineering-portfolio-story.html"), presentation.ToHtmlPage());
        File.WriteAllBytes(Path.Combine(output, "animated-engineering-portfolio-story.png"), presentation.ToPng());
    }

    private static MetricCard Metric(string label, string value, string caption) => MetricCard.Create()
        .Metric(label, value)
        .Caption(caption)
        .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
        .Size(250, 150);
}
