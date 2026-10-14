using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a single-root Sunburst with explicit IDs, authored sizes and independent numeric colors.</summary>
    /// <remarks>Leaves require finite non-negative sizes. Groups aggregate leaves by default; authored-total sizing preserves unused capacity.</remarks>
    public Chart AddSunburst(string name, IEnumerable<ChartHierarchyItem> items, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = ChartSeries.CreateSunburst(name, items, color);
        _ = series.Relationships!.ResolveHierarchyValues(Options.Sunburst.ParentValuePolicy);
        AppendSeries(series);
        return this;
    }

    /// <summary>Configures Sunburst parent-size interpretation and independent numeric color.</summary>
    public Chart ConfigureSunburst(Action<ChartSunburstOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.Sunburst = ChartOptionConfiguration.Configure(Options.Sunburst, configure, options => options.Clone());
        return this;
    }
}
