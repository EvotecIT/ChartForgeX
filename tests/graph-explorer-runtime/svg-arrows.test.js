const assert = require('node:assert/strict');
const test = require('node:test');
const { graphRuntimeSource } = require('../helpers/graph-runtime-source');

const source = graphRuntimeSource('return { graphVirtualElement, graphVirtualMatches, graphThemePalette, graphEdgePaint, edgePathData, edgeControl, appendExportedEdgeArrows, syncPhysicalSvgEdgePaint, materializeAcceleratedSvg };');

function fixture(kind = 'line') {
  const api = new Function('document', 'window', source)({ readyState: 'loading', addEventListener() {} }, {});
  const document = { createElementNS(_namespace, name) { return element(name); } };
  function element(name, attributes = {}) {
    const node = api.graphVirtualElement(name, attributes, []), values = { ...attributes };
    const set = node.setAttribute, remove = node.removeAttribute;
    node.setAttribute = (key, value) => { values[key] = String(value); set(key, value); };
    node.removeAttribute = key => { delete values[key]; remove(key); };
    node.ownerDocument = document; node.localName = name; node.children = []; node.parentNode = null;
    node.style = { setProperty(key, value) { this[key] = value; } };
    Object.defineProperties(node, {
      firstElementChild: { get: () => node.children[0] || null },
      nextElementSibling: { get: () => node.parentNode?.children[node.parentNode.children.indexOf(node) + 1] || null }
    });
    node.remove = () => {
      if (node.parentNode) node.parentNode.children.splice(node.parentNode.children.indexOf(node), 1);
      node.parentNode = null;
    };
    node.insertBefore = (child, before) => {
      if (child === before) return child;
      child.remove(); child.parentNode = node;
      const index = before ? node.children.indexOf(before) : node.children.length;
      assert.ok(index >= 0); node.children.splice(index, 0, child); return child;
    };
    node.appendChild = child => node.insertBefore(child, null);
    node.after = child => node.parentNode.insertBefore(child, node.nextElementSibling);
    node.querySelectorAll = selector => {
      const found = [], tag = selector.match(/^[a-z]+/)?.[0];
      const visit = child => {
        if ((!tag || child.localName === tag) && api.graphVirtualMatches(child, selector)) found.push(child);
        child.children.forEach(visit);
      };
      node.children.forEach(visit); return found;
    };
    node.querySelector = selector => node.querySelectorAll(selector)[0] || null;
    node.cloneNode = deep => {
      const clone = element(name, { ...values, class: node.getAttribute('class') });
      if (deep) node.children.forEach(child => clone.appendChild(child.cloneNode(true)));
      return clone;
    };
    return node;
  }
  const scene = element('svg'), layer = element('g'); scene.appendChild(layer);
  const root = api.graphVirtualElement('root', { 'data-cfx-graph-theme-active': 'light' }, []);
  root.querySelector = () => scene;
  const a = { id: 'a', x: 100, y: 100, size: 20, shape: 'circle', el: api.graphVirtualElement('graph-node', {}, []) };
  const b = { id: 'b', x: 350, y: 240, size: 24, shape: 'circle', el: api.graphVirtualElement('graph-node', {}, []) };
  const edge = { id: 'e', source: a, target: kind === 'loop' ? a : b, shape: kind === 'curve' ? 'curve' : 'line',
    curvature: kind === 'curve' ? 70 : 0, weight: 2, strokeWidth: 3, strokeColor: '#0f766e', sourceArrow: true, targetArrow: true,
    el: api.graphVirtualElement('graph-edge', {}, ['cfx-graph-edge']), dashPattern: [8, 4] };
  if (kind === 'route') edge.routePoints = [{ x: 100, y: 100 }, { x: 100, y: 240 }, { x: 350, y: 240 }];
  const path = element('path', { 'data-cfx-role': 'graph-edge', 'data-edge-id': 'e' }); layer.appendChild(path);
  const state = { nodes: [], edges: [edge], byId: new Map([['a', a], ['b', b]]) };
  const sync = () => {
    path.setAttribute('d', api.edgePathData(edge, api.edgeControl(edge)));
    api.syncPhysicalSvgEdgePaint(root, state);
    return layer.querySelector('[data-cfx-role="graph-edge-arrows"]');
  };
  return { api, root, scene, layer, state, edge, a, b, path, sync, element, document };
}

