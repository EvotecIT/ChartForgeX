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
- reusable topology icon catalogs with built-in common, network, Microsoft AD, cloud, and people/team packs plus vendor-pack registration through `TopologyIconPack`
- artwork-backed icon definitions through `TopologyIconArtwork`, so vendor packs can provide trusted inline SVG fragments or host-managed image hrefs while keeping shape/symbol/color fallbacks for export paths that do not embed artwork
- dependency-free JSON icon-pack manifests through `TopologyIconPackJson.FromJson(...)`, `LoadJsonManifest(...)`, `LoadJsonManifestsFromDirectory(...)`, `LoadJsonManifestResultsFromDirectory(...)`, `LoadJsonCatalogFromDirectory(...)`, `TopologyIconCatalogLoadOptions`, `TopologyIconPackConflictBehavior`, `SaveJsonManifest(...)`, `.ToJsonManifest(...)`, `WriteJsonManifest(...)`, `SaveJsonManifestsToDirectory(...)`, `TopologyIconCatalogExportOptions`, `Validate()`, `TopologyIconCatalog.AddJsonPack(...)`, `AddJsonPackFile(...)`, and `AddJsonPacksFromDirectory(...)`, so host applications and vendor adapters can ship reusable packs without adding a JSON dependency to ChartForgeX
- dependency-free SVG folder import through `TopologyIconSvgPackImporter.ImportSvgPackFromDirectory(...)`, `TopologyIconSvgPackImportOptions`, and `TopologyIconSvgPackImportResult`, so large external stencil collections can become auditable icon manifests with source URL, revision, license, source path, inferred categories, and skipped-file diagnostics
- catalog-to-palette rendering through `TopologyIconCatalog.ToPaletteChart(...)`, so hosts can display built-in and vendor packs as grouped topology stencil/picker diagrams with stable icon metadata
- catalog-to-stencil-browser rendering through `TopologyIconCatalog.ToStencilBrowserHtmlPage(...)`, giving examples and host prototypes a searchable picker with vendor, pack, category, selection-detail, and `cfx-icon-browser-select` metadata events
- catalog search/filtering through `TopologyIconCatalogQuery`, covering search text, pack ids, vendors, categories, and built-in/custom pack source for host pickers
- catalog pack/vendor summaries through `GetPackSummaries(...)` and `GetVendorSummaries(...)`, so hosts can present picker folders, tabs, counters, categories, tags, source paths, and vendor groupings without parsing catalog internals
- pack/icon metadata and tag helpers plus picker facets through `WithMetadata(...)`, `WithTags(...)`, `WithIconTags(...)`, `GetPackIds(...)`, `GetVendors(...)`, `GetCategories(...)`, and `GetTags(...)`
- SVG and PNG glyph parity for core stencil shapes such as storage, application, certificate, desktop, laptop, forest, domain, switch, router, load balancer, firewall, site, server, database, service, people, and teams; SVG and HTML additionally render icon artwork when a pack supplies it

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

### 3. Turn The Stencil Browser Into A Builder Surface

The catalog can now render both static palette diagrams and an HTML stencil browser with search, folders, filters, selection details, and host-readable selection events. It is intentionally still a picker, not a full diagram editor.

Next work:

- add a small builder demo that lets users place selected stencils onto a canvas and export the resulting `TopologyChart`
- add optional icon-pack provenance and license notes in the inspector so official/vendor packs can be distinguished from sample packs
- decide how hosts should register official asset bundles: embedded manifests, app-provided URLs, or package-discovered folders
- add a simple custom-pack authoring workflow that validates artwork, tags, categories, aliases, and preview rendering before a pack is accepted
- keep editor state outside ChartForgeX core unless it is a reusable data contract rather than UI state

### 4. Create The Microsoft/Azure Stencil Asset PR

The importer now makes a large generated asset PR practical, but the full external stencil ingest should be reviewed separately from core topology APIs. The source currently identified for this work is `https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio`, pinned to a commit/release in the generated manifest metadata and accompanied by the MIT license text from that repository.

Next work:

- create a dedicated asset-import branch/PR that contains the conversion/import tooling run, generated manifests, optional generated PNG previews, copied license/provenance notes, and a manifest summary
- keep the source checkout outside the repo or under a clearly ignored staging folder; commit only cleaned/generated pack assets that belong to ChartForgeX
- preserve source folder categories such as Azure, Office 365, Power Platform, Security and Governance, Databases and Analytics, IoT, and Other Providers unless a manual classification pass improves them
- generate one or more manageable packs instead of one giant unreviewable pack when the source folders map naturally to picker tabs
- use PNG conversion only for preview thumbnails or non-SVG hosts; keep ChartForgeX core dependency-free and do not add SkiaSharp, Magick.NET, ImageSharp, Inkscape, or resvg as runtime dependencies
- include a generated import report with source revision, license, icon counts, skipped files, duplicate-id suffixes, unsafe SVG findings, and category counts so reviewers can verify what changed without opening every icon
- review Microsoft trademark/brand-use expectations separately from the MIT repository license before shipping any pack as an official built-in vendor bundle

