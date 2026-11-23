using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class DenseLegendExamples {
    internal static void Write(string output, ChartRasterScale scale) {
        var line = Base("Dense series legend", "Legend entries are summarized; all 100 series remain plotted.", scale);
        for (var i = 0; i < 100; i++) line.AddLine("Service " + i.ToString("000"), Enumerable.Range(0, 12).Select(x => new ChartPoint(x, 10 + i + Math.Sin(x * 0.5) * 8)));
        Save(line, output, "dense-legend-series");
        var values = Enumerable.Range(0, 40).Select(i => new ChartPoint(i, 1 + i % 7)).ToArray();
        Save(Base("Dense category legend", "Forty categories with an explicit count of additional legend entries.", scale).AddPie("Categories", values), output, "dense-legend-pie");
        var radialValues = Enumerable.Range(0, 40).Select(i => new ChartPoint(i, 35 + i * 13 % 61)).ToArray();
        Save(Base("Dense radial legend", "All forty values remain visible as compact concentric rings while the legend stays bounded.", scale).AddProgressRing("Categories", radialValues), output, "dense-legend-radial");
    }

    private static Chart Base(string title, string subtitle, ChartRasterScale scale) => Chart.Create()
        .Title(title).Subtitle(subtitle).Size(800, 512)
        .Theme(VisualTheme.Graphite()).Raster(p => p.OutputScale((int)scale))
        .Legend(l => l.MaxRows(4));

    private static void Save(Chart chart, string output, string name) {
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SavePng(Path.Combine(output, name + ".png"));
        File.WriteAllText(Path.Combine(output, name + ".html"), chart.ToInteractiveHtmlPage(options =>
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit));
    }
}
