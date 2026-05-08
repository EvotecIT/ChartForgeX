# ChartForgeX Topology

`ChartForgeX.Topology` is a reusable diagram family for deterministic, SVG-first topology views. It is intentionally product-neutral: ChartForgeX owns the model, validation, layout helpers, SVG rendering, and export-ready output; dashboard shells and data collection belong to host projects.

Use it for static or embeddable diagrams such as service maps, SQL/server dependency views, people/team relationship diagrams, network connectivity maps, replication meshes, geographic-style location views, and product-specific topology views supplied by host applications.

## Model

- `TopologyChart` contains viewport, layout mode, layout direction, groups, nodes, edges, legend, and theme.
- `TopologyGroup` represents a cluster, region, tier, team, location, or any other logical grouping.
- `TopologyNode` represents a logical object such as a server, database, service, endpoint, person, team, queue, network segment, or application.
- `TopologyEdge` represents a link, connection, dependency, replication path, trust, mapping, data flow, membership, or certificate chain.

Groups, nodes, and edges can carry `Href`, `Tooltip`, `CssClass`, `Metrics`, and `Metadata`. Edges can also carry explicit `Waypoints` for deterministic manual route bends around dense content. Use `SourcePort`, `TargetPort`, or `.WithEdgePorts(...)` when a route should attach to a specific node side such as hub-to-hub site links, vertical replication paths, or selected-object connectivity spokes. Use `RouteLane` or `.WithEdgeRouteLane(...)` to shift the generated orthogonal lane while keeping the endpoints anchored to their ports. Use `.WithEdgeLabelOffset(...)` when a dense monitoring layout needs a latency or transport label nudged away from a nearby site title without changing the route itself. Set `TopologyEdgeRouting.ObstacleAvoidingOrthogonal` when a route should score deterministic Manhattan lanes and prefer paths that avoid nearby node cards, group headers, labels, and already-routed edges. Nodes have an optional `Symbol` for short visual glyphs such as `SQL`, `API`, `DC`, `GC`, initials, or role abbreviations without baking those product roles into ChartForgeX enums. A node can override the chart-wide display mode with `DisplayMode` and can expose a short `Badge`, which is useful for collapsed clusters, counts, roles, or small site markers. The SVG renderer escapes text safely, emits native SVG `<title>` tooltips, emits stable `data-*` attributes including source/target group ids on grouped edges, and wraps linked elements in SVG anchors. Unsafe `javascript:`, `data:`, and `vbscript:` hrefs are skipped.

Use `AddAutoGroup(id, label, status, ...)` and `AddAutoNode(id, label, kind, status, groupId, ...)` when a host has structured topology facts but does not want to hand-place every item. Pair those helpers with `DenseGrouped`, `GroupGrid`, `HubAndSpoke`, `Layered`, `Matrix`, or `Geographic` layout so examples and product adapters remain data-first instead of SVG- or pixel-first. Coordinate overloads remain available for intentional manual diagrams.

When `TopologyRenderOptions.IncludeDataAttributes` is enabled, which is the default, SVG output emits sanitized `data-cfx-meta-*` and `data-cfx-metric-*` attributes for groups, nodes, and edges. Caller-provided `CssClass` tokens are sanitized and appended to the matching SVG element so host wrappers can target product-specific states without ChartForgeX needing product-specific enums.

Edge labels are planned deterministically from the edge midpoint plus any caller-provided label offset, then nudged away from node boxes and previously placed labels where possible. SVG edge wrappers expose `data-label-offset-x` and `data-label-offset-y`, and SVG edge labels expose `data-label-x` and `data-label-y` so host wrappers can inspect or target final label placement without running their own layout logic.

Use `TopologyView` to render a focused perspective from the same source model. A view can select groups, nodes, or edges, override title/subtitle text, and keep connected edges between visible nodes. This is intended for dashboard cards such as "selected region", "critical paths", or "affected links" without duplicating topology data.

