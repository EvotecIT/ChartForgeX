namespace ChartForgeX.Core;

/// <summary>Immutable series facts supplied to preparation-time presentation callbacks.</summary>
public sealed class ChartSeriesInfo {
    internal ChartSeriesInfo(ChartSeries series) { Id = series.Id; Name = series.Name; Kind = series.Kind; State = series.StateRole; }
    /// <summary>Gets the stable authored or chart-local series identity.</summary>
    public ChartSeriesId Id { get; }
    /// <summary>Gets the display name at preparation time.</summary>
    public string Name { get; }
    /// <summary>Gets the rendering family.</summary>
    public ChartSeriesKind Kind { get; }
    /// <summary>Gets the semantic series state independently of its explicit paint.</summary>
    public ChartSeriesState State { get; }
}

/// <summary>Typed authored observation and static presentation state for one item-styling invocation.</summary>
/// <typeparam name="TDatum">The family's authored observation type, such as ChartPoint, ChartBubble or ChartHierarchyItem.</typeparam>
public sealed class ChartItemStyleContext<TDatum> {
    internal ChartItemStyleContext(TDatum datum, ChartSeriesInfo series, int index, ChartMarkOptions? options, ChartSeriesState? state = null) {
        Datum = datum; Series = series; Index = index; State = state ?? series.State;
        IsCurrent = options?.Current ?? false; IsSelected = options?.Selected ?? false;
    }
    /// <summary>Gets the complete authored observation, rather than a renderer's numeric projection.</summary>
    public TDatum Datum { get; }
    /// <summary>Gets detached series identity, name, family and semantic state.</summary>
    public ChartSeriesInfo Series { get; }
    /// <summary>Gets the zero-based authored display observation ordinal, before geometry sorting or clipping.</summary>
    public int Index { get; }
    /// <summary>Gets the effective authored semantic state, including a node or flow override where present.</summary>
    public ChartSeriesState State { get; }
    /// <summary>Gets whether the observation's static mark metadata declares it current.</summary>
    public bool IsCurrent { get; }
    /// <summary>Gets whether the observation's static mark metadata declares it selected.</summary>
    public bool IsSelected { get; }
    /// <summary>Gets whether the static observation is current or selected. Browser hover remains adapter-owned.</summary>
    public bool IsHighlighted => IsCurrent || IsSelected;
}
