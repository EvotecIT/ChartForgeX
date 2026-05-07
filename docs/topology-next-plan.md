# ChartForgeX Topology Next Plan

This document is an agent handoff map for the current topology surface. It is no longer a branch-start plan: obstacle-aware routing, dense grouped layout, geographic topology, static-default HTML, and topology-local interaction controls already exist.

## Current State

`ChartForgeX.Topology` owns a product-neutral topology model, validation, deterministic layout helpers, SVG rendering, PNG rendering, and a small HTML wrapper. It is suitable for embeddable diagrams and static report fragments, not full TestimoX dashboard pages.

Already shipped topology capabilities:

- groups, nodes, edges, legends, health states, ports, route lanes, manual waypoints, selected states, muted edges, metadata, metrics, tooltips, links, data attributes, and SVG/HTML/PNG output
- deterministic layout modes: `Manual`, `GroupGrid`, `HubAndSpoke`, `Layered`, `Matrix`, `DenseGrouped`, and `Geographic`
- opt-in `TopologyEdgeRouting.ObstacleAvoidingOrthogonal` with route diagnostics for host inspection
- dense grouped panel policies for hub/branch, grid, pair-row, mini-mesh, and collapsed-dot layouts
- topology-native geographic projection through `ChartMapViewport`, with generated land dots, regional geometry where available, curved route arcs, and optional group callouts
- static and script-free complete HTML pages by default; `EnableHtmlInteractions` opts into pointer/focus hover, click selection, keyboard selection, viewport controls, export controls, and synchronized state

Representative topology and topology-adjacent demos already exist for logical topology exploration, replication meshes, subnets and site-link maps, geographic region/site distribution, WAN latency, DC connectivity, AD sites hierarchy, replication health, directory health, and service dependency topology.

## Boundary

Keep ChartForgeX focused on reusable visual output:

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

## Remaining Work

### 1. Finish SVG Markup Cleanup

The SVG markup engine exists, and topology now uses `SvgDocument`/`SvgElement` for the root shell, defs, background, header, group wrappers/cards/labels, edge wrappers, edge links, edge labels, node wrappers, node links, node bodies/cards/dots, node labels, node badges/subtitle chips, node icons/glyphs, node status overlays, legends, geographic callouts, and edge paths. It also uses `SvgPathDataBuilder` for path data and focused partials for node, legend, and geographic callout rendering.

This is not a full repository-wide string-to-markup migration yet. Topology body composition now stays in the SVG element tree, but chart grids embed scoped child SVG as raw markup, and dotted/region/tile maps plus some hot chart paths still use targeted `StringBuilder` or raw text insertion where the migration has not paid for itself yet.

Next work:

- continue reducing non-topology raw/string render paths
- keep path geometry helpers independent of SVG serialization so PNG parity remains intact
- preserve existing SVG contracts: ids, `data-cfx-role`, data attributes, selected/highlight classes, href behavior, title tooltips, accessibility metadata, and deterministic output
- migrate remaining raw/string render paths only when tests can protect the exact host-facing output

Suggested follow-up: `Continue SVG markup cleanup outside topology`.

### 2. Polish Dense Routing And Layout

Obstacle-aware routing and `DenseGrouped` are implemented. Remaining work is breadth and polish, not the first implementation.

Next work:

- add denser replication/site-link fixtures that prove routes do not cross node cards in common screenshot-like layouts
- tune label-clearance and route-overlap weights with those fixtures
- expand candidate corridors and fallback reasons in `TopologyEdgeRouter`
- extend per-group layout policies only where real dense examples need more than hub/branch, pair-row, grid, mini-mesh, and collapsed-dot modes
- keep layout output deterministic and export-safe; force-directed simulation remains out of scope for the static default

Suggested follow-up: `Polish dense topology routing`.

### 3. Keep Geographic Topology Generic

`TopologyLayoutMode.Geographic` is now topology-native while dotted maps remain available for map-first weighted marker visuals.

