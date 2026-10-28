using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class SequenceArtifactParticipant {
    private double? _minimumWidth;
    /// <summary>Gets or sets an immutable shadow of this participant glyph/box surface only; captions, links and facts remain retained separately.</summary>
    public ChartForgeX.Effects.VisualDropShadow? SurfaceShadow { get; set; }
    /// <summary>Gets or sets a literal UML stereotype caption; null keeps the ordinary participant header.</summary>
    public string? Stereotype { get; set; }
    /// <summary>Gets or sets a literal native text symbol preceding the display label. It never selects or fetches an icon pack.</summary>
    public string? Symbol { get; set; }
    /// <summary>Gets or sets this participant's explicit native fill; null uses the sequence role paint.</summary>
    public ChartColor? Fill { get; set; }
    /// <summary>Gets or sets an optional participant border; null retains the shared role border.</summary>
    public ChartColor? Border {get;set;}
    /// <summary>Gets or sets an optional activation surface for this participant; null retains the shared activation paint.</summary>
    public ChartColor? ActivationFill {get;set;}
    /// <summary>Gets or sets an optional activation border for this participant; null retains the shared activation paint.</summary>
    public ChartColor? ActivationBorder {get;set;}
    /// <summary>Gets or sets a positive finite minimum header width in logical units. Natural output grows its lanes; an explicit undersized context rejects.</summary>
    public double? MinimumWidth {
        get => _minimumWidth;
        set { if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0)) throw new ArgumentOutOfRangeException(nameof(value)); _minimumWidth=value; }
    }
}
public sealed partial class SequenceArtifactBlock {
    /// <summary>Gets optional reference participant targets in declaration order. Only Reference uses targets; an empty list spans the diagram.</summary>
    public List<string> ParticipantIds { get; } = new();
}
