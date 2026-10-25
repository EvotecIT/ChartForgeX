using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    public static TopologyChart Prepare(TopologyChart chart, TopologyView? view = null, TopologyRenderOptions? options = null, TextMeasurementContext? measurement = null, TopologyChart? hierarchySource = null) {
        var external = new TopologyChartValidator().ValidateExternalPresentationInputs(chart, options ?? new TopologyRenderOptions());
        if (!external.IsValid) throw new TopologyValidationException(external);
        var paints=new TopologyChartValidator().ValidateSurfacePaintInputs(chart);
        if(!paints.IsValid)throw new TopologyValidationException(paints);
        if (chart.Alignments.Count > 0) {
            var validation = new TopologyChartValidator().ValidateAlignmentInputs(chart);
            if (!validation.IsValid) throw new TopologyValidationException(validation);
            TopologyAlignmentLayout.ValidateState(chart);
        }
        ValidateRankOptimizationLayout(chart, options);
        var copy = Clone(chart);
        TopologyPreparedNodeBasis.ApplyLifecycle(copy);
        TopologyPreparedEdgePlan.ApplyLifecycle(copy);
        var authoredEdges = copy.Edges.Zip(chart.Edges, (working, authored) => (working, authored))
            .ToDictionary(pair => pair.working, pair => pair.authored);
        copy.PreparedGroupHeaderParts = null;
        copy.PreparedGroupHeaderLayoutParts = null;
        copy.GroupHeaderLayout = null;
        copy.TextMeasurement = measurement ?? new TextMeasurementContext((copy.ResolvedTheme).FontFamily, options?.TextMeasurementMode ?? TextMeasurementMode.PortableEstimate);
        if (options != null) {
            options.TextMeasurement = copy.TextMeasurement;
            options.ResolvedSubtitleChipFontSize = copy.Theme.Typography.DataLabelSize * (9.5 / 11);
            options.ResolvedCaptionStrokeWidth ??= copy.Theme.AxisStrokeWidth;
        }
        copy.RenderOptions = options;
        if (options?.HierarchyView != null) TopologyHierarchyPreparation.Apply(copy, hierarchySource ?? chart, options.HierarchyView, options);
        ApplyNamedPortSides(copy);
        if (options != null) {
            ApplyNodeDisplayMode(copy, options, copy.LayoutMode == TopologyLayoutMode.MindMap);
        }

        if (view != null) ApplyView(copy, view);
        if (chart.Alignments.Count > 0) ProjectAlignments(copy);
        if (options != null) copy.Legend = options.IncludeLegend ? TopologyLegend.Resolve(copy, options.LegendMode) : null;
        var constrained = copy.Alignments.Count > 0 && copy.LayoutMode == TopologyLayoutMode.Layered;
        var compoundLayout = false;
        switch (copy.LayoutMode) {
            case TopologyLayoutMode.GroupGrid:
                ApplyGroupGrid(copy);
                break;
            case TopologyLayoutMode.HubAndSpoke:
                ApplyHubAndSpoke(copy, options);
                break;
            case TopologyLayoutMode.Swimlane:
                ApplySwimlane(copy, options);
                break;
            case TopologyLayoutMode.Layered:
                compoundLayout = TryApplyCompoundLayered(copy, options, !constrained);
                if (!compoundLayout) ApplyLayered(copy, options, !constrained);
                break;
            case TopologyLayoutMode.Matrix:
                ApplyMatrix(copy, options);
                break;
            case TopologyLayoutMode.DenseGrouped:
                ApplyDenseGrouped(copy);
                break;
            case TopologyLayoutMode.ForceDirected:
                ApplyForceDirected(copy, options);
                break;
            case TopologyLayoutMode.RelationshipRadial:
                ApplyRelationshipRadial(copy, options);
                break;
            case TopologyLayoutMode.MindMap:
                ApplyMindMap(copy);
                break;
            case TopologyLayoutMode.Geographic:
                ApplyGeographic(copy);
                break;
        }

        if (constrained) {
            TopologyAlignmentLayout.Apply(copy, options);
            if (compoundLayout) InferCompoundRoutes(copy, copy.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal), preserveRouting: true);
            TopologyLayoutNormalizer.NormalizeAligned(copy, options);
        } else {
            if (copy.LayoutMode == TopologyLayoutMode.Layered && !compoundLayout) PlaceAutomaticLayeredGroups(copy, chart, options, authoredEdges);
            if (copy.Alignments.Count > 0) TopologyAlignmentLayout.ValidateManual(copy, options);
            // A restored Manual scene already carries measured output-space bounds and route hints.
            // Authoring normalization would repack it or reapply an absent/default padding policy.
            // Explicit output fitting remains in the compiler; selecting a fresh layout resumes normalization.
            if (!copy.UsesPreparedSceneCoordinates || copy.LayoutMode != TopologyLayoutMode.Manual) {
                if (copy.Alignments.Count > 0 && copy.LayoutMode == TopologyLayoutMode.Manual) TopologyLayoutNormalizer.NormalizeAligned(copy, options);
                else TopologyLayoutNormalizer.Normalize(copy, options);
            }
        }
        SyncHierarchyRouteDiagnostics(copy);
        return copy;
    }

    private static void ApplyNamedPortSides(TopologyChart chart) {
        var nodes = new Dictionary<string, TopologyNode>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (!nodes.ContainsKey(node.Id)) nodes.Add(node.Id, node);
        }
        foreach (var edge in chart.Edges) {
            if (!string.IsNullOrWhiteSpace(edge.SourcePortId) && nodes.TryGetValue(edge.SourceNodeId, out var source)) {
                var port = source.Ports.FirstOrDefault(candidate => string.Equals(candidate.Id, edge.SourcePortId, StringComparison.Ordinal));
                if (port != null) edge.SourcePort = port.Side;
            }
            if (!string.IsNullOrWhiteSpace(edge.TargetPortId) && nodes.TryGetValue(edge.TargetNodeId, out var target)) {
                var port = target.Ports.FirstOrDefault(candidate => string.Equals(candidate.Id, edge.TargetPortId, StringComparison.Ordinal));
                if (port != null) edge.TargetPort = port.Side;
            }
        }
    }

    private static void ApplyNodeDisplayMode(TopologyChart chart, TopologyRenderOptions options, bool preserveMindMapSizes) {
        foreach (var node in chart.Nodes) {
            if (node.PreserveDisplayModeSize) continue;
            if (options.IncludeNodeLabels && node.Details.Count > 0 && (node.DisplayMode ?? options.NodeDisplayMode) == TopologyNodeDisplayMode.Card) {
                node.Width = Math.Max(node.Width, 184);
                node.Height = Math.Max(node.Height, NodeDetailStartOffset(node, options) + NodeDetailRows(node, options) * 18 + 1);
            }
            switch (node.DisplayMode ?? options.NodeDisplayMode) {
                case TopologyNodeDisplayMode.CompactCard:
                    if (!preserveMindMapSizes) {
                        node.Width = Math.Min(node.Width, 108);
                        node.Height = Math.Min(node.Height, 52);
                    }
                    break;
                case TopologyNodeDisplayMode.Tile:
                    node.Width = Math.Min(node.Width, 64);
                    node.Height = Math.Min(node.Height, 46);
                    break;
                case TopologyNodeDisplayMode.Pill:
                    if (!preserveMindMapSizes) {
                        node.Width = Math.Min(node.Width, 112);
                        node.Height = Math.Min(node.Height, 34);
                    }
                    break;
                case TopologyNodeDisplayMode.Icon:
                    node.Width = 44;
                    node.Height = 44;
                    break;
                case TopologyNodeDisplayMode.Dot:
                    node.Width = Math.Min(node.Width, 22);
                    node.Height = Math.Min(node.Height, 22);
                    break;
                case TopologyNodeDisplayMode.Hidden:
                    node.Width = Math.Min(node.Width, 1);
                    node.Height = Math.Min(node.Height, 1);
                    break;
            }
        }
    }

    private static void ApplyView(TopologyChart chart, TopologyView view) {
        if (!string.IsNullOrWhiteSpace(view.Id)) chart.Id = string.IsNullOrWhiteSpace(chart.Id) ? view.Id : chart.Id + "-" + view.Id;
        if (!string.IsNullOrWhiteSpace(view.Title)) chart.Title = view.Title;
        if (!string.IsNullOrWhiteSpace(view.Subtitle)) chart.Subtitle = view.Subtitle;

        var selectedGroupIds = new HashSet<string>(view.GroupIds, StringComparer.Ordinal);
        var selectedNodeIds = new HashSet<string>(view.NodeIds, StringComparer.Ordinal);
        var selectedEdgeIds = new HashSet<string>(view.EdgeIds, StringComparer.Ordinal);
        var focusNodeIds = new HashSet<string>(view.FocusNodeIds, StringComparer.Ordinal);
        var hasGroupFilter = selectedGroupIds.Count > 0;
        var hasNodeFilter = selectedNodeIds.Count > 0;
        var hasEdgeFilter = selectedEdgeIds.Count > 0;
        var hasFocusFilter = focusNodeIds.Count > 0;
        var hasNodeKindFilter = view.NodeKinds.Count > 0;
        var hasEdgeKindFilter = view.EdgeKinds.Count > 0;
        var hasStatusFilter = view.HealthStatuses.Count > 0;
        if (!hasGroupFilter && !hasNodeFilter && !hasEdgeFilter && !hasFocusFilter && !hasNodeKindFilter && !hasEdgeKindFilter && !hasStatusFilter) return;
        if (hasGroupFilter) IncludeSelectedGroupDescendants(chart, selectedGroupIds);

        var baseNodes = chart.Nodes
            .Where(node => IsNodeEligible(node, selectedGroupIds, hasGroupFilter, selectedNodeIds, hasNodeFilter, view, false))
            .ToList();
        // References resolve to an authored node before a same-id group. Filtering a
        // node out must not silently retarget its edges to that group's shell.
        var sourceNodeIds = new HashSet<string>(chart.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        var baseGroupIds = new HashSet<string>(chart.Groups
            .Where(group => (!hasGroupFilter || selectedGroupIds.Contains(group.Id)) && !sourceNodeIds.Contains(group.Id))
            .Select(group => group.Id), StringComparer.Ordinal);
        var baseNodeIds = new HashSet<string>(baseNodes.Select(node => node.Id), StringComparer.Ordinal);
        var baseEndpointIds = new HashSet<string>(baseNodeIds, StringComparer.Ordinal);
        baseEndpointIds.UnionWith(baseGroupIds);
        var eligibleEdges = chart.Edges
            .Where(edge => IsEdgeEligible(edge, selectedEdgeIds, hasEdgeFilter, view))
            .Where(edge => baseEndpointIds.Contains(edge.SourceNodeId) && baseEndpointIds.Contains(edge.TargetNodeId))
            .ToList();

        List<TopologyNode> visibleNodes;
        List<TopologyEdge> visibleEdges;
        if (hasFocusFilter) {
            var focused = ExpandFocusEndpoints(focusNodeIds, baseNodeIds, baseEndpointIds, eligibleEdges, Math.Max(0, view.NeighborDepth), view.IncludeIncomingEdges, view.IncludeOutgoingEdges);
            visibleNodes = baseNodes.Where(node => focused.Contains(node.Id)).ToList();
            visibleEdges = eligibleEdges.Where(edge => focused.Contains(edge.SourceNodeId) && focused.Contains(edge.TargetNodeId)).ToList();
        } else {
            var visibleNodeIds = new HashSet<string>(baseNodes.Where(node => !hasStatusFilter || view.HealthStatuses.Contains(node.Status)).Select(node => node.Id), StringComparer.Ordinal);
            visibleNodeIds.UnionWith(baseGroupIds);
            if (hasEdgeFilter || hasEdgeKindFilter || hasStatusFilter) {
                foreach (var edge in eligibleEdges) {
                    visibleNodeIds.Add(edge.SourceNodeId);
                    visibleNodeIds.Add(edge.TargetNodeId);
                }
            }

            visibleNodes = baseNodes.Where(node => visibleNodeIds.Contains(node.Id)).ToList();
            visibleEdges = hasEdgeFilter || hasEdgeKindFilter || hasStatusFilter
                ? eligibleEdges.Where(edge => visibleNodeIds.Contains(edge.SourceNodeId) && visibleNodeIds.Contains(edge.TargetNodeId)).ToList()
                : view.IncludeConnectedEdges
                    ? chart.Edges.Where(edge => visibleNodeIds.Contains(edge.SourceNodeId) && visibleNodeIds.Contains(edge.TargetNodeId)).ToList()
                    : new List<TopologyEdge>();
        }

        var visibleGroupIds = new HashSet<string>(chart.Groups
            .Where(group => (!hasGroupFilter || selectedGroupIds.Contains(group.Id)) && (!hasStatusFilter || view.HealthStatuses.Contains(group.Status)))
            .Select(group => group.Id), StringComparer.Ordinal);
        if (view.IncludeNodeGroups) {
            foreach (var groupId in visibleNodes.Select(node => node.GroupId).Where(groupId => !string.IsNullOrWhiteSpace(groupId))) visibleGroupIds.Add(groupId!);
        }

        var allGroups = chart.Groups.Select(Clone).ToList();
        var sourceGroupIds = new HashSet<string>(allGroups.Select(group => group.Id), StringComparer.Ordinal);
        foreach (var edge in visibleEdges) {
            if (baseGroupIds.Contains(edge.SourceNodeId)) visibleGroupIds.Add(edge.SourceNodeId);
            if (baseGroupIds.Contains(edge.TargetNodeId)) visibleGroupIds.Add(edge.TargetNodeId);
        }
        chart.Groups.Clear();
        foreach (var group in allGroups.Where(group => visibleGroupIds.Contains(group.Id))) {
            if (group.ParentGroupId != null && sourceGroupIds.Contains(group.ParentGroupId) && !visibleGroupIds.Contains(group.ParentGroupId)) group.ParentGroupId = null;
            chart.Groups.Add(group);
        }
        foreach (var node in visibleNodes) {
            if (node.GroupId != null && sourceGroupIds.Contains(node.GroupId) && !visibleGroupIds.Contains(node.GroupId)) node.GroupId = null;
        }
        chart.Nodes.Clear();
        chart.Nodes.AddRange(visibleNodes);
        chart.Edges.Clear();
        chart.Edges.AddRange(visibleEdges);
    }

    private static bool IsNodeEligible(TopologyNode node, HashSet<string> selectedGroupIds, bool hasGroupFilter, HashSet<string> selectedNodeIds, bool hasNodeFilter, TopologyView view, bool includeStatus) {
        if (hasNodeFilter && !selectedNodeIds.Contains(node.Id)) return false;
        if (hasGroupFilter && (string.IsNullOrWhiteSpace(node.GroupId) || !selectedGroupIds.Contains(node.GroupId!))) return false;
        if (view.NodeKinds.Count > 0 && !view.NodeKinds.Contains(node.Kind)) return false;
        if (includeStatus && view.HealthStatuses.Count > 0 && !view.HealthStatuses.Contains(node.Status)) return false;
        return true;
    }

    private static bool IsEdgeEligible(TopologyEdge edge, HashSet<string> selectedEdgeIds, bool hasEdgeFilter, TopologyView view) {
        if (hasEdgeFilter && !selectedEdgeIds.Contains(edge.Id)) return false;
        if (view.EdgeKinds.Count > 0 && !view.EdgeKinds.Contains(edge.Kind)) return false;
        if (view.HealthStatuses.Count > 0 && !view.HealthStatuses.Contains(edge.Status)) return false;
        return true;
    }

    private static HashSet<string> ExpandFocusEndpoints(HashSet<string> focusNodeIds, HashSet<string> eligibleNodeIds, HashSet<string> eligibleEndpointIds, List<TopologyEdge> eligibleEdges, int depth, bool includeIncoming, bool includeOutgoing) {
        var visible = new HashSet<string>(focusNodeIds.Where(eligibleNodeIds.Contains), StringComparer.Ordinal);
        var frontier = new HashSet<string>(visible, StringComparer.Ordinal);
        for (var hop = 0; hop < depth && frontier.Count > 0; hop++) {
            var next = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in eligibleEdges) {
                if (includeOutgoing && frontier.Contains(edge.SourceNodeId) && eligibleEndpointIds.Contains(edge.TargetNodeId)) next.Add(edge.TargetNodeId);
                if (includeIncoming && frontier.Contains(edge.TargetNodeId) && eligibleEndpointIds.Contains(edge.SourceNodeId)) next.Add(edge.SourceNodeId);
            }

            next.ExceptWith(visible);
            foreach (var nodeId in next) visible.Add(nodeId);
            frontier = next;
        }

        return visible;
    }

    private static void ApplyGroupGrid(TopologyChart chart) {
        if (chart.GridLayout != null) { ApplyBoundedGroupGrid(chart); return; }
        if (chart.Groups.Count == 0) {
            ApplyMatrix(chart);
            return;
        }

        var pad = Math.Max(16, chart.Viewport.Padding);
        var titleOffset = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 64;
        var legendOffset = LegendReservedHeight(chart.Legend, chart.Viewport);
        var maximumColumns = Math.Max(1, (int)Math.Floor((chart.Viewport.Width - pad * 2 + 28) / (220 + 28)));
        var columns = Math.Max(1, Math.Min(chart.Groups.Count, Math.Min(maximumColumns, (int)Math.Ceiling(Math.Sqrt(chart.Groups.Count)) + 1)));
        var rows = (int)Math.Ceiling(chart.Groups.Count / (double)columns);
        var cellW = Math.Max(220, (chart.Viewport.Width - pad * 2 - (columns - 1) * 28) / columns);
        var cellH = Math.Max(180, (chart.Viewport.Height - pad * 2 - titleOffset - legendOffset - (rows - 1) * 28) / rows);

        for (var i = 0; i < chart.Groups.Count; i++) {
            var group = chart.Groups[i];
            var col = i % columns;
            var row = i / columns;
            if (group.Width <= 0) group.Width = Math.Max(220, cellW);
            if (group.Height <= 0) group.Height = Math.Max(180, cellH);
            if (IsUnset(group.X) && IsUnset(group.Y)) {
                group.X = pad + col * (cellW + 28);
                group.Y = pad + titleOffset + row * (cellH + 28);
            }

            PlaceNodesInGroup(chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).ToList(), group);
        }
    }

    private static void ApplyHubAndSpoke(TopologyChart chart, TopologyRenderOptions? options) {
        foreach (var group in chart.Groups) {
            var nodes = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).ToList();
            if (nodes.Count == 0) continue;
            var hub = nodes.FirstOrDefault(node => node.Kind == TopologyNodeKind.Hub || node.Kind == TopologyNodeKind.Location || node.Metadata.ContainsKey("hub")) ?? nodes[0];
            if (IsUnset(hub.X) && IsUnset(hub.Y)) {
                hub.X = group.X + group.Width / 2 - hub.Width / 2;
                hub.Y = group.Y + 72;
            }

            var branches = nodes.Where(node => !ReferenceEquals(node, hub)).ToList();
            var columns = Math.Max(1, Math.Min(4, branches.Count));
            for (var i = 0; i < branches.Count; i++) {
                var node = branches[i];
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                var col = i % columns;
                var row = i / columns;
                var gapX = (group.Width - 48) / columns;
                node.X = group.X + 24 + col * gapX + (gapX - node.Width) / 2;
                node.Y = hub.Y + hub.Height + 62 + row * (node.Height + 38);
            }
        }

        if (chart.Groups.Count == 0) ApplyMatrix(chart, options);
    }

    private static void ApplyMatrix(TopologyChart chart, TopologyRenderOptions? options = null) {
        var pad = Math.Max(24, chart.Viewport.Padding);
        var nodes = chart.Nodes.OrderBy(node => node.GroupId ?? string.Empty, StringComparer.Ordinal).ThenBy(node => node.Id, StringComparer.Ordinal).ToList();
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(nodes.Count)));
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        var naturalCellWidth = options?.FitContentToViewport == true
            ? (nodes.Select(node => node.Width).DefaultIfEmpty(120).Max() + 42) * columns
            : chart.Viewport.Width - pad * 2;
        var cellW = naturalCellWidth / columns;

        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
            var col = i % columns;
            var row = i / columns;
            node.X = pad + col * cellW + (cellW - node.Width) / 2;
            node.Y = top + row * (node.Height + 42);
        }
    }

    private static void ApplyDenseGrouped(TopologyChart chart) {
        if (chart.Groups.Count == 0) {
            ApplyMatrix(chart);
            return;
        }

        var explicitGroupPositions = ExplicitGroupPositions(chart.Groups);
        var surfaceInset = chart.RenderOptions == null ? 0 : CanvasSurfaceInset(chart, chart.RenderOptions);
        var pad = Math.Max(24, chart.Viewport.Padding) + surfaceInset;
        var titleOffset = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72;
        var legendOffset = LegendReservedHeight(chart.Legend, chart.Viewport);
        if (UsesWrappedDenseRows(chart)) {
            PlaceWrappedDenseGroups(chart, explicitGroupPositions, pad, titleOffset);
        } else {
            PlaceDenseGroupGrid(chart, explicitGroupPositions, pad, titleOffset, legendOffset);
        }

        ApplyDenseGroupEdgeDefaults(chart);
        // Grow the automatic mirror frame before pinned groups are restored, avoiding a later global translation.
        var automaticGroups = chart.Groups.Where(group => !explicitGroupPositions.ContainsKey(group.Id));
        if (chart.LayoutDirection == TopologyLayoutDirection.RightToLeft) {
            var right = chart.Viewport.Width - pad;
            var automaticRight = automaticGroups.Select(group => group.X + group.Width).DefaultIfEmpty(0).Max();
            if (automaticRight > right) right = Math.Ceiling(automaticRight);
            MirrorLayoutHorizontally(chart, pad, right);
            var restoredGroups = RestoreExplicitDenseGroupPositions(chart, explicitGroupPositions);
            ShiftManualWaypointsForRestoredGroups(chart, restoredGroups);
            ResetDenseInferenceForRestoredGroups(chart, restoredGroups);
            ApplyDenseGroupEdgeDefaults(chart);
        }

        if (chart.LayoutDirection == TopologyLayoutDirection.BottomToTop) {
            // Readable panels take the height of their content, so they are flipped within the content instead of the
            // viewport, which would leave the unused height as a gap under the title.
            var bottom = UsesReadableDenseLayout(chart) ? chart.Groups.Max(group => group.Y + group.Height) : chart.Viewport.Height - pad - legendOffset;
            if (!UsesReadableDenseLayout(chart)) {
                var automaticBottom = automaticGroups.Select(group => group.Y + group.Height).DefaultIfEmpty(0).Max();
                if (automaticBottom > bottom) bottom = Math.Ceiling(automaticBottom);
            }
            MirrorLayoutVertically(chart, pad + titleOffset, bottom);
            var restoredGroups = RestoreExplicitDenseGroupPositions(chart, explicitGroupPositions);
            ShiftManualWaypointsForRestoredGroups(chart, restoredGroups);
            ResetDenseInferenceForRestoredGroups(chart, restoredGroups);
            ApplyDenseGroupEdgeDefaults(chart);
        }
    }

    private static void ApplyGeographic(TopologyChart chart) {
        var map = TopologyMapProjection.MapRect(chart);
        foreach (var group in chart.Groups) {
            if (!group.Longitude.HasValue || !group.Latitude.HasValue) continue;
            if (group.Width <= 0) group.Width = 140;
            if (group.Height <= 0) group.Height = 76;
            var projected = TopologyMapProjection.Project(map, chart.MapViewport, group.Longitude.Value, group.Latitude.Value);
            group.X = projected.X - group.Width / 2;
            group.Y = projected.Y - group.Height / 2;
            group.Metadata["longitude"] = group.Longitude.Value.ToString("0.######", CultureInfo.InvariantCulture);
            group.Metadata["latitude"] = group.Latitude.Value.ToString("0.######", CultureInfo.InvariantCulture);
            group.Metadata["geoVisible"] = projected.IsInsideViewport ? "true" : "false";
        }

        foreach (var node in chart.Nodes) {
            if (!node.Longitude.HasValue || !node.Latitude.HasValue) continue;
            var projected = TopologyMapProjection.Project(map, chart.MapViewport, node.Longitude.Value, node.Latitude.Value);
            node.X = projected.X - node.Width / 2;
            node.Y = projected.Y - node.Height / 2;
            node.Metadata["longitude"] = node.Longitude.Value.ToString("0.######", CultureInfo.InvariantCulture);
            node.Metadata["latitude"] = node.Latitude.Value.ToString("0.######", CultureInfo.InvariantCulture);
            node.Metadata["geoVisible"] = projected.IsInsideViewport ? "true" : "false";
        }

        PlaceGeographicFallbackNodes(chart, map);
    }

    private static void PlaceGeographicFallbackNodes(TopologyChart chart, ChartRect map) {
        var fallback = chart.Nodes
            .Where(node => (!node.Longitude.HasValue || !node.Latitude.HasValue) && IsUnset(node.X) && IsUnset(node.Y))
            .OrderBy(node => node.Id, StringComparer.Ordinal)
            .ToList();
        if (fallback.Count == 0) return;
        var columns = Math.Max(1, Math.Min(4, fallback.Count));
        var cellW = map.Width / columns;
        var y = Math.Min(map.Bottom + 18, chart.Viewport.Height - chart.Viewport.Padding - TopologyRenderPrimitives.LegendReservedHeight(chart.Legend, chart.Viewport) - 52);
        for (var i = 0; i < fallback.Count; i++) {
            var node = fallback[i];
            var col = i % columns;
            var row = i / columns;
            node.X = map.Left + col * cellW + (cellW - node.Width) / 2;
            node.Y = y + row * (node.Height + 18);
            node.Metadata["geoVisible"] = "false";
        }
    }

    private static void PlaceDenseGroupGrid(TopologyChart chart, IReadOnlyDictionary<string, (double X, double Y)> explicitGroupPositions, double pad, double titleOffset, double legendOffset) {
        const double gap = 24;
        var columns = DenseGroupColumns(chart);
        var rows = (int)Math.Ceiling(chart.Groups.Count / (double)columns);
        var cellW = (chart.Viewport.Width - pad * 2 - (columns - 1) * gap) / columns;
        var cellH = (chart.Viewport.Height - pad * 2 - titleOffset - legendOffset - (rows - 1) * gap) / rows;
        var placements = new List<(TopologyGroup Group, List<TopologyNode> Nodes, int Column, int Row)>(chart.Groups.Count);
        var columnWidths = new double[columns];
        var rowHeights = new double[rows];

        for (var i = 0; i < chart.Groups.Count; i++) {
            var group = chart.Groups[i];
            var nodes = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).ToList();
            var col = i % columns;
            var row = i / columns;
            var policy = ResolveDenseGroupPolicy(chart, group, nodes);
            if (UsesReadableDenseLayout(chart)) {
                // Panels fill the row width but take the height of their content, not of the viewport; the panels of
                // one row are evened out below and the canvas height follows the content (TopologyLayoutNormalizer).
                group.Width = Math.Max(group.Width, Math.Max(Math.Max(190, cellW), DenseGroupWidth(chart, nodes, policy, group)));
                group.Height = Math.Max(group.Height, Math.Max(170, DenseGroupHeight(chart, nodes, policy, group)));
            } else {
                // Reserve an automatic grid before packing neighbors, including declared small panel sizes.
                var automaticGrid = (policy is TopologyGroupLayoutPolicy.Grid or TopologyGroupLayoutPolicy.VerticalStack) && !explicitGroupPositions.ContainsKey(group.Id);
                if (group.Width <= 0) group.Width = Math.Max(Math.Max(190, cellW), DenseGroupWidth(chart, nodes, policy, group));
                else if (automaticGrid) group.Width = Math.Max(group.Width, DenseGroupWidth(chart, nodes, policy, group));
                if (group.Height <= 0) group.Height = Math.Max(Math.Max(170, cellH), DenseGroupHeight(chart, nodes, policy, group));
                else if (automaticGrid) group.Height = Math.Max(group.Height, DenseGroupHeight(chart, nodes, policy, group));
            }
            columnWidths[col] = Math.Max(columnWidths[col], group.Width);
            rowHeights[row] = Math.Max(rowHeights[row], group.Height);
            placements.Add((group, nodes, col, row));
        }

        var columnX = new double[columns];
        var nextX = pad;
        for (var col = 0; col < columns; col++) {
            columnX[col] = nextX;
            nextX += columnWidths[col] + gap;
        }

        var rowY = new double[rows];
        var nextY = pad + titleOffset;
        for (var row = 0; row < rows; row++) {
            rowY[row] = nextY;
            nextY += rowHeights[row] + gap;
        }

        foreach (var placement in placements) {
            var group = placement.Group;
            if (!explicitGroupPositions.ContainsKey(group.Id)) {
                group.X = columnX[placement.Column];
                group.Y = rowY[placement.Row];
            }

            if (UsesReadableDenseLayout(chart)) group.Height = rowHeights[placement.Row];
            PlaceDenseNodesInGroup(chart, placement.Nodes, group);
        }

    }

    private static void PlaceNodesInGroup(IList<TopologyNode> nodes, TopologyGroup group) {
        if (nodes.Count == 0) return;
        var columns = Math.Max(1, Math.Min(3, nodes.Count));
        var innerX = group.X + 24;
        var innerY = group.Y + 74;
        var usableW = Math.Max(80, group.Width - 48);
        var cellW = usableW / columns;

        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
            var col = i % columns;
            var row = i / columns;
            node.X = innerX + col * cellW + (cellW - node.Width) / 2;
            node.Y = innerY + row * (node.Height + 34);
        }
    }

    private static void PlaceDenseNodesInGroup(TopologyChart chart, IList<TopologyNode> nodes, TopologyGroup group) {
        var policy = ResolveDenseGroupPolicy(chart, group, nodes);
        group.AppliedLayoutPolicy = policy;
        if (nodes.Count == 0) return;
        if (policy == TopologyGroupLayoutPolicy.VerticalStack) { PlaceVerticalStack(chart, nodes, group); return; }
        if (policy == TopologyGroupLayoutPolicy.CollapsedDots) {
            foreach (var node in nodes) {
                node.DisplayMode = TopologyNodeDisplayMode.Dot;
                node.Width = 22;
                node.Height = 22;
            }
        }

        if (policy == TopologyGroupLayoutPolicy.Grid || policy == TopologyGroupLayoutPolicy.CollapsedDots) {
            var gridRows = (int)Math.Ceiling(nodes.Count / (double)DenseGridColumns(nodes.Count));
            PlaceDenseGrid(nodes, group, policy == TopologyGroupLayoutPolicy.CollapsedDots ? DenseCollapsedDotColumns(nodes.Count) : 4, policy == TopologyGroupLayoutPolicy.CollapsedDots ? 12 : DenseRowGap(chart, nodes, gridRows) + DenseCaptionHeight(chart, nodes), useRequestedColumns: policy == TopologyGroupLayoutPolicy.CollapsedDots);
            return;
        }

        if (policy == TopologyGroupLayoutPolicy.PairRows) {
            PlaceDenseGrid(nodes, group, 2, DenseRowGap(chart, nodes, (int)Math.Ceiling(nodes.Count / 2.0)) + DenseCaptionHeight(chart, nodes));
            return;
        }

        if (policy == TopologyGroupLayoutPolicy.MiniMesh) {
            PlaceDenseMiniMesh(chart, nodes, group);
            return;
        }

        var hub = FindDenseHub(nodes);
        var innerX = group.X + 18;
        var innerY = group.Y + 74;
        var usableW = Math.Max(80, group.Width - 36);
        if (hub != null && IsUnset(hub.X) && IsUnset(hub.Y)) {
            hub.X = group.X + group.Width / 2 - hub.Width / 2;
            hub.Y = innerY;
        }

        var remaining = nodes.Where(node => !ReferenceEquals(node, hub)).ToList();
        var columns = DenseNodeColumns(remaining.Count);
        var cellW = usableW / columns;
        var startY = hub == null ? innerY : innerY + TopologyNodeFootprint.Height(chart, hub) + 40;
        var branchRowHeight = nodes.Select(node => TopologyNodeFootprint.Height(chart, node)).DefaultIfEmpty(44).Max();
        for (var i = 0; i < remaining.Count; i++) {
            var node = remaining[i];
            if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
            var col = i % columns;
            var row = i / columns;
            node.X = innerX + col * cellW + (cellW - node.Width) / 2;
            node.Y = startY + row * (branchRowHeight + 34);
        }
    }

    private static void PlaceDenseMiniMesh(TopologyChart chart, IList<TopologyNode> nodes, TopologyGroup group) {
        var innerX = group.X + 18;
        var innerY = group.Y + 78;
        var usableW = Math.Max(80, group.Width - 36);
        var columns = Math.Max(1, (int)Math.Ceiling(nodes.Count / 2.0));
        var cellW = usableW / columns;
        var maxNodeHeight = nodes.Select(node => TopologyNodeFootprint.Height(chart, node)).DefaultIfEmpty(44).Max();
        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
            var row = i >= columns ? 1 : 0;
            var col = i % columns;
            var stagger = row == 1 && columns > 1 ? cellW / 2 : 0;
            var captionOverhang = Math.Max(0, (TopologyNodeFootprint.Width(chart, node) - node.Width) / 2);
            node.X = Math.Max(innerX + captionOverhang, Math.Min(group.X + group.Width - 18 - node.Width - captionOverhang, innerX + col * cellW + stagger + (cellW - node.Width) / 2));
            node.Y = innerY + row * (maxNodeHeight + 44);
        }
    }

    private static void PlaceDenseGrid(IList<TopologyNode> nodes, TopologyGroup group, int maxColumns, double rowGap, bool useRequestedColumns = false) {
        var innerX = group.X + 18;
        var innerY = group.Y + 74;
        var usableW = Math.Max(80, group.Width - 36);
        var columns = useRequestedColumns ? Math.Max(1, maxColumns) : Math.Max(1, Math.Min(maxColumns, (int)Math.Ceiling(Math.Sqrt(nodes.Count))));
        var cellW = usableW / columns;
        var maxNodeHeight = nodes.Select(node => node.Height).DefaultIfEmpty(44).Max();
        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
            var col = i % columns;
            var row = i / columns;
            node.X = innerX + col * cellW + (cellW - node.Width) / 2;
            node.Y = innerY + row * (maxNodeHeight + rowGap);
        }
    }

    private static TopologyNode? FindDenseHub(IList<TopologyNode> nodes) {
        return nodes.FirstOrDefault(node => node.Kind == TopologyNodeKind.Hub || node.Kind == TopologyNodeKind.Location || node.Metadata.ContainsKey("hub")) ?? nodes[0];
    }

    private static TopologyGroupLayoutPolicy ResolveDenseGroupPolicy(TopologyChart chart, TopologyGroup group, IList<TopologyNode> nodes) {
        if (group.LayoutPolicy != TopologyGroupLayoutPolicy.Auto) return group.LayoutPolicy;
        if (nodes.Count > (UsesReadableDenseLayout(chart) ? DenseCollapsedDotThreshold : ClassicCollapsedDotThreshold)) return TopologyGroupLayoutPolicy.CollapsedDots;
        if (nodes.Count >= 10) return TopologyGroupLayoutPolicy.Grid;
        if (nodes.Count > 0 && nodes.All(node => node.Kind == TopologyNodeKind.Server)) return TopologyGroupLayoutPolicy.PairRows;
        return TopologyGroupLayoutPolicy.HubAndBranch;
    }

    private static void ApplyDenseGroupEdgeDefaults(TopologyChart chart) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var interGroupEdges = chart.Edges
            .Where(edge => TryGetEdgeGroups(edge, nodes, groups, out _, out _))
            .ToList();

        foreach (var edge in interGroupEdges) {
            if (!TryGetEdgeGroups(edge, nodes, groups, out var sourceGroup, out var targetGroup)) continue;
            ApplyDenseEdgePorts(edge, sourceGroup, targetGroup);
        }

        foreach (var pair in interGroupEdges.GroupBy(edge => DenseGroupPairKey(edge, nodes), StringComparer.Ordinal)) {
            var ordered = pair.OrderBy(edge => edge.Id, StringComparer.Ordinal).ToList();
            if (ordered.Count <= 1) continue;
            for (var i = 0; i < ordered.Count; i++) {
                var edge = ordered[i];
                if (Math.Abs(edge.RouteLane) > 0.0001) continue;
                edge.RouteLane = (i - (ordered.Count - 1) / 2.0) * 18;
                edge.LayoutInference |= TopologyEdgeLayoutInference.RouteLane;
            }
        }
    }

    private static bool TryGetEdgeGroups(TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyDictionary<string, TopologyGroup> groups, out TopologyGroup sourceGroup, out TopologyGroup targetGroup) {
        sourceGroup = null!;
        targetGroup = null!;
        if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) return false;
        if (string.IsNullOrWhiteSpace(source.GroupId) || string.IsNullOrWhiteSpace(target.GroupId)) return false;
        if (string.Equals(source.GroupId, target.GroupId, StringComparison.Ordinal)) return false;
        if (!groups.TryGetValue(source.GroupId!, out var foundSourceGroup) || !groups.TryGetValue(target.GroupId!, out var foundTargetGroup)) return false;
        sourceGroup = foundSourceGroup;
        targetGroup = foundTargetGroup;
        return true;
    }

    private static void ApplyDenseEdgePorts(TopologyEdge edge, TopologyGroup sourceGroup, TopologyGroup targetGroup) {
        if (edge.SourcePort != TopologyEdgePort.Auto && edge.TargetPort != TopologyEdgePort.Auto) return;
        var sourceX = sourceGroup.X + sourceGroup.Width / 2;
        var sourceY = sourceGroup.Y + sourceGroup.Height / 2;
        var targetX = targetGroup.X + targetGroup.Width / 2;
        var targetY = targetGroup.Y + targetGroup.Height / 2;
        var dx = targetX - sourceX;
        var dy = targetY - sourceY;
        TopologyEdgePort sourcePort;
        TopologyEdgePort targetPort;
        if (Math.Abs(dx) >= Math.Abs(dy)) {
            sourcePort = dx >= 0 ? TopologyEdgePort.Right : TopologyEdgePort.Left;
            targetPort = dx >= 0 ? TopologyEdgePort.Left : TopologyEdgePort.Right;
        } else {
            sourcePort = dy >= 0 ? TopologyEdgePort.Bottom : TopologyEdgePort.Top;
            targetPort = dy >= 0 ? TopologyEdgePort.Top : TopologyEdgePort.Bottom;
        }

        if (edge.SourcePort == TopologyEdgePort.Auto) {
            edge.SourcePort = sourcePort;
            edge.LayoutInference |= TopologyEdgeLayoutInference.SourcePort;
        }

        if (edge.TargetPort == TopologyEdgePort.Auto) {
            edge.TargetPort = targetPort;
            edge.LayoutInference |= TopologyEdgeLayoutInference.TargetPort;
        }
    }

    private static int GetLayer(TopologyNode node) {
        if (node.Metadata.TryGetValue("layer", out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var layer)) return layer;
        return node.Kind switch {
            TopologyNodeKind.Group => 0,
            TopologyNodeKind.Namespace => 1,
            TopologyNodeKind.Location => 2,
            TopologyNodeKind.Hub => 3,
            TopologyNodeKind.Branch => 4,
            TopologyNodeKind.Server => 5,
            TopologyNodeKind.Service => 6,
            TopologyNodeKind.Endpoint => 7,
            _ => 4
        };
    }

    private static bool IsUnset(double value) => Math.Abs(value) < 0.0001;

}
