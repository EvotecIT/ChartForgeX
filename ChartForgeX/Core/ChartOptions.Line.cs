using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartLineVisualStyle _lineVisualStyle = ChartLineVisualStyle.Premium();
    private readonly ChartLineVisualStyle _plainLineVisualStyle = ChartLineVisualStyle.Plain();
    private bool _hasExplicitLineStyle;
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
        get => !_hasExplicitLineStyle && Theme.FlatMarks ? _plainLineVisualStyle : _lineVisualStyle;
        set { _lineVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone(); _hasExplicitLineStyle = true; }
    }

    /// <summary>Resolves explicit line treatments over the shared single-stroke prepared default.</summary>
    internal ChartLineVisualStyle ResolvePreparedLineVisualStyle() {
        if (_hasExplicitLineStyle) return _lineVisualStyle.Clone();
        if (Changed(_plainLineVisualStyle, ChartLineVisualStyle.Plain())) return _plainLineVisualStyle.Clone();
        if (Changed(_lineVisualStyle, ChartLineVisualStyle.Premium())) return _lineVisualStyle.Clone();
        return ChartLineVisualStyle.Plain();

        // Halo opacity and width are coupled: a directly edited style keeps its remaining meaningful defaults.
        static bool Changed(ChartLineVisualStyle configured, ChartLineVisualStyle original) =>
            configured.AmbientHaloOpacity != original.AmbientHaloOpacity || configured.AmbientHaloStrokeExtra != original.AmbientHaloStrokeExtra
            || configured.HaloOpacity != original.HaloOpacity || configured.HaloStrokeExtra != original.HaloStrokeExtra
            || configured.HighlightOpacity != original.HighlightOpacity || configured.HighlightStrokeRatio != original.HighlightStrokeRatio;
    }
}
