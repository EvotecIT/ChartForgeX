using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the explicitly authored interval represented by each time-axis bar, or null for point observations.</summary>
    public ChartTimeTickInterval? TimeBucketInterval { get; private set; }

    /// <summary>Declares each ordinary bar's X value as the inclusive start of a time bucket.</summary>
    /// <remarks>The exclusive end is the start advanced by the interval. Linked target ranges expose an inclusive end
    /// immediately before that boundary at the browser's supported time precision. Days, weeks and months advance in the X axis's
    /// display time zone; shorter units advance elapsed UTC time. This does not infer intervals from point spacing or alter
    /// bar geometry. Preparation requires a time X axis. Null restores point-observation semantics.</remarks>
    /// <param name="interval">The immutable bucket interval, or null to clear it.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithTimeBuckets(ChartTimeTickInterval? interval) {
        if (Kind != ChartSeriesKind.Bar || HistogramBinLayout != null)
            throw new InvalidOperationException("Time buckets require an ordinary bar series.");
        TimeBucketInterval = interval;
        return this;
    }
}
