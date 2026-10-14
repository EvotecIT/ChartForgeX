using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double? _polarGridFillOpacity;
    /// <summary>Gets or sets immutable spoke-stroke overrides for dedicated radar and polar charts. Null uses the prepared axis color and width; a zero width hides spoke ink.</summary>
    public ChartStrokeStyle? PolarAxisStroke { get; set; }

    /// <summary>Gets or sets immutable concentric-guide stroke overrides for dedicated radar and polar charts. Null uses the prepared grid color and width; a zero width hides guide outlines.</summary>
    public ChartStrokeStyle? PolarGridStroke { get; set; }

    /// <summary>Gets or sets the concentric guides' fill color, including alpha. Null uses the guide stroke color when a fill opacity is authored. Nested guides composite in drawing order in both native exporters.</summary>
    public ChartColor? PolarGridFill { get; set; }

    /// <summary>Gets or sets the guide-fill opacity multiplier from zero through one. Null retains an explicit fill's alpha or keeps guides transparent when no fill is authored.</summary>
    public double? PolarGridFillOpacity {
        get => _polarGridFillOpacity;
        set {
            if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value));
            _polarGridFillOpacity = value;
        }
    }
}
