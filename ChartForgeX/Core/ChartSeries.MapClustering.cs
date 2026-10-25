namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets optional immutable screen-grid aggregation. Null preserves individual source marks.</summary>
    public ChartMapClustering? MapClustering { get; private set; }

    /// <summary>Replaces this DottedMap dataset's clustering policy without changing any observation or its identity.</summary>
    /// <param name="clustering">The immutable policy, or null to restore individual source marks.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithMapClustering(ChartMapClustering? clustering) {
        RequireObservationKind(ChartSeriesKind.DottedMap); MapClustering = clustering; return this;
    }
}
