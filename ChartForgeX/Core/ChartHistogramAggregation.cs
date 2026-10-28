namespace ChartForgeX.Core;

/// <summary>Specifies the quantity represented by the observations in each histogram interval.</summary>
public enum ChartHistogramAggregation {
    /// <summary>Counts observations, ignoring their quantities.</summary>
    Count,
    /// <summary>Sums the signed quantities of the observations.</summary>
    Sum,
    /// <summary>Computes the arithmetic mean of quantities; an empty bin has no aggregate.</summary>
    Mean,
    /// <summary>Computes the quantity mean using nonnegative observation weights; a bin with no positive weight has no aggregate.</summary>
    WeightedMean
}

/// <summary>Specifies how raw histogram aggregates map to value, density or probability heights.</summary>
public enum ChartHistogramEncoding {
    /// <summary>Uses the aggregate as the bar height.</summary>
    Value,
    /// <summary>Uses aggregate divided by the actual bin width, so the full rectangular area represents the aggregate.</summary>
    Density,
    /// <summary>Uses each nonnegative Count or Sum aggregate divided by the series total. A zero total retains zero heights.</summary>
    Probability,
    /// <summary>Uses each nonnegative Count or Sum aggregate divided by the series total and actual interval width, so rectangular areas sum to one for a positive total.</summary>
    ProbabilityDensity
}
