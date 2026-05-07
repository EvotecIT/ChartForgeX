using System;
using System.Globalization;
using System.Threading;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual blocks as dependency-free static HTML.
/// </summary>
public sealed class HtmlVisualBlockRenderer {
    private static long ScopeCounter;
    private readonly SvgVisualBlockRenderer _svg = new();

    /// <summary>Renders a visual block as an embeddable HTML fragment.</summary>
    public string RenderFragment(IVisualBlock block) {
        VisualBlockRendering.Validate(block);
        return "<div class=\"chartforgex-visual-block\" style=\"width:100%;max-width:" + block.Options.Size.Width.ToString(CultureInfo.InvariantCulture) + "px;box-sizing:border-box\">" + _svg.Render(block, NextScope()) + "</div>";
    }

    /// <summary>Renders a visual block as a complete HTML document.</summary>
    public string RenderPage(IVisualBlock block) {
        VisualBlockRendering.Validate(block);
        var theme = block.Options.Theme;
        var bg = block.Options.TransparentBackground ? theme.CardBackground.ToCss() : theme.Background.A == 0 ? theme.CardBackground.ToCss() : theme.Background.ToCss();
        var title = string.IsNullOrWhiteSpace(block.AccessibleName) ? "ChartForgeX visual block" : block.AccessibleName;
        return "<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>" + VisualBlockRendering.Escape(title) + "</title>\n<style>body{margin:0;min-height:100vh;display:grid;place-items:start center;background:" + bg + ";font-family:" + VisualBlockRendering.CssFontFamily(theme.FontFamily) + ";padding:clamp(20px,5vh,48px) 24px 24px;box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-visual-block svg{max-width:100%;height:auto;display:block}@media(max-width:680px){body{padding:16px}}</style>\n</head>\n<body>\n" + RenderFragment(block) + "\n</body>\n</html>";
    }

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "html-visual-block-" + value.ToString(CultureInfo.InvariantCulture);
    }
}
