using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a treemap forest. Parent areas aggregate leaf sizes; explicit IDs identify all nodes independently of labels.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The authored groups and leaves. Standalone root leaves also support flat treemaps.</param>
    /// <param name="color">An optional base color. When null, the theme palette colors the tiles.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTreemap(string name, IEnumerable<ChartHierarchyItem> items, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = new ChartSeries(name, ChartSeriesKind.Treemap, Array.Empty<ChartPoint>()) { Color = color };
        series.SetRelationships(ChartRelationshipIndex.AreaHierarchy(items, allowForest: true, requireNullGroups: true));
        AppendSeries(series);
        return this;
    }

    /// <summary>Configures treemap spacing, group captions and the independent numeric color scale.</summary>
    public Chart ConfigureTreemap(Action<ChartTreemapOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.Treemap);
        return this;
    }
}
