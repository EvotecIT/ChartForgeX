using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;

namespace ChartForgeX.Stories;

internal static partial class NativeVisualStoryRenderer {
    private static void DrawStoryWindowHeader(VisualSceneBuilder builder, ChartRect bounds, TerminalWindowStyle style,
        TerminalTheme theme, string title, double opacity = 1, bool paintHeader = true) {
        var height = TerminalWindowChrome.HeaderHeight(style);
        if (bounds.Width < (style == TerminalWindowStyle.WindowsTerminal ? 208 : 144))
            throw new System.InvalidOperationException("The desktop title bar cannot fit its controls. Enlarge the panel, rebalance its weight or choose Minimal window chrome.");
        var y = bounds.Y + height / 2;
        var text = ChartColorMath.WithOpacity(theme.Text, opacity);
        using (builder.PushGroup(null, "story-window-chrome", new Dictionary<string, string> { ["data-cfx-window-style"] = style.ToString() })) {
            if (paintHeader) DrawStoryWindowHeaderBackground(builder, bounds, height, theme);
            if (style == TerminalWindowStyle.MacOS) {
                var colors = new[] { "#FF5F57", "#FEBC2E", "#28C840" };
                for (var i = 0; i < colors.Length; i++) builder.Ellipse(bounds.X + 16 + i * 18, y, 5.5, 5.5,
                    ChartColorMath.WithOpacity(ChartColor.FromHex(colors[i]), opacity), role: "story-window-control");
                DrawCenteredWindowTitle(builder, title, bounds, y, text, 144);
            } else {
                var right = bounds.X + bounds.Width;
                var titleWidth = bounds.Width - 140;
                if (style == TerminalWindowStyle.WindowsTerminal) {
                    var tabWidth = System.Math.Min(300, titleWidth);
                    builder.Rect(new ChartRect(bounds.X + 8, bounds.Y + 8, tabWidth, height - 8), ChartColorMath.WithOpacity(theme.Background, opacity), radius: 7, role: "story-window-tab");
                    builder.Line(bounds.X + 20, y - 4, bounds.X + 24, y, text, 1.5);
                    builder.Line(bounds.X + 24, y, bounds.X + 20, y + 4, text, 1.5);
                    builder.Line(bounds.X + 28, y + 4, bounds.X + 34, y + 4, text, 1.5);
                    FitText(builder, title, bounds.X + 43, y + 5, tabWidth - 48, 12, text, 600);
                } else FitText(builder, title, bounds.X + 14, y + 5, titleWidth, 12, text, 600);
                builder.Line(right - 102, y, right - 92, y, text, 1.3, role: "story-window-minimize");
                builder.Rect(new ChartRect(right - 66, y - 5, 10, 10), fill: null, stroke: text, role: "story-window-maximize");
                if (style == TerminalWindowStyle.Linux) builder.Ellipse(right - 24, y, 11, 11,
                    ChartColorMath.WithOpacity(theme.Accent, opacity), role: "story-window-control");
                var close = style == TerminalWindowStyle.Linux ? ChartColorMath.WithOpacity(theme.Background, opacity) : text;
                builder.Line(right - 28, y - 4, right - 20, y + 4, close, 1.3, role: "story-window-close");
                builder.Line(right - 20, y - 4, right - 28, y + 4, close, 1.3);
            }
        }
    }

    private static void DrawStoryWindowHeaderBackground(VisualSceneBuilder builder, ChartRect bounds, double height, TerminalTheme theme) {
        builder.Rect(new ChartRect(bounds.X, bounds.Y, bounds.Width, height), theme.HeaderBackground, radius: 9);
        builder.Rect(new ChartRect(bounds.X, bounds.Y + height - 9, bounds.Width, 9), theme.HeaderBackground);
        builder.Line(bounds.X, bounds.Y + height, bounds.X + bounds.Width, bounds.Y + height, theme.Border);
    }

    private static void DrawCenteredWindowTitle(VisualSceneBuilder builder, string title, ChartRect bounds, double y, ChartColor color, double reserved) {
        var fitted = TerminalTextWidth.Fit(title, System.Math.Max(1, bounds.Width - reserved), value => builder.MeasureText(value, 12, 600).Width);
        builder.Text(fitted, bounds.X + bounds.Width / 2, y + 5, 12, color, 600, alignment: TextAlignment.Center);
    }
}
