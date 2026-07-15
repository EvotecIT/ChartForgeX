namespace ChartForgeX.Data;

/// <summary>Represents one equal-width numeric bin and its source rows.</summary>
public sealed class ChartDataBin<T> {
    internal ChartDataBin(double lowerBound, double upperBound, ChartDataset<T> dataset) {
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Dataset = dataset;
    }

    /// <summary>Gets the inclusive lower bound.</summary>
    public double LowerBound { get; }

    /// <summary>Gets the upper bound. The final bin includes its upper bound.</summary>
    public double UpperBound { get; }

    /// <summary>Gets the midpoint used for chart coordinates.</summary>
    public double Center => (LowerBound + UpperBound) / 2.0;

    /// <summary>Gets the number of source rows in the bin.</summary>
    public int Count => Dataset.Count;

    /// <summary>Gets the source rows in the bin.</summary>
    public ChartDataset<T> Dataset { get; }
}
