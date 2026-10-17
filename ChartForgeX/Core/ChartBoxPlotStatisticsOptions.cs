using System;

namespace ChartForgeX.Core;

/// <summary>Identifies the calculation used for quartiles.</summary>
public enum ChartBoxPlotQuartileEstimator {
    /// <summary>Interpolates at zero-based position (n - 1) * p in sorted measurements.</summary>
    LinearInterpolated
}

/// <summary>Determines which measurements lie inside the whiskers.</summary>
public enum ChartBoxPlotWhiskerPolicy {
    /// <summary>Uses the complete observed minimum and maximum, without outliers.</summary>
    MinMax,
    /// <summary>Uses inclusive quartile fences and draws measurements outside them as outliers.</summary>
    Iqr
}

/// <summary>Configures calculation of raw box-plot groups.</summary>
public sealed class ChartBoxPlotStatisticsOptions {
    private ChartBoxPlotWhiskerPolicy _whiskerPolicy;
    private double _iqrMultiplier = 1.5;
    /// <summary>Gets the linearly interpolated quartile estimator.</summary>
    public ChartBoxPlotQuartileEstimator Estimator => ChartBoxPlotQuartileEstimator.LinearInterpolated;
    /// <summary>Gets or sets the whisker policy; the default is MinMax.</summary>
    public ChartBoxPlotWhiskerPolicy WhiskerPolicy {
        get => _whiskerPolicy;
        set {
            if (!Enum.IsDefined(typeof(ChartBoxPlotWhiskerPolicy), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _whiskerPolicy = value;
        }
    }
    /// <summary>Gets or sets the finite positive IQR multiplier; the default is 1.5.</summary>
    public double IqrMultiplier {
        get => _iqrMultiplier;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            _iqrMultiplier = value;
        }
    }
    internal ChartBoxPlotStatisticsOptions Clone() => new() { WhiskerPolicy = WhiskerPolicy, IqrMultiplier = IqrMultiplier };
}
