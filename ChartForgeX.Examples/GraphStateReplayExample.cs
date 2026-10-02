using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphStateReplayExample {
    public static void Write(string output) {
        var graph = GraphScene.Create("state-replay", "Graph state replay")
            .AddNode("shared", "API", node => { node.X = 240; node.Y = 220; })
            .AddNode("db", "Database", node => { node.X = 680; node.Y = 220; })
            .AddEdge("shared", "shared", "db", "queries");
        graph.Options.Enable(GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.History | GraphSceneFeatures.Manipulation);
        graph.Options.Manipulation.CanEditNodes = true;
        graph.Subtitle = "Select the API, capture its state, and restore it. The node and edge share an id; the edge inherits its default width.";
        foreach (var backend in new[] { HtmlGraphRenderBackend.Svg, HtmlGraphRenderBackend.Canvas, HtmlGraphRenderBackend.WebGl }) {
            var html = graph.ToGraphExplorerHtmlPage(options => options.RenderBackend = backend);
            File.WriteAllText(Path.Combine(output, "graph-state-replay-" + backend.ToString().ToLowerInvariant() + ".html"), html.Replace("</body>", Controls + "</body>"));
        }
    }

    private const string Controls = """
        <section aria-label="State replay controls" style="margin:16px auto;padding:16px;max-width:960px;background:white;color:#172554;border:1px solid #cbd5e1;border-radius:12px">
          <h2>Replay a selection</h2>
          <p>Capture and restore keeps the selected API node and the visible connection. A cyclic parent patch is rejected before either node changes.</p>
          <div style="display:flex;flex-wrap:wrap;gap:8px">
            <button id="state-select">Select API</button>
            <button id="state-capture">Capture state</button>
            <button id="state-restore">Restore state</button>
            <button id="state-cycle">Try cyclic parents</button>
            <button id="state-host-update">Verify host update history</button>
          </div>
          <pre id="state-status" role="status" style="white-space:pre-wrap;overflow-wrap:anywhere"></pre>
        </section>
        <script>
        (() => {
          const api = window.ChartForgeXGraphExplorer;
          const root = document.querySelector('[data-cfx-graph-id="state-replay"]');
          const status = document.getElementById('state-status');
          let snapshot;
          const inspect = (action, extra) => {
            const document = api.get(root);
            status.textContent = JSON.stringify({ action, backend: root.dataset.cfxGraphRendererActive || root.getAttribute('data-cfx-graph-renderer'),
              selection: api.captureState(root).selection, edgeWidth: document.edges[0].style.width,
              svgStrokeWidth: getComputedStyle(root.querySelector('[data-cfx-role="graph-edge"]')).strokeWidth,
              parents: document.nodes.map(node => ({ id: node.id, parentId: node.parentId })), ...extra }, null, 2);
          };
          document.getElementById('state-select').onclick = () => {
            const state = api.captureState(root);
            state.selection = [{ role: 'graph-node', id: 'shared' }];
            api.applyState(root, state); inspect('selected');
          };
          document.getElementById('state-capture').onclick = () => { snapshot = api.captureState(root); inspect('captured'); };
          document.getElementById('state-restore').onclick = () => { if (snapshot) api.applyState(root, snapshot); inspect(snapshot ? 'restored' : 'Capture a state first'); };
          document.getElementById('state-cycle').onclick = () => {
            try { api.update(root, { upsertNodes: [{ id: 'shared', parentId: 'db' }, { id: 'db', parentId: 'shared' }] }); }
            catch (error) { inspect('Rejected: ' + error.message); }
          };
          document.getElementById('state-host-update').onclick = () => {
            api.change(root, { upsertNodes: [{ ...api.get(root).nodes.find(node => node.id === 'db'), label: 'Database edited' }] }, 'example', 'Rename database');
            const before = root.dataset.cfxGraphUndoCount;
            let undoDuringPatch;
            root.addEventListener('cfxgraphpatch', () => { undoDuringPatch = api.undo(root); }, { once: true });
            api.update(root, { upsertNodes: [{ id: 'host-data', label: 'New host data', x: 680, y: 360 }] });
            const undone = api.undo(root);
            inspect('host updated', { beforeUndoCount: before, afterUndoCount: root.dataset.cfxGraphUndoCount, undoDuringPatch, undone, hostDataRetained: api.get(root).nodes.some(node => node.id === 'host-data') });
          };
          inspect('ready');
        })();
        </script>
        """;
}