Views can also be composed from generic selectors instead of hardcoded ids. Use `NodeKinds`, `EdgeKinds`, and `HealthStatuses` to render product-neutral perspectives such as dependencies, connectivity, warning paths, or critical links. Use `TopologyView.AroundNode("api", depth: 1)` or `FocusNodeIds` with `NeighborDepth` to render a selected-node connectivity view; `IncludeIncomingEdges` and `IncludeOutgoingEdges` control traversal direction. Matching filtered edges keep their endpoints visible even when endpoint nodes have a different health status, which keeps views such as "critical links" readable.

`TopologyLegend.Default()` is intentionally product-neutral and only adds health-status entries. Add node-kind and edge-kind legend entries explicitly for the domain being rendered, for example service dependencies, transport links, replication paths, mappings, ownership, or team relationships. Legend entries can also use `symbol` overrides.

Set `TopologyRenderOptions.LegendMode` to `Auto`, `AutoWhenMissing`, or `Merge` when the renderer should infer legend items from the statuses, node kinds, node symbols, and edge kinds that are actually present in the chart.

## Layout

V1 layouts are deterministic and report-friendly:

- `Manual` uses explicit coordinates.
- `GroupGrid` arranges groups in a grid and places unpositioned nodes inside their groups.
- `HubAndSpoke` places a hub and branch nodes inside each group.
- `Layered` uses node kind or `Metadata["layer"]` for simple top-to-bottom or left-to-right layers.
- `Matrix` places nodes in a deterministic grid.
- `DenseGrouped` places groups in packed panels and lays out each group with a hub row plus deterministic branch rows for dense site-link, replication, and subnet maps. Use `.WithGroupLayout("site-id", TopologyGroupLayoutPolicy.Grid)`, `PairRows`, `MiniMesh`, or `CollapsedDots` when a panel should render a subnet grid, replication-pair rows, dense partner mesh, or many compact site dots. SVG group wrappers expose both `data-group-layout-policy` and `data-group-applied-layout-policy`, so hosts can tell requested `Auto` policy apart from the resolved policy. Inter-group dense edges get outside-facing ports and deterministic lanes when the caller has not already set ports or `RouteLane`; SVG edge wrappers expose inferred values through `data-edge-layout-inference`.
- `Geographic` projects typed node and group longitude/latitude coordinates into the topology viewport using `ChartMapViewport` and the same equirectangular coordinate model used by dotted maps. Use `.WithMapViewport(ChartMapViewport.World())`, `.WithNodeCoordinates("site-id", longitude, latitude)`, and `.WithGroupCoordinates("region-id", longitude, latitude)` for map-like region/site topology. SVG and PNG include a geographic frame, graticule lines, generated land-dot background or soft land silhouettes, regional boundary/land-area shapes, optional status-colored region hulls, and curved map-arc links for `TopologyEdgeRouting.Curved`. Set `IncludeGeographicCallouts = true` to add opt-in region callout cards for coordinated groups; the renderer places them near the projected group anchor, avoids node visuals, route labels, and sampled WAN route corridors, draws leader lines, summarizes grouped node health counts, and emits `data-cfx-visual-role="topology-geographic-callout"` plus `data-callout-*` hooks. SVG also exposes `data-cfx-projection`, viewport bounds, `data-cfx-map-background-style`, `data-node-longitude`, `data-node-latitude`, `data-group-longitude`, `data-group-latitude`, `*-geo-visible` flags for clamped out-of-viewport coordinates, and `data-route-curve` / `data-route-control-*` diagnostics for geographic route arcs.

Set `TopologyChart.LayoutDirection` or use `.WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)` when a hierarchy should read horizontally, such as namespace -> service -> database, source -> processor -> sink, or selected object -> connected dependencies.

