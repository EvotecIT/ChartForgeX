# ChartForgeX Topology Next Plan

This branch starts from fresh `origin/main` at `d7d1ba2` (`Add SVG rendering engine foundation (#4)`).

## Current State

`ChartForgeX.Topology` already owns a product-neutral topology model, validation, deterministic layout helpers, SVG rendering, PNG rendering, and a small HTML wrapper. It is suitable for embeddable diagrams, not full TestimoX dashboard pages.

Representative topology and topology-adjacent demos already exist for:

- logical topology explorer
- replication mesh explorer
- subnets and site-links map
- geographic region/site distribution/WAN latency map visuals through dotted maps
- DC connectivity topology
- AD sites hierarchy
- replication health hub topology
- directory health replication topology
- service dependency topology

The present topology surface supports groups, nodes, edges, legends, health states, ports, route lanes, manual waypoints, selected states, muted edges, multi-line metric labels, tooltips, links, metadata/data attributes, SVG/HTML/PNG output, and basic HTML selection events.

## Boundary

Keep ChartForgeX focused on reusable diagram rendering:

- topology model and view filtering
- deterministic layouts
- route planning
- SVG/PNG/HTML fragment output
- host-readable metadata and events
- generic examples and smoke tests

Do not move these into ChartForgeX:

- TestimoX dashboard shell
- sidebars, topbars, filters, KPI card grids, inspectors, tables, or report panels
- live data collection or Active Directory-specific calculations
- HtmlForgeX dashboard component composition

HtmlForgeX/TestimoX should host ChartForgeX outputs inside dashboard panels and map real collected data into the generic topology model.

## Main Gaps

### 1. Topology SVG Markup Migration

The new SVG markup engine exists. `TopologySvgRenderer` now uses `SvgDocument`/`SvgElement` for the root SVG shell, SVG defs, background, header, group wrappers/cards/labels, edge wrappers, edge links, edge labels, node wrappers, node links, node bodies/cards/dots, node labels, node badges/subtitle chips, node icons/glyphs, node status overlays, legends, and edge paths, plus `SvgPathDataBuilder` for path data. The renderer is also split into focused partials for the main orchestration, node rendering, and legend rendering.

Next work:

- continue peeling out focused topology SVG partials or helper builders if edge/group rendering grows further
- reduce the remaining `Raw(BuildBodyMarkup(...))` handoff once the SVG document API has a convenient way to compose larger reusable subtrees
- preserve existing SVG output contracts: `data-cfx-role`, ids, data attributes, selected/highlight classes, href behavior, title tooltips, and accessibility metadata
- keep path geometry helpers independent of SVG serialization so PNG parity remains intact
- keep smoke tests asserting topology SVG is generated through the SVG engine and that unsafe strings/links still behave as before

Suggested PR: `Migrate topology SVG renderer to SVG markup engine`.

### 2. Obstacle-Aware Edge Routing

Current topology routing now has an opt-in `TopologyEdgeRouting.ObstacleAvoidingOrthogonal` mode backed by an internal `TopologyEdgeRouter`. The first implementation scores deterministic Manhattan lanes against node boxes, group header bands, estimated edge-label boxes, and route-to-route overlap. SVG edge wrappers expose route strategy, selected corridor, candidate count, fallback reason, segment count, node obstacle hits, label obstacle hits, and overlap score for host diagnostics.

Remaining gaps are mostly breadth and polish: broader dense-layout fixtures, better candidate generation for crowded clusters, clearer fallback diagnostics, and more screenshot-like examples.

Next work:

- expand the extracted `TopologyEdgeRouter` with more candidate corridors and fallback reasons
- preserve manual `Waypoints` as an explicit override while still emitting route diagnostics
- tune label clearance and route-to-route overlap weights with dense replication/site-link fixtures
- prefer stable deterministic output over physics-like movement
- use the route diagnostics in denser examples and host-facing inspector mocks
- add dense replication/site-link fixtures that prove routes do not cross node cards in common screenshot-like layouts

Suggested PR: `Add obstacle-aware topology routing`.

### 3. Better Dense Graph Layout

Existing layout modes are deterministic and report-friendly: `Manual`, `GroupGrid`, `HubAndSpoke`, `Layered`, `Matrix`, and the new `DenseGrouped` panel layout. `DenseGrouped` covers the first dense screenshot class by packing groups into panels and placing each group's hub plus branch rows without manual coordinates. It also has typed group policies for hub/branch, grid, pair-row, mini-mesh, and collapsed-dot panels, SVG exposes both requested and applied policies, and inter-group dense links get outside-facing ports plus stable lanes when the caller has not supplied them. SVG edge wrappers also expose whether source port, target port, or route lane were inferred. More advanced dense replication meshes and subnet maps still need richer internal policies.

Next work:

- extend per-group internal layout policies beyond the current hub/branch, pair-row, grid, mini-mesh, and collapsed-dot modes with lane-reservation policies
- expand lane reservations into named group corridors and inspector-ready diagnostics for dense site-link examples
- add optional node packing/compaction for dense card and tile diagrams
- keep layout output deterministic and export-safe, avoiding force-directed simulation as the default

Suggested PR: `Add dense grouped topology layout`.

### 4. Geographic Topology Rendering

The geographic demos can still use the dotted-map chart family for full map-style weighted markers, labels, and routes. Topology now also has a first-class `Geographic` layout mode that projects typed node/group longitude and latitude through `ChartMapViewport`, emits a topology-native geographic frame, graticule, generated land-dot background, regional boundary/land-area geometry, and optional group callout summaries with health counts, and keeps node/edge/group topology semantics intact for SVG, HTML, and PNG output.

