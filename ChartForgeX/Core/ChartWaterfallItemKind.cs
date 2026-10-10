namespace ChartForgeX.Core;

/// <summary>Distinguishes authored numeric observations from calculated waterfall checkpoints.</summary>
public enum ChartWaterfallItemKind {
    /// <summary>A supplied positive, negative or zero change to the running balance.</summary>
    Delta,
    /// <summary>The sum of changes since the opening balance or preceding subtotal or total.</summary>
    Subtotal,
    /// <summary>The opening balance plus every preceding change, drawn from zero.</summary>
    Total,
    /// <summary>A supplied positive, negative or zero initial balance, drawn from zero before any changes.</summary>
    OpeningBalance
}