After layout, ChartForgeX runs a deterministic normalization pass. It keeps nodes out of group headers, separates overlapping sibling nodes, expands groups around their members, shifts negative or title-overlapping manual coordinates and edge waypoints into the report-safe area, and expands the viewport when content, explicit edge routes, or the legend would otherwise be clipped. This is a safety net for reusable chart builders; callers should still provide intentional coordinates when using `Manual`.

The normalization pass also measures rendered tile labels, subtitle chips, badges, status markers, and edge label boxes before finalizing the viewport. Hidden legends do not reserve layout space. This matters for dashboard-like topology exports where compact site tiles, subnet chips, replication lag labels, and selected-object badges can extend beyond the raw node rectangle.

When a dashboard host needs stable panel dimensions, set `TopologyRenderOptions.FitContentToViewport = true` or call `.WithFitContentToViewport()`. In that mode ChartForgeX still measures the full rendered content, then fits the expanded SVG viewBox or sampled PNG raster output into the requested viewport. That keeps text, badges, strokes, and node proportions scaling together instead of shrinking only geometry. SVG output emits `data-fit-content-to-viewport` so host tests can verify fixed-panel behavior.

Group headers render their icon and title as one measured row, and node card titles fit to the available card width before SVG and PNG output. ChartForgeX first reduces dense card-title font size within a small readable range, then truncates only if the text still cannot fit. This avoids the common dashboard failure mode where a status icon, role glyph, or title is painted on top of adjacent text or outside its card.

For dense replication, subnet, and connectivity views, combine explicit ports with route lanes before reaching for manual waypoints:

```csharp
chart
    .AddEdge("emea-apac", "emea-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional)
    .WithEdgePorts("emea-apac", TopologyEdgePort.Right, TopologyEdgePort.Left)
    .AddEdge("fra-sin", "fra-dc2", "sin-dc1", "238 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
    .WithEdgePorts("fra-sin", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
    .WithEdgeRouteLane("fra-sin", 24);
```

Use `.WithEdgeRouteBundle(centerLane, spacing, "edge-a", "edge-b", ...)` when several reciprocal or related paths should share a planned corridor. The helper assigns centered `RouteLane` values in the order provided, so host code can express "these routes belong together" without hand-calculating every lane offset. Use `.WithReciprocalEdgeRouteBundles(spacing)` when the model has simple reciprocal pairs and the renderer should infer centered lanes for edges that do not already have explicit route lanes.

When several routes share the same explicit source or target port, ChartForgeX spreads those endpoints along the selected node side in deterministic target order. That keeps hub-and-spoke replication fans from leaving a cloud, hub, or bridgehead at one identical midpoint. SVG output exposes `data-route-start-x`, `data-route-start-y`, `data-route-end-x`, and `data-route-end-y` so host dashboards and tests can inspect the final rendered attachment points without parsing path data.

Reciprocal straight or curved routes still get a small visual separation. Orthogonal routes keep their explicit port anchors and should use `RouteLane` for separation instead, so top/bottom or left/right replication lanes do not get pulled off-card by a whole-route offset.

Obstacle-aware routes are opt-in so existing reports stay stable. SVG output exposes `data-route-strategy`, `data-route-corridor`, `data-route-candidate-count`, `data-route-fallback-reason`, `data-route-segment-count`, `data-route-obstacle-hits`, `data-route-label-obstacle-hits`, and `data-route-overlap-score` on edge wrappers so host dashboards and tests can inspect how a route was planned.

Force-directed and physics layouts are intentionally not implemented in v1.

## Rendering

