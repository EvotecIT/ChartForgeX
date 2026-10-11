namespace ChartForgeX.Core;

/// <summary>Provides captured authored interval facts and numeric text to an endpoint caption formatter.</summary>
public readonly struct ChartRangeBarEndpointLabelContext {
    /// <summary>Creates a context for one endpoint of an authored observation.</summary>
    public ChartRangeBarEndpointLabelContext(string seriesName, int pointIndex, string categoryLabel,
        ChartInterval interval, ChartRangeBarEndpoint endpoint, string formattedValue) {
        SeriesName = seriesName; PointIndex = pointIndex; CategoryLabel = categoryLabel;
        Interval = interval; Endpoint = endpoint; FormattedValue = formattedValue;
    }

    /// <summary>Gets the captured series name.</summary>
    public string SeriesName { get; }
    /// <summary>Gets the zero-based authored observation index, rather than an encoded endpoint index.</summary>
    public int PointIndex { get; }
    /// <summary>Gets the complete resolved category caption.</summary>
    public string CategoryLabel { get; }
    /// <summary>Gets the authored category coordinate and raw Start/End pair.</summary>
    public ChartInterval Interval { get; }
    /// <summary>Gets the authored endpoint identity, unaffected by reversal or descending values.</summary>
    public ChartRangeBarEndpoint Endpoint { get; }
    /// <summary>Gets the raw value of the selected endpoint.</summary>
    public double Value => Endpoint == ChartRangeBarEndpoint.Start ? Interval.Start : Interval.End;
    /// <summary>Gets the endpoint value captured with the chart's numeric display policy.</summary>
    public string FormattedValue { get; }
}
