using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Polar axis and grid layout. Configure it through <see cref="ChartOptions.Polar"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartPolarLayoutOptions {
    internal ChartPolarLayoutOptions Clone() => (ChartPolarLayoutOptions)MemberwiseClone();
    private double _polarAxisScaleFactor = 1;

    private double _polarAxisLabelFactor = 1;


    /// <summary>Gets or sets the spoke radius relative to the radar or polar value-axis radius. Finite nonnegative factors preserve the observations and value domain; layout fits visible spokes inside the plot.</summary>
    public double AxisScaleFactor {
        get => _polarAxisScaleFactor;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _polarAxisScaleFactor = value;
        }
    }


    /// <summary>Gets or sets the category-label anchor radius relative to the radar or polar value-axis radius. Finite nonnegative factors retain the shared theme's label gap; layout reserves measured caption space.</summary>
    public double AxisLabelFactor {
        get => _polarAxisLabelFactor;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _polarAxisLabelFactor = value;
        }
    }

    private ChartPolarGridShape _polarGridShape;

    private double? _polarGridRingCount;


    /// <summary>
    /// Gets or sets the concentric grid shape for radar and polar line charts.
    /// Automatic preserves each native chart family's default. Series forms and value scales are unchanged.
    /// </summary>
    public ChartPolarGridShape GridShape {
        get => _polarGridShape;
        set {
            if (!Enum.IsDefined(typeof(ChartPolarGridShape), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _polarGridShape = value;
        }
    }


    /// <summary>
    /// Gets or sets the number of equally spaced radial intervals from zero through one hundred.
    /// Fractional values draw the ceiling number of guides with the authored interval divisor;
    /// the outer guide can extend beyond the value axis, and the scene fits that guide inside its viewport.
    /// Zero hides concentric guides; spokes and observations remain visible. Null uses the radial axis's generated ticks.
    /// This does not change the value domain or observed values.
    /// </summary>
    public double? GridRingCount {
        get => _polarGridRingCount;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0 || value.Value > 100)) throw new ArgumentOutOfRangeException(nameof(value));
            _polarGridRingCount = value;
        }
    }

    private double? _polarGridFillOpacity;

    /// <summary>Gets or sets immutable spoke-stroke overrides for dedicated radar and polar charts. Null uses the prepared axis color and width; a zero width hides spoke ink.</summary>
    public ChartStrokeStyle? AxisStroke { get; set; }


    /// <summary>Gets or sets immutable concentric-guide stroke overrides for dedicated radar and polar charts. Null uses the prepared grid color and width; a zero width hides guide outlines.</summary>
    public ChartStrokeStyle? GridStroke { get; set; }


    /// <summary>Gets or sets the concentric guides' fill color, including alpha. Null uses the guide stroke color when a fill opacity is authored. Nested guides composite in drawing order in both native exporters.</summary>
    public ChartColor? GridFill { get; set; }


    /// <summary>Gets or sets the guide-fill opacity multiplier from zero through one. Null retains an explicit fill's alpha or keeps guides transparent when no fill is authored.</summary>
    public double? GridFillOpacity {
        get => _polarGridFillOpacity;
        set {
            if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value));
            _polarGridFillOpacity = value;
        }
    }

    private double _polarRotationDegrees;


    /// <summary>Gets or sets the clockwise rotation of dedicated Radar, Polar and PolarArea marks, guides and captions in degrees. Zero preserves their existing starting rays.</summary>
    /// <remarks>Any finite rotation is accepted and reduced modulo one turn only for projection; source radians and categories stay unchanged.
    /// Mixed numeric radial charts keep their separate RadialGeometry start/end-angle policy and reject a nonzero value here.</remarks>
    public double RotationDegrees {
        get => _polarRotationDegrees;
        set { ChartGuards.Finite(value, nameof(value)); _polarRotationDegrees = value; }
    }
}
