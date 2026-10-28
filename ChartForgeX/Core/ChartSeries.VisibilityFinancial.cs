using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Captures authored overrides, retaining both directions for native rising/falling/doji resolution.</summary>
    internal IReadOnlyList<ChartFinancialItemOptions?> CaptureFinancialStyles() => Array.AsReadOnly(_financialStyles.Select(style => style?.Clone()).ToArray());

    internal long FinancialSourceValueCount => FinancialItemValueCount(Financial.Rising) + FinancialItemValueCount(Financial.Falling)
        + _financialStyles.Sum(style => style == null ? 1L : FinancialItemValueCount(style));
    private static long FinancialItemValueCount(ChartFinancialItemOptions item) => 32L + (item.DashPattern?.Count ?? 0) + (item.Wick.DashPattern?.Count ?? 0);

    private void RestoreVisibilityFinancial(ChartVisibilitySource.Series source, IReadOnlyList<int> selected) {
        Financial = source.CopyFinancial(); var styles = source.CopyFinancialStyles();
        _financialStyles = Array.AsReadOnly(RemapObservationOverrides(styles, selected));
    }
}
