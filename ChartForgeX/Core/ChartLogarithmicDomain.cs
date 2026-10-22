namespace ChartForgeX.Core;

/// <summary>Selects the nonzero sign domain of a logarithmic axis.</summary>
public enum ChartLogarithmicDomain {
    /// <summary>Accept positive source values and bounds only.</summary>
    Positive,

    /// <summary>Accept negative source values and bounds only, preserving their increasing source order.</summary>
    Negative
}
