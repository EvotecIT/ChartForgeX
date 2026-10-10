using System;

namespace ChartForgeX.Core;

/// <summary>Controls Sunburst sector geometry, parent sizes and its independent numeric color scale.</summary>
public sealed class ChartSunburstOptions {
    private ChartHierarchyValuePolicy _parentValuePolicy;
    private double _cornerRadius;

    /// <summary>Gets or sets the maximum inward corner radius in pixels for each Sunburst sector. Default: zero.</summary>
    /// <remarks>Must be finite and nonnegative. Clamped to each sector's thickness and angular space. A central apex stays sharp; full rings have no corners.</remarks>
    public double CornerRadius {
        get => _cornerRadius;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _cornerRadius = value;
        }
    }

    /// <summary>Gets or sets the group-size interpretation. Default: descendant leaf aggregation.</summary>
    /// <remarks>Authored totals must contain their resolved children. Preparation validates changes made after ingestion.</remarks>
    public ChartHierarchyValuePolicy ParentValuePolicy {
        get => _parentValuePolicy;
        set {
            if (!Enum.IsDefined(typeof(ChartHierarchyValuePolicy), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _parentValuePolicy = value;
        }
    }
    /// <summary>Gets or sets the numeric color scale. When omitted, supplied color observations use the theme's sequential ramp.</summary>
    public ChartColorScale? ColorScale { get; set; }
    /// <summary>Gets or sets whether the independent color legend is drawn when the chart legend is enabled. Default: true.</summary>
    public bool ShowColorScaleLegend { get; set; } = true;
    /// <summary>Gets or sets the color legend and tooltip title. Null uses the localized Color label.</summary>
    public string? ColorLegendTitle { get; set; }
}
