using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Primitives;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string GraphiteInteractionTokens(Chart chart) {
        var t = chart.Options.ResolvedTheme;
        return InteractionTokens(t.Background, t.CardBackground, t.Text, t.MutedText, t.CardBorder, t.Axis, t.Palette[0],
            t.FontFamily, chart.Options.HostOwnsFrame || chart.Options.TransparentBackground);
    }

    internal static string PreparedInteractionTokens(VisualRenderContext context) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        return InteractionTokens(colors.Background, colors.ElevatedSurface, colors.Foreground, colors.MutedForeground,
            colors.Border, colors.Axis, colors.Palette[0], context.Theme.Typography.Family, context.Frame.TransparentBackground);
    }

    private static string InteractionTokens(ChartColor background, ChartColor card, ChartColor text, ChartColor muted,
        ChartColor line, ChartColor axis, ChartColor accent, string family, bool transparent) {
        var dark = (0.2126 * card.R + 0.7152 * card.G + 0.0722 * card.B) / 255 < 0.5;
        var shadow = dark
            ? "0 10px 30px rgba(0,0,0,.55),0 0 0 1px " + line.ToCss()
            : "0 8px 24px rgba(0,0,0,.14),0 1px 3px rgba(0,0,0,.08)";
        return ";--cfx-host-background:" + (transparent ? "transparent" : background.ToCss())
            + ";--cfx-tooltip-font:" + HtmlChartRenderer.CssFontFamily(family)
            + ";--cfx-tooltip-surface:var(--cfx-surface-card," + card.ToCss() + ")"
            + ";--cfx-tooltip-text:var(--cfx-text-primary," + text.ToCss() + ")"
            + ";--cfx-tooltip-muted:var(--cfx-text-muted," + muted.ToCss() + ")"
            + ";--cfx-tooltip-line:var(--cfx-surface-line," + line.ToCss() + ")"
            + ";--cfx-tooltip-shadow:var(--cfx-shadow-tooltip," + shadow + ")"
            + ";--cfx-crosshair-axis:var(--cfx-guide-axis," + axis.ToCss() + ")"
            + ";--cfx-default-host-accent:var(--cfx-accent-base," + accent.ToCss() + ")";
    }
}
