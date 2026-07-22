using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class DenseSignalExamples {
    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var chart = Chart.Create()
            .WithTitle("Dense Signal Review")
            .WithSubtitle("50,000 source samples resolved against the intended report width with source-index provenance")
            .WithXAxis("Sample")
            .WithYAxis("Latency")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 640)
            .WithPngOutputScale(pngOutputScale)
            .AddAdaptiveLine(
                "P95 latency",
                Enumerable.Range(0, 50000).Select(index => new ChartPoint(index, 140 + Math.Sin(index / 190d) * 22 + Math.Sin(index / 37d) * 5 + (index is > 31700 and < 31900 ? 84 : 0))),
                1180,
                ChartResolutionPolicy.Trend(),
                ChartColor.FromRgb(37, 99, 235));

        chart.SaveSvg(Path.Combine(output, "dense-signal-decimated-light.svg"));
        chart.SaveHtml(Path.Combine(output, "dense-signal-decimated-light.html"));
        chart.SavePng(Path.Combine(output, "dense-signal-decimated-light.png"));
    }
}
