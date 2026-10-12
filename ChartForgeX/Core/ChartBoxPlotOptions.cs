using System;

namespace ChartForgeX.Core;

/// <summary>Controls box plot value direction while retaining category coordinates and five-number summaries.</summary>
public sealed class ChartBoxPlotOptions {
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
