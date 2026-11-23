using ChartForgeX.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

/// <summary>
/// Provides convenience rendering and export methods for topology charts.
/// </summary>
public static partial class TopologyChartExtensions {

    /// <summary>
    /// Adds an auto-placed group to the topology chart. Deterministic layout modes assign the coordinates and size.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The group id.</param>
    /// <param name="label">The group label.</param>
    /// <param name="status">The group status.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <param name="symbol">The optional short visual symbol shown in the group header.</param>
    /// <param name="color">The optional group accent color, independent from health status.</param>
    /// <param name="iconId">The optional reusable icon id from a topology icon catalog.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddAutoGroup(this TopologyChart chart, string id, string label, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? subtitle = null, string? href = null, string? tooltip = null, string? cssClass = null, string? symbol = null, string? color = null, string? iconId = null) {
        return AddGroup(chart, id, label, 0, 0, 0, 0, status, subtitle, href, tooltip, cssClass, symbol, color, iconId);
    }

    /// <summary>
    /// Adds a group to the topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The group id.</param>
    /// <param name="label">The group label.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The group width.</param>
    /// <param name="height">The group height.</param>
    /// <param name="status">The group status.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <param name="symbol">The optional short visual symbol shown in the group header.</param>
    /// <param name="color">The optional group accent color, independent from health status.</param>
    /// <param name="iconId">The optional reusable icon id from a topology icon catalog.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddGroup(this TopologyChart chart, string id, string label, double x, double y, double width, double height, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? subtitle = null, string? href = null, string? tooltip = null, string? cssClass = null, string? symbol = null, string? color = null, string? iconId = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var groupId = RequiredText(id, nameof(id), "Topology group ids");
        var groupLabel = RequiredText(label, nameof(label), "Topology group labels");
        ValidateFinite(x, nameof(x), "Topology group x-coordinates");
        ValidateFinite(y, nameof(y), "Topology group y-coordinates");
        ValidateNonNegative(width, nameof(width), "Topology group widths");
        ValidateNonNegative(height, nameof(height), "Topology group heights");
        ValidateEnum(typeof(TopologyHealthStatus), status, nameof(status), "Topology health statuses");
        chart.Groups.Add(new TopologyGroup { Id = groupId, Label = groupLabel, X = x, Y = y, Width = width, Height = height, Status = status, Subtitle = subtitle, Href = href, Tooltip = tooltip, CssClass = cssClass, Symbol = symbol, IconId = OptionalText(iconId), Color = color, HasPositionOverride = HasCoordinateOverride(x) || HasCoordinateOverride(y) });
        return chart;
    }

    /// <summary>
    /// Adds an auto-placed node to the topology chart. Deterministic layout modes assign the coordinates.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The node id.</param>
    /// <param name="label">The node label.</param>
    /// <param name="kind">The node kind.</param>
    /// <param name="status">The node status.</param>
    /// <param name="groupId">The optional group id.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="width">The node width.</param>
    /// <param name="height">The node height.</param>
    /// <param name="symbol">The optional short visual symbol shown inside the node icon.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <param name="color">The optional node accent color, independent from health status.</param>
    /// <param name="iconId">The optional reusable icon id from a topology icon catalog.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddAutoNode(this TopologyChart chart, string id, string label, TopologyNodeKind kind = TopologyNodeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? groupId = null, string? subtitle = null, string? href = null, string? tooltip = null, double width = 120, double height = 64, string? symbol = null, string? cssClass = null, string? color = null, string? iconId = null) {
        return AddNodeCore(chart, id, label, 0, 0, kind, status, groupId, subtitle, href, tooltip, width, height, symbol, cssClass, color, iconId, backgroundColor: null, hasPositionOverride: false);
    }

    /// <summary>
    /// Adds a node to the topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The node id.</param>
    /// <param name="label">The node label.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="kind">The node kind.</param>
    /// <param name="status">The node status.</param>
    /// <param name="groupId">The optional group id.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="width">The node width.</param>
    /// <param name="height">The node height.</param>
    /// <param name="symbol">The optional short visual symbol shown inside the node icon.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <param name="color">The optional node accent color, independent from health status.</param>
    /// <param name="iconId">The optional reusable icon id from a topology icon catalog.</param>
    /// <param name="backgroundColor">The optional node surface fill color.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddNode(this TopologyChart chart, string id, string label, double x, double y, TopologyNodeKind kind = TopologyNodeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? groupId = null, string? subtitle = null, string? href = null, string? tooltip = null, double width = 120, double height = 64, string? symbol = null, string? cssClass = null, string? color = null, string? iconId = null, string? backgroundColor = null) {
        return AddNodeCore(chart, id, label, x, y, kind, status, groupId, subtitle, href, tooltip, width, height, symbol, cssClass, color, iconId, backgroundColor, hasPositionOverride: true);
    }

    private static TopologyChart AddNodeCore(TopologyChart chart, string id, string label, double x, double y, TopologyNodeKind kind, TopologyHealthStatus status, string? groupId, string? subtitle, string? href, string? tooltip, double width, double height, string? symbol, string? cssClass, string? color, string? iconId, string? backgroundColor, bool hasPositionOverride) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var nodeId = RequiredText(id, nameof(id), "Topology node ids");
        var nodeLabel = RequiredText(label, nameof(label), "Topology node labels");
        ValidateFinite(x, nameof(x), "Topology node x-coordinates");
        ValidateFinite(y, nameof(y), "Topology node y-coordinates");
        ValidatePositive(width, nameof(width), "Topology node widths");
        ValidatePositive(height, nameof(height), "Topology node heights");
        ValidateEnum(typeof(TopologyNodeKind), kind, nameof(kind), "Topology node kinds");
        ValidateEnum(typeof(TopologyHealthStatus), status, nameof(status), "Topology health statuses");
        var nodeGroupId = string.IsNullOrWhiteSpace(groupId) ? null : groupId!.Trim();
        chart.Nodes.Add(new TopologyNode { Id = nodeId, Label = nodeLabel, X = x, Y = y, Kind = kind, Symbol = symbol, IconId = OptionalText(iconId), Status = status, GroupId = nodeGroupId, Subtitle = subtitle, Href = href, Tooltip = tooltip, Width = width, Height = height, CssClass = cssClass, Color = color, BackgroundColor = backgroundColor, HasPositionOverride = hasPositionOverride });
        return chart;
    }

    /// <summary>
    /// Adds an edge to the topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The edge id.</param>
    /// <param name="sourceNodeId">The source node or group id; node ids take precedence.</param>
    /// <param name="targetNodeId">The target node or group id; node ids take precedence.</param>
    /// <param name="label">The optional edge label.</param>
    /// <param name="kind">The edge kind.</param>
    /// <param name="status">The edge status.</param>
    /// <param name="direction">The edge direction.</param>
    /// <param name="routing">The edge routing.</param>
    /// <param name="secondaryLabel">The optional secondary label.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <param name="tertiaryLabel">The optional tertiary label.</param>
    /// <param name="color">The optional edge color independent from health status.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddEdge(this TopologyChart chart, string id, string sourceNodeId, string targetNodeId, string? label = null, TopologyEdgeKind kind = TopologyEdgeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, VisualLinkDirection direction = VisualLinkDirection.None, TopologyEdgeRouting routing = TopologyEdgeRouting.Orthogonal, string? secondaryLabel = null, string? href = null, string? tooltip = null, string? cssClass = null, string? tertiaryLabel = null, string? color = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var edgeId = RequiredText(id, nameof(id), "Topology edge ids");
        var sourceId = RequiredText(sourceNodeId, nameof(sourceNodeId), "Topology edge source node ids");
        var targetId = RequiredText(targetNodeId, nameof(targetNodeId), "Topology edge target node ids");
        ValidateEnum(typeof(TopologyEdgeKind), kind, nameof(kind), "Topology edge kinds");
        ValidateEnum(typeof(TopologyHealthStatus), status, nameof(status), "Topology health statuses");
        ValidateEnum(typeof(VisualLinkDirection), direction, nameof(direction), "Topology directions");
        ValidateEnum(typeof(TopologyEdgeRouting), routing, nameof(routing), "Topology edge routing modes");
        chart.Edges.Add(new TopologyEdge { Id = edgeId, SourceNodeId = sourceId, TargetNodeId = targetId, Label = label, Kind = kind, Status = status, Direction = direction, Routing = routing, SecondaryLabel = secondaryLabel, TertiaryLabel = tertiaryLabel, Href = href, Tooltip = tooltip, CssClass = cssClass, Color = color });
        return chart;
    }

    /// <summary>
    /// Sets explicit route bend points on an edge.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="waypoints">The route bend points.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart EdgeWaypoints(this TopologyChart chart, string edgeId, params ChartPoint[] waypoints) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.Waypoints.Clear();
            if (waypoints != null) edge.Waypoints.AddRange(waypoints);
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Assigns centered orthogonal route lanes to a related set of edges.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="centerLane">The center lane offset in pixels.</param>
    /// <param name="spacing">The spacing between adjacent lanes in pixels.</param>
    /// <param name="edgeIds">The related edge ids in visual lane order.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart EdgeRouteBundle(this TopologyChart chart, double centerLane, double spacing, params string[] edgeIds) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateFinite(centerLane, nameof(centerLane), "Topology edge route bundle center lanes");
        ValidateFinite(spacing, nameof(spacing), "Topology edge route bundle spacing");
        if (spacing <= 0) throw new ArgumentOutOfRangeException(nameof(spacing), "Topology edge route bundle spacing must be greater than zero.");
        if (edgeIds == null || edgeIds.Length == 0) throw new ArgumentException("Topology edge route bundles require at least one edge id.", nameof(edgeIds));
        for (var i = 0; i < edgeIds.Length; i++) {
            var lane = centerLane + (i - (edgeIds.Length - 1) / 2.0) * spacing;
            chart.UpdateEdge(edgeIds[i], edge => edge.RouteLane = lane);
        }

        return chart;
    }

    /// <summary>
    /// Assigns centered orthogonal route lanes to reciprocal edge pairs that do not already have explicit lanes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="spacing">The spacing between adjacent lanes in pixels.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart ReciprocalEdgeRouteBundles(this TopologyChart chart, double spacing = 18) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateFinite(spacing, nameof(spacing), "Topology reciprocal edge route bundle spacing");
        if (spacing <= 0) throw new ArgumentOutOfRangeException(nameof(spacing), "Topology reciprocal edge route bundle spacing must be greater than zero.");
        var groups = chart.Edges
            .GroupBy(edge => EdgePairKey(edge.SourceNodeId, edge.TargetNodeId), StringComparer.Ordinal)
            .Where(group => group.Count() > 1);
        foreach (var group in groups) {
            var edges = group
                .Where(edge => !edge.HasRouteLaneOverride && Math.Abs(edge.RouteLane) < 0.000001)
                .OrderBy(edge => edge.SourceNodeId, StringComparer.Ordinal)
                .ThenBy(edge => edge.TargetNodeId, StringComparer.Ordinal)
                .ThenBy(edge => edge.Id, StringComparer.Ordinal)
                .ToList();
            if (edges.Count < 2) continue;
            for (var i = 0; i < edges.Count; i++) {
                edges[i].RouteLane = (i - (edges.Count - 1) / 2.0) * spacing;
            }
        }

        return chart;
    }

    /// <summary>
    /// Configures the current topology theme.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="configure">The theme customization callback.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart Theme(this TopologyChart chart, Action<VisualThemeBuilder> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        chart.Theme = chart.Theme.Configure(configure);
        return chart;
    }

    /// <summary>Configures the node with the given id.</summary>
    /// <param name="chart">The chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="configure">Configures the node in place.</param>
    /// <returns>The same chart.</returns>
    /// <exception cref="ArgumentException">No node has the id.</exception>
    public static TopologyChart UpdateNode(this TopologyChart chart, string nodeId, Action<TopologyNode> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(RequiredNode(chart, nodeId));
        return chart;
    }

    /// <summary>Configures every node of the given kind.</summary>
    /// <param name="chart">The chart.</param>
    /// <param name="kind">The node kind to match.</param>
    /// <param name="configure">Configures each matching node in place.</param>
    /// <returns>The same chart.</returns>
    public static TopologyChart UpdateNodes(this TopologyChart chart, TopologyNodeKind kind, Action<TopologyNode> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyNodeKind), kind, nameof(kind), "Topology node kinds");
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        foreach (var node in chart.Nodes) {
            if (node.Kind == kind) configure(node);
        }

        return chart;
    }

    /// <summary>Configures the edge with the given id.</summary>
    /// <param name="chart">The chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="configure">Configures the edge in place.</param>
    /// <returns>The same chart.</returns>
    /// <exception cref="ArgumentException">No edge has the id.</exception>
    public static TopologyChart UpdateEdge(this TopologyChart chart, string edgeId, Action<TopologyEdge> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(RequiredEdge(chart, edgeId));
        return chart;
    }

    /// <summary>Configures every edge of the given kind.</summary>
    /// <param name="chart">The chart.</param>
    /// <param name="kind">The edge kind to match.</param>
    /// <param name="configure">Configures each matching edge in place.</param>
    /// <returns>The same chart.</returns>
    public static TopologyChart UpdateEdges(this TopologyChart chart, TopologyEdgeKind kind, Action<TopologyEdge> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyEdgeKind), kind, nameof(kind), "Topology edge kinds");
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        foreach (var edge in chart.Edges) {
            if (edge.Kind == kind) configure(edge);
        }

        return chart;
    }

    /// <summary>Configures the group with the given id.</summary>
    /// <param name="chart">The chart.</param>
    /// <param name="groupId">The group id.</param>
    /// <param name="configure">Configures the group in place.</param>
    /// <returns>The same chart.</returns>
    /// <exception cref="ArgumentException">No group has the id.</exception>
    public static TopologyChart UpdateGroup(this TopologyChart chart, string groupId, Action<TopologyGroup> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        groupId = RequiredText(groupId, nameof(groupId), "Topology group ids");
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var group = chart.Groups.FirstOrDefault(candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal))
            ?? throw new ArgumentException("Topology group '" + groupId + "' was not found.", nameof(groupId));
        configure(group);
        return chart;
    }

    private static void ValidateCoordinate(double longitude, double latitude) {
        if (double.IsNaN(longitude) || double.IsInfinity(longitude)) throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be a finite number.");
        if (double.IsNaN(latitude) || double.IsInfinity(latitude)) throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be a finite number.");
        if (longitude < -180 || longitude > 180) throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180 degrees.");
        if (latitude < -90 || latitude > 90) throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90 degrees.");
    }

    private static string EdgePairKey(string sourceNodeId, string targetNodeId) =>
        string.Compare(sourceNodeId, targetNodeId, StringComparison.Ordinal) <= 0
            ? sourceNodeId + "\u001F" + targetNodeId
            : targetNodeId + "\u001F" + sourceNodeId;

    private static string RequiredText(string? value, string parameterName, string displayName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException(displayName + " must not be empty.", parameterName);
        return trimmed;
    }

    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
    private static void ValidateFinite(double value, string parameterName, string displayName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, displayName + " must be finite numbers.");
    }

    private static void ValidatePositive(double value, string parameterName, string displayName) {
        ValidateFinite(value, parameterName, displayName);
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, displayName + " must be greater than zero.");
    }

    private static void ValidateNonNegative(double value, string parameterName, string displayName) {
        ValidateFinite(value, parameterName, displayName);
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, displayName + " must be zero or greater.");
    }

    private static bool HasCoordinateOverride(double value) => Math.Abs(value) >= 0.0001;

    private static void ValidateEnum(Type enumType, object value, string parameterName, string displayName) {
        if (!Enum.IsDefined(enumType, value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown " + displayName.ToLowerInvariant() + ".");
    }
}
