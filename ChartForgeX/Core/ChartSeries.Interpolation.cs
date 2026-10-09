using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartInterpolation _interpolation;
    private ChartStepPosition _stepPosition = ChartStepPosition.End;

    /// <summary>
    /// Gets or sets the interpolation of connected line, area and range boundaries.
    /// Step-line and step-area series default to <see cref="ChartInterpolation.Step"/>;
    /// other series default to <see cref="ChartInterpolation.Linear"/>.
    /// Members of one stacked-area group and axis must use the same interpolation and step position.
    /// </summary>
    public ChartInterpolation Interpolation {
        get => _interpolation;
        set {
            if (!Enum.IsDefined(typeof(ChartInterpolation), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown interpolation.");
            _interpolation = value;
            if (value != ChartInterpolation.Step) _stepPosition = ChartStepPosition.End;
        }
    }

    /// <summary>
    /// Gets or sets where step interpolation changes value. The default is End.
    /// A non-default position requires <see cref="ChartInterpolation.Step"/> during preparation.
    /// </summary>
    public ChartStepPosition StepPosition {
        get => _stepPosition;
        set {
            if (!Enum.IsDefined(typeof(ChartStepPosition), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown step position.");
            _stepPosition = value;
        }
    }

    /// <summary>Sets interpolation and, for a step, its transition position.</summary>
    /// <param name="interpolation">The interpolation applied to each connected source segment.</param>
    /// <param name="stepPosition">The step transition position; other modes require End.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithInterpolation(ChartInterpolation interpolation, ChartStepPosition stepPosition = ChartStepPosition.End) {
        if (!Enum.IsDefined(typeof(ChartInterpolation), interpolation)) throw new ArgumentOutOfRangeException(nameof(interpolation));
        if (!Enum.IsDefined(typeof(ChartStepPosition), stepPosition)) throw new ArgumentOutOfRangeException(nameof(stepPosition));
        if (interpolation != ChartInterpolation.Step && stepPosition != ChartStepPosition.End)
            throw new ArgumentException("A step position requires step interpolation.", nameof(stepPosition));
        Interpolation = interpolation;
        StepPosition = stepPosition;
        return this;
    }

    internal void ValidateInterpolation() {
        if (Interpolation != ChartInterpolation.Step && StepPosition != ChartStepPosition.End)
            throw new InvalidOperationException("Series '" + Name + "' requires step interpolation for its step position.");
        if (Interpolation == ChartInterpolation.Linear) return;
        if (Kind != ChartSeriesKind.Line && Kind != ChartSeriesKind.StepLine && Kind != ChartSeriesKind.Area
            && Kind != ChartSeriesKind.StepArea && Kind != ChartSeriesKind.StackedArea
            && Kind != ChartSeriesKind.RangeBand && Kind != ChartSeriesKind.RangeArea && Kind != ChartSeriesKind.TrendLine)
            throw new InvalidOperationException("Series '" + Name + "' does not support connected-path interpolation.");
    }
}
