using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Primitives;
using System.Globalization;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string GraphiteInteractionTokens(Chart chart) {
        var t = chart.Options.ResolvedTheme;
        return InteractionTokens(t.Background, t.Axis, t.Palette[0],
            t.FontFamily, chart.Options.HostOwnsFrame || chart.Options.TransparentBackground,
            chart.Options.Theme.Resolve(chart.Options.ThemeMode).Chrome);
    }

    internal static string PreparedInteractionTokens(VisualRenderContext context) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        return InteractionTokens(colors.Background, colors.Axis, colors.Palette[0],
            context.Theme.Typography.Family, context.Frame.TransparentBackground, colors.Chrome);
    }

    private static string InteractionTokens(ChartColor background, ChartColor axis, ChartColor accent,
        string family, bool transparent, VisualChromeTokens chrome) {
        return ";--cfx-host-background:" + (transparent ? "transparent" : "var(--hfx-color-surface-page," + background.ToCss() + ")")
            + ";--cfx-tooltip-font:var(--hfx-font-sans," + HtmlChartRenderer.CssFontFamily(family) + ")"
            + ";--cfx-tooltip-surface:var(--cfx-surface-card,var(--hfx-color-surface," + chrome.Surface.ToCss() + "))"
            + ";--cfx-tooltip-text:var(--cfx-text-primary,var(--hfx-color-text," + chrome.Text.ToCss() + "))"
            + ";--cfx-tooltip-muted:var(--cfx-text-muted,var(--hfx-color-text-muted," + chrome.Muted.ToCss() + "))"
            + ";--cfx-tooltip-line:var(--cfx-surface-line,var(--hfx-color-border-strong," + chrome.Line.ToCss() + "))"
            + ";--cfx-tooltip-shadow:var(--cfx-shadow-tooltip,var(--hfx-elevation-2," + chrome.Shadow + "))"
            + ";--cfx-tooltip-radius:var(--hfx-radius-overlay," + chrome.TooltipRadius.ToString(CultureInfo.InvariantCulture) + "px)"
            + ";--cfx-control-radius:var(--hfx-radius-control," + chrome.ControlRadius.ToString(CultureInfo.InvariantCulture) + "px)"
            + ";--cfx-axis-label-radius:min(4px,var(--cfx-control-radius))"
            + ";--cfx-swatch-radius:min(2px,var(--cfx-control-radius))"
            + ";--cfx-chip-radius:var(--hfx-radius-pill,999px)"
            + ";--cfx-focus-ring:var(--hfx-color-focus-ring," + chrome.FocusRing.ToCss() + ")"
            + ";--cfx-crosshair-axis:var(--cfx-guide-axis,var(--hfx-color-text-muted," + axis.ToCss() + "))"
            + ";--cfx-default-host-accent:var(--cfx-accent-base,var(--hfx-color-accent," + accent.ToCss() + "))";
    }
}
