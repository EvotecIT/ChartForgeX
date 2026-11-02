namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    // Native category filtering keeps the full source's rank, independently of sparse point overrides.
    internal int[]? CategoryPaletteOrdinals { get; private set; }
}
