using System;

namespace ChartForgeX.Terminal;

/// <summary>
/// Specifies the visible window chrome surrounding a terminal story.
/// </summary>
public enum TerminalWindowStyle {
    /// <summary>macOS-style title bar with traffic-light controls.</summary>
    MacOS,
    /// <summary>Windows Terminal-style tab strip and window controls.</summary>
    WindowsTerminal,
    /// <summary>Restrained title bar without platform-specific controls.</summary>
    Minimal,
    /// <summary>Terminal surface without a visible title bar.</summary>
    None
}

internal static class TerminalWindowChrome {
    internal const double TitleFontSize = 12;
    internal const double WindowsTabLeft = 16;
    internal const double WindowsTitleX = 56;
    internal const double WindowsTabCloseOffset = 19;
    internal const double WindowsTabCloseRadius = 4;
    private const double WindowsTitleControlGap = 10;
    private const double WindowsTitleColumnWidth = TitleFontSize;

    internal static double HeaderHeight(TerminalWindowStyle style) {
        Validate(style);
        switch (style) {
            case TerminalWindowStyle.MacOS: return 42;
            case TerminalWindowStyle.WindowsTerminal: return 50;
            case TerminalWindowStyle.Minimal: return 38;
            case TerminalWindowStyle.None: return 0;
            default: throw new ArgumentOutOfRangeException(nameof(style));
        }
    }

    internal static double FrameRadius(TerminalWindowStyle style) {
        Validate(style);
        return style == TerminalWindowStyle.WindowsTerminal ? 9 : 14;
    }

    internal static string FitTitle(string value, int width, TerminalWindowStyle style) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Validate(style);
        if (style == TerminalWindowStyle.None) return string.Empty;
        if (style == TerminalWindowStyle.WindowsTerminal) {
            var maximumWindowsColumns = Math.Max(1, (int)Math.Floor(WindowsTitleAvailableWidth(width) / WindowsTitleColumnWidth));
            return TerminalTextWidth.Fit(value, maximumWindowsColumns);
        }

        var reservedWidth = style == TerminalWindowStyle.MacOS ? 180 : 72;
        var available = Math.Max(12, width - reservedWidth);
        var maximum = Math.Max(1, available / 12);
        return TerminalTextWidth.Fit(value, maximum);
    }

    internal static double WindowsTabWidth(int width) {
        return Math.Min(360, Math.Max(220, width - 230));
    }

    internal static double WindowsTabRight(int width) {
        return WindowsTabLeft + WindowsTabWidth(width);
    }

    internal static double WindowsTabCloseX(int width) {
        return WindowsTabRight(width) - WindowsTabCloseOffset;
    }

    internal static double WindowsTitleAvailableWidth(int width) {
        return Math.Max(
            WindowsTitleColumnWidth,
            WindowsTabCloseX(width) - WindowsTabCloseRadius - WindowsTitleControlGap - WindowsTitleX);
    }

    internal static void Validate(TerminalWindowStyle style) {
        if (!Enum.IsDefined(typeof(TerminalWindowStyle), style)) throw new ArgumentOutOfRangeException(nameof(style));
    }
}
