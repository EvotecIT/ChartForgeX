namespace ChartForgeX.Primitives;

/// <summary>
/// Describes direction markers for a product-neutral visual link or connector.
/// </summary>
public enum VisualLinkDirection {
    /// <summary>No direction marker.</summary>
    None,
    /// <summary>Source-to-target direction marker.</summary>
    Forward,
    /// <summary>Target-to-source direction marker.</summary>
    Backward,
    /// <summary>Direction markers at both ends.</summary>
    Bidirectional
}
