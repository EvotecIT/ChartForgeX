using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Captures a native visibility view through the incumbent prepared HTML section and lifetime.</summary>
    /// <param name="chart">The retained complete source chart with authored series identities.</param>
    /// <param name="visibility">Immutable absolute targets, native layout and label layering policy; null preserves ordinary preparation.</param>
    /// <param name="viewport">Optional existing Cartesian source window.</param>
    /// <param name="configure">Existing HTML host configuration.</param>
    /// <remarks>Preserve retains full-data domains/layout. Explicit Reflow recomputes finite Line/Bar, supplied/raw BoxPlot and single-series Pie/Donut observation or captured full-source Other membership, and authored Treemap/Sunburst subtree/root membership through native owners while retaining the full catalog/legend. Preserve partial motion with enabled labels requires
    /// explicit GroupWithSeries, whose native series/context paint ordering differs from the default global overlay.</remarks>
    public HtmlChartSection PrepareVisibilitySection(Chart chart, ChartVisibilityView? visibility = null, ChartCartesianViewport? viewport = null,
        Action<HtmlChartInteractionOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions(); configure?.Invoke(options);
        return PrepareChartSection(chart, options, options.PageTitle ?? ChartTitle(chart, "ChartForgeX interactive chart"), "host", viewport, visibility: visibility);
    }
    private static void AppendPreparedVisibility(StringBuilder json, PreparedVisual prepared) {
        var captured = prepared.Scene.VisibilityCapture; if (captured == null) return;
        if (captured.ReflowSource != null) { AppendPreparedReflowVisibility(json, captured); return; }
        json.Append(",\"visibility\":{\"labelLayering\":").Append(HtmlJsonString.Encode(captured.View.LabelLayering == ChartVisibilityLabelLayering.GroupWithSeries ? "groupWithSeries" : "preserveOverlay"))
            .Append(",\"hiddenSeries\":[");
        var first = true; foreach (var key in captured.SeriesKeys.Where(captured.View.IsHidden)) {
            if (!first) json.Append(','); first = false; json.Append(HtmlJsonString.Encode(key));
        }
        json.Append("]}");
    }
}
