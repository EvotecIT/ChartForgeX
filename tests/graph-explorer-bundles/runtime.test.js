const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const names = ['00-core', '01-document', '02-geometry', '05-viewport', '06-theme', '09-edge-bundles', '09-performance', '10-layout', '10-neighborhood-plan', '10-neighborhood', '11-state-sync', '29-selection', '30-bindings', '39-patch-validation', '40-api'];
const code = names.map(name => fs.readFileSync(path.join(assets, `graph-explorer.${name}.js`), 'utf8')).join('\n');
const loadRuntime = new Function('document', 'window', 'CustomEvent', code + '\nreturn { graphVirtualElement, graphVirtualClassList, graphState, applyCollapsedEdgeBundles, syncBundledEdgePresentation, graphItemAccessible, graphOverviewDisclosure, exportGraphJson, acceleratedGraphCandidates, moveAcceleratedGraphSelection, upsertGraphEdge, applyFilters, applyNeighborhoodFocus, clearHiddenSelections, attr };');
const runtime = loadRuntime({ readyState: 'loading', addEventListener() {} }, {}, class CustomEvent { constructor(name, options) { this.type = name; this.detail = options.detail; } });

function scene(siteCount, edgeSpecs, renderer = 'canvas') {
  const physicalLabels = [];
  const viewport = { appendChild(label) { physicalLabels.push(label); } };
  const stage = { appendChild(note) { root.note = note; } };
  const root = runtime.graphVirtualElement('root', { 'data-cfx-graph-id': 'bundle-test', 'data-cfx-graph-features': 'Selection,Clustering' }, []);
  root.dataset = { cfxGraphRendererActive: renderer };
  root.dispatchEvent = () => true;
  root.ownerDocument = {
    createElementNS() {
      const label = runtime.graphVirtualElement('graph-edge-label', {}, []);
      label.style = { setProperty(name, value) { label.setAttribute(name, value); } };
      return label;
    },
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

test('a hidden same-ID edge cannot clear an active node neighborhood', () => {
  const root = runtime.graphVirtualElement('root', { 'data-cfx-graph-id': 'same-id' }, []);
  root.dataset = { cfxGraphFocus: 'active', cfxGraphFocusNode: 'shared' };
  root.dispatchEvent = () => true;
  root.querySelector = () => null;
  root.querySelectorAll = () => [];
  const node = runtime.graphVirtualElement('graph-node', {
    'data-cfx-role': 'graph-node', 'data-node-id': 'shared', 'data-node-label': 'Focused node'
  }, ['cfx-graph-selected']);
  const edge = runtime.graphVirtualElement('graph-edge', {
    'data-cfx-role': 'graph-edge', 'data-edge-id': 'shared', 'data-edge-label': 'Hidden route'
  }, ['cfx-graph-selected', 'cfx-graph-neighborhood-hidden']);
  root.__cfxGraphVirtualItems = [node, edge];

  assert.equal(runtime.clearHiddenSelections(root), true);
  assert.equal(root.dataset.cfxGraphFocus, 'active');
  assert.equal(node.classList.contains('cfx-graph-selected'), true);
  assert.equal(edge.classList.contains('cfx-graph-selected'), false);
});

test('filtering out the focused root clears focus without restoring the overview viewport', () => {
  const root = runtime.graphVirtualElement('root', {
    'data-cfx-graph-id': 'filtered-focus',
    'data-cfx-graph-features': 'Selection,Viewport,NeighborhoodFocus',
    'data-cfx-viewport-x': '12', 'data-cfx-viewport-y': '18', 'data-cfx-viewport-scale': '1'
  }, []);
  root.dataset = { cfxGraphRendererActive: 'svg' };
  root.dispatchEvent = () => true;
  root.ownerDocument = { activeElement: null };
  root.search = { value: '' };
  root.querySelector = selector => selector.includes('graph-search') ? root.search : null;
  root.querySelectorAll = () => [];
  const focused = runtime.graphVirtualElement('graph-node', {
    'data-cfx-role': 'graph-node', 'data-node-id': 'focused', 'data-node-label': 'Focused service',
    'data-node-x': '80', 'data-node-y': '80', 'data-node-size': '12'
  }, ['cfx-graph-selected']);
  const other = runtime.graphVirtualElement('graph-node', {
    'data-cfx-role': 'graph-node', 'data-node-id': 'other', 'data-node-label': 'Other service',
    'data-node-x': '180', 'data-node-y': '80', 'data-node-size': '12'
  }, []);
  root.__cfxGraphVirtualItems = [focused, other];
  root.__cfxGraphState = runtime.graphState(root);

  assert.equal(runtime.applyNeighborhoodFocus(root, 'focused', {}, { fit: false }), true);
  root.setAttribute('data-cfx-viewport-x', '91');
  root.setAttribute('data-cfx-viewport-y', '73');
  root.setAttribute('data-cfx-viewport-scale', '1.4');
  root.search.value = 'Other';
  runtime.applyFilters(root);

  assert.equal(root.dataset.cfxGraphFocus, 'none');
  assert.equal(root.getAttribute('data-cfx-viewport-x'), '91');
  assert.equal(root.getAttribute('data-cfx-viewport-y'), '73');
  assert.equal(root.getAttribute('data-cfx-viewport-scale'), '1.4');
});

test('reversed endpoint data for the same directed route is bundled together', () => {
  const { root, edges } = scene(2, [
    [0, 1, 'A to B', 'healthy'], [1, 0, 'B receives from A', 'warning'], [1, 0, 'B to A', 'critical']
  ]);
  edges[0].setAttribute('data-edge-target-arrow', 'true');
  edges[1].setAttribute('data-edge-source-arrow', 'true');
  edges[2].setAttribute('data-edge-target-arrow', 'true');
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(runtime.attr(edges[1], 'data-cfx-bundle-count'), '2');
  assert.equal(runtime.attr(edges[2], 'data-cfx-bundle-count'), '');
  assert.equal(edges[0].classList.contains('cfx-graph-bundle-member'), true);
});

test('directed edges without an explicit target arrow keep their direction', () => {
  const { root, edges } = scene(2, [
    [0, 1, 'A to B', 'healthy'], [1, 0, 'B to A', 'warning'], [0, 1, 'Another A to B', 'critical']
  ]);
  edges.forEach(edge => edge.setAttribute('data-edge-directed', 'true'));
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(runtime.attr(edges[2], 'data-cfx-bundle-count'), '2');
  assert.equal(runtime.attr(edges[1], 'data-cfx-bundle-count'), '');
});

test('bundle accessibility identifies each route and retains unknown status ahead of healthy', () => {
  const { root, edges } = scene(3, [
    [0, 1, 'Healthy route', 'healthy'], [0, 1, 'Unknown route', 'unknown'],
    [1, 2, 'Healthy second', 'healthy'], [1, 2, 'Warning second', 'warning']
  ]);
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(runtime.attr(edges[1], 'data-cfx-bundle-count'), '2');
  assert.equal(runtime.attr(edges[0], 'data-cfx-bundle-count'), '');
  assert.match(runtime.attr(edges[1], 'aria-label'), /Site 0 and Site 1, Trust/);
  assert.match(runtime.attr(edges[3], 'aria-label'), /Site 1 and Site 2, Trust/);
  assert.notEqual(runtime.attr(edges[1], 'aria-label'), runtime.attr(edges[3], 'aria-label'));
});

test('dense priority route disclosure carries the hidden relationship count', () => {
  const edges = [];
  for (let source = 0; source < 7; source++) {
    for (let target = source + 1; target < 7; target++) edges.push([source, target, `Route ${source}-${target}`, 'healthy']);
  }
  edges[0][3] = 'critical';
  edges.push([0, 2, 'Unknown link', 'unknown']);
  const { root, edges: renderedEdges } = scene(7, edges);
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(root.classList.contains('cfx-graph-priority-overview'), true);
  assert.equal(root.dataset.cfxGraphOverviewTotal, '22');
  assert.match(runtime.graphOverviewDisclosure(root), /^Priority overview: \d+ routes from 22 inspectable relationships$/);
  assert.equal(renderedEdges.at(-1).classList.contains('cfx-graph-overview-member'), false);
  renderedEdges[1].setAttribute('data-edge-hidden', 'true');
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(root.dataset.cfxGraphOverviewTotal, '21');
  assert.match(root.note.textContent, /from 21 inspectable relationships/);
});

test('new SVG bundle labels have geometry immediately after filter restoration', () => {
  const { root, edges, physicalLabels } = scene(2, [
    [0, 1, 'Original one', 'healthy'], [0, 1, 'Original two', 'Warning']
  ], 'svg');
  edges[1].setAttribute('data-edge-label-color', '#123456');
  root.setAttribute('data-cfx-graph-theme-active', 'dark');
  root.__cfxGraphState = runtime.graphState(root);
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels.length, 1);
  assert.equal(runtime.attr(edges[1], 'data-cfx-bundle-count'), '2');
  assert.equal(physicalLabels[0].textContent, '2 relationships');
  assert.equal(runtime.attr(physicalLabels[0], '--cfx-edge-label-explicit'), '#123456');
  assert.equal(runtime.attr(physicalLabels[0], '--cfx-edge-label-adaptive'), '#d2dbea');
  assert.notEqual(runtime.attr(physicalLabels[0], 'x'), '');
  assert.ok(Number.isFinite(Number(runtime.attr(physicalLabels[0], 'y'))));
  root.search = { value: 'Original' };
  runtime.applyCollapsedEdgeBundles(root);
  root.search.value = '';
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels.filter(label => !label.__cfxRemoved).length, 1);
  assert.notEqual(runtime.attr(physicalLabels[1], 'x'), '');
});

