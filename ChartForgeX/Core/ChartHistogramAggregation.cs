namespace ChartForgeX.Core;

/// <summary>Specifies the quantity represented by the observations in each histogram interval.</summary>
public enum ChartHistogramAggregation {
    /// <summary>Counts observations, ignoring their quantities.</summary>
    Count,
    /// <summary>Sums the signed quantities of the observations.</summary>
    Sum,
    /// <summary>Computes the arithmetic mean of quantities; an empty bin has no aggregate.</summary>
    Mean
}

/// <summary>Specifies whether histogram height represents a quantity or quantity per measurement unit.</summary>
public enum ChartHistogramEncoding {
    /// <summary>Uses the aggregate as the bar height.</summary>
    Value,
    /// <summary>Uses aggregate divided by the actual bin width, so the full rectangular area represents the aggregate.</summary>
    Density
}
