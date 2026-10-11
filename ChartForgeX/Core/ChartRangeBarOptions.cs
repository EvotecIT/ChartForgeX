using System;

namespace ChartForgeX.Core;

/// <summary>Controls the value direction of range bars without changing their category coordinates or endpoints.</summary>
public sealed class ChartRangeBarOptions {
    private ChartOrientation _orientation;

    /// <summary>Gets or sets the value direction. Vertical is the default; horizontal uses X for values and Y for categories.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }
}
