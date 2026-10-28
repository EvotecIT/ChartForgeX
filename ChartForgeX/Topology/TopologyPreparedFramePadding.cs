using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    internal TopologyPreparedFramePadding? PreparedFramePadding { get; set; }
}

/// <summary>Retains only the existing immutable layout value while its prepared viewport/lifecycle still matches.</summary>
internal sealed partial class TopologyPreparedFramePadding {
    private TopologyPreparedFramePadding(VisualLayoutOptions layout, TopologyPreparedContentClip? clip = null) { Layout = layout; ContentClip = clip; }
    internal TopologyPreparedContentClip? ContentClip { get; }
    internal VisualLayoutOptions Layout { get; }
    private bool Matches(TopologyChart chart) => chart.Viewport.Width == Layout.Size.Width && chart.Viewport.Height == Layout.Size.Height
        && chart.Viewport.Padding == Layout.Padding;
    internal static TopologyPreparedFramePadding? Resolve(TopologyChart chart) =>
        chart.PreparedFramePadding is {} captured && captured.Matches(chart) ? captured : null;
    internal static TopologyPreparedFramePadding? ForRequest(TopologyChart chart) =>
        chart.LayoutMode == TopologyLayoutMode.Manual && chart.UsesPreparedSceneCoordinates ? Resolve(chart) : null;
    internal static TopologyPreparedFramePadding? Capture(VisualLayoutOptions layout, TopologyPreparedContentClip? clip = null, bool retainForAnnotationUnits = false) {
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        return IsDefault(layout.PaddingEdges) && clip == null && !retainForAnnotationUnits ? null : new TopologyPreparedFramePadding(layout, clip);
    }
    internal static ChartPadding? WirePadding(TopologyChart chart) {
        var padding = Resolve(chart)?.Layout.PaddingEdges ?? ChartPadding.All(chart.Viewport.Padding);
        return IsDefault(padding) ? (ChartPadding?)null : padding;
    }
    internal static void ApplyLifecycle(TopologyChart chart, TopologyRenderOptions options) {
        if (options.FitContentToViewport || ForRequest(chart) == null) chart.PreparedFramePadding = null;
    }
    private static bool IsDefault(ChartPadding padding) {
        var nativeDefault = new TopologyViewport().Padding;
        return padding.Left == nativeDefault && padding.Top == nativeDefault && padding.Right == nativeDefault && padding.Bottom == nativeDefault;
    }
}
