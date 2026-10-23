namespace ChartForgeX.Core;

/// <summary>Identifies one value in the rendered five-number summary, independent of physical orientation or reversal.</summary>
public enum ChartBoxPlotStatistic {
    /// <summary>The lower whisker, which is not necessarily the smallest raw sample under IQR policy.</summary>
    Minimum,
    /// <summary>The first quartile.</summary>
    Q1,
    /// <summary>The median.</summary>
    Median,
    /// <summary>The third quartile.</summary>
    Q3,
    /// <summary>The upper whisker, which is not necessarily the largest raw sample under IQR policy.</summary>
    Maximum
}
