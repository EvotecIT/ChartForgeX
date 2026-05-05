# ChartForgeX Topology

`ChartForgeX.Topology` is a reusable diagram family for deterministic, SVG-first topology views. It is intentionally product-neutral: ChartForgeX owns the model, validation, layout helpers, SVG rendering, and export-ready output; dashboard shells and data collection belong to host projects.

Use it for static or embeddable diagrams such as site topologies, replication meshes, subnet and site-link maps, geographic-style region views, domain-controller connection maps, and service dependency maps.

## Model

- `TopologyChart` contains viewport, layout mode, groups, nodes, edges, legend, and theme.
- `TopologyGroup` represents a cluster or region.
- `TopologyNode` represents a logical object, asset, service, endpoint, subnet, site, or server.
- `TopologyEdge` represents a connection, dependency, replication path, trust, subnet mapping, or certificate chain.

Groups, nodes, and edges can carry `Href`, `Tooltip`, `Metrics`, and `Metadata`. The SVG renderer escapes text safely, emits native SVG `<title>` tooltips, emits stable `data-*` attributes, and wraps linked elements in SVG anchors. Unsafe `javascript:`, `data:`, and `vbscript:` hrefs are skipped.

Use `TopologyView` to render a focused perspective from the same source model. A view can select groups, nodes, or edges, override title/subtitle text, and keep connected edges between visible nodes. This is intended for dashboard cards such as "selected region", "critical paths", or "affected links" without duplicating topology data.

## Layout

V1 layouts are deterministic and report-friendly:

- `Manual` uses explicit coordinates.
- `RegionGrid` arranges groups in a grid and places unpositioned nodes inside their groups.
- `HubAndSpoke` places a hub and branch nodes inside each group.
- `Layered` uses node kind or `Metadata["layer"]` for simple top-to-bottom layers.
- `Matrix` places nodes in a deterministic grid.

Force-directed and physics layouts are intentionally not implemented in v1.

## Rendering

```csharp
using ChartForgeX.Topology;

var chart = TopologyDemoCharts.SiteTopologyDemo();
var svg = chart.ToSvg();
chart.SaveSvg("site-topology.svg");
chart.SaveHtml("site-topology.html");
chart.SavePng("site-topology.png");

var emeaOnly = chart.ToSvg(new TopologyRenderOptions {
    View = new TopologyView {
        Id = "emea",
        Title = "EMEA Topology",
        GroupIds = { "EMEA" }
    }
});
```

`TopologySvgRenderer` outputs a complete standalone SVG with `viewBox`, `defs`, scoped CSS, groups below edges, edge labels above edges, nodes above labels, status badges, optional legends, and accessibility metadata. `TopologyPngRenderer` draws the same model through ChartForgeX's dependency-free raster canvas for report exports. `TopologyHtmlRenderer` only wraps the generated SVG in a neutral `.cfx-topology-wrapper` div with chart metadata.

## Host Boundaries

HtmlForgeX can later provide cards, toolbars, sidebars, filters, tabs, inspectors, and event panels around the SVG. TestimoX or another product can later collect and calculate product-specific health, then convert that data into `TopologyChart`. ChartForgeX should not connect to Active Directory, hardcode TestimoX data, or implement dashboard page layout.

The example console app writes four sample diagrams to `artifacts/topology-demo/` and to the normal generated example output folder:

- `site-topology.svg`
- `replication-mesh.svg`
- `subnets-site-links.svg`
- `geographic-topology.svg`
- matching `.png` and `.html` files
- focused view examples for EMEA and critical replication paths
- `index.html`
