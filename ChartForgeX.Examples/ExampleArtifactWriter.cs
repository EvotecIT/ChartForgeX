using ChartForgeX;
using ChartForgeX.Core;

internal static class ExampleArtifactWriter {
    internal static void WriteText(string path, string text) => File.WriteAllText(path, text.Replace("\r\n", "\n").Replace("\r", "\n"));

    internal static void SaveChart(Chart chart, string output, string name, ChartPngOutputScale pngOutputScale) {
        chart.WithPngOutputScale(pngOutputScale);
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
        chart.SavePng(Path.Combine(output, name + ".png"));
    }

    internal static void SaveGrid(ChartGrid grid, string output, string name, ChartPngOutputScale pngOutputScale) {
        grid.WithPngOutputScale(pngOutputScale);
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SavePng(Path.Combine(output, name + ".png"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
    }
}
