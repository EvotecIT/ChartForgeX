using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Retains authored frame token identity independently of equal resolved color values.</summary>
internal sealed class VisualFramePaints {
    internal SvgPaint Background { get; set; }
    internal SvgPaint Surface { get; set; }
    internal SvgPaint ElevatedSurface { get; set; }
    internal SvgPaint Border { get; set; }
    internal SvgPaint Foreground { get; set; }
    internal SvgPaint MutedForeground { get; set; }
}
