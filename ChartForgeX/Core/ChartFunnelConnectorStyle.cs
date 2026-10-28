using System;

namespace ChartForgeX.Core;

/// <summary>Immutable paint for an adjacent Funnel connection; it never changes either stage value or connection geometry.</summary>
public sealed class ChartFunnelConnectorStyle {
    /// <summary>Gets whether the connection paints. False retains its complete source facts and semantic region.</summary>
    public bool Visible { get; }
    /// <summary>Gets the optional fill. Null inherits the source stage's existing point/series paint.</summary>
    public ChartColor? Fill { get; }
    /// <summary>Gets the optional fill alpha multiplier. Null retains .18 for bars or .3 for Cone.</summary>
    public double? FillOpacity { get; }
    /// <summary>Gets the optional pattern. Null retains the existing form policy; None explicitly disables a pattern.</summary>
    public ChartFillPattern? FillPattern { get; }
    /// <summary>Gets the optional shared outline policy. Null paints no outline; width zero disables it.</summary>
    public ChartStrokeStyle? OutlineStroke { get; }
    /// <summary>Gets the finite logical dash phase, independent of geometry.</summary>
    public double StrokeDashOffset { get; }

    /// <summary>Creates validated connection paint. No supplied fields retain the normal family paint.</summary>
    /// <param name="visible">Whether to paint the authored adjacent connection.</param>
    /// <param name="fill">Optional explicit fill.</param>
    /// <param name="fillOpacity">Optional alpha multiplier from zero through one.</param>
    /// <param name="fillPattern">Optional existing native hatch pattern.</param>
    /// <param name="outlineStroke">Optional existing immutable outline policy.</param>
    /// <param name="strokeDashOffset">Finite logical dash phase.</param>
    public ChartFunnelConnectorStyle(bool visible = true, ChartColor? fill = null, double? fillOpacity = null,
        ChartFillPattern? fillPattern = null, ChartStrokeStyle? outlineStroke = null, double strokeDashOffset = 0) {
        if (fillOpacity.HasValue) ChartGuards.UnitInterval(fillOpacity.Value, nameof(fillOpacity));
        if (fillPattern.HasValue && !Enum.IsDefined(typeof(ChartFillPattern), fillPattern.Value)) throw new ArgumentOutOfRangeException(nameof(fillPattern));
        ChartGuards.Finite(strokeDashOffset, nameof(strokeDashOffset));
        Visible = visible; Fill = fill; FillOpacity = fillOpacity; FillPattern = fillPattern;
        OutlineStroke = outlineStroke; StrokeDashOffset = strokeDashOffset;
    }
}
