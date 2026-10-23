namespace ChartForgeX.VisualBlocks;

/// <summary>Bounds one native grammar independently of frontend, portable encoding or layout.</summary>
internal static class RailroadDiagramLimits {
    internal const int MaximumRules = 512;
    internal const int MaximumExpressionOccurrences = 32768;
    internal const int MaximumExpressionDepth = 128;
}
