# ChartForgeX Topology

`ChartForgeX.Topology` is a reusable diagram family for deterministic, SVG-first topology views. It is intentionally product-neutral: ChartForgeX owns the model, validation, layout helpers, SVG rendering, and export-ready output; dashboard shells and data collection belong to host projects.

Use it for static or embeddable diagrams such as service maps, SQL/server dependency views, people/team relationship diagrams, network connectivity maps, replication meshes, geographic-style location views, and product-specific topology views supplied by host applications.

## Model

- `TopologyChart` contains viewport, layout mode, layout direction, groups, nodes, edges, legend, and theme.
- `TopologyGroup` represents a cluster, region, tier, team, location, or any other logical grouping.
- `TopologyNode` represents a logical object such as a server, database, service, endpoint, person, team, queue, network segment, or application.
- `TopologyEdge` represents a link, connection, dependency, replication path, trust, mapping, data flow, membership, or certificate chain.

Groups, nodes, and edges can carry `Href`, `Tooltip`, `CssClass`, `Metrics`, and `Metadata`. Edges can also carry explicit `Waypoints` for deterministic manual route bends around dense content. Use `SourcePort`, `TargetPort`, or `.WithEdgePorts(...)` when a route should attach to a specific node side such as hub-to-hub site links, vertical replication paths, or selected-object connectivity spokes. Use `RouteLane` or `.WithEdgeRouteLane(...)` to shift the generated orthogonal lane while keeping the endpoints anchored to their ports. Use `.WithEdgeLabelOffset(...)` when a dense monitoring layout needs a latency or transport label nudged away from a nearby site title without changing the route itself. Set `TopologyEdgeRouting.ObstacleAvoidingOrthogonal` when a route should score deterministic Manhattan lanes and prefer paths that avoid nearby node cards, group headers, labels, and already-routed edges. Nodes have an optional `Symbol` for short visual glyphs such as `SQL`, `API`, `DC`, `GC`, initials, or role abbreviations without baking those product roles into ChartForgeX enums. A node can override the chart-wide display mode with `DisplayMode` and can expose a short `Badge`, which is useful for collapsed clusters, counts, roles, or small site markers. The SVG renderer escapes text safely, emits native SVG `<title>` tooltips, emits stable `data-*` attributes including source/target group ids on grouped edges, and wraps linked elements in SVG anchors. Unsafe `javascript:`, `data:`, and `vbscript:` hrefs are skipped.

For Visio-style diagram building, nodes and groups can also reference reusable icon ids through `IconId` or the `.AddIconNode(...)`, `.AddAutoIconNode(...)`, `.WithNodeIcon(...)`, and `.WithGroupIcon(...)` helpers. `TopologyIconCatalog.Default()` includes product-neutral packs for common infrastructure, network devices, Microsoft Active Directory, cloud elements, and people/teams. Hosts can register vendor packs with `new TopologyIconPack("veeam", "Veeam").AddIcon(...)` or import a portable JSON manifest with `TopologyIconPackJson.FromJson(...)` / `TopologyIconCatalog.AddJsonPack(...)`, then pass the catalog through `TopologyRenderOptions.IconCatalog`. Vendor packs can add pre-built `TopologyIconDefinition` instances, and both packs and icons support `.WithMetadata(...)` for adapter data such as product family, documentation URL, SKU, or source pack metadata. They also support `.WithTags(...)`, and packs can use `.WithIconTags(...)`, so picker search can handle aliases such as `loadbalancer`, `repo`, `dc`, or vendor vocabulary without changing display labels. Icons map to a generic `TopologyNodeKind`, a renderer-owned `TopologyIconShape`, a fallback symbol, optional color, category, and optional display-mode default. SVG and PNG render explicit glyphs for core stencil shapes such as server, storage, database, application, service, endpoint, certificate, network, switch, router, firewall, load balancer, forest, domain, site, people, and team. SVG and HTML renderers can also use `TopologyIconArtwork` for real inline SVG fragments or host-managed image hrefs, while PNG keeps using the shape/symbol fallback until a host supplies raster artwork. Group headers that carry an `IconId` use the same glyph language, while explicit `region` / `globe` symbols keep the map-style header mark. `catalog.Search(new TopologyIconCatalogQuery { ... })` gives host pickers a product-neutral way to filter by search text, pack id, vendor, category, tags, and built-in/custom pack source. `catalog.GetPackIds(...)`, `catalog.GetVendors(...)`, `catalog.GetCategories(...)`, and `catalog.GetTags(...)` expose picker facets without forcing a UI into ChartForgeX. `catalog.ToPaletteChart(...)` uses the same filter model through `TopologyIconPaletteOptions`, so hosts can offer built-in and vendor packs from one reusable model. SVG and HTML selection payloads expose icon id, pack, label, shape, artwork type, metadata, and tags so a dashboard palette or inspector can work from stable catalog metadata instead of parsing labels.

