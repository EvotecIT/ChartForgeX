namespace ChartForgeX.Core;

/// <summary>Distinguishes authored changes from calculated waterfall checkpoints.</summary>
public enum ChartWaterfallItemKind {
    /// <summary>A supplied positive, negative or zero change to the running balance.</summary>
    Delta,
    /// <summary>The sum of changes since the preceding subtotal or total.</summary>
    Subtotal,
    /// <summary>The sum of every preceding change, drawn from zero.</summary>
    Total
}
