using System;

namespace ChartForgeX.Core;

/// <summary>Controls funnel anatomy and orientation without changing its ordered source stages.</summary>
public sealed class ChartFunnelOptions {
    private ChartFunnelForm _form;
    private ChartOrientation _orientation;

    /// <summary>Gets or sets the anatomy. The default is proportional stage bars.</summary>
    public ChartFunnelForm Form {
        get => _form;
        set {
            if (!Enum.IsDefined(typeof(ChartFunnelForm), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _form = value;
        }
    }

    /// <summary>Gets or sets the direction of the ordered stages. The default is top to bottom.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }
}