test('existing labels for collapsed relationships follow the visibility of their edges', () => {
  const { root, edges, physicalLabels } = scene(2, [
    [0, 1, 'Healthy', 'healthy'], [0, 1, 'Critical', 'Critical']
  ], 'svg');
  for (const edge of edges) {
    const label = root.ownerDocument.createElementNS();
    label.setAttribute('data-edge-label-for', runtime.attr(edge, 'data-edge-id'));
    physicalLabels.push(label);
  }
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(edges[1].classList.contains('cfx-graph-bundle-member'), false);
  assert.equal(physicalLabels[0].classList.contains('cfx-graph-bundle-member'), true);
  assert.equal(physicalLabels[1].textContent, '2 relationships');
  root.search = { value: 'Critical' };
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels[0].classList.contains('cfx-graph-bundle-member'), false);
});

test('a bundled route honors a hidden-label request without losing its accessible summary', () => {
  const { root, edges, physicalLabels } = scene(2, [
    [0, 1, 'First', 'healthy'], [0, 1, 'Second', 'warning']
  ], 'svg');
  edges[1].setAttribute('data-edge-show-label', 'false');
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels.length, 0);
  assert.equal(runtime.attr(edges[1], 'data-cfx-bundle-count'), '2');
  assert.match(runtime.attr(edges[1], 'aria-label'), /2 relationships/);
});

