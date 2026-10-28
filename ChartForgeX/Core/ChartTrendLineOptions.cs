using System;

namespace ChartForgeX.Core;

/// <summary>Configures one retained-source regression; installing configuration refits the complete source atomically.</summary>
public sealed class ChartTrendLineOptions {
    /// <summary>Gets or sets the fit model. Linear preserves the existing least-squares endpoint behavior.</summary>
    public ChartTrendLineModel Model { get; set; }
    /// <summary>Gets or sets the Polynomial model's degree, either two or three. The default is two.</summary>
    public int PolynomialDegree { get; set; } = 2;
    /// <summary>Gets or sets the optional pointwise interval. The default is None.</summary>
    public ChartTrendLineIntervalKind Interval { get; set; }
    /// <summary>Gets or sets the pointwise confidence level, from 0.5 through 0.999. The default is 0.95.</summary>
    /// <remarks>Student t intervals assume independent normal constant-variance errors on the fitted response scale.
    /// Exponential and Power models fit log responses; their confidence interval is for the conditional median,
    /// not an arithmetic-mean correction. These intervals do not diagnose model adequacy or establish simultaneous coverage.</remarks>
    public double ConfidenceLevel { get; set; } = .95;
    /// <summary>Gets or sets uniform source-X samples, from 17 through 513. The default is 129.</summary>
    /// <remarks>Curves and ribbons are native piecewise-linear geometry. Polynomial stationary points are retained
    /// in addition to these samples. Ordinary Linear without an interval retains only its two existing endpoints.</remarks>
    public int SampleCount { get; set; } = 129;

    internal ChartTrendLineOptions Capture() {
        if (!Enum.IsDefined(typeof(ChartTrendLineModel), Model)) throw new ArgumentOutOfRangeException(nameof(Model));
        if (PolynomialDegree < 2 || PolynomialDegree > 3) throw new ArgumentOutOfRangeException(nameof(PolynomialDegree), "Polynomial degree must be two or three.");
        if (!Enum.IsDefined(typeof(ChartTrendLineIntervalKind), Interval)) throw new ArgumentOutOfRangeException(nameof(Interval));
        ChartGuards.Finite(ConfidenceLevel, nameof(ConfidenceLevel));
        if (ConfidenceLevel < .5 || ConfidenceLevel > .999) throw new ArgumentOutOfRangeException(nameof(ConfidenceLevel));
        if (SampleCount < 17 || SampleCount > 513) throw new ArgumentOutOfRangeException(nameof(SampleCount));
        return (ChartTrendLineOptions)MemberwiseClone();
    }
}
