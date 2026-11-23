using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a legend entry that does not come from a series, listed after the series entries and never toggleable.</summary>
    /// <param name="item">The entry, for example <see cref="ChartLegendItem.Line"/> for a reference line.</param>
    /// <returns>The current chart.</returns>
    public Chart AddLegendItem(ChartLegendItem item) { Options.Legend.Items.Add(item ?? throw new ArgumentNullException(nameof(item))); return this; }
}
