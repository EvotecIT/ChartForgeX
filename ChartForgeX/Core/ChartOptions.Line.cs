using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartLineVisualStyle _lineVisualStyle = ChartLineVisualStyle.Plain();
    private ChartOrientation _lineOrientation;

    /// <summary>
    /// Gets or sets the orientation of line and step-line series. Horizontal lines retain
    /// source X as the category coordinate on the physical Y axis and source Y as the
    /// value on the physical X axis. They use the primary axis and a linear or time
    /// category scale, and can share that orientation with horizontal bars.
    /// </summary>
    public ChartOrientation LineOrientation {
        get => _lineOrientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _lineOrientation = value;
        }
    }

    /// <summary>
    /// Gets or sets reusable visual tokens for line, area boundary, trend, and slope strokes.
    /// </summary>
    public ChartLineVisualStyle LineVisualStyle {
        get => _lineVisualStyle;
        set => _lineVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone();
    }

    /// <summary>Snapshots the chart's canonical line treatment for detached preparation.</summary>
    internal ChartLineVisualStyle ResolvePreparedLineVisualStyle() => _lineVisualStyle.Clone();
}
