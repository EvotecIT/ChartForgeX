using System.Text.RegularExpressions;
using ChartForgeX.SvgRaster;

/// <summary>
/// HTML-specific artifact quality helpers for generated examples.
/// </summary>
public static partial class GalleryWriter {
    private static HtmlHealth ReadHtmlHealth(string htmlFileName) {
        if (!File.Exists(htmlFileName)) return default;
        var html = File.ReadAllText(htmlFileName);
        var css = string.Join("\n", Regex.Matches(html, "<style[^>]*>(?<css>[\\s\\S]*?)</style>", RegexOptions.IgnoreCase)
            .Select(match => match.Groups["css"].Value));
        var hasWatermark = html.Contains("data-cfx-watermark", StringComparison.Ordinal);
        var hasExpectedOverflow = hasWatermark
            ? html.Contains("overflow:hidden", StringComparison.Ordinal)
            : html.Contains("overflow:visible", StringComparison.Ordinal) || HasUnclippedNativeGrid(html, css);
        var hasFlatBody = Regex.Matches(css, "(?:^|})\\s*body\\s*\\{(?<body>[^{}]*)}", RegexOptions.IgnoreCase)
            .Select(match => Regex.Match(match.Groups["body"].Value, "(?:^|;)\\s*background(?:-color)?\\s*:\\s*(?<color>[^;]+)", RegexOptions.IgnoreCase))
            .Where(match => match.Success).Take(1)
            .Any(match => SvgRasterColor.TryParse(match.Groups["color"].Value.Trim(), out _));
        var hasSceneBackground = Regex.Matches(html, "<rect\\s[^>]*>", RegexOptions.IgnoreCase)
            .Where(match => match.Value.Contains("data-cfx-role=\"background\"", StringComparison.Ordinal))
            .Select(match => Regex.Match(match.Value, "(?:^|\\s)fill=\"(?<color>[^\"]+)\""))
            .Any(match => match.Success && SvgRasterColor.TryParse(match.Groups["color"].Value, out _));
        return new HtmlHealth(
            ReadFileLength(htmlFileName),
            html.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase) && html.Contains("<title>", StringComparison.OrdinalIgnoreCase),
            html.Contains("name=\"viewport\"", StringComparison.OrdinalIgnoreCase),
            html.Contains("<svg", StringComparison.OrdinalIgnoreCase),
            html.Contains("linear-gradient(180deg", StringComparison.Ordinal),
            html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal) && html.Contains("text-rendering:geometricPrecision", StringComparison.Ordinal),
            hasExpectedOverflow,
            html.Contains("@media print", StringComparison.Ordinal) && html.Contains("background:transparent", StringComparison.Ordinal),
            (html.Contains("data-cfx-role=\"frame-card\"", StringComparison.Ordinal) || hasFlatBody || hasSceneBackground)
                && !css.Contains("linear-gradient(", StringComparison.Ordinal) && !css.Contains("radial-gradient(", StringComparison.Ordinal));
    }

    private static bool HasUnclippedNativeGrid(string html, string css) {
        // A native grid is one responsive SVG in an ordinary section. Its host defaults to
        // visible overflow; bounded SVG clips remain part of the prepared scene itself.
        var wrapper = Regex.Match(html, "<section\\s[^>]*class=\"chartforgex-grid\"[^>]*>", RegexOptions.IgnoreCase);
        if (!wrapper.Success || !wrapper.Value.Contains("width:100%;max-width:", StringComparison.Ordinal)
            || !wrapper.Value.Contains("box-sizing:border-box", StringComparison.Ordinal)
            || !html.Contains("data-cfx-role=\"panel\"", StringComparison.Ordinal)) return false;
        var svg = Regex.Match(html, "<svg\\s[^>]*>", RegexOptions.IgnoreCase);
        if (!svg.Success || !svg.Value.Contains("max-width:100%;height:auto;display:block", StringComparison.Ordinal)) return false;
        // Do not accept default overflow merely because the wrapper has a familiar class.
        // A clipping/scrolling declaration on its host is still a quality failure.
        return !Regex.IsMatch(css + wrapper.Value, "overflow(?:-[xy])?\\s*:\\s*(?:hidden|clip|auto|scroll)\\b", RegexOptions.IgnoreCase);
    }
}
