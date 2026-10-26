namespace ChartForgeX.Raster;

/// <summary>One internal cumulative reservation of declared pixel arrays and sample visits, without refunds or allocation policy.</summary>
internal sealed class RasterCumulativeDebit {
    internal RasterCumulativeDebit(long maximumBytes, long maximumWorkSamples) {
        MaximumBytes = maximumBytes; MaximumWorkSamples = maximumWorkSamples;
    }
    internal long MaximumBytes { get; }
    internal long MaximumWorkSamples { get; }
    internal long Bytes { get; private set; }
    internal long WorkSamples { get; private set; }

    /// <summary>Reserves both amounts atomically using the incumbent subtraction guards; rejection leaves both totals unchanged.</summary>
    internal bool TryReserve(long bytes, long workSamples) {
        if (bytes < 0 || workSamples < 0 || bytes > MaximumBytes - Bytes || workSamples > MaximumWorkSamples - WorkSamples) return false;
        Bytes += bytes; WorkSamples += workSamples;
        return true;
    }
}
