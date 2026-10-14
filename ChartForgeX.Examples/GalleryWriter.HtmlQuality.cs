using System.Text.RegularExpressions;
using System.Globalization;
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
            : html.Contains("overflow:visible", StringComparison.Ordinal) || HasUnclippedNativeGrid(html, css)
                || HasClippedStaticArtifact(html, css) || HasBoundedInteractiveChart(html, css);
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

    private static bool HasClippedStaticArtifact(string html, string css) {
        // A completed static source has the same finite picture boundary as its PNG.
        // Clip its SVG viewport, while preserving ordinary overflow on the page and host.
        if (!Regex.IsMatch(html, "<div\\s[^>]*class=\"chartforgex-visual-artifact\"[^>]*>", RegexOptions.IgnoreCase)) return false;
        var rule = Regex.Match(css, "\\.chartforgex-visual-artifact\\s*>\\s*svg\\s*\\{(?<body>[^{}]*)}", RegexOptions.IgnoreCase);
        if (!rule.Success || !Regex.IsMatch(rule.Groups["body"].Value, "(?:^|;)\\s*overflow\\s*:\\s*hidden\\s*(?:;|$)", RegexOptions.IgnoreCase)) return false;
        var hostRules = Regex.Matches(css, "(?:^|})\\s*(?:html,body|body|\\.chartforgex-visual-artifact)\\s*\\{(?<body>[^{}]*)}", RegexOptions.IgnoreCase);
        if (hostRules.Any(host => Regex.IsMatch(host.Groups["body"].Value, "overflow(?:-[xy])?\\s*:", RegexOptions.IgnoreCase))) return false;
        var svg = Regex.Match(html, "<svg\\s[^>]*>", RegexOptions.IgnoreCase);
        var viewBox = Regex.Match(svg.Value, "(?:^|\\s)viewBox=\"(?<bounds>[^\"]+)\"", RegexOptions.IgnoreCase);
        var bounds = Regex.Split(viewBox.Groups["bounds"].Value.Trim(), "[\\s,]+");
        return viewBox.Success && bounds.Length == 4 && bounds.All(value =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
            && double.Parse(bounds[2], CultureInfo.InvariantCulture) > 0 && double.Parse(bounds[3], CultureInfo.InvariantCulture) > 0;
    }

    private static bool HasBoundedInteractiveChart(string html, string css) {
        // Pan/zoom is bounded by the stage; readable mode scrolls only that stage.
        // Accept the generated native responsive contract, never page or host clipping.
        var host = Regex.Match(html, "<section\\s[^>]*class=\"cfx-interactive-chart\"[^>]*>", RegexOptions.IgnoreCase);
        var layout = Regex.Match(host.Value, "data-cfx-responsive-layout=\"(?<layout>fit|readable)\"");
        var nativeWidth = Regex.Match(host.Value, "--cfx-native-width:(?<size>[0-9.]+)px(?:;|\")");
        var nativeHeight = Regex.Match(host.Value, "--cfx-native-height:(?<size>[0-9.]+)px(?:;|\")");
        if (!host.Success || !layout.Success || !nativeWidth.Success || !nativeHeight.Success) return false;
        var stage = Regex.Match(html, "<div\\s[^>]*class=\"cfx-stage\"[^>]*>\\s*(?<svg><svg\\s[^>]*>)", RegexOptions.IgnoreCase);
        if (!stage.Success || !stage.Groups["svg"].Value.Contains("xmlns=\"http://www.w3.org/2000/svg\"", StringComparison.Ordinal)) return false;
        var viewBox = Regex.Match(stage.Groups["svg"].Value, "(?:^|\\s)viewBox=\"(?<bounds>[^\"]+)\"");
        var bounds = Regex.Split(viewBox.Groups["bounds"].Value.Trim(), "[\\s,]+");
        if (!viewBox.Success || bounds.Length != 4 || bounds.Any(value =>
            !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))) return false;
        if (!double.TryParse(nativeWidth.Groups["size"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
            || !double.TryParse(nativeHeight.Groups["size"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
            || !double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0
            || width != double.Parse(bounds[2], CultureInfo.InvariantCulture) || height != double.Parse(bounds[3], CultureInfo.InvariantCulture)) return false;
        var stageRule = Regex.Match(css, "\\.cfx-stage\\s*\\{(?<body>[^{}]*)}");
        if (!stageRule.Success || !Regex.IsMatch(stageRule.Groups["body"].Value, "(?:^|;)\\s*overflow\\s*:\\s*hidden\\s*(?:;|$)")) return false;
        var selector = ".cfx-interactive-chart[data-cfx-responsive-layout=\"" + layout.Groups["layout"].Value + "\"] .cfx-stage svg";
        var sizing = Regex.Match(css, Regex.Escape(selector) + "\\s*\\{(?<body>[^{}]*)}");
        if (!sizing.Success || !Regex.IsMatch(sizing.Groups["body"].Value, "(?:^|;)\\s*width\\s*:\\s*100%\\s*(?:;|$)")) return false;
        if (layout.Groups["layout"].Value == "fit"
            && !Regex.IsMatch(sizing.Groups["body"].Value, "(?:^|;)\\s*height\\s*:\\s*auto\\s*(?:;|$)")) return false;
        if (layout.Groups["layout"].Value == "readable"
            && !sizing.Groups["body"].Value.Contains("min-width: min(var(--cfx-native-width, 640px), 38rem)", StringComparison.Ordinal)) return false;
        var hostRules = Regex.Matches(css, "(?:^|})\\s*(?:html|html,body|body|\\.cfx-shell|\\.cfx-interactive-chart|\\.cfx-frame)\\s*\\{(?<body>[^{}]*)}");
        return !Regex.IsMatch(host.Value, "overflow(?:-[xy])?\\s*:", RegexOptions.IgnoreCase)
            && !hostRules.Any(rule => Regex.IsMatch(rule.Groups["body"].Value, "overflow(?:-[xy])?\\s*:", RegexOptions.IgnoreCase));
    }
}
