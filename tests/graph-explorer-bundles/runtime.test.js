const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const names = ['00-core', '01-document', '02-geometry', '05-viewport', '09-edge-bundles', '10-layout', '11-state-sync', '30-bindings'];
const code = names.map(name => fs.readFileSync(path.join(assets, `graph-explorer.${name}.js`), 'utf8')).join('\n');
const loadRuntime = new Function('document', 'window', code + '\nreturn { graphVirtualElement, graphVirtualClassList, graphState, applyCollapsedEdgeBundles, syncBundledEdgePresentation, graphItemAccessible, graphOverviewDisclosure, exportGraphJson, attr };');
const runtime = loadRuntime({ readyState: 'loading', addEventListener() {} }, {});

function scene(siteCount, edgeSpecs, renderer = 'canvas') {
  const physicalLabels = [];
  const viewport = { appendChild(label) { physicalLabels.push(label); } };
  const stage = { appendChild(note) { root.note = note; } };
  const root = runtime.graphVirtualElement('root', { 'data-cfx-graph-id': 'bundle-test', 'data-cfx-graph-features': 'Selection,Clustering' }, []);
  root.dataset = { cfxGraphRendererActive: renderer };
  root.ownerDocument = {
    createElementNS() { return runtime.graphVirtualElement('graph-edge-label', {}, []); },
    createElement() { return { setAttribute() {}, remove() { root.note = null; } }; }
  };
  root.querySelector = selector => selector.includes('graph-overview-note') ? root.note || null
    : selector === '.cfx-graph-stage' ? stage
    : selector.includes('graph-viewport') ? viewport
    : selector.includes('graph-search') ? root.search || null
    : null;
  root.querySelectorAll = selector => selector.includes('graph-edge-label')
    ? physicalLabels.filter(label => !label.__cfxRemoved)
    : [];
  const clusters = Array.from({ length: siteCount }, (_, index) => runtime.graphVirtualElement('graph-cluster', {
    'data-cluster-id': `site-${index}`, 'data-cluster-label': `Site ${index}`,
    'data-cluster-node-ids': `node-${index}`, 'data-cluster-collapsed': 'true'
  }, []));
  const nodes = clusters.map((_, index) => runtime.graphVirtualElement('graph-node', {
    'data-node-id': `node-${index}`, 'data-node-label': `Node ${index}`,
    'data-node-cluster': `site-${index}`, 'data-node-x': String(80 + 100 * index),
    'data-node-y': String(100 + 20 * index), 'data-node-size': '20'
  }, ['cfx-graph-cluster-collapsed-member']));
  const edges = edgeSpecs.map(([source, target, label, status], index) => runtime.graphVirtualElement('graph-edge', {
    'data-edge-id': `edge-${index}`, 'data-edge-label': label, 'data-edge-kind': 'Trust',
    'data-cfx-status': status, 'data-source-node-id': `node-${source}`,
    'data-target-node-id': `node-${target}`, 'data-source-cluster-id': `site-${source}`,
    'data-target-cluster-id': `site-${target}`, 'data-edge-weight': '1'
  }, []));
  root.__cfxGraphVirtualItems = [...nodes, ...edges, ...clusters];
  return { root, edges, physicalLabels };
}

test('accelerated edges keep canonical labels while Canvas state and keyboard expose the bundle', () => {
  const { root, edges } = scene(2, [
    [0, 1, 'Original healthy', 'healthy'], [0, 1, 'Original critical', 'critical'], [0, 1, 'Original warning', 'warning']
  ]);
  const state = runtime.graphState(root);
  root.__cfxGraphState = state;
  runtime.applyCollapsedEdgeBundles(root);
  runtime.syncBundledEdgePresentation(root, state);

  assert.equal(runtime.attr(edges[1], 'data-cfx-bundle-count'), '3');
  assert.equal(runtime.attr(edges[1], 'data-edge-label'), 'Original critical');
  assert.equal(state.edges[1].label, '3 relationships');
  assert.equal(runtime.exportGraphJson(root).edges[1].label, 'Original critical');
  assert.equal(runtime.graphItemAccessible(root, edges[1]), true);
  assert.equal(edges.filter(edge => edge.classList.contains('cfx-graph-bundle-member')).length, 2);

  root.search = { value: 'Original' };
  runtime.applyCollapsedEdgeBundles(root);
  runtime.syncBundledEdgePresentation(root, state);
  assert.equal(state.edges[1].label, 'Original critical');
  assert.equal(edges.filter(edge => edge.classList.contains('cfx-graph-bundle-member')).length, 0);
  assert.equal(runtime.graphItemAccessible(root, edges[0]), true);
  edges[0].classList.add('cfx-graph-hidden');
  assert.equal(runtime.graphItemAccessible(root, edges[0]), false);
});

test('hidden hierarchy edges never replace an in-scope bundle route', () => {
  const { root, edges } = scene(2, [
    [0, 1, 'Visible first', 'healthy'], [0, 1, 'Visible second', 'warning'], [0, 1, 'Hidden critical', 'critical']
  ]);
  edges[2].classList.add('cfx-graph-hierarchy-hidden');
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(runtime.attr(edges[1], 'data-cfx-bundle-count'), '2');
  assert.equal(edges[1].classList.contains('cfx-graph-bundle-member'), false);
  assert.equal(runtime.attr(edges[2], 'data-cfx-bundle-count'), '');
});

test('dense priority route disclosure carries the hidden relationship count', () => {
  const edges = [];
  for (let source = 0; source < 7; source++) {
    for (let target = source + 1; target < 7; target++) edges.push([source, target, `Route ${source}-${target}`, 'healthy']);
  }
  edges[0][3] = 'critical';
  const { root } = scene(7, edges);
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(root.classList.contains('cfx-graph-priority-overview'), true);
  assert.equal(root.dataset.cfxGraphOverviewTotal, '21');
  assert.match(runtime.graphOverviewDisclosure(root), /^Priority overview: \d+ routes from 21 relationships$/);
});

test('new SVG bundle labels have geometry immediately after filter restoration', () => {
  const { root, physicalLabels } = scene(2, [
    [0, 1, 'Original one', 'healthy'], [0, 1, 'Original two', 'warning']
  ], 'svg');
  root.__cfxGraphState = runtime.graphState(root);
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels.length, 1);
  assert.notEqual(runtime.attr(physicalLabels[0], 'x'), '');
  assert.ok(Number.isFinite(Number(runtime.attr(physicalLabels[0], 'y'))));
  root.search = { value: 'Original' };
  runtime.applyCollapsedEdgeBundles(root);
  root.search.value = '';
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels.filter(label => !label.__cfxRemoved).length, 1);
  assert.notEqual(runtime.attr(physicalLabels[1], 'x'), '');
});
