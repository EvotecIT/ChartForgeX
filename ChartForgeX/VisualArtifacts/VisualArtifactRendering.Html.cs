using System;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactRendering {
    private static VisualArtifactHtmlSizing HtmlSizing(VisualArtifact artifact, VisualArtifactRenderOptions? options) {
        var sizing = options?.HtmlSizing ?? VisualArtifactHtmlSizing.Automatic;
        return sizing == VisualArtifactHtmlSizing.Automatic
            ? artifact.Model is SequenceArtifact || artifact.Kind == VisualArtifactKind.Sequence ? VisualArtifactHtmlSizing.PreserveSize : VisualArtifactHtmlSizing.FitToWidth
            : sizing;
    }

    internal static string WrapSvgPage(string title, string svg, string? language = null, bool clipSvgViewport = false,
        VisualArtifactHtmlSizing sizing = VisualArtifactHtmlSizing.FitToWidth, bool plainScene = false) {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "ChartForgeX visual artifact" : title.Trim();
        var safeLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language!.Trim();
        var preserve = sizing == VisualArtifactHtmlSizing.PreserveSize;
        var svgOverflow = clipSvgViewport ? "hidden" : "visible";
        var body = plainScene
            ? "body{margin:0;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}"
            : "html,body{margin:0;min-height:100%;background:linear-gradient(180deg,#f8fafc,#e2e8f0)}body{display:grid;place-items:center;padding:24px;box-sizing:border-box;font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}";
        var host = preserve
            ? ".chartforgex-visual-artifact{max-width:100%;min-width:0;height:auto;overflow-x:auto}.chartforgex-visual-artifact:focus-visible{outline:2px solid currentColor;outline-offset:-2px}"
            : ".chartforgex-visual-artifact{max-width:100%;height:auto}";
        var attributes = preserve
            ? " data-cfx-html-sizing=\"preserve-size\" tabindex=\"0\" role=\"region\" aria-label=\"" + EscapeHtml(safeTitle) + "\""
            : string.Empty;
        return "<!doctype html><html lang=\"" + EscapeHtml(safeLanguage) + "\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + EscapeHtml(safeTitle) + "</title><style>" + body + host +
            ".chartforgex-visual-artifact svg{display:block;max-width:" + (preserve ? "none" : "100%") + ";height:auto;overflow:" + svgOverflow + "}" +
            "@media print{html,body{background:transparent}body{padding:0}.chartforgex-visual-artifact{max-width:none" + (preserve ? ";overflow:visible" : string.Empty) + "}.chartforgex-visual-artifact svg{max-width:100%}}</style></head><body><div class=\"chartforgex-visual-artifact\"" + attributes + ">" + svg + "</div></body></html>";
    }
}
