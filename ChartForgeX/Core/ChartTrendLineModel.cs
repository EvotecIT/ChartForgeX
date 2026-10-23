namespace ChartForgeX.Core;

/// <summary>The least-squares model fitted to a trend line's retained observations.</summary>
public enum ChartTrendLineModel {
    /// <summary>Fits Y = a + bX in the original response scale.</summary>
    Linear,
    /// <summary>Fits a quadratic or cubic polynomial in the original response scale.</summary>
    Polynomial,
    /// <summary>Fits Y = a + b ln(X), requiring positive X.</summary>
    Logarithmic,
    /// <summary>Fits ln(Y) = a + bX, requiring positive Y; the displayed curve estimates the conditional median.</summary>
    Exponential,
    /// <summary>Fits ln(Y) = a + b ln(X), requiring positive X and Y; the displayed curve estimates the conditional median.</summary>
    Power
}