Use `AddAutoGroup(id, label, status, ...)` and `AddAutoNode(id, label, kind, status, groupId, ...)` when a host has structured topology facts but does not want to hand-place every item. Pair those helpers with `DenseGrouped`, `ForceDirected`, `GroupGrid`, `HubAndSpoke`, `Layered`, `Matrix`, or `Geographic` layout so examples and product adapters remain data-first instead of SVG- or pixel-first. Coordinate overloads remain available for intentional manual diagrams. For large user or device populations, prefer readable aggregate cards, sampled cohorts, health summaries, or drill-through links over thousands of static dots; SVG and PNG exports are meant to communicate topology, not replace an interactive object explorer.

Use `.AddHierarchy(...)` when the source data is naturally parent-child, such as forest -> domain -> group -> user, tenant -> subscription -> resource group -> service, team -> manager -> member, or network core -> distribution -> access. Each `TopologyHierarchyItem` can provide an explicit `Level`, or ChartForgeX can infer levels from `ParentId`. `TopologyHierarchyOptions.MinLevel` and `MaxLevel` let a host render levels `0`, `0..1`, `0..2`, `0..3`, or focused windows such as `2..3` from the same source data without rebuilding the model. When `IncludeAncestorContext` is enabled, ancestors below `MinLevel` remain visible as breadcrumb context; disable it for exact level-only slices. Generated nodes receive `Metadata["layer"]`, `Metadata["hierarchy.level"]`, `Metadata["hierarchy.context"]`, and optional `Metadata["hierarchy.parentId"]`, then the chart defaults to layered layout. The layered layout wraps crowded levels into deterministic rows or columns, centers child buckets beneath their visible parents where space allows, and routes multi-child hierarchy fan-outs through shared bus connectors instead of drawing a separate cramped stem for every child.

