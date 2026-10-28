namespace ChartForgeX.Core;

/// <summary>Pointwise uncertainty on the fitted response scale, rather than simultaneous coverage of the whole curve.</summary>
public enum ChartTrendLineIntervalKind {
    /// <summary>Draws the fitted curve without an uncertainty interval.</summary>
    None,
    /// <summary>Confidence interval for the mean response; log-fitted models instead report a median confidence interval.</summary>
    Confidence,
    /// <summary>Prediction interval for one independent future response, including residual variation.</summary>
    Prediction
}
