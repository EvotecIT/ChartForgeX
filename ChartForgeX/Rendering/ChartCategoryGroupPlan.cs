using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Detaches numeric category membership and contiguous parent runs without measuring or projecting paint.</summary>
internal sealed class ChartCategoryGroupPlan {
    private readonly ChartAxis _axis;

    private ChartCategoryGroupPlan(ChartAxis axis, bool horizontal, ChartAxisLabel[] catalog, ChartAxisLabel[] bindings,
        ChartCategoryGroupRun[] runs) {
        _axis = axis.Clone();
        _axis.Labels.Clear(); _axis.Labels.AddRange(catalog);
        Horizontal = horizontal;
        Catalog = Array.AsReadOnly(catalog); Bindings = Array.AsReadOnly(bindings); Runs = Array.AsReadOnly(runs);
    }

    /// <summary>Returns a detached physical category-axis policy; legacy formatter routing remains with the axis owner.</summary>
    internal ChartAxis Axis => _axis.Clone();
    internal bool Horizontal { get; }
    internal IReadOnlyList<ChartAxisLabel> Catalog { get; }
    internal IReadOnlyList<ChartAxisLabel> Bindings { get; }
    internal IReadOnlyList<ChartCategoryGroupRun> Runs { get; }

    /// <summary>Validates an opted-in chart and captures its catalog; flat charts retain their existing fast path.</summary>
    internal static ChartCategoryGroupPlan? TryCreate(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (!HasGroups(chart.Options.XAxis) && !HasGroups(chart.Options.YAxis) && !HasGroups(chart.Options.SecondaryYAxis)) return null;
        if (chart.Options.IsSparkline || chart.Series.Any(series => !VisualCartesianCompiler.Supports(series.Kind)
            || series.HistogramBinLayout != null || series.TimeBucketInterval != null))
            throw new NotSupportedException("Grouped categories require an ordinary Cartesian chart without histogram or time-bucket aggregation.");
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        var physical = ChartCartesianOrientation.CategoryAxis(chart);
        var labels = ChartCartesianOrientation.CategoryLabels(chart);
        var legacy = horizontal && ChartCartesianOrientation.UsesLegacyCategoryLabels(chart) && chart.Options.YAxis.Labels.Count == 0;
        if (HasGroups(chart.Options.SecondaryYAxis) || horizontal && !legacy && HasGroups(chart.Options.XAxis)
            || !horizontal && HasGroups(chart.Options.YAxis))
            throw new NotSupportedException("Grouped labels belong to the physical Cartesian category axis, not a value axis.");
        if (physical.Scale != ChartScaleKind.Linear)
            throw new NotSupportedException("Grouped categories require a linear category axis.");
        var catalog = CaptureCatalog(labels);
        var observed = chart.Series.SelectMany(ChartCartesianOrientation.Categories).Distinct().OrderBy(value => value).ToArray();
        var bindings = new ChartAxisLabel[observed.Length];
        for (var index = 0; index < observed.Length; index++) {
            var mapping = ChartAxisValueFormatter.FindExplicitMapping(catalog, observed[index], rejectAmbiguous: true)
                ?? throw new InvalidOperationException("Every observed grouped category coordinate requires an explicit leaf label.");
            bindings[index] = mapping.Groups.Count == 0 ? new ChartAxisLabel(observed[index], mapping.Text)
                : new ChartAxisLabel(observed[index], mapping.Text, mapping.Groups.ToArray());
        }
        return new ChartCategoryGroupPlan(physical, horizontal, catalog, bindings, CaptureRuns(catalog));
    }

