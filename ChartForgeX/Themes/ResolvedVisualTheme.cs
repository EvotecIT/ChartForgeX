using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>A read-only per-mode paint projection for static painters. It never owns theme policy.</summary>
internal sealed class ResolvedVisualTheme {
    internal const double DefaultDataLabelFontSize = 10.5;
    internal const double DefaultTitleFontSize = 25;
    internal const double DefaultSubtitleFontSize = 12.5;
    private readonly VisualTheme _theme;
    private readonly VisualThemeColors _colors;
    private readonly ChartColor[] _palette;
    private readonly ChartColor[]? _sequentialRamp;
    internal ResolvedVisualTheme(VisualTheme theme, VisualThemeMode mode) {
        _theme = theme; _colors = theme.Resolve(mode); _palette = _colors.Palette.ToArray();
        _sequentialRamp = _colors.SequentialRamp.Count == 0 ? null : _colors.SequentialRamp.ToArray();
    }
    internal ChartColor Background => _colors.Background;
    internal ChartColor CardBackground => _colors.ElevatedSurface;
    internal ChartColor PlotBackground => _colors.Surface;
    internal ChartColor CardBorder => _colors.Border;
    internal ChartColor PlotBorder => _colors.PlotBorder;
    internal ChartColor Text => _colors.Foreground;
    internal ChartColor Text2 => _colors.MutedForeground;
    internal ChartColor MutedText => _colors.Muted;
    internal ChartColor Grid => _colors.Grid;
    internal ChartColor Axis => _colors.Axis;
    internal ChartColor Positive => _colors.Positive;
    internal ChartColor Warning => _colors.Warning;
    internal ChartColor Negative => _colors.Negative;
    internal ChartColor Disabled => _colors.Disabled;
    internal ChartColor Info => _colors.Info;
    internal ChartColor Quiet => _colors.Quiet;
    internal ChartColor QuietLine => _colors.QuietLine;
    internal ChartColor Neutral => _colors.Neutral;
    internal ChartColor Neutral2 => _colors.Neutral2;
    internal ChartColor Neutral3 => _colors.Neutral3;
    internal ChartColor[] Palette => _palette;
    internal ChartColor[]? SequentialRampValue => _sequentialRamp;
    internal ChartColor[]? SequentialRamp => SequentialRampValue;
    internal double CornerRadius => _theme.CardRadius;
    internal double PlotCornerRadius => _theme.PlotRadius;
    internal double StrokeWidth => _theme.SeriesStrokeWidth;
    internal double MarkerRadius => _theme.MarkerRadius;
    internal double ShadowOpacity => _theme.CardShadowOpacity;
    internal ChartColor ShadowColor => _theme.CardShadowColor;
    internal string FontFamily => _theme.Typography.Family;
    internal double TitleFontSize => _theme.Typography.TitleSize;
    internal double SubtitleFontSize => _theme.Typography.SubtitleSize;
    internal double AxisTitleFontSize => _theme.Typography.AxisTitleSize;
    internal double TickLabelFontSize => _theme.Typography.AxisSize;
    internal double LegendFontSize => _theme.Typography.LegendSize;
    internal double DataLabelFontSize => _theme.Typography.DataLabelSize;
}
