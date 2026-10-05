using ChartForgeX.Core;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string GraphiteInteractionTokens(Chart chart) {
        var t = chart.Options.Theme;
        if (!t.UseGraphiteLayout) return string.Empty;
        return ";--cfx-tooltip-surface:var(--cfx-surface-card," + t.CardBackground.ToCss() + ")"
            + ";--cfx-tooltip-text:var(--cfx-text-primary," + t.Text.ToCss() + ")"
            + ";--cfx-tooltip-muted:var(--cfx-text-muted," + t.MutedText.ToCss() + ")"
            + ";--cfx-tooltip-line:var(--cfx-surface-line," + t.CardBorder.ToCss() + ")"
            + ";--cfx-crosshair-axis:var(--cfx-guide-axis," + t.Axis.ToCss() + ")"
            + ";--cfx-host-accent:var(--cfx-accent-base," + t.Palette[0].ToCss() + ")";
    }
}
