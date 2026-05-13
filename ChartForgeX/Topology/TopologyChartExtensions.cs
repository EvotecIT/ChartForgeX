using System;
using System.IO;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Provides convenience rendering and export methods for topology charts.
/// </summary>
public static partial class TopologyChartExtensions {
    /// <summary>
    /// Sets the topology chart id.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The chart id.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithId(this TopologyChart chart, string id) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Id = RequiredText(id, nameof(id), "Topology chart ids");
        return chart;
    }

    /// <summary>
    /// Sets the topology chart title.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="title">The chart title.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithTitle(this TopologyChart chart, string title) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Title = title;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart subtitle.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="subtitle">The chart subtitle.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithSubtitle(this TopologyChart chart, string subtitle) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Subtitle = subtitle;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart layout mode.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="layoutMode">The layout mode.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLayout(this TopologyChart chart, TopologyLayoutMode layoutMode) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyLayoutMode), layoutMode, nameof(layoutMode), "Topology layout modes");
        chart.LayoutMode = layoutMode;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart layout mode and flow direction.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="layoutMode">The layout mode.</param>
    /// <param name="layoutDirection">The layout flow direction.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLayout(this TopologyChart chart, TopologyLayoutMode layoutMode, TopologyLayoutDirection layoutDirection) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyLayoutMode), layoutMode, nameof(layoutMode), "Topology layout modes");
        ValidateEnum(typeof(TopologyLayoutDirection), layoutDirection, nameof(layoutDirection), "Topology layout directions");
        chart.LayoutMode = layoutMode;
        chart.LayoutDirection = layoutDirection;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart layout flow direction.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="layoutDirection">The layout flow direction.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLayoutDirection(this TopologyChart chart, TopologyLayoutDirection layoutDirection) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyLayoutDirection), layoutDirection, nameof(layoutDirection), "Topology layout directions");
        chart.LayoutDirection = layoutDirection;
        return chart;
    }

    /// <summary>
    /// Sets the topology viewport.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    /// <param name="padding">The viewport padding.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithViewport(this TopologyChart chart, double width, double height, double padding = 24) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidatePositive(width, nameof(width), "Topology viewport widths");
        ValidatePositive(height, nameof(height), "Topology viewport heights");
        ValidateNonNegative(padding, nameof(padding), "Topology viewport padding");
        chart.Viewport = new TopologyViewport { Width = width, Height = height, Padding = padding };
        return chart;
    }

    /// <summary>
    /// Sets the longitude/latitude viewport used by geographic topology layouts.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="viewport">The map viewport.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithMapViewport(this TopologyChart chart, ChartMapViewport viewport) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.MapViewport = viewport;
        return chart;
    }

    /// <summary>
    /// Sets the topology legend.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="legend">The legend to use.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLegend(this TopologyChart chart, TopologyLegend? legend) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Legend = legend;
        return chart;
    }

    /// <summary>
    /// Sets the topology theme.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="theme">The theme to use.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithTheme(this TopologyChart chart, TopologyTheme theme) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        return chart;
    }

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
        return AddNode(chart, id, label, 0, 0, kind, status, groupId, subtitle, href, tooltip, width, height, symbol, cssClass, color, iconId);
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
        chart.Nodes.Add(new TopologyNode { Id = nodeId, Label = nodeLabel, X = x, Y = y, Kind = kind, Symbol = symbol, IconId = OptionalText(iconId), Status = status, GroupId = nodeGroupId, Subtitle = subtitle, Href = href, Tooltip = tooltip, Width = width, Height = height, CssClass = cssClass, Color = color, BackgroundColor = backgroundColor });
        return chart;
    }

    /// <summary>
    /// Sets a node-specific display mode and optional badge text.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="displayMode">The node display mode.</param>
    /// <param name="badge">Optional short badge text.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeDisplay(this TopologyChart chart, string nodeId, TopologyNodeDisplayMode displayMode, string? badge = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        ValidateEnum(typeof(TopologyNodeDisplayMode), displayMode, nameof(displayMode), "Topology node display modes");
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.DisplayMode = displayMode;
            node.Badge = badge;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Sets a display mode for all nodes of a kind and optionally assigns a badge.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="kind">The node kind to update.</param>
    /// <param name="displayMode">The node display mode.</param>
    /// <param name="badge">Optional short badge text.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodesDisplay(this TopologyChart chart, TopologyNodeKind kind, TopologyNodeDisplayMode displayMode, string? badge = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyNodeKind), kind, nameof(kind), "Topology node kinds");
        ValidateEnum(typeof(TopologyNodeDisplayMode), displayMode, nameof(displayMode), "Topology node display modes");
        foreach (var node in chart.Nodes) {
            if (node.Kind != kind) continue;
            node.DisplayMode = displayMode;
            if (badge != null) node.Badge = badge;
        }

        return chart;
    }

    /// <summary>
    /// Sets optional short badge text on a topology node.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="badge">The badge text.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeBadge(this TopologyChart chart, string nodeId, string? badge) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.Badge = badge;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Sets longitude/latitude coordinates for a topology node used by geographic layouts.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="longitude">The longitude in degrees.</param>
    /// <param name="latitude">The latitude in degrees.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeCoordinates(this TopologyChart chart, string nodeId, double longitude, double latitude) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        ValidateCoordinate(longitude, latitude);
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.Longitude = longitude;
            node.Latitude = latitude;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Sets an optional short group symbol.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="groupId">The group id.</param>
    /// <param name="symbol">The symbol text or built-in token such as region.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithGroupSymbol(this TopologyChart chart, string groupId, string? symbol) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        groupId = RequiredText(groupId, nameof(groupId), "Topology group ids");
        foreach (var group in chart.Groups) {
            if (!string.Equals(group.Id, groupId, StringComparison.Ordinal)) continue;
            group.Symbol = symbol;
            return chart;
        }

        throw new ArgumentException("Topology group '" + groupId + "' was not found.", nameof(groupId));
    }

    /// <summary>
    /// Sets an optional group accent color independent from the group health status.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="groupId">The group id.</param>
    /// <param name="color">The group accent color.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithGroupColor(this TopologyChart chart, string groupId, string? color) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        groupId = RequiredText(groupId, nameof(groupId), "Topology group ids");
        foreach (var group in chart.Groups) {
            if (!string.Equals(group.Id, groupId, StringComparison.Ordinal)) continue;
            group.Color = color;
            return chart;
        }

        throw new ArgumentException("Topology group '" + groupId + "' was not found.", nameof(groupId));
    }

    /// <summary>
    /// Sets the preferred node arrangement policy for a group when using dense grouped layouts.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="groupId">The group id.</param>
    /// <param name="layoutPolicy">The dense group layout policy.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithGroupLayout(this TopologyChart chart, string groupId, TopologyGroupLayoutPolicy layoutPolicy) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        groupId = RequiredText(groupId, nameof(groupId), "Topology group ids");
        ValidateEnum(typeof(TopologyGroupLayoutPolicy), layoutPolicy, nameof(layoutPolicy), "Topology group layout policies");
        foreach (var group in chart.Groups) {
            if (!string.Equals(group.Id, groupId, StringComparison.Ordinal)) continue;
            group.LayoutPolicy = layoutPolicy;
            return chart;
        }

        throw new ArgumentException("Topology group '" + groupId + "' was not found.", nameof(groupId));
    }

    /// <summary>
    /// Sets longitude/latitude coordinates for a topology group used by geographic layouts.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="groupId">The group id.</param>
    /// <param name="longitude">The longitude in degrees.</param>
    /// <param name="latitude">The latitude in degrees.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithGroupCoordinates(this TopologyChart chart, string groupId, double longitude, double latitude) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        groupId = RequiredText(groupId, nameof(groupId), "Topology group ids");
        ValidateCoordinate(longitude, latitude);
        foreach (var group in chart.Groups) {
            if (!string.Equals(group.Id, groupId, StringComparison.Ordinal)) continue;
            group.Longitude = longitude;
            group.Latitude = latitude;
            return chart;
        }

        throw new ArgumentException("Topology group '" + groupId + "' was not found.", nameof(groupId));
    }

    /// <summary>
    /// Adds an edge to the topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The edge id.</param>
    /// <param name="sourceNodeId">The source node id.</param>
    /// <param name="targetNodeId">The target node id.</param>
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
    public static TopologyChart AddEdge(this TopologyChart chart, string id, string sourceNodeId, string targetNodeId, string? label = null, TopologyEdgeKind kind = TopologyEdgeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, TopologyDirection direction = TopologyDirection.None, TopologyEdgeRouting routing = TopologyEdgeRouting.Orthogonal, string? secondaryLabel = null, string? href = null, string? tooltip = null, string? cssClass = null, string? tertiaryLabel = null, string? color = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var edgeId = RequiredText(id, nameof(id), "Topology edge ids");
        var sourceId = RequiredText(sourceNodeId, nameof(sourceNodeId), "Topology edge source node ids");
        var targetId = RequiredText(targetNodeId, nameof(targetNodeId), "Topology edge target node ids");
        ValidateEnum(typeof(TopologyEdgeKind), kind, nameof(kind), "Topology edge kinds");
        ValidateEnum(typeof(TopologyHealthStatus), status, nameof(status), "Topology health statuses");
        ValidateEnum(typeof(TopologyDirection), direction, nameof(direction), "Topology directions");
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
    public static TopologyChart WithEdgeWaypoints(this TopologyChart chart, string edgeId, params ChartPoint[] waypoints) {
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
    /// Adjusts the rendered label position for a specific edge without changing the route geometry.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="x">The horizontal label offset in pixels.</param>
    /// <param name="y">The vertical label offset in pixels.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeLabelOffset(this TopologyChart chart, string edgeId, double x, double y) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        TopologyModelGuards.Finite(x, nameof(x));
        TopologyModelGuards.Finite(y, nameof(y));
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.LabelOffsetX = x;
            edge.LabelOffsetY = y;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Sets preferred attachment sides for an edge.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="sourcePort">The preferred source attachment side.</param>
    /// <param name="targetPort">The preferred target attachment side.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgePorts(this TopologyChart chart, string edgeId, TopologyEdgePort sourcePort, TopologyEdgePort targetPort) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        ValidateEnum(typeof(TopologyEdgePort), sourcePort, nameof(sourcePort), "Topology edge ports");
        ValidateEnum(typeof(TopologyEdgePort), targetPort, nameof(targetPort), "Topology edge ports");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.SourcePort = sourcePort;
            edge.TargetPort = targetPort;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Sets a deterministic orthogonal route lane offset for an edge.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="routeLane">The route lane offset in pixels.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeRouteLane(this TopologyChart chart, string edgeId, double routeLane) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        ValidateFinite(routeLane, nameof(routeLane), "Topology edge route lanes");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.RouteLane = routeLane;
            edge.HasRouteLaneOverride = true;
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
    public static TopologyChart WithEdgeRouteBundle(this TopologyChart chart, double centerLane, double spacing, params string[] edgeIds) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateFinite(centerLane, nameof(centerLane), "Topology edge route bundle center lanes");
        ValidateFinite(spacing, nameof(spacing), "Topology edge route bundle spacing");
        if (spacing <= 0) throw new ArgumentOutOfRangeException(nameof(spacing), "Topology edge route bundle spacing must be greater than zero.");
        if (edgeIds == null || edgeIds.Length == 0) throw new ArgumentException("Topology edge route bundles require at least one edge id.", nameof(edgeIds));
        for (var i = 0; i < edgeIds.Length; i++) {
            var lane = centerLane + (i - (edgeIds.Length - 1) / 2.0) * spacing;
            chart.WithEdgeRouteLane(edgeIds[i], lane);
        }

        return chart;
    }

    /// <summary>
    /// Assigns centered orthogonal route lanes to reciprocal edge pairs that do not already have explicit lanes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="spacing">The spacing between adjacent lanes in pixels.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithReciprocalEdgeRouteBundles(this TopologyChart chart, double spacing = 18) {
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
                edges[i].HasRouteLaneOverride = true;
            }
        }

        return chart;
    }

    /// <summary>
    /// Sets an explicit line style for an edge independent from health status.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="lineStyle">The edge line style.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeLineStyle(this TopologyChart chart, string edgeId, TopologyEdgeLineStyle lineStyle) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        ValidateEnum(typeof(TopologyEdgeLineStyle), lineStyle, nameof(lineStyle), "Topology edge line styles");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.LineStyle = lineStyle;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Sets edge visual emphasis independently from health status and line style.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="emphasis">The edge visual emphasis.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeEmphasis(this TopologyChart chart, string edgeId, TopologyEdgeEmphasis emphasis) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        ValidateEnum(typeof(TopologyEdgeEmphasis), emphasis, nameof(emphasis), "Topology edge emphasis values");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.Emphasis = emphasis;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Applies reusable visual styling to all edges of a given kind.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="kind">The edge kind to update.</param>
    /// <param name="lineStyle">Optional line style to apply.</param>
    /// <param name="emphasis">Optional visual emphasis to apply.</param>
    /// <param name="isMuted">Optional muted-state override to apply.</param>
    /// <param name="color">Optional edge color to apply independent from health status.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgesOfKind(this TopologyChart chart, TopologyEdgeKind kind, TopologyEdgeLineStyle? lineStyle = null, TopologyEdgeEmphasis? emphasis = null, bool? isMuted = null, string? color = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyEdgeKind), kind, nameof(kind), "Topology edge kinds");
        if (lineStyle.HasValue) ValidateEnum(typeof(TopologyEdgeLineStyle), lineStyle.Value, nameof(lineStyle), "Topology edge line styles");
        if (emphasis.HasValue) ValidateEnum(typeof(TopologyEdgeEmphasis), emphasis.Value, nameof(emphasis), "Topology edge emphasis values");
        foreach (var edge in chart.Edges) {
            if (edge.Kind != kind) continue;
            if (lineStyle.HasValue) edge.LineStyle = lineStyle.Value;
            if (emphasis.HasValue) edge.Emphasis = emphasis.Value;
            if (isMuted.HasValue) edge.IsMuted = isMuted.Value;
            if (color != null) edge.Color = color;
        }

        return chart;
    }

    /// <summary>
    /// Marks an edge as a quiet structural relationship instead of a status route.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="isMuted">Whether the edge is muted.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeMuted(this TopologyChart chart, string edgeId, bool isMuted = true) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        edgeId = RequiredText(edgeId, nameof(edgeId), "Topology edge ids");
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.IsMuted = isMuted;
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Renders the topology chart to SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>Complete SVG markup.</returns>
    public static string ToSvg(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologySvgRenderer().Render(chart, options);

    /// <summary>
    /// Renders the topology chart to an HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An embeddable HTML fragment.</returns>
    public static string ToHtmlFragment(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderFragment(chart, options);

    /// <summary>
    /// Renders the topology chart to a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A complete HTML page.</returns>
    public static string ToHtmlPage(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderPage(chart, options);

    /// <summary>
    /// Renders the topology chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyPngRenderer().Render(chart, options);

    /// <summary>
    /// Saves the topology chart as SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveSvg(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToSvg(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToHtmlPage(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as PNG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SavePng(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllBytes(path, chart.ToPng(options));

    /// <summary>
    /// Configures the current topology theme.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="configure">The theme customization callback.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithTheme(this TopologyChart chart, Action<TopologyTheme> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        chart.Theme ??= TopologyTheme.Light();
        configure(chart.Theme);
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