Suggested follow-up: `Import Microsoft Azure stencil assets`.

### 5. Keep Geographic Topology Generic

`TopologyLayoutMode.Geographic` is now topology-native while dotted maps remain available for map-first weighted marker visuals.

Next work:

- improve label placement, route arc trimming, clustering, and callout placement through generic fixtures
- add more reusable map definitions only as data definitions, not renderer assumptions
- keep identifiers and map data generic enough for US, Europe, cloud regions, tenants, inventory zones, and custom domains
- add additional projection modes only when static output visibly benefits

Suggested follow-up: `Polish topology geographic map visuals`.

### 6. Broaden Neutral Visual Blocks

The first neutral visual-block surface now exists in `ChartForgeX.VisualBlocks`: `ChartTable`, `ChartList`, `MetricCard`, and `VisualGrid`. `ChartGrid` remains intentionally chart-only.

Next work:

- broaden table/list/card styling from real PowerBGInfo, ImagePlayground, email, Word, and wallpaper examples
- add small icon/status symbol options only when they stay renderer-owned and dependency-free
- keep the scope bounded: no spreadsheet engine, no arbitrary HTML renderer, no region-specific assumptions, and no external table library

Suggested follow-up: `Polish visual block primitives`.

### 7. Hand Dashboard Composition To Hosts

ChartForgeX should make host dashboards easier, but it should not become the dashboard shell.

Next work in ChartForgeX:

- provide topology fragments sized for common dashboard panel ratios
- keep focused view presets for selected region, selected site, selected path, critical links, affected links, and compact map cards
- improve metadata/event contracts where host panels need richer details without parsing labels
- continue expanding renderer-owned shapes from real AD, network, cloud, M365, backup, storage, and team diagrams while keeping vendor branding in packs and JSON manifests rather than topology enums
- add host-facing palette affordances only as neutral topology outputs, query contracts, and summaries: searchable/filterable UI belongs in HtmlForgeX/TestimoX, while ChartForgeX should keep exporting stable catalog metadata and palette diagrams

Next work outside ChartForgeX:

- HtmlForgeX topology host components for cards, panels, controls, inspector slots, legends, and chart/table companions
- TestimoX adapters that convert monitoring data into `TopologyChart`, dotted-map charts, visual blocks, icon-pack selections, and panel data models

Suggested follow-up: `Add HtmlForgeX topology host panels`.

### 8. Align Browser Interaction Adapters

Topology HTML is static by default and has topology-local interaction support when callers opt in. The general interactive chart wrapper still has brush and richer controls that topology does not reuse.

Next work:

- unify topology HTML with `ChartForgeX.Interactivity.Html` where it reduces duplicated event/control code
- keep complete topology HTML script-free unless `EnableHtmlInteractions = true`
- preserve existing event names and payloads for hover, selection, navigation, viewport, export, and synchronized state

Suggested follow-up: `Unify topology HTML interactions`.

### 9. Promote Visual Baselines When Stable

The example generator validates topology manifests, required SVG/HTML/PNG artifacts, PNG size, and key geographic route metadata. Topology and geographic topology examples intentionally stay outside `visual-baseline.json` for now; their current release gate is `visual-capability-manifest.json` plus required metadata checks in `Build.ps1`.

Next work:

- promote topology artifacts into `visual-baseline.json` only after dense routing and geographic layout polish are stable enough for numeric baselines
- start promotion from the manifest's `baselineCandidates` list
- keep dense, routed, and geographic examples small enough to inspect in PRs
- track which screenshot families are represented by ChartForgeX versus host dashboard components

## Recommended Order

1. Add dense routing/layout fixtures and polish based on real screenshot-like cases.
2. Create the dedicated Microsoft/Azure stencil asset-import PR once importer provenance and folder classification are accepted.
3. Promote topology/geographic visual baseline coverage only after layout polish stabilizes.
4. Polish visual block primitives from real consumer examples.
5. Hand dashboard shells and product adapters to HtmlForgeX/TestimoX.
6. Unify topology browser interaction code with the shared adapter after host needs settle.

This order keeps the public static-output contract stable first, then improves the renderer internals, then opens the reusable visual-block surface that PowerBGInfo, ImagePlayground, email, Word, and wallpaper scenarios need.

## Validation Loop

Use the repo's existing quality loop before trusting visual changes:

- `dotnet test .\ChartForgeX.Tests\ChartForgeX.Tests.csproj -c Release`
- `.\Build.ps1`
- `.\Build.ps1 -UpdateVisualBaseline` when examples or baselines intentionally change
- inspect generated SVG for stable data hooks and route diagnostics
- compare SVG and PNG parity for route order, labels, selected outlines, and dense examples
