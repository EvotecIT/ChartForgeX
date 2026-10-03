using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphRichRenderingExample {
    public static void Write(string output) {
        var graph = GraphScene.Create("rich-rendering", "Rich graph rendering");
        graph.Subtitle = "The same shapes, images, labels, status badges and styled routes in SVG, Canvas and WebGL. Select a mark or route, zoom, switch theme, and export the current view.";
        graph.Options.Enable(GraphSceneFeatures.Export | GraphSceneFeatures.IncrementalUpdates);
        graph.Options.Cluster.CollapseOnLoad = false;
        graph.Options.LevelOfDetail.CollapseClustersOnLoad = false;
        var shapes = Enum.GetValues<GraphNodeShape>();
        for (var index = 0; index < shapes.Length; index++) {
            var shape = shapes[index];
            graph.AddNode("n-" + index, shape.ToString(), node => {
                node.X = 150 + index % 4 * 215;
                node.Y = 125 + index / 4 * 170;
                node.Fixed = true;
                node.Size = 23;
                node.Shape = shape;
                node.Status = index % 3 == 0 ? "warning" : "healthy";
                node.SecondaryLabel = "Shared renderer details";
                node.BadgeText = "2";
                node.IconText = shape == GraphNodeShape.Text ? null : "X";
                node.Style.BackgroundColor = index % 2 == 0 ? "#2563eb" : "#7c3aed";
                node.Style.BorderColor = "#c4b5fd";
                node.Style.LabelBackgroundColor = "#e0e7ff";
                node.Style.Shadow = index == 1;
                if (shape == GraphNodeShape.Box) {
                    node.Size = 54;
                    node.Metadata["topology.card"] = "true";
                }
                if (shape == GraphNodeShape.Image || shape == GraphNodeShape.RectangularImage) {
                    const string image = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='80'><rect width='80' height='80' fill='#0f766e'/><path d='M18 40L34 56L62 24' fill='none' stroke='white' stroke-width='8'/></svg>";
                    node.ImageUrl = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(image));
                    node.ImageAlt = "Check mark";
                }
            });
        }
        graph.AddEdge("thin", "n-0", "n-1", "0.8 width", edge => edge.Style.Width = .8);
        graph.AddEdge("wide", "n-1", "n-2", "5 width", edge => { edge.Style.Width = 5; edge.Directed = true; edge.Style.Color = "#0f766e"; });
        graph.AddEdge("curve", "n-2", "n-3", "Curved, dashed", edge => {
            edge.Shape = GraphEdgeShape.Curve; edge.Curvature = -75; edge.Dashed = true;
            edge.Style.DashPattern = "9 5"; edge.Style.Width = 3; edge.SourceArrow = true; edge.TargetArrow = true;
        });
        graph.AddEdge("route", "n-4", "n-7", "Prepared route", edge => {
            edge.Shape = GraphEdgeShape.Polyline; edge.Style.Width = 4; edge.Directed = true;
            edge.RoutePoints.Add(new GraphScenePoint(180, 295));
            edge.RoutePoints.Add(new GraphScenePoint(180, 360));
            edge.RoutePoints.Add(new GraphScenePoint(765, 360));
            edge.RoutePoints.Add(new GraphScenePoint(765, 295));
        });
        graph.AddEdge("loop", "n-8", "n-8", "Self loop", edge => { edge.Shape = GraphEdgeShape.SelfReference; edge.Directed = true; edge.Style.Width = 2; });
        graph.AddCluster("data", "Data group", new[] { "n-8", "n-9" });
        foreach (var backend in new[] { HtmlGraphRenderBackend.Svg, HtmlGraphRenderBackend.Canvas, HtmlGraphRenderBackend.WebGl }) {
            var html = graph.ToGraphExplorerHtmlPage(options => {
                options.PageTitle = "Rich graph rendering — " + backend;
                options.RenderBackend = backend;
            });
            File.WriteAllText(Path.Combine(output, "graph-rich-rendering-" + backend.ToString().ToLowerInvariant() + ".html"), html.Replace("</body>", Controls + "</body>"));
        }
    }

    private const string Controls = """
        <section aria-label="Renderer exercise" style="margin:16px auto;padding:16px;max-width:960px">
          <button id="rich-select">Select the wide route</button>
          <button id="rich-patch">Update the route and node</button>
          <button id="rich-join">Show a sharp routed corner</button>
          <button id="rich-lose">Lose WebGL context</button>
          <button id="rich-restore">Restore WebGL context</button>
          <button id="rich-preview">Preview PNG export</button>
          <button id="rich-svg">Inspect SVG export</button>
          <pre id="rich-status" role="status" style="white-space:pre-wrap"></pre>
          <img id="rich-export" alt="Exported graph preview" hidden style="max-width:100%;height:auto">
        </section>
        <script>
        (() => {
          const root = document.querySelector('[data-cfx-graph-id="rich-rendering"]');
          const api = window.ChartForgeXGraphExplorer;
          const status = document.getElementById('rich-status');
          let contextExtension, previewFormat, exported;
          const report = action => {
            const document = api.get(root), state = api.captureState(root);
            const edge = document.edges.find(item => item.id === 'wide'), node = document.nodes.find(item => item.id === 'n-1');
            status.textContent = JSON.stringify({ action, renderer: root.dataset.cfxGraphRendererActive,
              fallback: root.dataset.cfxGraphRendererFallback, selection: state.selection, viewport: state.viewport,
              route: { label: edge.label, width: edge.style.width }, node: { label: node.label, badge: node.badge, shape: node.shape, size: node.size }, exported }, null, 2);
          };
          document.getElementById('rich-select').onclick = () => {
            const state = api.captureState(root); state.selection = [{ role: 'graph-edge', id: 'wide' }];
            api.applyState(root, state); report('selected');
          };
          document.getElementById('rich-patch').onclick = () => {
            const graph = api.get(root), edge = graph.edges.find(item => item.id === 'wide'), node = graph.nodes.find(item => item.id === 'n-1');
            api.update(root, { upsertEdges: [{ ...edge, label: 'Updated width 9', style: { ...edge.style, width: 9, color: '#dc2626' } }],
              upsertNodes: [{ ...node, label: 'Updated box', secondaryLabel: 'Host update retained', badge: '3' }] });
            report('patched');
          };
          document.getElementById('rich-lose').onclick = () => {
            contextExtension = root.querySelector('[data-cfx-role="graph-webgl"]').getContext('webgl2')?.getExtension('WEBGL_lose_context');
            contextExtension?.loseContext(); setTimeout(() => report('context lost'), 100);
          };
          document.getElementById('rich-join').onclick = () => {
            const edge = api.get(root).edges.find(item => item.id === 'route');
            api.update(root, { upsertEdges: [{ ...edge, label: 'Sharp routed join',
              routePoints: [{ x: 180, y: 295 }, { x: 560, y: 400 }, { x: 320, y: 500 }, { x: 765, y: 295 }],
              style: { ...edge.style, width: 12 } }] });
            report('sharp join');
          };
          document.getElementById('rich-restore').onclick = () => { contextExtension?.restoreContext(); setTimeout(() => report('context restored'), 100); };
          document.getElementById('rich-preview').onclick = async () => {
            previewFormat = 'png'; try { await api.export(root, 'png'); } finally { previewFormat = undefined; }
          };
          document.getElementById('rich-svg').onclick = async () => {
            previewFormat = 'svg'; try { await api.export(root, 'svg'); } finally { previewFormat = undefined; }
          };
          root.addEventListener('cfxgraphexport', event => {
            if (event.detail.format !== previewFormat) return;
            event.preventDefault(); exported = { format: event.detail.format, length: event.detail.content.length };
            if (previewFormat === 'png') {
              const image = document.getElementById('rich-export'); image.hidden = false; image.src = event.detail.content;
            } else {
              const svg = new DOMParser().parseFromString(event.detail.content, 'image/svg+xml');
              exported.valid = !svg.querySelector('parsererror');
              exported.nodes = svg.querySelectorAll('[data-cfx-role="graph-node"]').length;
              exported.edges = svg.querySelectorAll('[data-cfx-role="graph-edge"]').length;
            }
            report('export ' + event.detail.format);
          });
          window.addEventListener('load', () => report('ready'), { once: true });
        })();
        </script>
        """;
}
