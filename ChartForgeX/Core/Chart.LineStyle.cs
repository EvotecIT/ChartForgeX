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
