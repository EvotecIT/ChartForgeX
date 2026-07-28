using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Motion;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static class AnimatedVisualStoryExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var theme = ChartTheme.ReportDark();
        var activity = Chart.Create()
            .WithTitle("Release activity")
            .WithSubtitle("A native chart can participate in the same story")
            .WithTheme(theme)
            .WithSize(780, 330)
            .WithAxes(false)
            .WithGrid(false)
            .WithLegend(false)
            .WithXLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun")
            .AddSmoothArea("Releases", Points(8, 11, 9, 16, 18, 23), ChartColor.FromRgb(56, 189, 248));

        var portfolio = ChartTable.Create()
            .WithTitle("Active portfolio")
            .WithSubtitle("Exact values remain readable when motion is unavailable")
            .WithTheme(theme)
            .WithSize(780, 330)
            .WithDenseMode()
            .WithColumns("Project", "Purpose", "Signal")
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
            .WithTitle("Engineering portfolio")
            .WithSubtitle("One reusable model for profile cards, release stories, status reports, and dashboards")
            .WithTheme(theme)
            .WithColumns(3)
            .WithPanelSize(250, 150)
            .WithGap(16)
            .WithPadding(28)
            .WithFrame()
            .WithPngOutputScale((int)pngOutputScale)
            .Add("metric-projects", Metric("Projects", "24", "Reusable libraries"))
            .Add("metric-users", Metric("Monthly users", "18.4K", "+12% this quarter"))
            .Add("metric-releases", Metric("Releases", "96", "Last 12 months"))
            .Add("activity", activity, columnSpan: 3, rowSpan: 2)
            .Add("portfolio", portfolio, columnSpan: 3, rowSpan: 2)
            .WithMotion(motion);

        story.SaveSvg(Path.Combine(output, "animated-engineering-portfolio-story.svg"));
        story.SaveHtml(Path.Combine(output, "animated-engineering-portfolio-story.html"));
        story.SavePng(Path.Combine(output, "animated-engineering-portfolio-story.png"));
    }

    private static MetricCard Metric(string label, string value, string caption) => MetricCard.Create()
        .WithMetric(label, value)
        .WithCaption(caption)
        .WithTheme(ChartTheme.ReportDark())
        .WithSize(250, 150);

    private static IEnumerable<ChartPoint> Points(params double[] values) {
        for (var index = 0; index < values.Length; index++) {
            yield return new ChartPoint(index, values[index]);
        }
    }
}
