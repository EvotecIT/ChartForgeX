namespace ChartForgeX.Core;

/// <summary>Provides captured summary facts and numeric text for one box plot statistic caption.</summary>
public readonly struct ChartBoxPlotStatisticLabelContext {
    /// <summary>Creates a context for one statistic of an authored summary or derived distribution.</summary>
    public ChartBoxPlotStatisticLabelContext(string seriesName, int pointIndex, string categoryLabel,
        ChartBoxPlot summary, ChartBoxPlotStatistic statistic, string formattedValue) {
        SeriesName = seriesName; PointIndex = pointIndex; CategoryLabel = categoryLabel;
        Summary = summary; Statistic = statistic; FormattedValue = formattedValue;
    }

    /// <summary>Gets the captured series name.</summary>
    public string SeriesName { get; }
    /// <summary>Gets the zero-based summary or group index, not an encoded tuple or outlier index.</summary>
    public int PointIndex { get; }
    /// <summary>Gets the complete resolved category caption.</summary>
    public string CategoryLabel { get; }
    /// <summary>Gets the rendered summary, including IQR whiskers when selected. Raw sample extrema remain separate source facts.</summary>
    public ChartBoxPlot Summary { get; }
    /// <summary>Gets the summary member being captioned.</summary>
    public ChartBoxPlotStatistic Statistic { get; }
    /// <summary>Gets the raw rendered value of the selected summary member.</summary>
    public double Value => Statistic switch {
        ChartBoxPlotStatistic.Minimum => Summary.Minimum, ChartBoxPlotStatistic.Q1 => Summary.Q1,
        ChartBoxPlotStatistic.Median => Summary.Median, ChartBoxPlotStatistic.Q3 => Summary.Q3,
        _ => Summary.Maximum
    };
    /// <summary>Gets the value captured with the chart's numeric display policy.</summary>
    public string FormattedValue { get; }
}