```csharp
using ChartForgeX.Topology;

var chart = TopologyChart.Create()
    .WithId("service-map")
    .WithTitle("Service Dependency Map")
    .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
    .WithLegend(TopologyLegend.Default()
        .AddNodeKind("Service", TopologyNodeKind.Service, symbol: "API")
        .AddNodeKind("Database", TopologyNodeKind.Database, symbol: "SQL")
        .AddEdgeKind("Dependency", TopologyEdgeKind.Dependency))
    .AddNode("api", "API", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "API")
    .AddNode("database", "Database", 0, 0, TopologyNodeKind.Database, TopologyHealthStatus.Warning, symbol: "SQL")
    .AddEdge("api-database", "api", "database", "32 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward);

chart.Nodes[0].Metadata["layer"] = "1";
chart.Nodes[1].Metadata["layer"] = "2";

var svg = chart.ToSvg();
chart.SaveSvg("service-map.svg");
chart.SaveHtml("service-map.html");
chart.SavePng("service-map.png");

var emeaOnly = chart.ToSvg(new TopologyRenderOptions {
    View = new TopologyView {
        Id = "emea",
        Title = "EMEA Topology",
        GroupIds = { "EMEA" }
    }
});

var selectedService = chart.ToSvg(new TopologyRenderOptions {
    View = TopologyView.AroundNode("api", depth: 1)
});

var criticalDependencies = chart.ToSvg(new TopologyRenderOptions {
    View = new TopologyView {
        EdgeKinds = { TopologyEdgeKind.Dependency },
        HealthStatuses = { TopologyHealthStatus.Critical }
    }
});

var denseSvg = chart.ToSvg(TopologyRenderOptions.FromPreset(TopologyViewPreset.Compact));
var offenderSvg = chart.ToSvg(TopologyRenderOptions.FromPreset(TopologyViewPreset.Offenders));

var metricSvg = chart.ToSvg(new TopologyRenderOptions {
    Preset = TopologyViewPreset.MetricLabels,
    EdgeLabelMetricKey = "lag",
    EdgeSecondaryLabelMetricKey = "queue",
    LegendMode = TopologyLegendMode.Merge
});
```

`TopologySvgRenderer` outputs a complete standalone SVG with `viewBox`, `defs`, scoped CSS, groups below edges, edge labels above edges, nodes above labels, status badges, optional legends, optional geographic callouts, and accessibility metadata. `TopologyPngRenderer` draws the same model through ChartForgeX's dependency-free raster canvas for report exports. `TopologyHtmlRenderer` wraps the generated SVG in a neutral `.cfx-topology-wrapper` div with chart metadata. Complete HTML pages are static and script-free by default. Set `TopologyRenderOptions.EnableHtmlInteractions = true` to include the tiny interaction script that marks clicked groups, nodes, or edges and dispatches `cfx-topology-select` / `cfx-topology-clear` events. Selection details include the selected id, kind, status, metadata, metrics, group/node/edge context, callout counts, route diagnostics, geographic route-arc diagnostics, and related node/edge/group ids so HtmlForgeX or TestimoX can populate inspector panels without reparsing SVG markup. Selectable topology elements are keyboard focusable, Enter/Space activates selection, Escape clears selection, pointer/focus hover highlights related elements and dispatches `cfx-topology-hover` / `cfx-topology-hover-clear`, arrow keys cycle focus through related topology elements and dispatch `cfx-topology-navigate`, and hosts can dispatch `cfx-topology-set-selection` or `cfx-topology-clear-selection` on the wrapper to control state. With interactions enabled, set `TopologyRenderOptions.EnableHtmlViewportControls = true` to add opt-in zoom, pan, wheel-zoom, reset, `cfx-topology-viewport`, `cfx-topology-set-viewport`, and `cfx-topology-reset-viewport` support for large diagrams. Set `TopologyRenderOptions.EnableHtmlExportControls = true` to add opt-in SVG/PNG export buttons and `cfx-topology-export` events. Set `TopologyRenderOptions.EnableHtmlSynchronizedState = true` with `HtmlSyncGroupName` to mirror selection clears and viewport state across same-page topology wrappers using `cfx-topology-sync` / `cfx-topology-apply-sync`.

`TopologyRenderOptions` also supports reusable render presets and node display modes:

