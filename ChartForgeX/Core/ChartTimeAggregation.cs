namespace ChartForgeX.Core;

/// <summary>Selects how timed values that fall into the same bucket are combined.</summary>
public enum ChartTimeAggregation {
    /// <summary>Counts the values; empty buckets are zero.</summary>
    Count,

    /// <summary>Adds the values; empty buckets are zero.</summary>
    Sum,

    /// <summary>Averages the values; empty buckets are masked.</summary>
    Mean,

    /// <summary>Takes the largest value; empty buckets are masked.</summary>
    Maximum
}