test('keyboard navigation reaches a bundled route before Enter activates it', () => {
  const { root, edges } = scene(2, [
    [0, 1, 'First', 'healthy'], [0, 1, 'Second', 'warning']
  ], 'svg');
  root.setAttribute('data-cfx-graph-accelerated-markup', 'true');
  runtime.applyCollapsedEdgeBundles(root);
  root.__cfxGraphState = runtime.graphState(root);
  const candidates = runtime.acceleratedGraphCandidates(root);
  assert.ok(candidates.some(candidate => candidate.el === edges[1]));
  assert.ok(!candidates.some(candidate => candidate.el === edges[0]));
  const surface = { setAttribute(name, value) { this[name] = value; } };
  for (let index = 0; index < candidates.length; index++) {
    const event = { key: 'ArrowRight', currentTarget: surface, preventDefault() {} };
    assert.equal(runtime.moveAcceleratedGraphSelection(root, event), true);
    if (root.dataset.cfxGraphSelectionPrimary === 'edge-1') break;
  }
  assert.equal(root.dataset.cfxGraphSelectionPrimary, 'edge-1');
  assert.equal(runtime.attr(root.__cfxGraphVirtualItems.at(-1), 'data-cluster-collapsed'), 'true');
  assert.match(surface['aria-label'], /2 relationships/);
  const expandedCluster = root.__cfxGraphVirtualItems.at(-1);
  expandedCluster.setAttribute('data-cluster-collapsed', 'false');
  expandedCluster.setAttribute('aria-hidden', 'true');
  assert.ok(!runtime.acceleratedGraphCandidates(root).some(candidate => candidate.el === expandedCluster));
  for (let index = 0; index < candidates.length + 2; index++) {
    runtime.moveAcceleratedGraphSelection(root, { key: 'ArrowRight', currentTarget: surface, preventDefault() {} });
    assert.notEqual(root.dataset.cfxGraphSelectionPrimary, runtime.attr(expandedCluster, 'data-cluster-id'));
  }
});