Next work:

- decide whether geographic topology remains a dotted-map host pattern or becomes `TopologyLayoutMode.Geographic` - done with a topology-native layout while keeping dotted maps available for full map visuals
- if it becomes topology-native, add latitude/longitude metadata or typed coordinates on nodes - done for nodes and groups
- reuse existing map projection and viewport logic where possible, but make topology nodes/edges render on the projected coordinates - done with `ChartMapViewport` and equirectangular projection
- improve route arcs, endpoint trimming, clustering, label placement, and region callouts for TestimoX-like regional views - route arcs now render through a shared quadratic map-arc primitive in SVG/PNG, edge labels use the arc midpoint, and coordinated groups can render opt-in callout summaries
- consider additional projection modes only if the static output gains a visible benefit
- reuse or expose the dotted-map land layer behind topology nodes when a full world/region silhouette is needed - done for generated land dots and regional boundaries in SVG/PNG

Suggested follow-up: `Polish topology geographic map visuals`.

### 5. Dashboard-Level Composition

The screenshots show complete monitoring dashboards. ChartForgeX should not recreate the whole shell, but it can make hosting easier.

Next work in ChartForgeX:

- provide topology fragments sized for common dashboard panel ratios
- add focused view presets for selected region, selected site, selected path, critical links, affected links, and compact map cards
- improve metadata/event contracts so HtmlForgeX/TestimoX can attach inspector panels without parsing labels

Next work outside ChartForgeX:

- HtmlForgeX topology host components for card/panel layout, controls, inspector slots, legends, and table/chart companions
- TestimoX adapters that convert monitoring data into `TopologyChart`, dotted-map charts, and panel data models

Suggested PR after ChartForgeX routing/layout: `Add HtmlForgeX topology host panels`.

### 6. Deeper Browser-Side Interactions

Topology HTML now provides pointer/focus hover, click selection, and keyboard selection with richer payloads. Hover highlights related nodes, edges, and groups, then dispatches `cfx-topology-hover` / `cfx-topology-hover-clear`. Arrow keys cycle focus through connected topology elements and dispatch `cfx-topology-navigate` with from/to details. A selected element dispatches id, kind, status, metadata, metrics, node/group/edge context, route diagnostics, geographic route-arc diagnostics, and related node/edge/group ids so host dashboards can drive inspector panels without reparsing the SVG. Hosts can also dispatch `cfx-topology-set-selection` and `cfx-topology-clear-selection` on the wrapper to synchronize external panels with the embedded topology. Large complete topology pages can opt into lightweight zoom, pan, wheel-zoom, reset, host-driven viewport state, SVG/PNG export controls, and same-page selection/viewport synchronization through `EnableHtmlViewportControls`, `EnableHtmlExportControls`, and `EnableHtmlSynchronizedState`. The general interactive chart wrapper still has brush and richer controls that topology does not yet reuse.

Next work:

- unify topology HTML with `ChartForgeX.Interactivity.Html` where practical - topology-local sync/export/viewport controls are in place; shared adapter reuse remains open
- add optional zoom/pan controls for large topology diagrams - lightweight topology-local controls are in place; adapter-level reuse remains open
- dispatch richer selection details: node/edge/group id, kind, status, metrics, metadata, source/target, connected edges - done for the default complete-page selection event
- support hover highlighting, keyboard navigation, and host-controlled selection state - pointer/focus hover, keyboard activation, arrow-key graph navigation, and host selection events are in place
- keep the default fragment static unless the caller opts into richer interactions

Suggested PR: `Upgrade topology HTML interactions`.

### 7. Visual Examples And Baselines

The topology demo generator now includes a topology-native geographic map artifact (`visual-geographic-topology-map`) with typed coordinates, projected site markers, SVG/PNG route arcs, selected state, regional callout summaries, and route-control metadata. The basic `geographic-topology` demo also uses `TopologyLayoutMode.Geographic` instead of manually positioned map-like coordinates. `Build.ps1` now validates the topology visual manifest, required SVG/HTML/PNG artifacts, and key geographic route metadata during normal example validation.

Next work:

- decide whether topology artifacts should join the numeric `visual-baseline.json` gallery baseline or stay as manifest/metadata validation plus generated preview artifacts
- keep dense, routed, and geographic examples small enough to inspect in PRs
- use the visual manifest to track which screenshot families are represented by ChartForgeX versus dashboard host components

## Recommended Order

1. Migrate topology SVG rendering onto the SVG markup engine.
2. Add obstacle-aware route planning with dense route tests.
3. Add a dense grouped layout mode and example baselines.
4. Add geographic topology adapter/layout once routing and markup are stable.
5. Hand off dashboard composition to HtmlForgeX/TestimoX host components.
6. Deepen browser interactions after the host contract is clear.

This order keeps serialization stable first, then improves the visual engine, then lets host dashboards consume a stronger and cleaner topology contract.

## Validation Loop

Use the repo's existing quality loop before trusting visual changes:

- `dotnet test ChartForgeX.Tests/ChartForgeX.Tests.csproj`
- `.\Build.ps1`
- `.\Build.ps1 -UpdateVisualBaseline` when examples or baselines intentionally change
- inspect generated SVG for stable data hooks and route diagnostics
- compare SVG and PNG parity for route order, labels, selected outlines, and dense examples
