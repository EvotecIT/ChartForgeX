using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

/// <summary>Overrides one numeric component caption while retaining the shared data-label visibility and collision policy.</summary>
public sealed class ChartValueLabelOptions {
    private ChartDataLabelPlacement? _placement;

    /// <summary>Gets or sets whether this component may display a caption. The chart or series must also enable data labels. Default: true.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Gets or sets the preferred physical placement. Null inherits the series or chart policy; collisions can still omit the caption.</summary>
    public ChartDataLabelPlacement? Placement {
        get => _placement;
        set {
            if (value.HasValue && !Enum.IsDefined(typeof(ChartDataLabelPlacement), value.Value)) throw new ArgumentOutOfRangeException(nameof(value));
            _placement = value;
        }
    }

    /// <summary>Gets typography overrides resolved over the chart, series and observation label style, in that order.</summary>
    public TextStyleOverride Style { get; private set; } = new();

    /// <summary>Copies the nested typography so configuration callbacks and preparation keep independent values.</summary>
    internal ChartValueLabelOptions Clone() => new() { Visible = Visible, Placement = Placement, Style = Style.Clone() };
}
