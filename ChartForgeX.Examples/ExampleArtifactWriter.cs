using ChartForgeX;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

internal static class ExampleArtifactWriter {
    internal static void WriteText(string path, string text) => File.WriteAllText(path, text.Replace("\r\n", "\n").Replace("\r", "\n"));

    // A standalone image cannot inherit the gallery page's private font alias.
    // Keep its captured ink without embedding two complete faces in every thumbnail.
    internal static string Svg(PreparedVisual prepared, string? idScope = null, VisualAccessibility? accessibility = null) {
        var names = accessibility ?? prepared.Accessibility;
        string Render(VisualSvgTextPresentation presentation) => prepared.ToSvg(names, idScope, textPresentation: presentation);
        try { return Render(VisualSvgTextPresentation.NativeContours); }
        catch (NotSupportedException) { return Render(VisualSvgTextPresentation.Automatic); }
    }

    internal static void SaveChart(Chart chart, string output, string name, ChartRasterScale pngOutputScale) {
        chart.Raster(p => p.OutputScale((int)pngOutputScale));
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
        chart.SavePng(Path.Combine(output, name + ".png"));
    }

    internal static void SaveGrid(ChartGrid grid, string output, string name, ChartRasterScale pngOutputScale) {
        grid.RasterScale((int)pngOutputScale);
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SavePng(Path.Combine(output, name + ".png"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
    }
}