    /// <summary>Builds full-domain source cells; clipping, reversal and pixel placement remain with the layout consumer.</summary>
    internal IReadOnlyList<ChartCategoryGroupSpan> CreateSpans(double fullMinimum, double fullMaximum) {
        ChartGuards.Finite(fullMinimum, nameof(fullMinimum)); ChartGuards.Finite(fullMaximum, nameof(fullMaximum));
        if (fullMaximum <= fullMinimum) throw new ArgumentOutOfRangeException(nameof(fullMaximum), "A category domain must be increasing.");
        var edges = new double[Catalog.Count + 1];
        edges[0] = Math.Min(fullMinimum, Catalog[0].Value);
        edges[Catalog.Count] = Math.Max(fullMaximum, Catalog[Catalog.Count - 1].Value);
        for (var index = 1; index < Catalog.Count; index++)
            edges[index] = ChartMath.InterpolateRange(Catalog[index - 1].Value, Catalog[index].Value, .5);
        return Array.AsReadOnly(Runs.Select(run => new ChartCategoryGroupSpan(run, edges[run.FirstLeafIndex], edges[run.LastLeafIndex + 1])).ToArray());
    }

    private static bool HasGroups(ChartAxis axis) => axis.Labels.Any(label => label.Groups.Count > 0);

    private static ChartAxisLabel[] CaptureCatalog(IReadOnlyList<ChartAxisLabel> labels) {
        var catalog = new List<ChartAxisLabel>();
        var depth = labels.Where(label => label.Groups.Count > 0).Select(label => label.Groups.Count).First();
        foreach (var label in labels.OrderBy(label => label.Value)) {
            ChartGuards.Finite(label.Value, nameof(labels));
            if (label.Text == null) throw new InvalidOperationException("Grouped category leaf text must not be null.");
            if (label.Groups.Count != 0 && label.Groups.Count != depth)
                throw new InvalidOperationException("Nonempty category group paths must have the same depth.");
            if (catalog.Count > 0 && catalog[catalog.Count - 1].Value == label.Value) {
                if (!catalog[catalog.Count - 1].Equals(label)) throw new InvalidOperationException("A category coordinate has conflicting leaf or group captions.");
                continue;
            }
            catalog.Add(label);
        }
        return catalog.ToArray();
    }

    private static ChartCategoryGroupRun[] CaptureRuns(IReadOnlyList<ChartAxisLabel> catalog) {
        var runs = new List<ChartCategoryGroupRun>();
        var depth = catalog.Max(label => label.Groups.Count);
        for (var level = 0; level < depth; level++) {
            var first = -1;
            for (var index = 0; index <= catalog.Count; index++) {
                var grouped = index < catalog.Count && catalog[index].Groups.Count > 0;
                if (first >= 0 && (!grouped || !SamePrefix(catalog[first].Groups, catalog[index].Groups, level))) {
                    runs.Add(new ChartCategoryGroupRun(level, catalog[first].Groups.Take(level + 1).ToArray(), first, index - 1,
                        catalog[first].Value, catalog[index - 1].Value));
                    first = -1;
                }
                if (grouped && first < 0) first = index;
            }
        }
        return runs.ToArray();
    }

    private static bool SamePrefix(IReadOnlyList<string> left, IReadOnlyList<string> right, int level) {
        for (var index = 0; index <= level; index++)
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal)) return false;
        return true;
    }
}

/// <summary>One contiguous prefix run in the sorted authored category catalog.</summary>
internal sealed class ChartCategoryGroupRun {
    internal ChartCategoryGroupRun(int depth, string[] path, int firstLeafIndex, int lastLeafIndex, double minimum, double maximum) {
        Depth = depth; Path = Array.AsReadOnly((string[])path.Clone());
        FirstLeafIndex = firstLeafIndex; LastLeafIndex = lastLeafIndex; Minimum = minimum; Maximum = maximum;
    }
    internal int Depth { get; }
    internal IReadOnlyList<string> Path { get; }
    internal int FirstLeafIndex { get; }
    internal int LastLeafIndex { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
}

/// <summary>Full source cell edges of a parent run, independent of the visible window.</summary>
internal sealed class ChartCategoryGroupSpan {
    internal ChartCategoryGroupSpan(ChartCategoryGroupRun run, double start, double end) { Run = run; Start = start; End = end; }
    internal ChartCategoryGroupRun Run { get; }
    internal double Start { get; }
    internal double End { get; }
}
