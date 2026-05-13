using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Html;

internal static class HtmlSurfacePolish {
    private const string BaseBodyCss = "body{margin:0;min-height:100vh;min-height:100svh;";
    private const string BrowserTextPolishCss = "-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision";

    internal static string CenteredBodyCss(ChartColor background, string fontFamily) =>
        BaseBodyCss + "display:grid;place-items:center;background:" + ChartSurfacePolish.CssGradient(background) + ";font-family:" + fontFamily + ";padding:clamp(16px,4vmin,52px);box-sizing:border-box;" + BrowserTextPolishCss + "}";

    internal static string ReportBodyCss(ChartColor background, string fontFamily, string padding) =>
        BaseBodyCss + "background:" + ChartSurfacePolish.CssGradient(background) + ";font-family:" + fontFamily + ";padding:" + padding + ";box-sizing:border-box;" + BrowserTextPolishCss + "}";

    internal static string ReportBodyCss(string backgroundCss, string fontFamily, string padding) =>
        BaseBodyCss + "background:" + CssBackground(backgroundCss) + ";font-family:" + fontFamily + ";padding:" + padding + ";box-sizing:border-box;" + BrowserTextPolishCss + "}";

    internal static string ResponsiveCenteredBodyCss =>
        "@media(max-width:680px){body{padding:16px;place-items:start center}}";

    internal static string PrintBodyCss(string padding) =>
        PrintBodyCss(padding, string.Empty);

    internal static string PrintBodyCss(string padding, string extraCss) =>
        "@media print{body{min-height:auto;padding:" + padding + ";background:transparent}" + extraCss + "}";

    private static string CssBackground(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "#FFFFFF";
        if (ChartColor.TryParse(value, out var parsed)) return ChartSurfacePolish.CssGradient(parsed);
        return value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ").Trim();
    }
}