Next work:

- improve label placement, route arc trimming, clustering, and callout placement through generic fixtures
- add more reusable map definitions only as data definitions, not renderer assumptions
- keep identifiers and map data generic enough for US, Europe, cloud regions, tenants, inventory zones, and custom domains
- add additional projection modes only when static output visibly benefits

Suggested follow-up: `Polish topology geographic map visuals`.

### 4. Broaden Neutral Visual Blocks

The first neutral visual-block surface now exists in `ChartForgeX.VisualBlocks`: `ChartTable`, `ChartList`, `MetricCard`, and `VisualGrid`. `ChartGrid` remains intentionally chart-only.

Next work:

- broaden table/list/card styling from real PowerBGInfo, ImagePlayground, email, Word, and wallpaper examples
- add small icon/status symbol options only when they stay renderer-owned and dependency-free
- keep the scope bounded: no spreadsheet engine, no arbitrary HTML renderer, no region-specific assumptions, and no external table library

Suggested follow-up: `Polish visual block primitives`.

### 5. Hand Dashboard Composition To Hosts

ChartForgeX should make host dashboards easier, but it should not become the dashboard shell.

Next work in ChartForgeX:

- provide topology fragments sized for common dashboard panel ratios
- keep focused view presets for selected region, selected site, selected path, critical links, affected links, and compact map cards
- improve metadata/event contracts where host panels need richer details without parsing labels

Next work outside ChartForgeX:

- HtmlForgeX topology host components for cards, panels, controls, inspector slots, legends, and chart/table companions
- TestimoX adapters that convert monitoring data into `TopologyChart`, dotted-map charts, visual blocks, and panel data models

Suggested follow-up: `Add HtmlForgeX topology host panels`.

### 6. Align Browser Interaction Adapters

Topology HTML is static by default and has topology-local interaction support when callers opt in. The general interactive chart wrapper still has brush and richer controls that topology does not reuse.

Next work:

- unify topology HTML with `ChartForgeX.Interactivity.Html` where it reduces duplicated event/control code
- keep complete topology HTML script-free unless `EnableHtmlInteractions = true`
- preserve existing event names and payloads for hover, selection, navigation, viewport, export, and synchronized state

Suggested follow-up: `Unify topology HTML interactions`.

### 7. Promote Visual Baselines When Stable

The example generator validates topology manifests, required SVG/HTML/PNG artifacts, PNG size, and key geographic route metadata. Topology and geographic topology examples intentionally stay outside `visual-baseline.json` for now; their current release gate is `visual-capability-manifest.json` plus required metadata checks in `Build.ps1`.

Next work:

- promote topology artifacts into `visual-baseline.json` only after dense routing and geographic layout polish are stable enough for numeric baselines
- start promotion from the manifest's `baselineCandidates` list
- keep dense, routed, and geographic examples small enough to inspect in PRs
- track which screenshot families are represented by ChartForgeX versus host dashboard components

## Recommended Order

1. Add dense routing/layout fixtures and polish based on real screenshot-like cases.
2. Promote topology/geographic visual baseline coverage only after layout polish stabilizes.
3. Polish visual block primitives from real consumer examples.
4. Hand dashboard shells and product adapters to HtmlForgeX/TestimoX.
5. Unify topology browser interaction code with the shared adapter after host needs settle.

This order keeps the public static-output contract stable first, then improves the renderer internals, then opens the reusable visual-block surface that PowerBGInfo, ImagePlayground, email, Word, and wallpaper scenarios need.

## Validation Loop

Use the repo's existing quality loop before trusting visual changes:

- `dotnet test .\ChartForgeX.Tests\ChartForgeX.Tests.csproj -c Release`
- `.\Build.ps1`
- `.\Build.ps1 -UpdateVisualBaseline` when examples or baselines intentionally change
- inspect generated SVG for stable data hooks and route diagnostics
- compare SVG and PNG parity for route order, labels, selected outlines, and dense examples
