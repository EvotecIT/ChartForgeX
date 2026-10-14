using System;

namespace ChartForgeX.Core;

/// <summary>Controls the value direction of a waterfall without changing its accumulation order or source coordinates.</summary>
public sealed class ChartWaterfallOptions {
    private ChartOrientation _orientation;

    /// <summary>Gets or sets the value direction. Vertical is the default; horizontal uses X for values and Y for categories.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }

    /// <summary>Copies the configured state for isolated chart configuration.</summary>
    internal ChartWaterfallOptions Clone() => (ChartWaterfallOptions)MemberwiseClone();
}
