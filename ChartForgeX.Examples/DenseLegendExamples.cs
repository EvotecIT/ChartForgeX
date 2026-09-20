using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class DenseLegendExamples {
    internal static void Write(string output, ChartPngOutputScale scale) {
        var line = Base("Dense series legend", "Legend entries are summarized; all 100 series remain plotted.", scale);
        for (var i = 0; i < 100; i++) line.AddLine("Service " + i.ToString("000"), Enumerable.Range(0, 12).Select(x => new ChartPoint(x, 10 + i + Math.Sin(x * 0.5) * 8)));
        Save(line, output, "dense-legend-series");
        var values = Enumerable.Range(0, 40).Select(i => new ChartPoint(i, 1 + i % 7)).ToArray();
        Save(Base("Dense category legend", "Forty categories with an explicit count of additional legend entries.", scale).AddPie("Categories", values), output, "dense-legend-pie");
        Save(Base("Dense radial legend", "The same legend budget applies to radial charts.", scale).AddRadialBar("Categories", values), output, "dense-legend-radial");
    }

    private static Chart Base(string title, string subtitle, ChartPngOutputScale scale) => Chart.Create()
        .WithTitle(title).WithSubtitle(subtitle).WithSize(1000, 640)
        .WithTheme(ChartTheme.ReportLight()).WithPngOutputScale(scale)
        .WithLegendBudget(maximumRows: 4);

    private static void Save(Chart chart, string output, string name) {
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SavePng(Path.Combine(output, name + ".png"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
    }
}