const paintSnapshot = group => group ? {
  class: group.getAttribute('class'), style: group.getAttribute('style'),
  arrows: group.children.map(polygon => ({ side: polygon.getAttribute('data-arrow-side'), points: polygon.getAttribute('points'), style: polygon.getAttribute('style') }))
} : null;

test('physical SVG refreshes geometry and paint without replacing retained arrow sides', () => {
  for (const kind of ['line', 'curve', 'route', 'loop']) {
    const f = fixture(kind), group = f.sync(), sides = [...group.children], initial = paintSnapshot(group);
    for (const classes of [[], ['cfx-graph-selected'], ['cfx-graph-highlighted'], ['cfx-graph-neighborhood-related'], ['cfx-graph-neighborhood-dim']]) {
      f.a.x += 12; f.b.y += 7; f.edge.strokeColor = '#be123c';
      f.edge.el.setAttribute('class', ['cfx-graph-edge', ...classes].join(' '));
      assert.equal(f.sync(), group, kind);
      assert.deepEqual(group.children, sides, kind);
      assert.equal(f.path.nextElementSibling, group);
      const fresh = f.element('g'), paint = f.api.graphEdgePaint(f.root, f.edge, f.state.byId, f.api.graphThemePalette(f.root), false, false);
      f.api.appendExportedEdgeArrows(f.document, fresh, f.edge, paint);
      assert.deepEqual(paintSnapshot(group), paintSnapshot(fresh.firstElementChild), kind);
    }
    assert.notDeepEqual(paintSnapshot(group), initial, kind);
  }
});

test('arrow reconciliation preserves source/target order and directed aliases through side changes', () => {
  const f = fixture(), group = f.sync(), source = group.children[0], target = group.children[1];
  f.edge.sourceArrow = false;
  assert.equal(f.sync(), group); assert.deepEqual(group.children, [target]);
  f.edge.sourceArrow = true;
  f.sync(); assert.equal(group.children[1], target);
  assert.notEqual(group.children[0], source);
  assert.deepEqual(group.children.map(n => n.getAttribute('data-arrow-side')), ['source', 'target']);
  f.edge.sourceArrow = false; f.edge.directed = true;
  f.sync(); f.edge.targetArrow = false;
  f.sync(); assert.deepEqual(group.children, [target], 'directed and explicit target share one polygon');
  f.edge.directed = false;
  assert.equal(f.sync(), null); assert.equal(group.parentNode, null);
  f.edge.sourceArrow = true;
  const next = f.sync(); assert.notEqual(next, group);
  assert.deepEqual(next.children.map(n => n.getAttribute('data-arrow-side')), ['source']);
  f.state.edges = []; f.api.syncPhysicalSvgEdgePaint(f.root, f.state);
  assert.equal(next.parentNode, null, 'removed edges release their arrow group');
  const hidden = f.element('g'); f.layer.appendChild(hidden);
  f.api.appendExportedEdgeArrows(f.document, f.layer, f.edge, { arrows: false }, hidden);
  assert.equal(hidden.parentNode, null);
});

test('physical export materialization updates clone-local arrows and leaves live nodes owned by the scene', () => {
  const f = fixture('curve'), live = f.sync(), liveSides = [...live.children], original = paintSnapshot(live);
  const clone = f.scene.cloneNode(true), cloneGroup = clone.querySelector('[data-cfx-role="graph-edge-arrows"]');
  const cloneSides = [...cloneGroup.children];
  f.a.x += 70; f.edge.strokeColor = '#7c3aed';
  f.api.materializeAcceleratedSvg(f.root, clone, f.state);
  assert.equal(clone.querySelector('[data-cfx-role="graph-edge-arrows"]'), cloneGroup);
  assert.deepEqual(cloneGroup.children, cloneSides);
  assert.notDeepEqual(paintSnapshot(cloneGroup), original);
  assert.deepEqual(paintSnapshot(live), original);
  assert.equal(live.parentNode, f.layer); assert.deepEqual(live.children, liveSides);
  f.sync(); assert.deepEqual(paintSnapshot(live), paintSnapshot(cloneGroup));
});
