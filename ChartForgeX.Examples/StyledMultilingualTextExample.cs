using ChartForgeX.SvgRaster;

/// <summary>Exports a styled SVG text specimen through the shared SVG rasterizer.</summary>
internal static class StyledMultilingualTextExample {
    public static void Write(string output) {
        var svg = ReadAsset("svg");
        File.WriteAllText(Path.Combine(output, "styled-multilingual-text.svg"), svg);
        File.WriteAllBytes(Path.Combine(output, "styled-multilingual-text.png"), SvgRasterizer.ToPng(svg));
        File.WriteAllText(Path.Combine(output, "styled-multilingual-text.html"), ReadAsset("html").Replace("{{SVG}}", svg));
    }

    private static string ReadAsset(string extension) {
        using var stream = typeof(StyledMultilingualTextExample).Assembly.GetManifestResourceStream("ChartForgeX.Examples.styled-multilingual-text." + extension)
            ?? throw new InvalidOperationException("Missing multilingual text example asset.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
