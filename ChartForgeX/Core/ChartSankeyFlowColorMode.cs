namespace ChartForgeX.Core;

/// <summary>Selects ribbon paint without changing Sankey weights, endpoint allocation or node geometry.</summary>
public enum ChartSankeyFlowColorMode {
    /// <summary>Inherits source-node paint, state and pattern. Explicit ID-keyed flow overrides retain precedence.</summary>
    Source,
    /// <summary>Inherits target-node paint, state and pattern. Explicit ID-keyed flow overrides retain precedence.</summary>
    Target,
    /// <summary>Maps independent ChartFlowLink.ColorValue measurements through ColorScale; missing measurements use its no-data paint.</summary>
    /// <remarks>Explicit flow, source-point, family and series colors retain precedence. All flows contribute color-domain facts, including omitted or precision-collapsed ribbons.</remarks>
    Quantitative,
    /// <summary>Interpolates the existing source and target paint policies along each ribbon. Explicit ID-keyed flow fill remains solid.</summary>
    /// <remarks>Both endpoint stops retain point, family, series and semantic paint precedence; geometry and raw flow facts are unchanged.</remarks>
    Gradient
}
