using System;

namespace ChartForgeX.Rendering;

/// <summary>Internal finite resource fact translated by the existing source export policy when present.</summary>
internal sealed class VisualResourceLimitException : InvalidOperationException {
    internal VisualResourceLimitException(string limit, long maximum, long observed, string? itemId = null)
        : base("Visual resource limit '" + limit + "' is " + maximum + "; observed " + observed + ".") {
        Limit = limit; Maximum = maximum; Observed = observed; ItemId = itemId;
    }
    internal string Limit { get; }
    internal long Maximum { get; }
    internal long Observed { get; }
    internal string? ItemId { get; }
}
