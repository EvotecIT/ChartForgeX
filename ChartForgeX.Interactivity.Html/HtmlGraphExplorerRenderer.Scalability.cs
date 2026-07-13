using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static bool ShouldUseAcceleratedMarkup(GraphScene scene, HtmlGraphExplorerOptions options) {
        if (!scene.Options.HasFeature(GraphSceneFeatures.LevelOfDetail) || !options.AllowCanvasFallback) return false;
        return scene.Nodes.Count >= scene.Options.LevelOfDetail.CanvasPreferredNodeThreshold
            || scene.Edges.Count > scene.Options.Performance.MaxInteractiveSvgEdges;
    }

    private static void WriteScalabilityAttributes(StringBuilder writer, GraphScene scene) {
        Attribute(writer, "data-cfx-lod-cluster-threshold", scene.Options.LevelOfDetail.ClusterNodeThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-hide-edge-labels-threshold", scene.Options.LevelOfDetail.HideEdgeLabelsThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-compact-node-threshold", scene.Options.LevelOfDetail.CompactNodeThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-canvas-threshold", scene.Options.LevelOfDetail.CanvasPreferredNodeThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-webgl-threshold", scene.Options.LevelOfDetail.WebGlPreferredNodeThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-overview-scale", Number(scene.Options.LevelOfDetail.OverviewScaleThreshold));
        Attribute(writer, "data-cfx-lod-detail-scale", Number(scene.Options.LevelOfDetail.DetailScaleThreshold));
        Attribute(writer, "data-cfx-lod-collapse-clusters", scene.Options.LevelOfDetail.CollapseClustersOnLoad ? "true" : "false");
        Attribute(writer, "data-cfx-performance-frame-budget", scene.Options.Performance.FrameBudgetMilliseconds.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-svg-nodes", scene.Options.Performance.MaxInteractiveSvgNodes.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-svg-edges", scene.Options.Performance.MaxInteractiveSvgEdges.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-canvas-nodes", scene.Options.Performance.MaxInteractiveCanvasNodes.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-canvas-edges", scene.Options.Performance.MaxInteractiveCanvasEdges.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-webgl-nodes", scene.Options.Performance.MaxInteractiveWebGlNodes.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-webgl-edges", scene.Options.Performance.MaxInteractiveWebGlEdges.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-telemetry-interval", scene.Options.Performance.TelemetrySampleInterval.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-warmup-frames", scene.Options.Performance.WarmupFrameCount.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-worker-progress-interval", scene.Options.Performance.WorkerProgressInterval.ToString(CultureInfo.InvariantCulture));
    }

    private static void WriteHierarchyAttributes(StringBuilder writer, GraphScene scene) {
        Attribute(writer, "data-cfx-graph-hierarchy-root", scene.Options.Hierarchy.InitialRootNodeId);
        Attribute(writer, "data-cfx-graph-hierarchy-depth", scene.Options.Hierarchy.InitialDepth.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-hierarchy-breadcrumbs", scene.Options.Hierarchy.IncludeAncestorBreadcrumbs ? "true" : "false");
        Attribute(writer, "data-cfx-graph-hierarchy-auto-fit", scene.Options.Hierarchy.AutoFitOnNavigate ? "true" : "false");
        Attribute(writer, "data-cfx-graph-hierarchy-drill", scene.Options.Hierarchy.DrillDownOnActivate ? "true" : "false");
    }

    private static void WriteHierarchyNavigation(StringBuilder writer, GraphScene scene) {
        if (!scene.Options.HasFeature(GraphSceneFeatures.HierarchyNavigation) || !scene.Nodes.Any(node => !string.IsNullOrWhiteSpace(node.ParentId))) return;
        writer.Append("<nav class=\"cfx-graph-breadcrumbs\" data-cfx-role=\"graph-breadcrumbs\" aria-label=\"Graph hierarchy\">");
        WriteButton(writer, "hierarchy-home", "Overview", false, true);
        WriteButton(writer, "hierarchy-up", "Up one level", false, true);
        writer.Append("<span class=\"cfx-graph-breadcrumb-path\" data-cfx-role=\"graph-breadcrumb-path\" aria-live=\"polite\">Overview</span></nav>");
    }
}
