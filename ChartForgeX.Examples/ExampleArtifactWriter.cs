using ChartForgeX;
using ChartForgeX.Core;

internal static class ExampleArtifactWriter {
    internal static void SaveGrid(ChartGrid grid, string output, string name, ChartPngOutputScale pngOutputScale) {
        grid.WithPngOutputScale(pngOutputScale);
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SavePng(Path.Combine(output, name + ".png"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
    }
}
