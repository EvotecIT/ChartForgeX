using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartOrientation _lollipopOrientation, _dumbbellOrientation, _slopeOrientation;

    /// <summary>Gets or sets lollipop orientation. Horizontal keeps source X as a category on Y and renders values on the physical X axis.</summary>
    /// <remarks>Vertical is the default. All series in one horizontal Cartesian chart must use horizontal orientation; primary and secondary values use XAxis and SecondaryXAxis.</remarks>
    public ChartOrientation LollipopOrientation {
        get => _lollipopOrientation;
        set { ValidateComparisonOrientation(value); _lollipopOrientation = value; }
    }

    /// <summary>Gets or sets dumbbell orientation without reordering authored Start and End values.</summary>
    /// <remarks>Horizontal keeps source X as a category on Y, with Start and End on XAxis or SecondaryXAxis. Vertical is the default.</remarks>
    public ChartOrientation DumbbellOrientation {
        get => _dumbbellOrientation;
        set { ValidateComparisonOrientation(value); _dumbbellOrientation = value; }
    }

    /// <summary>Gets or sets slope orientation without sorting or replacing its stage observations.</summary>
    /// <remarks>Vertical is the default. Horizontal places source X stages on physical Y and source Y values on XAxis or SecondaryXAxis.
    /// Every series in a horizontal Cartesian mixture must use horizontal orientation.</remarks>
    public ChartOrientation SlopeOrientation {
        get => _slopeOrientation;
        set { ValidateComparisonOrientation(value); _slopeOrientation = value; }
    }

    private static void ValidateComparisonOrientation(ChartOrientation orientation) {
        if (!Enum.IsDefined(typeof(ChartOrientation), orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
    }
}
