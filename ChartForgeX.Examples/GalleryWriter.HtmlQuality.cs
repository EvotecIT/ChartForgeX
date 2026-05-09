/// <summary>
/// HTML-specific artifact quality helpers for generated examples.
/// </summary>
public static partial class GalleryWriter {
    private static HtmlHealth ReadHtmlHealth(string htmlFileName) {
        if (!File.Exists(htmlFileName)) return default;
        var html = File.ReadAllText(htmlFileName);
        return new HtmlHealth(
            ReadFileLength(htmlFileName),
            html.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase) && html.Contains("<title>", StringComparison.OrdinalIgnoreCase),
            html.Contains("name=\"viewport\"", StringComparison.OrdinalIgnoreCase),
            html.Contains("<svg", StringComparison.OrdinalIgnoreCase),
            html.Contains("linear-gradient(180deg", StringComparison.Ordinal),
            html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal) && html.Contains("text-rendering:geometricPrecision", StringComparison.Ordinal),
            html.Contains("overflow:visible", StringComparison.Ordinal),
            html.Contains("@media print", StringComparison.Ordinal) && html.Contains("background:transparent", StringComparison.Ordinal));
    }
}