- `TopologyViewPreset.Grouped`, `Ungrouped`, `Connectivity`, `Dependency`, `Offenders`, `Compact`, and `MetricLabels` compose common static dashboard perspectives.
- `.WithMonitoringDashboardStyle()` applies a reusable premium monitoring treatment: softer group fills, thinner muted hierarchy edges, compact arrow markers, no edge-label plates, merged legends, and `TopologyMapBackgroundStyle.SoftSilhouette` for geographic topology. Use it when matching operational dashboards such as topology explorers, replication mesh views, subnet/site-link maps, and region maps without writing SVG markup in examples.
- `.WithNeutralGroupSurfaces()` composes with the monitoring style when site or replication panels should read as white dashboard cards with status-colored borders instead of tinted region containers.
- `IncludeGroupStatusDots` renders compact monitoring-style health dots in group headers. The monitoring dashboard style enables these by default so grouped site cards expose state without relying only on border color.
- `IncludeGeographicRegionHulls` renders soft status-colored region circles around coordinated geographic groups. The monitoring dashboard style enables these by default so map/topology mixes read like regional health clusters rather than loose map pins.
- `GeographicRegionHullPadding`, `GeographicRegionHullMinRadius`, and `GeographicRegionHullMaxRadius` tune cluster hull sizing without changing node coordinates; monitoring dashboard style uses tighter hull defaults so map clusters do not dominate WAN routes or callout cards.
- `PreferGeographicCalloutMapEdges` makes geographic callouts prefer dashboard-style map-edge placements before near-anchor placements. The monitoring dashboard style enables this so callout cards do not cover the main route arcs.
- Monitoring geographic routes render a clean underlay below WAN arcs, and callout leaders are routed as compact polylines with their own underlay so route labels, callouts, and map silhouettes remain visually separated in SVG and PNG exports.
- Monitoring non-map routes also render a slim route underlay for primary links, replication paths, mappings, and connectivity edges. Subtle or muted fabric stays quiet, while primary crossing paths keep the small visual separation expected in dense mesh diagrams.
- `TopologyMapBackgroundStyle.Auto`, `DottedLand`, and `SoftSilhouette` let hosts choose whether geographic topology uses point-sampled land or filled world/regional silhouettes. World topology now has boundary geometry, so monitoring-style world maps no longer have to fall back to the dotted land layer.
- `TopologyNodeDisplayMode.Card`, `CompactCard`, `Tile`, `Pill`, `Icon`, `Dot`, and `Hidden` let the same model render as full report cards, screenshot-style site tiles, dense dashboard summaries, or invisible route anchors for clean edge termination at group borders.
- `IncludeIconLabels` lets icon-mode nodes render short labels below the glyph, which is useful for replication partner diagrams where the marker should stay compact but domain controller names must remain visible.
- Individual nodes can set `DisplayMode` or use `.WithNodeDisplay("node-id", TopologyNodeDisplayMode.Dot, badge: "+12")` so a single topology can mix full cards, compact markers, icons, and collapsed count badges.
- Node kinds can use `.WithNodesDisplay(TopologyNodeKind.Server, TopologyNodeDisplayMode.Card)` so a host can keep branches as compact tiles while rendering servers, bridgeheads, databases, or selected assets as wider cards.
- `IncludeTileSubtitles` adds compact subtitle chips below tile labels for subnet CIDRs, roles, queue labels, or site metadata when a dense map needs those details visible.
- `CardSubtitleMode = TopologyCardSubtitleMode.Chip` renders card and compact-card subtitles as status-like chips inside node cards for bridgeheads, servers, selected assets, or health labels.
- `TopologyRenderOptions.IncludeEdgeLabelBackplates` can be disabled for route-label styling closer to network maps where latency labels sit directly on the route.
- When monitoring-style edge label backplates are enabled, they render as lightweight visible plates in SVG and PNG. Multi-line monitoring labels reserve extra no-plate clearance from their own route and draw a subtle route mask behind stacked text. Masks are surface-aware, so a label inside a tinted group panel uses that group fill instead of painting a white patch over the panel. Label placement is priority-aware so selected/critical labels reserve readable lanes before subtle context labels.
- Monitoring edge labels use wider fallback candidates and a larger reserved gap around labels already placed. This keeps dense replication metrics from stacking into unreadable clusters when several routes share the same natural midpoint.
- Groups can carry a short `Symbol`; the built-in `region` / `globe` symbol renders a small geographic mark in the group header.
- Groups can set `.WithGroupColor("group-id", "#8B5CF6")` to keep region or product identity color independent from health status; the health remains in `data-cfx-status`.
- Nodes can set `.WithNodeColor("node-id", "#2563EB")` for the same identity/status split on hubs, selected sites, collapsed clusters, or service nodes.
- Render options can set `.WithSelectedGroup("region-id")`, `.WithSelectedNode("node-id")`, and `.WithSelectedEdge("edge-id")` when a static export should show the currently selected site, region, or path without filtering or dimming other elements. SVG emits `data-cfx-selected` and selected classes; PNG renders matching selected outlines.
- Edges can set `.WithEdgeLineStyle("edge-id", TopologyEdgeLineStyle.Dashed)` or `Dotted` when relationship type should control line style separately from health status.
- Edges can set `.WithEdgeEmphasis("edge-id", TopologyEdgeEmphasis.Subtle)` when they should preserve health color but recede behind primary paths in SVG and PNG output, such as replication fan fabric or backup route context. Use `.WithEdgeMuted("edge-id")` for quiet internal structure that should become neutral/gray, such as hub-to-branch hierarchy lines.
- Edge kinds can be styled in one reusable pass with `.WithEdgesOfKind(TopologyEdgeKind.Dependency, lineStyle: TopologyEdgeLineStyle.Dashed, emphasis: TopologyEdgeEmphasis.Subtle)`, which is useful for map spokes, dependency fabric, or any relationship class that should recede consistently across projects.
- Edges can set `.WithEdgeLabelOffset("edge-id", x, y)` to tune label placement in dense panel-like diagrams while preserving route geometry and SVG/PNG parity.
- SVG and PNG render routes by visual priority instead of declaration order: subtle dependency fabric draws first, critical/strong routes draw later, and selected routes draw last. SVG emits `data-edge-render-order` so hosts can validate layering deterministically.
- `EdgeLabelMetricKey`, `EdgeSecondaryLabelMetricKey`, and `EdgeTertiaryLabelMetricKey` allow hosts to switch route labels from default edge text to metrics such as `transport`, `cost`, `lag`, `queue`, `owner`, or `lastSuccess`; `.WithEdgeMetricLabels("lag", "queue", "transport")` applies the stacked metric label keys in one call.

## Host Boundaries

HtmlForgeX can later provide cards, toolbars, sidebars, filters, tabs, inspectors, and event panels around the SVG. TestimoX or another product can later collect and calculate product-specific health, then convert that data into `TopologyChart`. ChartForgeX should not connect to Active Directory, hardcode TestimoX data, or implement dashboard page layout.

The example console app writes sample diagrams to `artifacts/topology-demo/` and to the normal generated example output folder. They intentionally use sample data only and demonstrate how a host can map its own product concepts onto the generic topology model:

- `site-topology.svg`
- `replication-mesh.svg`
- `subnets-site-links.svg`
- `geographic-topology.svg`
- `dc-connectivity.svg`
- `service-dependency.svg`
- `visual-geographic-topology-map.svg`
- `service-dependency-api-neighbors-view.svg`
- `service-dependency-critical-dependencies-view.svg`
- matching `.png` and `.html` files
- focused view examples for EMEA, selected-node neighbors, DC-level replication, offender highlighting, compact service dependencies, critical dependencies, and critical replication paths
- `index.html`
