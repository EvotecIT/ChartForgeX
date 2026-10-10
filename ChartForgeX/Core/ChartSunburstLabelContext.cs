namespace ChartForgeX.Core;

/// <summary>Provides authored hierarchy facts and the resolved size to a Sunburst label formatter.</summary>
public readonly struct ChartSunburstLabelContext {
    /// <summary>Initializes a label context with the copied hierarchy item, resolved size and chart-formatted size.</summary>
    public ChartSunburstLabelContext(ChartHierarchyItem item, double value, string formattedValue) {
        Item = item;
        Value = value;
        FormattedValue = formattedValue;
    }

    /// <summary>Gets the authored item. Its nullable <see cref="ChartHierarchyItem.Value"/> remains the supplied size.</summary>
    public ChartHierarchyItem Item { get; }

    /// <summary>Gets the size resolved under <see cref="ChartSunburstOptions.ParentValuePolicy"/>.</summary>
    public double Value { get; }

    /// <summary>Gets the resolved size formatted with the chart's shared value formatter.</summary>
    public string FormattedValue { get; }
}
