using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Themes;

internal static class PagedFacetExamples {
    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var samples = ChartDataset<Sample>.From(Enumerable.Range(1, 13).SelectMany(site =>
            Enumerable.Range(0, 24).Select(hour => new Sample(site, hour,
                25 + site * 2 + Math.Sin(hour * 0.4 + site) * 12))));
        var report = ChartGrid.FromFacets(samples, sample => sample.Site, (site, rows) => Chart.Create()
                .WithTitle("Site " + site.ToString("00"))
                .WithSize(440, 280)
                .WithTheme(ChartTheme.ReportLight())
                .WithDashboardTrendPanelStyle(showLegend: false, showYAxis: true)
                .WithTitleStyle(style => style.WithFontSize(20))
                .WithXAxis("Hour")
                .WithYAxis("CPU (%)")
                .AddLine("CPU", rows, sample => sample.Hour, sample => sample.Cpu), columns: 2)
            .WithTitle("Fleet utilization")
            .WithPanelSize(440, 280)
            .WithSharedAxes();
        foreach (var page in report.Paginate(6)) {
            page.Grid.WithSubtitle($"Page {page.Number} of {page.TotalPages} · Sites {page.FirstChartIndex + 1}–{page.FirstChartIndex + page.ChartCount} of {page.TotalCharts} · Shared axes");
            ExampleArtifactWriter.SaveGrid(page.Grid, output, "paged-facets-" + page.Number, pngOutputScale);
        }
    }

    private sealed record Sample(int Site, int Hour, double Cpu);
}
