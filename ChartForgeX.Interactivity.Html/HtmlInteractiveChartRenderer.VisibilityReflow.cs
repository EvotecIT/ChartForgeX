using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string VisibilityKind(ChartVisibilityTargetKind kind) => kind switch {
        ChartVisibilityTargetKind.Series => "series", ChartVisibilityTargetKind.Observation => "observation", ChartVisibilityTargetKind.BoxPlotGroup => "boxPlotGroup",
        ChartVisibilityTargetKind.RadialOther => "radialOther", ChartVisibilityTargetKind.HierarchyItem => "hierarchyItem", _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static void AppendVisibilityTargets(StringBuilder json, System.Collections.Generic.IEnumerable<ChartVisibilityTarget> targets) {
        var comma = false; foreach (var target in targets) {
            if (comma) json.Append(','); comma = true;
            json.Append("{\"kind\":").Append(HtmlJsonString.Encode(VisibilityKind(target.Kind)))
                .Append(",\"seriesKey\":").Append(HtmlJsonString.Encode(target.SeriesId.Value));
            if (target.NodeId != null) json.Append(",\"nodeId\":").Append(HtmlJsonString.Encode(target.NodeId));
            if (target.ObservationId.HasValue) json.Append(",\"observationId\":").Append(HtmlJsonString.Encode(target.ObservationId.Value.Value));
            json.Append('}');
        }
    }
    private static void AppendPreparedReflowVisibility(StringBuilder json, VisualSeriesVisibilityCapture captured) {
        json.Append(",\"visibility\":{\"labelLayering\":").Append(HtmlJsonString.Encode(captured.View.LabelLayering == ChartVisibilityLabelLayering.GroupWithSeries ? "groupWithSeries" : "preserveOverlay"))
            .Append(",\"hiddenSeries\":[");
        var comma = false; foreach (var key in captured.SeriesKeys.Where(captured.View.IsHidden)) {
            if (comma) json.Append(','); comma = true; json.Append(HtmlJsonString.Encode(key));
        }
        json.Append("],\"layout\":\"reflow\",\"sourceKey\":").Append(HtmlJsonString.Encode(captured.SourceKey!)).Append(",\"targetCatalog\":[");
        AppendVisibilityTargets(json, captured.TargetCatalog); json.Append("],\"hiddenTargets\":[");
        AppendVisibilityTargets(json, captured.TargetCatalog.Where(captured.View.IsHidden));
        json.Append(']');
        if (captured.ReflowSource?.Hierarchy != null) json.Append(",\"hierarchyRootId\":").Append(captured.View.HierarchyView == null ? "null" : HtmlJsonString.Encode(captured.View.HierarchyView.RootId));
        json.Append('}');
    }
    private static void AppendNativeReflowVisibility(StringBuilder json, PreparedVisual from, PreparedVisual to) {
        var first = from.Scene.VisibilityCapture!; var last = to.Scene.VisibilityCapture!;
        json.Append(",\"visibility\":{\"layout\":\"reflow\",\"labelLayering\":")
            .Append(HtmlJsonString.Encode(first.View.LabelLayering == ChartVisibilityLabelLayering.GroupWithSeries ? "groupWithSeries" : "preserveOverlay"))
            .Append(",\"targetCatalog\":["); AppendVisibilityTargets(json, first.TargetCatalog);
        json.Append("],\"fromHiddenTargets\":["); AppendVisibilityTargets(json, first.TargetCatalog.Where(first.View.IsHidden));
        json.Append("],\"toHiddenTargets\":["); AppendVisibilityTargets(json, last.TargetCatalog.Where(last.View.IsHidden));
        json.Append(']');
        if (first.ReflowSource?.Hierarchy != null) {
            json.Append(",\"fromHierarchyRootId\":").Append(first.View.HierarchyView == null ? "null" : HtmlJsonString.Encode(first.View.HierarchyView.RootId));
            json.Append(",\"toHierarchyRootId\":").Append(last.View.HierarchyView == null ? "null" : HtmlJsonString.Encode(last.View.HierarchyView.RootId));
        }
        json.Append('}');
    }
}