test('read-only clustering still exposes and activates bundled routes', () => {
  const { root, edges } = scene(2, [[0, 1, 'First', 'healthy'], [0, 1, 'Second', 'warning']]);
  root.setAttribute('data-cfx-graph-features', 'Clustering');
  root.__cfxGraphState = runtime.graphState(root);
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(runtime.graphItemAccessible(root, edges[1]), true);
  assert.equal(runtime.graphItemAccessible(root, edges[0]), false);
  const surface = { setAttribute(name, value) { this[name] = value; } };
  for (let index = 0; index < 3; index++)
    assert.equal(runtime.moveAcceleratedGraphSelection(root, { key: 'ArrowRight', currentTarget: surface, preventDefault() {} }), true);
  assert.match(surface['aria-label'], /Site 0 and Site 1/);
});

test('accelerated pointer activation expands a bundle without Selection enabled', () => {
  const source = fs.readFileSync(path.join(assets, 'graph-explorer.27-pointer-interactions.js'), 'utf8');
  const handlers = {};
  const stage = { addEventListener(name, handler) { handlers[name] = handler; } };
  const canonical = runtime.graphVirtualElement('graph-edge', { 'data-cfx-role': 'graph-edge', 'data-cfx-bundle-count': '2' }, []);
  const overlay = runtime.graphVirtualElement('graph-edge', {
    'data-cfx-role': 'graph-edge', 'data-cfx-runtime-overlay': 'true', 'data-edge-id': 'route'
  }, []);
  overlay.closest = selector => selector.includes('graph-edge') ? overlay : null;
  const nearbyNode = { id: 'nearby', el: runtime.graphVirtualElement('graph-node', {}, []) };
  const root = { dataset: {}, querySelector: () => stage, __cfxGraphState: { nodes: [nearbyNode], edges: [{ id: 'route', el: canonical }], clusters: [] } };
  const selected = [];
  const bind = new Function('attr', 'scenePoint', 'hasFeature', 'hitNodeAt', 'hitGraphItemAt', 'select',
    source + '\nreturn bindPointerInteractions;')(
    runtime.attr, () => ({ x: 1, y: 2 }), (_, feature) => feature === 'Clustering' || feature === 'Viewport',
    () => nearbyNode, () => null, (_, item) => selected.push(item)
  );
  bind(root);
  let prevented = false;
  handlers.pointerdown({ button: 0, target: overlay, preventDefault() { prevented = true; }, pointerId: 1 });
  assert.equal(prevented, true);
  assert.deepEqual(selected, [canonical]);
  assert.notEqual(root.dataset.cfxGraphLastPointerMode, 'pan');
});

test('patching a bundled route replaces stale accessible and SVG label text', () => {
  const { root, edges, physicalLabels } = scene(2, [
    [0, 1, 'Old first', 'healthy'], [0, 1, 'Old second', 'warning']
  ], 'svg');
  root.__cfxGraphState = runtime.graphState(root);
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels.length, 1);
  runtime.upsertGraphEdge(root, {
    id: 'edge-1', label: 'New second', kind: 'Trust', status: 'warning',
    sourceNodeId: 'node-0', targetNodeId: 'node-1', showLabel: true
  });
  assert.equal(physicalLabels[0].textContent, 'New second');
  assert.equal(runtime.attr(edges[1], 'aria-label'), 'New second');
  runtime.applyCollapsedEdgeBundles(root);
  root.search = { value: 'Old' };
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(physicalLabels[0].textContent, 'New second');
  assert.equal(runtime.attr(edges[1], 'aria-label'), 'New second');
});

