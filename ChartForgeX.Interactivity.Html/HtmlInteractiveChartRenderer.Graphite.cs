using ChartForgeX.Core;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string GraphiteInteractionTokens(Chart chart) {
        var t = chart.Options.Theme;
        if (!t.UseGraphiteLayout) return string.Empty;
        var dark = (0.2126 * t.CardBackground.R + 0.7152 * t.CardBackground.G + 0.0722 * t.CardBackground.B) / 255 < 0.5;
        var shadow = dark
            ? "0 10px 30px rgba(0,0,0,.55),0 0 0 1px " + t.CardBorder.ToCss()
            : "0 8px 24px rgba(0,0,0,.14),0 1px 3px rgba(0,0,0,.08)";
        return ";--cfx-tooltip-surface:var(--cfx-surface-card," + t.CardBackground.ToCss() + ")"
            + ";--cfx-tooltip-text:var(--cfx-text-primary," + t.Text.ToCss() + ")"
            + ";--cfx-tooltip-muted:var(--cfx-text-muted," + t.MutedText.ToCss() + ")"
            + ";--cfx-tooltip-line:var(--cfx-surface-line," + t.CardBorder.ToCss() + ")"
            + ";--cfx-tooltip-shadow:var(--cfx-shadow-tooltip," + shadow + ")"
            + ";--cfx-crosshair-axis:var(--cfx-guide-axis," + t.Axis.ToCss() + ")"
            + ";--cfx-host-accent:var(--cfx-accent-base," + t.Palette[0].ToCss() + ")";
    }
}
