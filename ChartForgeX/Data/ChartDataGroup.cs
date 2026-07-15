namespace ChartForgeX.Data;

/// <summary>Represents one deterministic dataset group.</summary>
public sealed class ChartDataGroup<TKey, T> {
    internal ChartDataGroup(TKey key, ChartDataset<T> dataset) {
        Key = key;
        Dataset = dataset;
    }

    /// <summary>Gets the grouping key.</summary>
    public TKey Key { get; }

    /// <summary>Gets the rows in this group.</summary>
    public ChartDataset<T> Dataset { get; }
}