test('activating a bundled route reheats the expanded graph once and falls back to fit', () => {
  const layout = fs.readFileSync(path.join(assets, 'graph-explorer.10-layout.js'), 'utf8');
  const start = layout.indexOf('  const select = (root, node, options) => {');
  const end = layout.indexOf('  const selectedGraphNodeId = (root) => {', start);
  assert.ok(start >= 0 && end > start);
  const calls = [];
  let reducedMotion = false;
  const activate = new Function('hasFeature', 'attr', 'num', 'applyClusterState', 'graphPrefersReducedMotion', 'reheatPhysics', 'fitViewport',
    layout.slice(start, end) + '\nreturn select;')(
    (_, feature) => ['Selection', 'Clustering', 'RuntimePhysics', 'Viewport'].includes(feature),
    runtime.attr, (element, name, fallback) => Number(runtime.attr(element, name)) || fallback,
    (_, expanded, clusterId) => calls.push(`expand:${clusterId}:${expanded}`),
    () => reducedMotion,
    (_, reason) => { calls.push(`reheat:${reason}`); return true; },
    () => calls.push('fit')
  );
  const edge = runtime.graphVirtualElement('graph-edge', {
    'data-cfx-bundle-count': '3', 'data-source-cluster-id': 'site-a', 'data-target-cluster-id': 'site-b'
  }, []);
  const root = runtime.graphVirtualElement('root', {}, []);
  activate(root, edge);
  assert.deepEqual(calls, ['expand:site-a:false', 'expand:site-b:false', 'reheat:bundle-expand']);
  calls.length = 0;
  reducedMotion = true;
  activate(root, edge);
  assert.deepEqual(calls, ['expand:site-a:false', 'expand:site-b:false', 'fit']);
  calls.length = 0;
  const activateWithoutSelection = new Function('hasFeature', 'attr', 'num', 'applyClusterState', 'graphPrefersReducedMotion', 'reheatPhysics', 'fitViewport',
    layout.slice(start, end) + '\nreturn select;')(
    (_, feature) => ['Clustering', 'Viewport'].includes(feature),
    runtime.attr, (element, name, fallback) => Number(runtime.attr(element, name)) || fallback,
    (_, expanded, clusterId) => calls.push(`expand:${clusterId}:${expanded}`),
    () => true, () => false, () => calls.push('fit')
  );
  activateWithoutSelection(root, edge);
  assert.deepEqual(calls, ['expand:site-a:false', 'expand:site-b:false', 'fit']);
});

test('accelerated SVG materialization preserves selected and focused route labels', () => {
  const source = fs.readFileSync(path.join(assets, 'graph-explorer.28-svg-export.js'), 'utf8');
  const start = source.indexOf('  const appendExportedEdgeLabel = (document, group, edge, rendered) => {');
  const end = source.indexOf('  const drawAcceleratedSvgRuntime = (root, state) => {', start);
  assert.ok(start >= 0 && end > start);
  const append = new Function('svgNode', 'edgeLabelPoint', 'edgeControl',
    source.slice(start, end) + '\nreturn appendExportedEdgeLabel;')(
    (_, tag, properties) => ({ ...properties, style: { setProperty() {} } }),
    () => ({ x: 100, y: 50 }), () => null
  );
  const edgeElement = runtime.graphVirtualElement('graph-edge', {}, ['cfx-graph-selected']);
  const edge = { id: 'route', label: '2 relationships', showLabel: true, el: edgeElement };
  const group = { appendChild(label) { this.label = label; } };
  append({}, group, edge, {});
  assert.match(group.label.class, /cfx-graph-label-selected/);
  edgeElement.classList.remove('cfx-graph-selected');
  edgeElement.classList.add('cfx-graph-neighborhood-related');
  append({}, group, edge, {});
  assert.match(group.label.class, /cfx-graph-neighborhood-related/);
});

test('host export includes numeric worker and picking counters', () => {
  const { root } = scene(2, [[0, 1, 'Link', 'healthy']]);
  root.dataset.cfxGraphPerformanceWorkerTransferBytes = '14400';
  root.dataset.cfxGraphPerformanceMaxSampleMs = '12.5';
  root.dataset.cfxGraphPerformanceBudget = 'within';
  root.dataset.cfxGraphPerformanceStaleWorkerUpdates = '3';
  Object.assign(root.dataset, { cfxGraphNodeHitCandidates: '9', cfxGraphEdgeHitCandidates: '2', cfxGraphEdgeHitBuilds: '1', cfxGraphEdgeHitRefits: '4', cfxGraphEdgeHitIndexMs: '0.75' });
  const performance = runtime.exportGraphJson(root).performance;
  assert.equal(performance.maxSampleMs, 12.5);
  assert.equal(performance.budget, 'within');
  assert.equal(performance.workerTransferBytes, 14400);
  assert.equal(performance.staleWorkerUpdates, 3);
  assert.equal(performance.nodeHitCandidates, 9);
  assert.equal(performance.edgeHitCandidates, 2);
  assert.equal(performance.edgeHitIndexBuilds, 1);
  assert.equal(performance.edgeHitIndexRefits, 4);
  assert.equal(performance.edgeHitIndexLastMs, .75);
});
