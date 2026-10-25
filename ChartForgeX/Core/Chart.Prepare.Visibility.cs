using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Prepares an immutable native visibility view with a retained source catalog and explicit Preserve or Reflow layout.</summary>
    /// <param name="context">The existing native frame, size, theme and captured typography.</param>
    /// <param name="visibility">Immutable authored visibility and label policy. Null uses ordinary preparation; Preserve retains whole-series Line/Bar layout; Reflow supports real Line/Bar, supplied/raw BoxPlot identities and single-series Pie/Donut observations or captured full-source Other membership, and authored Treemap/Sunburst hierarchy items and native roots.</param>
    /// <param name="viewport">Optional existing Cartesian source window for Preserve. Reflow rejects an explicit window.</param>
    /// <remarks>PreserveOverlay static endpoints keep globally placed visible labels and hide only labels owned by hidden series.
    /// Enabled-label partial motion requires explicit GroupWithSeries. That policy keeps placement but changes series/context occlusion.
    /// Native opacity uses the shared isolated paint/budget owner; large or deeply nested views may exceed its finite limits.</remarks>
    public PreparedVisual PrepareVisibility(VisualRenderContext context, ChartVisibilityView? visibility = null, ChartCartesianViewport? viewport = null) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (visibility == null) return PrepareCore(context, viewport);
        if (visibility.Layout == ChartVisibilityLayout.Reflow) return PrepareReflowVisibility(context, visibility, viewport);
        if (visibility.HiddenTargets.Any(target => target.Kind != ChartVisibilityTargetKind.Series))
            throw new NotSupportedException("Observation/group native visibility requires explicit Reflow; Preserve keeps the incumbent whole-series layout.");
        var labels = VisibilityAdmission(visibility);
        return VisualSeriesVisibilityCapture.Prepare(PrepareCore(context, viewport), visibility, labels);
    }

    /// <summary>Uses the incumbent complete aligned full-domain envelope, then applies the same native view to every drawing.</summary>
    internal (PreparedVisual First, PreparedVisual Second, PreparedVisual? FullFirst, PreparedVisual? FullSecond) PrepareAlignedVisibility(
        VisualRenderContext first, VisualRenderContext second, ChartVisibilityView visibility, ChartCartesianViewport? viewport = null, bool retainFullDrawings = false) {
        if (visibility.Layout == ChartVisibilityLayout.Reflow) return PrepareAlignedReflowVisibility(first, second, visibility, viewport, retainFullDrawings);
        if (visibility.HiddenTargets.Any(target => target.Kind != ChartVisibilityTargetKind.Series)) throw new NotSupportedException("Observation/group visibility requires Reflow.");
        var labels = VisibilityAdmission(visibility);
        var rows = PrepareAligned(first, second, viewport, retainFullDrawings);
        return (Apply(rows.First), Apply(rows.Second), rows.FullFirst == null ? null : Apply(rows.FullFirst), rows.FullSecond == null ? null : Apply(rows.FullSecond));
        PreparedVisual Apply(PreparedVisual source) => VisualSeriesVisibilityCapture.Prepare(source, visibility, labels);
    }

    private bool VisibilityAdmission(ChartVisibilityView visibility) {
        if (visibility == null) throw new ArgumentNullException(nameof(visibility));
        if (Options.ShowPointLegend) throw new NotSupportedException("Native visibility does not target point or derived legends.");
        if (Series.Count == 0 || Series.Any(series => series.Kind != ChartSeriesKind.Line && series.Kind != ChartSeriesKind.Bar))
            throw new NotSupportedException("Native visibility currently requires authored Line/Bar series; other families and point/group views are unsupported.");
        if (Series.Any(series => series.Id.IsAutomatic)) throw new NotSupportedException("Native visibility requires authored series identities.");
        foreach (var hidden in visibility.HiddenSeries) if (!Series.Any(series => series.Id == hidden))
            throw new ArgumentException("The visibility view contains an unknown source series.", nameof(visibility));
        return Series.Any(series => series.ResolveDataLabels(Options, defaultVisible: false));
    }
}
