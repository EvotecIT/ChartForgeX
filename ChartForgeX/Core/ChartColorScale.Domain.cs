using System;

namespace ChartForgeX.Core;

public sealed partial class ChartColorScale {
    /// <summary>Resolves optional observations without inventing a numeric endpoint for an empty source.</summary>
    internal (double? Minimum, double? Maximum) ResolveDomain(double? sourceMinimum, double? sourceMaximum) {
        EnsureContinuous();
        if (sourceMinimum.HasValue != sourceMaximum.HasValue)
            throw new ArgumentException("Observed color bounds must either both be supplied or both be absent.");
        if (sourceMinimum.HasValue) ValidateSourceRange(sourceMinimum.Value, sourceMaximum!.Value);
        return (sourceMinimum.HasValue ? EffectiveMinimum(sourceMinimum.Value) : MinimumValue,
            sourceMaximum.HasValue ? EffectiveMaximum(sourceMaximum.Value) : MaximumValue);
    }

    /// <summary>Uses the same effective range and collapsed-endpoint policy as continuous paint.</summary>
    internal double DomainRatio(double value, double sourceMinimum, double sourceMaximum) {
        ValidateFinite(value, nameof(value));
        var (min, max) = ResolveRange(sourceMinimum, sourceMaximum);
        return max == min ? CollapsedRatio(value, min) : Ratio(value, min, max);
    }

    private (double Minimum, double Maximum) ResolveRange(double sourceMinimum, double sourceMaximum) {
        var domain = ResolveDomain(sourceMinimum, sourceMaximum);
        return (domain.Minimum!.Value, domain.Maximum!.Value);
    }

    private double CollapsedRatio(double value, double boundary) {
        if (MinimumValue.HasValue && !MaximumValue.HasValue) return value > boundary ? 1 : 0;
        if (MaximumValue.HasValue && !MinimumValue.HasValue) return value < boundary ? 0 : 1;
        return 0;
    }
}
