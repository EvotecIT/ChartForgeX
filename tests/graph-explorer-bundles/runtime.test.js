const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { adapterLifetimes } = require('../helpers/adapter-lifetime');

const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const { graphRuntimeSource } = require('../helpers/graph-runtime-source');
const code = graphRuntimeSource('return { graphVirtualElement, graphVirtualClassList, graphVirtualMatches, graphState, applyCollapsedEdgeBundles, syncBundledEdgePresentation, graphItemAccessible, graphOverviewDisclosure, exportGraphJson, acceleratedGraphCandidates, moveAcceleratedGraphSelection, upsertGraphEdge, syncGraphEdgeAccessibleNames, select, applyFilters, applyNeighborhoodFocus, applyClusterState, applyHierarchyView, clearHiddenSelections, appendExportedEdgeLabel, graphThemePalette, attr };');
const loadRuntime = new Function('document', 'window', 'CustomEvent', code);
const lifetimes = adapterLifetimes();
test.after(() => lifetimes.dispose());
const runtime = loadRuntime({ readyState: 'loading', addEventListener() {} }, lifetimes.window, class CustomEvent { constructor(name, options) { this.type = name; this.detail = options.detail; } });

function scene(siteCount, edgeSpecs, renderer = 'canvas') {
  const physicalLabels = [];
  const document = {
    createElementNS(_namespace, name) {
      const element = runtime.graphVirtualElement(name, {}, []);
      element.ownerDocument = document; element.children = [];
      element.style = { setProperty(name, value) { element.setAttribute(name, value); } };
      element.appendChild = child => { child.remove(); child.__cfxRemoved = false; child.parent = element; element.children.push(child); };
      element.insertBefore = (child, before) => {
        if (!before) { element.appendChild(child); return; }
        assert.ok(element.children.includes(before));
        child.remove(); child.__cfxRemoved = false; child.parent = element;
        element.children.splice(element.children.indexOf(before), 0, child);
      };
      element.append = (...children) => children.forEach(element.appendChild);
      element.remove = () => {
        element.__cfxRemoved = true;
        if (element.parent) element.parent.children.splice(element.parent.children.indexOf(element), 1);
        element.parent = null;
      };
      element.querySelectorAll = selector => {
        const found = [];
        const visit = child => { if (runtime.graphVirtualMatches(child, selector)) found.push(child); child.children.forEach(visit); };
        element.children.forEach(visit); return found;
      };
      element.querySelector = selector => element.querySelectorAll(selector)[0] || null;
      return element;
    },
    createElement() { return { setAttribute() {}, remove() { root.note = null; } }; }
  };
  const viewport = document.createElementNS('', 'g'), append = viewport.appendChild;
  viewport.appendChild = child => {
    if (child.getAttribute('data-cfx-role') === 'graph-edge-label') physicalLabels.push(child);
    append(child);
  };
  const stage = { appendChild(note) { root.note = note; }, getBoundingClientRect: () => ({ width: 960, height: 560 }) };
  const root = runtime.graphVirtualElement('root', { 'data-cfx-graph-id': 'bundle-test', 'data-cfx-graph-features': 'Selection,Clustering' }, []);
  root.dataset = { cfxGraphRendererActive: renderer };
  root.dispatchEvent = () => true;
  root.ownerDocument = document;
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
  lifetimes.mount(root);
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

function structuralFocusRoot(withHierarchy) {
  const root = runtime.graphVirtualElement('root', {
    'data-cfx-graph-id': withHierarchy ? 'hierarchy-focus' : 'cluster-focus',
    'data-cfx-graph-features': 'Selection,Viewport,NeighborhoodFocus,Clustering,HierarchyNavigation',
    'data-cfx-viewport-x': '12', 'data-cfx-viewport-y': '18', 'data-cfx-viewport-scale': '1'
  }, []);
  root.dataset = { cfxGraphRendererActive: 'svg' };
  root.dispatchEvent = () => true;
  root.ownerDocument = { activeElement: null };
  root.querySelector = () => null;
  root.querySelectorAll = () => [];
  const parent = runtime.graphVirtualElement('graph-node', {
    'data-node-id': 'parent', 'data-node-label': 'Parent', 'data-node-cluster': 'site',
    'data-node-x': '80', 'data-node-y': '80', 'data-node-size': '12'
  }, []);
  const focused = runtime.graphVirtualElement('graph-node', {
    'data-node-id': 'focused', 'data-node-label': 'Focused', 'data-node-cluster': 'site',
    'data-node-parent': withHierarchy ? 'parent' : '', 'data-node-x': '180', 'data-node-y': '80', 'data-node-size': '12'
  }, ['cfx-graph-selected']);
  const cluster = runtime.graphVirtualElement('graph-cluster', {
    'data-cluster-id': 'site', 'data-cluster-label': 'Site', 'data-cluster-node-ids': 'parent,focused',
    'data-cluster-collapsed': 'false'
  }, ['cfx-graph-cluster-expanded']);
  root.__cfxGraphVirtualItems = [parent, focused, cluster];
  root.__cfxGraphState = runtime.graphState(root);
  assert.equal(runtime.applyNeighborhoodFocus(root, 'focused', {}, { fit: false }), true);
  root.setAttribute('data-cfx-viewport-x', '91');
  root.setAttribute('data-cfx-viewport-y', '73');
  root.setAttribute('data-cfx-viewport-scale', '1.4');
  return root;
}

test('cluster and hierarchy invalidation clear focus without restoring a stale overview', () => {
  const clusterRoot = structuralFocusRoot(false);
  runtime.applyClusterState(clusterRoot, true, 'site', { reheat: false });
  assert.equal(clusterRoot.dataset.cfxGraphFocus, 'none');
  assert.equal(clusterRoot.getAttribute('data-cfx-viewport-x'), '91');
  assert.equal(clusterRoot.getAttribute('data-cfx-viewport-y'), '73');
  assert.equal(clusterRoot.getAttribute('data-cfx-viewport-scale'), '1.4');

  const hierarchyRoot = structuralFocusRoot(true);
  assert.equal(runtime.applyHierarchyView(hierarchyRoot, 'parent', 0, { fit: false, restartPhysics: false }), true);
  assert.equal(hierarchyRoot.dataset.cfxGraphFocus, 'none');
  assert.equal(hierarchyRoot.getAttribute('data-cfx-viewport-x'), '91');
  assert.equal(hierarchyRoot.getAttribute('data-cfx-viewport-y'), '73');
  assert.equal(hierarchyRoot.getAttribute('data-cfx-viewport-scale'), '1.4');
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
  lifetimes.mount(root);
  const selected = [];
  const bind = new Function('attr', 'scenePoint', 'hasFeature', 'hitNodeAt', 'hitGraphItemAt', 'select', 'graphLifetime', 'listenGraph',
    source + '\nreturn bindPointerInteractions;')(
    runtime.attr, () => ({ x: 1, y: 2 }), (_, feature) => feature === 'Clustering' || feature === 'Viewport',
    () => nearbyNode, () => null, (_, item) => selected.push(item), lifetimes.graphLifetime, lifetimes.listenGraph
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
  const edgeElement = runtime.graphVirtualElement('graph-edge', {}, ['cfx-graph-selected']);
  const edge = { id: 'route', label: '2 relationships', showLabel: true, el: edgeElement };
  const group = { appendChild(label) { this.label = label; } };
  const root = runtime.graphVirtualElement('root', {}, []);
  const document = { createElementNS: () => { const element = runtime.graphVirtualElement('text', {}, []); element.style = {}; return element; } };
  const paint = { rendered: { source: { x: 0, y: 50 }, target: { x: 200, y: 50 } }, control: null };
  runtime.appendExportedEdgeLabel(document, group, edge, paint, root, runtime.graphThemePalette(root));
  assert.match(group.label.getAttribute('class'), /cfx-graph-label-selected/);
  edgeElement.classList.remove('cfx-graph-selected');
  edgeElement.classList.add('cfx-graph-neighborhood-related');
  runtime.appendExportedEdgeLabel(document, group, edge, paint, root, runtime.graphThemePalette(root));
  assert.match(group.label.getAttribute('class'), /cfx-graph-neighborhood-related/);
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


test('blank authored edge labels retain current endpoint names through patching and bundle restoration', () => {
  const { root, edges } = scene(2, [[0, 1, ' \t\u0085 ', 'critical'], [0, 1, '  Authored flow  ', 'healthy']]);
  root.setAttribute('data-cfx-graph-accelerated-markup', 'true');
  runtime.syncGraphEdgeAccessibleNames(root);
  assert.equal(runtime.attr(edges[0], 'aria-label'), 'Node 0 to Node 1');
  assert.equal(runtime.attr(edges[1], 'aria-label'), '  Authored flow  ');
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(runtime.attr(edges[0], 'data-cfx-bundle-count'), '2');
  root.__cfxGraphVirtualItems.find(item => runtime.attr(item, 'data-node-id') === 'node-1').setAttribute('data-node-label', 'Renamed {source}');
  runtime.syncGraphEdgeAccessibleNames(root);
  root.__cfxGraphVirtualItems.filter(item => runtime.attr(item, 'data-cfx-role') === 'graph-cluster').forEach(cluster => cluster.setAttribute('data-cluster-collapsed', 'false'));
  runtime.applyCollapsedEdgeBundles(root);
  assert.equal(runtime.attr(edges[0], 'aria-label'), 'Node 0 to Renamed {source}');
  assert.equal(runtime.attr(edges[1], 'aria-label'), '  Authored flow  ');
  runtime.upsertGraphEdge(root, { id: 'edge-0', source: 'node-0', target: 'node-1', label: ' \t ' });
  runtime.syncGraphEdgeAccessibleNames(root);
  assert.equal(runtime.attr(edges[0], 'aria-label'), 'Node 0 to Renamed {source}');
  const state = runtime.graphState(root);
  runtime.syncBundledEdgePresentation(root, state);
  assert.equal(state.edges[0].label, '');
  assert.equal(runtime.exportGraphJson(root).edges[0].label, ' \t ');
  assert.equal(runtime.exportGraphJson(root).edges[1].label, '  Authored flow  ');
  const announcer = { textContent: '' }, tip = { textContent: '', hidden: true }, events = [];
  const originalQuery = root.querySelector;
  root.querySelector = selector => selector.includes('graph-announcer') ? announcer : selector === '.cfx-graph-tooltip' ? tip : originalQuery(selector);
  root.dispatchEvent = event => { events.push(event); return true; };
  root.setAttribute('data-cfx-graph-features', 'Selection,MultiSelection');
  runtime.select(root, edges[0], { activateBundle: false });
  assert.equal(announcer.textContent, 'Node 0 to Renamed {source} selected. 1 item selected.');
  assert.equal(tip.textContent, 'Node 0 to Renamed {source}');
  assert.equal(tip.hidden, false);
  assert.equal(events.findLast(event => event.type === 'cfxgraphselect').detail.label, ' \t ');
  assert.equal(events.findLast(event => event.type === 'cfxgraphselection').detail.items[0].label, ' \t ');
  runtime.select(root, edges[0], { additive: true, toggle: true, activateBundle: false });
  assert.equal(announcer.textContent, 'Node 0 to Renamed {source} removed from selection.');
  assert.equal(tip.hidden, true);
  assert.equal(events.findLast(event => event.type === 'cfxgraphselect').detail.label, ' \t ');
});
