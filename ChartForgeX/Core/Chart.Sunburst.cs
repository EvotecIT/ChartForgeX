using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a single-root Sunburst with explicit IDs, authored sizes and independent numeric colors.</summary>
    /// <remarks>Leaves require finite non-negative sizes. Groups aggregate leaves by default; authored-total sizing preserves unused capacity.</remarks>
    public Chart AddSunburst(string name, IEnumerable<ChartHierarchyItem> items, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = new ChartSeries(name, ChartSeriesKind.Sunburst, Array.Empty<ChartPoint>()) { Color = color };
        var facts = ChartRelationshipIndex.AreaHierarchy(items, allowForest: false, requireNullGroups: false);
        _ = facts.ResolveHierarchyValues(Options.Sunburst.ParentValuePolicy);
        series.SetRelationships(facts);
        AppendSeries(series);
        return this;
    }

    /// <summary>Configures Sunburst parent-size interpretation and independent numeric color.</summary>
    public Chart ConfigureSunburst(Action<ChartSunburstOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.Sunburst);
        return this;
    }
}
