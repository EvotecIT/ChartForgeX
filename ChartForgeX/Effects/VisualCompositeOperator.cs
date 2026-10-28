namespace ChartForgeX.Effects;

/// <summary>Selects one explicit Porter-Duff operation over a source and a destination backdrop.</summary>
/// <remarks>Channels use the existing native effect's gamma-encoded sRGB premultiplied16 owner.
/// Channel blending and additive PlusLighter remain VisualBlendMode; this enum does not duplicate them.</remarks>
public enum VisualCompositeOperator {
    /// <summary>Places source over the destination.</summary>
    SourceOver = 0,
    /// <summary>Keeps source only where destination alpha covers it.</summary>
    SourceIn = 1,
    /// <summary>Keeps source only outside destination alpha.</summary>
    SourceOut = 2,
    /// <summary>Places source atop destination while retaining destination coverage.</summary>
    SourceAtop = 3,
    /// <summary>Places destination over source.</summary>
    DestinationOver = 4,
    /// <summary>Keeps destination only where source alpha covers it.</summary>
    DestinationIn = 5,
    /// <summary>Removes destination where source alpha covers it.</summary>
    DestinationOut = 6,
    /// <summary>Places destination atop source while retaining source coverage.</summary>
    DestinationAtop = 7,
    /// <summary>Keeps the nonoverlapping source and destination contributions.</summary>
    Xor = 8,
    /// <summary>Copies exact source RGBA inside its extent and clears the remaining destination frame.</summary>
    Copy = 9,
    /// <summary>Clears the complete destination frame to transparent black.</summary>
    Clear = 10
}