Use `.AddTeam(...)` for people/team diagrams. It is a small convenience wrapper over `.AddHierarchy(...)`: it creates an optional team root, maps members to `TopologyNodeKind.Person`, preserves `ParentId` reporting lines, and keeps roles in node subtitles. Team diagrams default to self-contained cards instead of external tile labels, so ownership connectors attach outside the card and do not run through names or role text. This keeps team/org charts, ownership views, and "who owns this service" diagrams on the same topology model as network and directory views.

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
- `Layered` uses node kind or `Metadata["layer"]` for top-to-bottom, bottom-to-top, left-to-right, or right-to-left layers. Crowded levels wrap inside their layer band; hierarchy levels with visible parents are bucketed near those parents before falling back to the generic grid. Multi-child hierarchy fan-outs receive shared connector bus waypoints and expose `hierarchy.route = shared-bus` on generated edges. Wrapped fan-outs also expose `hierarchy.route.tier` plus `hierarchy.route.busY` or `hierarchy.route.busX`, so dashboards can inspect row/column bus lanes that avoid cutting through child cards. Prepared nodes expose `layout.layer`, `layout.layerIndex`, `layout.row`, and `layout.column` metadata for host diagnostics.
- `Matrix` places nodes in a deterministic grid.
- `DenseGrouped` places groups in packed panels and lays out each group with a hub row plus deterministic branch rows for dense site-link, replication, and subnet maps. Use `.WithGroupLayout("site-id", TopologyGroupLayoutPolicy.Grid)`, `PairRows`, `MiniMesh`, or `CollapsedDots` when a panel should render a subnet grid, replication-pair rows, dense partner mesh, or many compact site dots. Collapsed dot panels size from a dashboard-wide balanced grid, so groups with hundreds or thousands of users/sites widen into readable rows instead of forming one long rail. SVG group wrappers expose both `data-group-layout-policy` and `data-group-applied-layout-policy`, so hosts can tell requested `Auto` policy apart from the resolved policy. Inter-group dense edges get outside-facing ports and deterministic lanes when the caller has not already set ports or `RouteLane`; SVG edge wrappers expose inferred values through `data-edge-layout-inference`.
- `ForceDirected` places connected nodes with a deterministic physics-style pass: node repulsion, edge attraction, weak center gravity, and group gravity. Use it for moderate relationship-heavy views, group membership overviews, service dependency clouds, routing/replication partner maps, and non-hierarchical diagrams where a rigid tree or grid would hide the actual topology. Grouped force layouts infer a local hub from node degree and kind, keep that hub near the group center, and emit `data-cfx-meta-layout-force-role="hub"` on the hub node. Group SVG wrappers expose anchor diagnostics such as `data-cfx-meta-layout-force-anchor-strategy`, `data-cfx-meta-layout-force-anchor-width`, and `data-cfx-meta-layout-force-anchor-node-count`, so host adapters can inspect whether a group was placed as a weighted row, grid cell, or explicit anchor. It defaults unconfigured relationships to straight links and emits `data-cfx-meta-layout-force="true"` diagnostics on force-routed edges. For static SVG/PNG output, keep force-directed diagrams intentionally bounded to readable object counts; large raw populations should be summarized or handled by a separate interactive renderer with zoom, filtering, clustering, and level-of-detail.
- `Geographic` projects typed node and group longitude/latitude coordinates into the topology viewport using `ChartMapViewport` and the same equirectangular coordinate model used by dotted maps. Use `.WithMapViewport(ChartMapViewport.World())`, `.WithNodeCoordinates("site-id", longitude, latitude)`, and `.WithGroupCoordinates("region-id", longitude, latitude)` for map-like region/site topology. SVG and PNG include a geographic frame, graticule lines, generated land-dot background or soft land silhouettes, regional boundary/land-area shapes, optional status-colored region hulls, and curved map-arc links for `TopologyEdgeRouting.Curved`. Set `IncludeGeographicCallouts = true` to add opt-in region callout cards for coordinated groups; the renderer places them near the projected group anchor, avoids node visuals, route labels, and sampled WAN route corridors, draws leader lines, summarizes grouped node health counts, and emits `data-cfx-visual-role="topology-geographic-callout"` plus `data-callout-*` hooks. SVG also exposes `data-cfx-projection`, viewport bounds, `data-cfx-map-background-style`, `data-node-longitude`, `data-node-latitude`, `data-group-longitude`, `data-group-latitude`, `*-geo-visible` flags for clamped out-of-viewport coordinates, and `data-route-curve` / `data-route-control-*` diagnostics for geographic route arcs.

Set `TopologyChart.LayoutDirection` or use `.WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)` when a hierarchy should read horizontally, such as namespace -> service -> database, source -> processor -> sink, or selected object -> connected dependencies. Use `BottomToTop` or `RightToLeft` when a host needs mirrored hierarchy, ownership, dependency, or dashboard comparison panels from the same data-first builder.

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

Use `ForceDirected` when the diagram should settle a readable relationship cloud without forcing a tree. The implementation is deterministic, so repeated exports of the same input produce stable report output. It is not intended as a full replacement for interactive graph engines when thousands of individual objects must be explored.

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

var vendorCatalog = TopologyIconCatalog.Default()
    .AddPack(new TopologyIconPack("veeam", "Veeam", vendor: "Veeam")
        .WithMetadata("website", "https://www.veeam.com")
        .WithTags("backup", "vendor")
        .AddIcon("backup-server", "Backup Server", TopologyNodeKind.Server, TopologyIconShape.Server, "VBR", "#00B336", "Backup")
        .AddIcon(new TopologyIconDefinition("veeam", "repository", "Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage) {
            Symbol = "REPO",
            Color = "#007A5A",
            Category = "Backup"
        }
        .WithArtwork(TopologyIconArtwork.InlineSvg("<rect x=\"8\" y=\"8\" width=\"28\" height=\"8\" rx=\"2\" fill=\"#007A5A\"/><rect x=\"8\" y=\"20\" width=\"28\" height=\"8\" rx=\"2\" fill=\"#00B336\"/><rect x=\"8\" y=\"32\" width=\"28\" height=\"8\" rx=\"2\" fill=\"#007A5A\"/>", "0 0 44 48"))
        .WithMetadata("product", "Backup and Replication")
        .WithTags("repo", "repository")));

