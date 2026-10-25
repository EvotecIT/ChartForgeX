using System;
using System.Linq;
using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;

/// <summary>A real host preparation boundary for organization collapse, sibling order and native layout direction.</summary>
public static class OrganizationHierarchyNavigationRecipe {
    /// <summary>Prepares a native source view and its matching adaptive alternate, without a second browser layout.</summary>
    public static HtmlChartSection PrepareSection(TopologyChart source, TopologyHierarchyView? view = null) =>
        new HtmlInteractiveTopologyRenderer().PrepareHierarchySection(source, view, options => {
            options.Interaction.ChartId = "native-organization"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 580;
        });

    /// <summary>Decodes an exact browser hierarchy request against the host's retained source and accepted basis scene.</summary>
    /// <remarks>The host verifies the opaque basis, retains its corresponding source snapshot, honors cancellation and returns a new basis
    /// alongside the resulting section. This method performs no transport, browser layout, source mutation or session advancement.</remarks>
    public static HtmlChartSection PrepareReply(TopologyChart source, HtmlChartSection basis, JsonElement request) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (basis == null) throw new ArgumentNullException(nameof(basis));
        if (request.GetProperty("channel").GetString() != "hierarchy") throw new ArgumentException("A hierarchy request is required.", nameof(request));
        var drawing = !request.TryGetProperty("drawing", out var selected) ? "wide" : selected.GetString();
        var captured = drawing == "wide" ? basis.PreparedVisual : drawing == "narrow" ? basis.NarrowPreparedVisual
            : throw new ArgumentException("The request names an unsupported drawing.", nameof(request));
        if (captured?.TopologyHierarchyView == null) throw new ArgumentException("The basis does not retain the requested organization drawing.", nameof(basis));
        var kind = request.GetProperty("kind").GetString();
        if (kind != "reset" && kind != "topology") throw new ArgumentException("The host does not support this hierarchy request.", nameof(request));
        var collapsed = request.GetProperty("collapsedNodeIds").EnumerateArray().Select(item => item.GetString() ?? throw new ArgumentException("A source node ID is required.", nameof(request))).ToArray();
        var order = request.GetProperty("nodeOrder").EnumerateArray().Select(item => item.GetString() ?? throw new ArgumentException("An ordered source node ID is required.", nameof(request))).ToArray();
        var requestedDirection = request.GetProperty("layoutDirection"); TopologyLayoutDirection? direction = null;
        if (requestedDirection.ValueKind != JsonValueKind.Null) {
            var token = requestedDirection.GetString();
            if (!Enum.TryParse<TopologyLayoutDirection>(token, out var parsed) || !Enum.IsDefined(typeof(TopologyLayoutDirection), parsed) || parsed.ToString() != token)
                throw new ArgumentException("The request requires an existing native direction.", nameof(request));
            direction = parsed;
        }
        if (kind == "reset" && (collapsed.Length != 0 || order.Length != 0 || direction.HasValue)) throw new ArgumentException("A reset cannot carry a partial hierarchy request.", nameof(request));
        return PrepareSection(source, new TopologyHierarchyView(collapsed, order, direction));
    }
}
