using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Line styling, markers and orientation. Configure it through <see cref="ChartOptions.Lines"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartLineOptions {
    internal ChartLineOptions Clone() {
        var copy = (ChartLineOptions)MemberwiseClone();
        copy._lineVisualStyle = _lineVisualStyle.Clone();
        return copy;
    }
    private ChartLineMarkerMode? _lineMarkerMode;

    /// <summary>Gets or sets optional line-marker visibility. Null uses the shared default (last point).</summary>
    public ChartLineMarkerMode? MarkerMode { get => _lineMarkerMode; set { if (value.HasValue && !Enum.IsDefined(typeof(ChartLineMarkerMode), value.Value)) throw new ArgumentOutOfRangeException(nameof(value)); _lineMarkerMode = value; } }

    private ChartLineVisualStyle _lineVisualStyle = ChartLineVisualStyle.Plain();

    private ChartOrientation _lineOrientation;


    /// <summary>
    /// Gets or sets the orientation of line and step-line series. Horizontal lines retain
    /// source X as the category coordinate on the physical Y axis and source Y as the
    /// value on the physical X axis. They use the primary axis and a linear or time
    /// category scale, and can share that orientation with horizontal bars.
    /// </summary>
    public ChartOrientation Orientation {
        get => _lineOrientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _lineOrientation = value;
        }
    }


    /// <summary>
    /// Gets or sets reusable visual tokens for line, area boundary, trend, and slope strokes.
    /// </summary>
    [FluentPreset("PlainStyle", typeof(ChartLineVisualStyle), nameof(ChartLineVisualStyle.Plain), Summary = "Uses plain lines without glow.")]
    [FluentPreset("LuminousStyle", typeof(ChartLineVisualStyle), nameof(ChartLineVisualStyle.Luminous), Summary = "Uses luminous lines with a soft glow.")]
    public ChartLineVisualStyle VisualStyle {
        get => _lineVisualStyle;
        set => _lineVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone();
    }


    /// <summary>Snapshots the chart's canonical line treatment for detached preparation.</summary>
    internal ChartLineVisualStyle ResolvePreparedVisualStyle() => _lineVisualStyle.Clone();
}
