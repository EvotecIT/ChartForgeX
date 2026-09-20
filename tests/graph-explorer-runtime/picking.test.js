const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const names = ['00-core', '01-document', '02-geometry', '05-viewport', '09-edge-bundles', '10-layout', '11-state-sync', '15-edge-hit-index', '15-hit-testing'];
const code = names.map(name => fs.readFileSync(path.join(assets, `graph-explorer.${name}.js`), 'utf8')).join('\n');
const runtime = new Function('document', 'window', 'CustomEvent', code + '\nreturn { graphVirtualElement, graphState, applyLayout, applyFilters, hitNodeAt, hitEdgeAt, edgeHitCandidates, quadraticPoint, cubicPoint, edgeControl, edgeRenderEndpoints, selfLoopGeometry };')({}, {}, class {});
function scene(count, edgeSpecs = []) {
  const root = runtime.graphVirtualElement('root', {}, []);
  root.dataset = { cfxGraphRendererActive: 'canvas' };
  root.ownerDocument = { activeElement: null };
  root.querySelector = () => null;
  root.querySelectorAll = () => [];
  root.dispatchEvent = () => true;
  root.contains = () => false;
  const nodes = Array.from({ length: count }, (_, i) => runtime.graphVirtualElement('graph-node', {
    'data-node-id': `n${i}`, 'data-node-label': `Node ${i}`, 'data-node-size': '8',
    'data-node-x': String(i % 100 * 100), 'data-node-y': String(Math.floor(i / 100) * 100)
  }, []));
  const edges = edgeSpecs.map(([a, b, extras = {}], i) => runtime.graphVirtualElement('graph-edge', {
    'data-edge-id': `e${i}`, 'data-source-node-id': `n${a}`, 'data-target-node-id': `n${b}`, ...extras
  }, []));
  root.__cfxGraphVirtualItems = [...nodes, ...edges];
  const state = runtime.graphState(root);
  runtime.applyLayout(root, state);
  return { root, state };
}
test('empty-space queries do not fall back to all nodes or the DOM at 10,000 nodes', () => {
  const { root, state } = scene(10000);
  let coordinateReads = 0;
  for (const node of state.nodes) {
    const x = node.x;
    Object.defineProperty(node, 'x', { get() { coordinateReads++; return x; } });
  }
  root.querySelectorAll = () => { throw new Error('Unexpected DOM scan'); };
  for (let i = 0; i < 100; i++) assert.equal(runtime.hitNodeAt(root, { x: -1000 - i, y: -1000 }), null);
  assert.equal(coordinateReads, 0);
  assert.equal(runtime.hitNodeAt(root, { x: 9900, y: 9900 }).id, 'n9999');
  assert.ok(coordinateReads < 10);
});
test('physics subsets retain picking for nodes revealed by a later filter', () => {
  const { root, state } = scene(200);
  const node = state.nodes[199];
  node.el.classList.add('cfx-graph-hidden');
  runtime.applyLayout(root, { ...state, nodes: state.nodes.slice(0, 199), fullState: state });
  assert.equal(runtime.hitNodeAt(root, node), null);
  runtime.applyFilters(root);
  assert.equal(runtime.hitNodeAt(root, node).id, node.id);
});
test('layout updates discard old node cells after dragging', () => {
  const { root, state } = scene(200);
  const node = state.nodes[0];
  node.x = -500; node.y = -500;
  runtime.applyLayout(root, state);
  assert.equal(runtime.hitNodeAt(root, { x: 0, y: 0 }), null);
  assert.equal(runtime.hitNodeAt(root, node).id, node.id);
});
test('edge bounds prune distant routes and reuse geometry across pointer events', () => {
  const { root, state } = scene(10000, Array.from({ length: 5000 }, (_, i) => [i * 2, i * 2 + 1]));
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 0 }).id, 'e0');
  let coordinateReads = 0;
  for (const node of state.nodes) {
    const x = node.x;
    Object.defineProperty(node, 'x', { get() { coordinateReads++; return x; } });
  }
  for (let i = 0; i < 100; i++) assert.equal(runtime.hitEdgeAt(root, { x: -1000 - i, y: -1000 }), null);
  assert.equal(coordinateReads, 0);
  assert.equal(root.dataset.cfxGraphEdgeHitCandidates, '0');
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 0 }).id, 'e0');
  assert.equal(root.dataset.cfxGraphEdgeHitCandidates, '1');
});
test('curves, self loops and routed bends stay selectable outside endpoint bounds', () => {
  const { root, state } = scene(5, [[0, 1, { 'data-edge-shape': 'curve', 'data-edge-curvature': '200' }], [2, 2], [3, 4, { 'data-edge-route-points': '300,0;350,150;400,0' }]]);
  const edge = state.edges[0], control = runtime.edgeControl(edge), ends = runtime.edgeRenderEndpoints(edge, control);
  assert.equal(runtime.hitEdgeAt(root, runtime.quadraticPoint(ends.source, control, ends.target, .5)).id, 'e0');
  assert.equal(runtime.hitEdgeAt(root, runtime.cubicPoint(runtime.selfLoopGeometry(state.nodes[2]), .5)).id, 'e1');
  assert.equal(runtime.hitEdgeAt(root, { x: 350, y: 150 }).id, 'e2');
});
test('edge bounds refit after movement and visibility changes rebuild their membership', () => {
  const { root, state } = scene(2, [[0, 1]]);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 0 }).id, 'e0');
  state.nodes.forEach(node => { node.y = 500; });
  runtime.applyLayout(root, state);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 0 }), null);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 500 }).id, 'e0');
  assert.equal(root.dataset.cfxGraphEdgeHitIndex, 'refit');
  state.edges[0].el.setAttribute('data-edge-hidden', 'true');
  runtime.applyFilters(root);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 500 }), null);
  state.edges[0].el.setAttribute('data-edge-hidden', 'false');
  runtime.applyFilters(root);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 500 }).id, 'e0');
});
test('collapsed group endpoints are picked at their rendered proxy positions', () => {
  const { root, state } = scene(3, [[0, 2]]);
  const clusterElement = runtime.graphVirtualElement('graph-cluster', { 'data-cluster-collapsed': 'true' }, []);
  const cluster = { el: clusterElement, nodeIds: ['n0', 'n1'], id: 'site' };
  state.nodes[0].el.classList.add('cfx-graph-cluster-collapsed-member');
  state.nodes[1].el.classList.add('cfx-graph-cluster-collapsed-member');
  state.edges[0].sourceCluster = cluster;
  runtime.applyLayout(root, state);
  assert.equal(runtime.hitEdgeAt(root, { x: 140, y: 0 }).id, 'e0');
  assert.equal(runtime.hitEdgeAt(root, { x: 10, y: 0 }), null);
  clusterElement.classList.add('cfx-graph-cluster-expanded');
  state.nodes[0].el.classList.remove('cfx-graph-cluster-collapsed-member');
  runtime.applyLayout(root, state);
  assert.equal(runtime.hitEdgeAt(root, { x: 10, y: 0 }).id, 'e0');
});
test('replacing graph state cannot reuse geometry from removed edges', () => {
  const { root, state } = scene(3, [[0, 1]]);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 0 }).id, 'e0');
  root.__cfxGraphVirtualItems = root.__cfxGraphVirtualItems.filter(el => el !== state.edges[0].el);
  const next = runtime.graphState(root);
  runtime.applyLayout(root, next);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 0 }), null);
});
test('equal-distance crossings retain source order and wide edges retain tolerance', () => {
  const { root } = scene(2, [[0, 1], [0, 1, { 'data-edge-weight': '20' }]]);
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 0 }).id, 'e0');
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 25 }).id, 'e1');
  assert.equal(runtime.hitEdgeAt(root, { x: 50, y: 27 }), null);
});
test('authored route endpoints follow live node positions after dragging', () => {
  const { root, state } = scene(2, [[0, 1, { 'data-edge-route-points': '0,0;50,100;100,0' }]]);
  assert.equal(runtime.hitEdgeAt(root, { x: 0, y: 0 }).id, 'e0');
  state.nodes[0].x = -100;
  runtime.applyLayout(root, state);
  assert.equal(runtime.hitEdgeAt(root, { x: -100, y: 0 }).id, 'e0');
  assert.equal(runtime.hitEdgeAt(root, { x: 0, y: 0 }), null);
});