var manifestJson = new TopologyIconPack("fortinet", "Fortinet", vendor: "Fortinet")
    .WithTags("network", "security", "vendor")
    .AddIcon("firewall", "FortiGate Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall, "FG", "#DA291C", "Security")
    .AddIcon("switch", "FortiSwitch", TopologyNodeKind.Network, TopologyIconShape.NetworkSwitch, "FSW", "#DA291C", "Network")
    .ToJsonManifest();

var manifestCatalog = TopologyIconCatalog.Default().AddJsonPack(manifestJson);
var importedPack = TopologyIconPackJson.FromJson(manifestJson);
var fileCatalog = TopologyIconCatalog.Default().AddJsonPackFile("docs/topology-icon-pack.fortinet.json");
var folderCatalog = TopologyIconCatalog.Default().AddJsonPacksFromDirectory("docs", "topology-icon-pack.*.json");

importedPack.SaveJsonManifest("artifacts/veeam-icon-pack.json");
var loadedPack = TopologyIconPackJson.LoadJsonManifest("artifacts/veeam-icon-pack.json");
var loadedPacks = TopologyIconPackJson.LoadJsonManifestsFromDirectory("docs", "topology-icon-pack.*.json");
var loadReport = TopologyIconPackJson.LoadJsonManifestResultsFromDirectory("docs", "topology-icon-pack.*.json");
var validPacks = loadReport.Where(result => result.Succeeded).Select(result => result.Pack);
var invalidPacks = loadReport.Where(result => !result.Succeeded).Select(result => new { result.FileName, result.ErrorMessage });
var catalogLoad = TopologyIconPackJson.LoadJsonCatalogFromDirectory("docs", "topology-icon-pack.*.json");
var loadedCatalog = catalogLoad.Catalog;
var catalogErrors = catalogLoad.FailedResults;
var layeredCatalog = TopologyIconPackJson.LoadJsonCatalogFromDirectory("docs", new TopologyIconCatalogLoadOptions {
    SearchPattern = "topology-icon-pack.*.json",
    IncludeBuiltInPacks = true,
    ConflictBehavior = TopologyIconPackConflictBehavior.Skip
});
var exportedPackFiles = vendorCatalog.SaveJsonManifestsToDirectory("artifacts/icon-packs");
var exportedAllPackFiles = vendorCatalog.SaveJsonManifestsToDirectory("artifacts/all-icon-packs", new TopologyIconCatalogExportOptions {
    IncludeBuiltInPacks = true
});
var packValidation = importedPack.Validate();
var catalogValidation = vendorCatalog.Validate();

var vendors = vendorCatalog.GetVendors();
var categories = vendorCatalog.GetCategories(new TopologyIconCatalogQuery { SearchText = "backup" });
var tags = vendorCatalog.GetTags();
var packFolders = vendorCatalog.GetPackSummaries();
var vendorFolders = vendorCatalog.GetVendorSummaries();

var network = TopologyChart.Create()
    .WithTitle("Network Diagram")
    .AddIconNode("fw", "Firewall", "network:firewall", 80, 120, TopologyHealthStatus.Healthy, catalog: vendorCatalog)
    .AddIconNode("sw", "Core Switch", "network:switch", 260, 120, TopologyHealthStatus.Warning, catalog: vendorCatalog)
    .AddIconNode("repo", "Backup Repo", "veeam:repository", 440, 120, TopologyHealthStatus.Healthy, catalog: vendorCatalog)
    .AddEdge("fw-sw", "fw", "sw", "10 Gbps", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy)
    .AddEdge("sw-repo", "sw", "repo", "backup", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning);

var networkSvg = network.ToSvg(new TopologyRenderOptions { IconCatalog = vendorCatalog });

var palette = vendorCatalog.ToPaletteChart(new TopologyIconPaletteOptions {
    Title = "Network Diagram Icons",
    Subtitle = "Choose reusable built-in and vendor icon models."
});

var vendorSecurity = new TopologyIconPaletteOptions {
    Title = "Vendor Security Icons",
    IncludeBuiltInPacks = false
};
vendorSecurity.Vendors.Add("Veeam");
vendorSecurity.Categories.Add("Backup");
var filteredPalette = vendorCatalog.ToPaletteChart(vendorSecurity);

var paletteHtml = palette.ToHtmlPage(new TopologyRenderOptions {
    IconCatalog = vendorCatalog,
    EnableHtmlInteractions = true
});

var stencilBrowserHtml = vendorCatalog.ToStencilBrowserHtmlPage(new TopologyIconStencilBrowserOptions {
    Title = "Topology Stencil Browser",
    Subtitle = "Search and choose built-in and vendor icons from one reusable catalog.",
    PacksPerRow = 3,
    ColumnsPerPack = 4
});

var svgImport = TopologyIconSvgPackImporter.ImportSvgPackFromDirectory("external/Microsoft-Azure-Stencils-Pack", new TopologyIconSvgPackImportOptions {
    PackId = "microsoft-azure-stencils",
    PackLabel = "Microsoft Azure Stencils",
    Vendor = "Microsoft",
    SourceUrl = "https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio",
    SourceRevision = "d332d1457fc8d43d972815eab59d0a2da3087c45",
    SourceLicense = "MIT",
    SourceLicenseUrl = "https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio/blob/master/LICENSE",
    SourceLicensePath = "LICENSE"
});
svgImport.Pack.SaveJsonManifest("artifacts/icon-packs/topology-icon-pack.microsoft-azure-stencils.json");
```

Portable icon-pack manifests use the schema id `chartforgex.topology.iconPack`. The core library reads and writes this manifest without external JSON dependencies, so packs can be shipped by host applications, vendor adapters, or report templates while keeping ChartForgeX dependency-free. Host apps can load from strings, `TextReader`, individual files, or deterministic manifest directories, and can save packs back to strings, `TextWriter`, files, or whole manifest folders. `SaveJsonManifestsToDirectory(...)` exports custom/vendor packs by default, while `TopologyIconCatalogExportOptions` can include built-ins, control overwrite behavior, use a query filter, and customize exported file names. `Validate()` on a pack or catalog reports blocking authoring errors and picker-quality warnings, which gives vendor pack tooling a simple preflight before accepting custom packs, including unsafe SVG or image href checks for artwork-backed icons. The normal directory APIs are fail-fast, while `LoadJsonManifestResultsFromDirectory(...)` returns one `TopologyIconPackLoadResult` per file so a picker can load valid packs and still show invalid vendor manifests with clear file-specific errors. `LoadJsonCatalogFromDirectory(...)` goes one step further and returns a `TopologyIconCatalogLoadResult` containing the ready-to-use catalog, successfully loaded packs, skipped packs, failed files, and add-time errors such as duplicate pack ids. `TopologyIconCatalogLoadOptions` lets hosts choose whether built-in packs are included, whether child folders are scanned, and whether duplicate pack ids should report errors, be skipped, or replace the existing pack. Packs loaded from files receive `manifest.path`, `manifest.fileName`, and `manifest.directory` metadata, which lets a host inspector show where a vendor pack came from without maintaining a side table. Runtime `manifest.*` metadata is not written back into portable manifest output. Large generated packs should prefer the sidecar layout documented in `docs/topology-icon-packs.md`, where manifests reference pack-local `svg/*.svg` artwork and optional `previews/*.png` thumbnails instead of embedding large SVG strings in JSON. First-party curated sidecar packs now live under `assets/topology-icons/`, including AD/network, backup, network-security, cloud-productivity, network-infrastructure, geo/incidents, charts/analytics, topology/network, security/risk/certificates, data/ownership/intelligence, people/org/workflow, identity/directory, and Microsoft 365-style collaboration packs, with provenance and license notes beside each manifest. Imported vendor packs live beside them, including the MIT-licensed `assets/topology-icons/tabler-icons/tabler-icons-outline` pack imported from `@tabler/icons` 3.44.0. Small copyable vendor samples live in `docs/topology-icon-pack.veeam.json`, `docs/topology-icon-pack.fortinet.json`, and `docs/topology-icon-pack.azure-example.json`:

Artwork-backed icons add an optional `artwork` object to the icon manifest. Use `svg` for a trusted SVG fragment inside the supplied `svgViewBox`, or `imageHref` for a host-managed asset URL or data URI. The icon still needs `shape`, `symbol`, and `color` because those are the fallback language for PNG rendering, text-only exports, and hosts that choose not to embed artwork.

For large stencil sources, `TopologyIconSvgPackImporter.ImportSvgPackFromDirectory(...)` turns a folder tree of `.svg` files into the same portable manifest model without adding a renderer dependency. The importer uses the source folder path as the category, omits common `SVG` leaf folders, creates deterministic ids from file names, suffixes duplicate ids, strips non-artwork SVG metadata such as `title`, `desc`, and `metadata`, rejects DTD content, and can skip unsafe SVG fragments before they enter the catalog. It also records source provenance on the pack (`source.url`, `source.revision`, `source.license`, `source.licenseUrl`, `source.licensePath`) and per icon (`source.path`, `source.fileName`, `source.viewBox`). That gives a future asset-import PR a clear audit trail: one reviewed importer command, one pinned source revision, one license file, and generated manifests that explain where every icon came from.

SVG-to-PNG conversion should remain an import/build step rather than a ChartForgeX runtime dependency. A vendor asset PR can use an external converter such as `resvg`, Inkscape, `Magick.NET`, or `Svg.Skia` to generate preview thumbnails beside the manifest, but the core manifest and SVG/HTML renderers only need trusted inline SVG artwork. PNG chart output keeps using the renderer-owned `shape`, `symbol`, and `color` fallback unless a host adds its own raster artwork pipeline.

```json
{
  "schema": "chartforgex.topology.iconPack",
  "version": 1,
  "id": "veeam",
  "label": "Veeam",
  "vendor": "Veeam",
  "packVersion": "13",
  "builtIn": false,
  "tags": [ "backup", "vendor" ],
  "metadata": {
    "website": "https://www.veeam.com"
  },
  "icons": [
    {
      "id": "repository",
      "label": "Backup Repository",
      "nodeKind": "Storage",
      "shape": "Storage",
      "symbol": "REPO",
      "color": "#007A5A",
      "category": "Backup",
      "displayMode": "Tile",
      "artwork": {
        "svgViewBox": "0 0 44 48",
        "svg": "<rect x=\"8\" y=\"8\" width=\"28\" height=\"8\" rx=\"2\" fill=\"#007A5A\"/><rect x=\"8\" y=\"20\" width=\"28\" height=\"8\" rx=\"2\" fill=\"#00B336\"/><rect x=\"8\" y=\"32\" width=\"28\" height=\"8\" rx=\"2\" fill=\"#007A5A\"/>"
      },
      "tags": [ "repo", "repository" ],
      "metadata": {
        "product": "Backup and Replication"
      }
    }
  ]
}
```

For picker and stencil-browser shells, `GetPackSummaries(...)` and `GetVendorSummaries(...)` produce stable folder data from the same `TopologyIconCatalogQuery` filters used by search and palette charts. Pack summaries expose matching icon counts, categories, tags, built-in/custom source, version, vendor, and manifest source path. Vendor summaries group packs under labels such as `ChartForgeX`, `Microsoft`, `Veeam`, or `Fortinet`, with roll-up pack ids, categories, tags, and icon counts. `ToStencilBrowserHtmlPage(...)` composes those summaries with a palette chart into a ready-to-host browser page with search, vendor filters, pack filters, category filters, click selection, and a details panel. Selecting an icon dispatches `cfx-icon-browser-select` with stable fields such as icon id, pack id, label, shape, artwork type, category, vendor, pack label, and tags. The host still owns the production UI, but it does not need to parse icons manually to build folders, tabs, counters, source badges, or a first-pass stencil picker.

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
- `TopologyNodeDisplayMode.Card`, `CompactCard`, `Tile`, `Pill`, `Icon`, `Dot`, and `Hidden` let the same model render as full report cards, site tiles, dense dashboard summaries, or invisible route anchors for clean edge termination at group borders.
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
- `icon-palette.svg`
- `visual-geographic-topology-map.svg`
- `service-dependency-api-neighbors-view.svg`
- `service-dependency-critical-dependencies-view.svg`
- matching `.png` and `.html` files
- focused view examples for EMEA, selected-node neighbors, DC-level replication, offender highlighting, compact service dependencies, critical dependencies, and critical replication paths
- `index.html`
