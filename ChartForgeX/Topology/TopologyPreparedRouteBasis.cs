using System;
using System.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

/// <summary>One exact detached comparison witness shared by captured routes from one restored graph.</summary>
/// <remarks>Only finite value vectors and the canonical planner key survive capture. No chart, option, callback or
/// catalog is retained. Appearance units remain on the edge when an edited Manual graph discards its geometry.</remarks>
internal sealed class TopologyPreparedRouteBasis {
    private readonly TopologyRouteInputVector _prefix;
    private readonly object? _planner;
    private readonly TopologyPreparedFramePadding.FrameWitness _frame;
    private TopologyPreparedRouteBasis(TopologyRouteInputVector prefix, object? planner, TopologyPreparedFramePadding.FrameWitness frame) {
        _prefix = prefix; _planner = planner; _frame = frame;
    }

    internal static void Attach(TopologyChart chart) {
        if (!chart.Edges.Any(edge => edge.PreparedEdgePlan?.Geometry != null)) return;
        var options = (chart.DefaultRenderOptions ?? new TopologyRenderOptions()).CloneForRendering();
        var context = TopologyPreparedFramePadding.ResolveContext(chart, options);
        VisualTopologyCompiler.InitializeOptionsMetrics(context, options);
        var basis = Capture(chart, options, context, recordedInputs: true);
        foreach (var edge in chart.Edges) if (edge.PreparedEdgePlan?.Geometry != null)
            edge.PreparedEdgePlan = edge.PreparedEdgePlan.WithBasis(basis);
    }

    internal bool Matches(TopologyChart chart, TopologyRenderOptions options, VisualRenderContext context) {
        if (!chart.UsesPreparedSceneCoordinates || chart.LayoutMode != TopologyLayoutMode.Manual || options.View != null || options.HierarchyView != null
            || !_frame.Matches(chart, options, context))
            return false;
        var prefix = TopologyRouteInputVector.Capture(chart, options, context.Font);
        if (!_prefix.Equals(prefix)) return false;
        return Equals(_planner, CapturePlannerInputs(chart, options, context.Font, recordedInputs: false));
    }

    private static TopologyPreparedRouteBasis Capture(TopologyChart chart, TopologyRenderOptions options, VisualRenderContext context, bool recordedInputs) =>
        new(TopologyRouteInputVector.Capture(chart, options, context.Font, recordedInputs), CapturePlannerInputs(chart, options, context.Font, recordedInputs),
            new TopologyPreparedFramePadding.FrameWitness(chart, options, context));

    private static object? CapturePlannerInputs(TopologyChart chart, TopologyRenderOptions options, FontSpec font, bool recordedInputs) {
        var input = TopologyLayoutEngine.Clone(chart);
        foreach (var edge in input.Edges) if (edge.PreparedEdgePlan != null) edge.PreparedEdgePlan = edge.PreparedEdgePlan.WithoutGeometry();
        input.Title = null; input.Subtitle = null; input.Legend = null;
        var layout = options.CloneForRendering(); layout.IncludeTitle = false; layout.IncludeLegend = false;
        var prepared = TopologyLayoutEngine.Prepare(input, options: layout, measurement: new TextMeasurementContext(font, layout.TextMeasurementMode));
        return TopologyDenseRoutePlanner.CaptureComparisonInputs(prepared, recordedInputs);
    }
}
