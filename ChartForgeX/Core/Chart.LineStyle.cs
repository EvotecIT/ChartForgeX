using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Sets reusable visual tokens for line, area boundary, trend, and slope strokes.
    /// </summary>
    public Chart WithLineVisualStyle(ChartLineVisualStyle style) {
        Options.LineVisualStyle = style;
        return this;
    }

    /// <summary>
    /// Uses crisp single-stroke line rendering without halo or highlight layers.
    /// </summary>
    public Chart WithPlainLineStyle() => WithLineVisualStyle(ChartLineVisualStyle.Plain());

    /// <summary>
    /// Uses stronger luminous line rendering for deliberate glow-heavy dashboard treatments.
    /// </summary>
    public Chart WithLuminousLineStyle() => WithLineVisualStyle(ChartLineVisualStyle.Luminous());

    /// <summary>
    /// Mutates a copy of the current reusable line visual tokens.
    /// </summary>
    public Chart WithLineVisualStyle(Action<ChartLineVisualStyle> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var style = Options.LineVisualStyle.Clone();
        configure(style);
        Options.LineVisualStyle = style;
        return this;
    }
}
