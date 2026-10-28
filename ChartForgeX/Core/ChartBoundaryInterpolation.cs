using System;

namespace ChartForgeX.Core;

/// <summary>Immutable interpolation and step placement for one member of a paired area.</summary>
public readonly struct ChartBoundaryInterpolation {
    /// <summary>Gets the connected boundary interpolation.</summary>
    public ChartInterpolation Interpolation { get; }
    private readonly ChartStepPosition _stepPosition;
    /// <summary>Gets the transition placement for step interpolation.</summary>
    public ChartStepPosition StepPosition => Interpolation == ChartInterpolation.Step ? _stepPosition : ChartStepPosition.End;

    /// <summary>Creates a boundary policy. Linear and Smooth require the default End step position.</summary>
    public ChartBoundaryInterpolation(ChartInterpolation interpolation, ChartStepPosition stepPosition = ChartStepPosition.End) {
        if (!Enum.IsDefined(typeof(ChartInterpolation), interpolation)) throw new ArgumentOutOfRangeException(nameof(interpolation));
        if (!Enum.IsDefined(typeof(ChartStepPosition), stepPosition)) throw new ArgumentOutOfRangeException(nameof(stepPosition));
        if (interpolation != ChartInterpolation.Step && stepPosition != ChartStepPosition.End)
            throw new ArgumentException("A step position requires step interpolation.", nameof(stepPosition));
        Interpolation = interpolation; _stepPosition = stepPosition;
    }
}
