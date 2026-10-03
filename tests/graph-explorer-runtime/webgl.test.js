const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const manifest = fs.readFileSync(path.join(assets, '../HtmlGraphExplorerAssets.cs'), 'utf8');
const source = [...manifest.matchAll(/"ChartForgeX\.Interactivity\.Html\.Assets\.(graph-explorer\.[^"]+\.js)"/g)]
  .map(match => fs.readFileSync(path.join(assets, match[1]), 'utf8')).join('\n');
function runtime() {
  const host = { document: { readyState: 'loading', addEventListener() {}, querySelectorAll: () => [] }, window: {}, setTimeout, clearTimeout };
  vm.runInNewContext(source + '\nthis.api = { graphVirtualElement, graphThemePalette, graphReadableNodeColors, graphColorContrast, graphEdgePaint, edgeArrowGeometry, webGlEdgePoints, webGlDashedPaths, webGlStrokePath, webGlEdgeMesh, webGlColor, webGlNodePoints, drawCanvasNodes, drawCanvasEdge, drawNodeMark, syncGraphThemeState, setGraphRenderer, graphVirtualMatches, appendExportedNodeDetails, drawAcceleratedSvgRuntime, materializeAcceleratedSvg };', host);
  const api = host.api, root = api.graphVirtualElement('root', { 'data-cfx-graph-theme-active': 'light' }, []);
  const node = (id, x, y) => ({ id, x, y, size: 12, shape: 'circle', el: api.graphVirtualElement('graph-node', { 'data-node-label': id }, []) });
  const a = node('a', 100, 100), b = node('b', 300, 100);
  const edge = { id: 'e', source: a, target: b, shape: 'line', curvature: 0, weight: 1, strokeWidth: 5, strokeColor: '#0f766e',
    el: api.graphVirtualElement('graph-edge', {}, []), label: 'Relationship', showLabel: true, dashPattern: [9, 5] };
  const state = { nodes: [a, b], edges: [edge], clusters: [], byId: new Map([['a', a], ['b', b]]) };
  return { api, root, a, b, edge, state, palette: api.graphThemePalette(root) };
}
const close = (actual, expected) => assert.ok(Math.abs(actual - expected) < 1e-8, `${actual} != ${expected}`);

test('GPU stroke geometry preserves explicit width, round caps and finite vertices', () => {
  const { api } = runtime(), mesh = { positions: [], colors: [] };
  api.webGlStrokePath(mesh, [{ x: 0, y: 0 }, { x: 40, y: 0 }], 5, [1, 0, 0, .58]);
  const x = mesh.positions.filter((_, index) => index % 2 === 0), y = mesh.positions.filter((_, index) => index % 2 === 1);
  close(Math.max(...y), 2.5); close(Math.min(...y), -2.5);
  close(Math.min(...x), -2.5); close(Math.max(...x), 42.5);
  assert.ok(mesh.positions.every(Number.isFinite));
  assert.equal(mesh.colors.length, mesh.positions.length * 2);
});
test('GPU routes use trimmed endpoints, curved samples and nondegenerate self loops', () => {
  const { api, a, edge } = runtime();
  edge.targetArrow = true;
  const line = api.webGlEdgePoints(edge, null, 1);
  assert.ok(line.at(-1).x < edge.target.x);
  const curve = api.webGlEdgePoints(edge, { x: 200, y: 30 }, 2);
  assert.ok(curve.some(point => point.y < 80));
  assert.ok(curve.length <= 65);
  edge.target = a;
  const loop = api.webGlEdgePoints(edge, null, 2);
  assert.ok(loop.some(point => point.y < a.y - 20));
  assert.notEqual(loop[0].x, loop.at(-1).x);
  const loopArrows = ['source', 'target'].map(side => api.edgeArrowGeometry(edge, null, side));
  edge.routePoints = [{ x: 100, y: 100 }, { x: 900, y: 900 }, { x: 100, y: 100 }];
  assert.deepEqual(api.webGlEdgePoints(edge, null, 2), loop, 'self-loop geometry precedes prepared routes on every renderer');
  assert.deepEqual(['source', 'target'].map(side => api.edgeArrowGeometry(edge, null, side)), loopArrows);
  edge.target = { ...a, x: 300 };
  edge.routePoints = [{ x: 120, y: 100 }, { x: 120, y: 150 }, { x: 280, y: 150 }, { x: 280, y: 100 }];
  const route = api.webGlEdgePoints(edge, null, 1);
  assert.equal(route.length, 4);
  assert.equal(route[1].y, 150);
});
test('dash phase continues through route corners and excessive patterns use the native stroker', () => {
  const { api } = runtime();
  const paths = api.webGlDashedPaths([{ x: 0, y: 0 }, { x: 5, y: 0 }, { x: 5, y: 15 }], [9, 5]);
  assert.equal(paths.length, 2);
  close(paths[0].at(-1).y, 4); close(paths[1][0].y, 9);
  assert.equal(api.webGlDashedPaths([{ x: 0, y: 0 }, { x: 100, y: 0 }], [.000001, .000001]), null);
  assert.equal(api.webGlDashedPaths([{ x: 0, y: 0 }, { x: 20, y: 0 }], [0, 0]).length, 1);
  assert.ok(api.webGlDashedPaths([{ x: 0, y: 0 }, { x: 20, y: 0 }], [0, 5]).every(path => path.length > 1));
});
test('WebGL shares Canvas edge widths, emphasis and label LOD decisions', () => {
  const { api, root, edge, state, palette } = runtime();
  let paint = api.graphEdgePaint(root, edge, state.byId, palette, true, false);
  close(paint.width, 5); assert.ok(paint.labels);
  root.classList.add('cfx-graph-lod-hide-edge-labels');
  assert.equal(api.graphEdgePaint(root, edge, state.byId, palette, true, false).labels, false);
  edge.el.classList.add('cfx-graph-selected');
  paint = api.graphEdgePaint(root, edge, state.byId, palette, true, true);
  close(paint.width, 6.6); assert.ok(paint.labels); assert.ok(paint.arrows);
  edge.strokeWidth = 0;
  assert.ok(api.graphEdgePaint(root, edge, state.byId, palette, true, false).width < 6);
});
test('retained geometry survives viewport movement and invalidates for drag, selection, theme, resize and patches', () => {
  const { api, root, a, edge, state, palette } = runtime();
  const mesh = () => api.webGlEdgeMesh(root, state, state.byId, palette, true, false, 1);
  const first = mesh();
  root.setAttribute('data-cfx-viewport-x', '200'); root.setAttribute('data-cfx-viewport-scale', '2');
  assert.equal(mesh(), first);
  a.x += 20; const dragged = mesh(); assert.notEqual(dragged, first);
  edge.el.classList.add('cfx-graph-selected'); const selected = mesh(); assert.notEqual(selected, dragged);
  palette.selected = '#dc2626'; const themed = mesh(); assert.notEqual(themed, selected);
  const resized = api.webGlEdgeMesh(root, state, state.byId, palette, true, false, 2); assert.notEqual(resized, themed);
  state.edges = [{ ...edge, strokeWidth: 9 }]; assert.notEqual(mesh(), resized);
});
test('crossing fallback routes share authored order transitively while separate routes retain GPU geometry', () => {
  const { api, root, a, b, edge, state, palette } = runtime();
  const node = (id, x, y) => ({ ...a, id, x, y, el: api.graphVirtualElement('graph-node', {}, []) });
  const c = node('c', 200, 0), d = node('d', 200, 200), e = node('e', 150, 170), f = node('f', 250, 170), g = node('g', 500, 500), h = node('h', 600, 500);
  state.nodes.push(c, d, e, f, g, h); state.byId = new Map(state.nodes.map(node => [node.id, node]));
  edge.dashed = true; edge.dashPattern = [.000001, .000001];
  state.edges = [edge, ...[[c, d], [e, f], [g, h]].map(([source, target], i) => ({ ...edge, id: 'other' + i, source, target, dashed: false, el: api.graphVirtualElement('graph-edge', {}, []) }))];
  const mesh = api.webGlEdgeMesh(root, state, state.byId, palette, false, false, 1);
  assert.deepEqual(Array.from(mesh.fallbackEdges, item => item.edge.id), ['e', 'other0', 'other1']);
  assert.ok(mesh.positions.length > 0);
  assert.ok(mesh.positions.filter((_, i) => i % 2 === 0).every(x => x > 450));
});
test('mixed moving GPU primitives submit strokes and arrowheads in authored edge order', () => {
  const { api, root, edge, state, palette } = runtime();
  edge.strokeWidth = 12; edge.label = ''; edge.el.classList.add('cfx-graph-selected');
  const thin = { ...edge, id: 'thin', strokeWidth: 1, sourceArrow: true, strokeColor: '#ef4444', el: api.graphVirtualElement('graph-edge', { class: 'cfx-graph-neighborhood-related' }, []) };
  thin.el.classList.add('cfx-graph-neighborhood-related');
  state.edges = [edge, thin];
  const mesh = api.webGlEdgeMesh(root, state, state.byId, palette, true, true, .1);
  assert.deepEqual(Array.from(mesh.batches, batch => batch.mode), ['TRIANGLES', 'LINES', 'TRIANGLES']);
  assert.equal(mesh.batches[1].start, mesh.triangleVertices);
  assert.equal(mesh.batches.reduce((sum, batch) => sum + batch.count, 0), mesh.positions.length / 2);
});
test('labels choose contrast against their actual background in light and dark themes', () => {
  const { api, root, a } = runtime();
  a.el.setAttribute('data-node-background-color', '#2563eb');
  a.el.setAttribute('data-node-label-background-color', '#e0e7ff');
  for (const theme of ['light', 'dark']) {
    root.setAttribute('data-cfx-graph-theme-active', theme);
    const colors = api.graphReadableNodeColors(root, a.el, api.graphThemePalette(root));
    assert.ok(api.graphColorContrast(colors.label, '#e0e7ff') >= 4.5);
    assert.equal(colors.halo, '#e0e7ff');
  }
  a.el.setAttribute('data-node-card', 'true');
  const colors = api.graphReadableNodeColors(root, a.el, api.graphThemePalette(root));
  assert.ok(api.graphColorContrast(colors.label, '#2563eb') >= 4.5);
});
test('translucent label plates and cards choose contrast against the composited surface', () => {
  const { api, root, a } = runtime();
  root.setAttribute('data-cfx-graph-theme-active', 'dark');
  const palette = api.graphThemePalette(root);
  for (const card of [false, true]) {
    a.el.setAttribute('data-node-card', String(card));
    a.el.setAttribute('data-node-label-color', 'rgba(255,255,255,.1)');
    for (const surface of ['rgba(255,255,255,.1)', '#fff1', '#ffffff1a', 'rgb(100% 100% 100% / 10%)', 'rgba(255,255,255,0)']) {
      a.el.setAttribute(card ? 'data-node-background-color' : 'data-node-label-background-color', surface);
      const colors = api.graphReadableNodeColors(root, a.el, palette);
      assert.ok(api.graphColorContrast(colors.label, colors.halo) >= 4.5, surface);
      assert.ok(api.graphColorContrast(colors.secondary, colors.halo) >= 4.5, surface);
      assert.notEqual(colors.label, 'rgba(255,255,255,.1)');
      assert.notEqual(colors.label, '#0f172a');
      if (surface === 'rgba(255,255,255,.1)') assert.equal(colors.halo, 'rgb(35, 42, 54)');
      if (surface === 'rgba(255,255,255,0)') assert.equal(colors.halo, 'rgb(11, 18, 32)');
    }
  }
});

test('materialized virtual SVG card details use the same readable colours as physical and Canvas cards', () => {
  const { api, root, a } = runtime();
  const element = () => {
    const properties = {};
    return { children: [], attributes: {}, properties, classList: { add() {} },
      setAttribute(name, value) { this.attributes[name] = value; }, appendChild(child) { this.children.push(child); },
      style: { setProperty(name, value) { properties[name] = value; } } };
  };
  const group = element(), document = { createElementNS: element };
  root.setAttribute('data-cfx-graph-theme-active', 'dark');
  a.card = true; a.label = 'White card'; a.labelColor = '#ffffff';
  a.el.setAttribute('data-node-card', 'true'); a.el.setAttribute('data-node-background-color', '#ffffff');
  a.el.setAttribute('data-node-label-color', '#ffffff');
  api.appendExportedNodeDetails(document, group, a, root);
  const fill = group.children[0].attributes.style?.match(/(?:^|;)fill:([^;]+)/)?.[1] || group.properties['--cfx-node-label-adaptive'];
  assert.ok(api.graphColorContrast(fill, '#ffffff') >= 4.5);
  assert.equal(group.properties['--cfx-node-label-halo'], '#ffffff');
});
test('accelerated SVG export replaces the live scene once with routes before marks and details', () => {
  const { api, root, state } = runtime();
  const document = { createElementNS: () => element() };
  function element() {
    const node = api.graphVirtualElement('g', {}, []);
    node.ownerDocument = document; node.children = []; node.style = { setProperty() {} };
    node.appendChild = child => { child.remove(); child.parent = node; node.children.push(child); };
    node.append = (...children) => children.forEach(node.appendChild);
    node.remove = () => { if (node.parent) node.parent.children.splice(node.parent.children.indexOf(node), 1); node.parent = null; };
    node.querySelectorAll = selector => {
      const role = selector.match(/data-cfx-role="([^"]+)"/)?.[1], found = [];
      const visit = child => { if (child.getAttribute('data-cfx-role') === role) found.push(child); child.children.forEach(visit); };
      node.children.forEach(visit); return found;
    };
    node.querySelector = selector => node.querySelectorAll(selector)[0] || null;
    return node;
  }
  const viewport = element(), clone = { querySelector: () => viewport };
  root.setAttribute('data-cfx-graph-accelerated-markup', 'true'); root.dataset = { cfxGraphRendererActive: 'svg' };
  root.ownerDocument = document; root.querySelector = () => viewport;
  assert.ok(api.drawAcceleratedSvgRuntime(root, state));
  for (let repeat = 0; repeat < 2; repeat++) {
    api.materializeAcceleratedSvg(root, clone, state);
    assert.equal(viewport.querySelectorAll('[data-cfx-role="graph-edge"]').length, 1);
    assert.equal(viewport.querySelectorAll('[data-cfx-role="graph-node"]').length, 2);
    assert.equal(viewport.querySelectorAll('[data-cfx-role="graph-node-details"]').length, 2);
    const layers = viewport.children[0].children;
    assert.equal(layers[0].children[0].getAttribute('data-cfx-role'), 'graph-edge');
    assert.equal(layers[2].children[0].getAttribute('data-cfx-role'), 'graph-node');
    assert.equal(layers[3].children[0].getAttribute('data-cfx-role'), 'graph-node-details');
  }
  state.edges[0].el.classList.add('cfx-graph-neighborhood-hidden');
  api.materializeAcceleratedSvg(root, clone, state);
  assert.equal(viewport.querySelectorAll('[data-cfx-role="graph-edge-label"]').length, 0);
  state.edges[0].el.classList.remove('cfx-graph-neighborhood-hidden');
  api.materializeAcceleratedSvg(root, clone, state);
  assert.equal(viewport.querySelectorAll('[data-cfx-role="graph-edge-label"]').length, 1);
});
test('the shared node layer retains labels, badges and selected details through compact and moving states', () => {
  const { api, root, a } = runtime(), text = [], paths = [];
  a.label = 'Service'; a.badge = '2'; a.icon = 'X'; a.secondaryLabel = 'Detail';
  a.el.setAttribute('data-cfx-status', 'warning'); root.classList.add('cfx-graph-semantic-detail');
  const context = new Proxy({ measureText: value => ({ width: value.length * 7 }) }, {
    get(target, key) { return target[key] || ((...args) => { if (key === 'fillText') text.push(args[0]); if (key === 'arc') paths.push(args); }); },
    set(target, key, value) { target[key] = value; return true; }
  });
  api.drawCanvasNodes(context, root, [a], false, false);
  assert.ok(text.includes('Service') && text.includes('Detail') && text.includes('2') && text.includes('X'));
  assert.ok(paths.some(args => args[2] === 4.5));
  text.length = 0;
  api.drawCanvasNodes(context, root, [a], true, true); assert.equal(text.length, 0);
  a.el.classList.add('cfx-graph-selected');
  api.drawCanvasNodes(context, root, [a], true, true); assert.ok(text.includes('Service') && text.includes('Detail'));
});
test('GPU styles retain CSS alpha independently of neighbourhood opacity', () => {
  const { api } = runtime();
  for (const [style, rgb, alpha] of [['rgba(15, 118, 110, 0.5)', [15,118,110], .5], ['#0f766e80', [15,118,110], 128/255], ['#0f78', [0,255,119], 136/255], ['rgb(100% 0% 0% / 25%)', [255,0,0], .25], ['rgba(255,255,255,0)', [255,255,255], 0]]) {
    const color = api.webGlColor(style, .6);
    rgb.forEach((channel, index) => close(color[index], channel / 255)); close(color[3], .6 * alpha);
  }
});
test('homogeneous compact GPU marks retain node fill, border and status dots', () => {
  const { api, state, a, b, palette } = runtime();
  a.backgroundColor = '#7c3aed'; a.borderColor = '#c4b5fd'; a.el.setAttribute('data-cfx-status', 'warning');
  const points = api.webGlNodePoints(state, palette, true, false, 1, 1024);
  assert.equal(points.nodes.size, 2); assert.ok(points.nodes.has(a) && points.nodes.has(b));
  assert.equal(points.positions.length, 6);
  close(points.sizes[0], 25.5); close(points.colors[0], 124 / 255); close(points.strokes[0], 196 / 255);
  close(points.positions[4], a.x - a.size * .8); close(points.sizes[4], 11);
  assert.equal(api.webGlNodePoints(state, palette, true, false, 3, 64).nodes.size, 0);
  a.icon = 'X'; assert.ok(!api.webGlNodePoints(state, palette, true, false, 1, 1024).nodes.has(a));
  assert.equal(api.webGlNodePoints(state, palette, true, true, 1, 1024).nodes.size, 2);
});
test('transitive mixed overlaps share one ordered pass while distant circles retain GPU batching', () => {
  const { api, root, state, a, b, palette } = runtime();
  a.shape = 'box'; a.x = 100; b.x = 125;
  const c = { ...b, id:'c', x:150, el:api.graphVirtualElement('graph-node', {}, []) }, d = { ...c, id:'d', x:300, el:api.graphVirtualElement('graph-node', {}, []) };
  state.nodes = [a,b,c,d]; state.nodes.forEach((node,index) => node.backgroundColor = ['#ef4444','#2563eb','#22c55e','#7c3aed'][index]);
  const points = api.webGlNodePoints(state, palette, true, false, 1, 1024), paints = [];
  assert.equal(points.nodes.size, 1); assert.ok(points.nodes.has(d));
  close(points.positions[0], d.x); close(points.colors[0], 124/255);
  const context = new Proxy({}, {
    get(target, key) { return target[key] || (() => { if (key === 'fill') paints.push(target.fillStyle); }); },
    set(target, key, value) { target[key] = value; return true; }
  });
  api.drawCanvasNodes(context, root, state.nodes, true, false, points.nodes);
  assert.deepEqual(paints, ['#ef4444','#2563eb','#22c55e']);
});
test('selected polygon miter tips retain body order with a later circle at DPR two', () => {
  const { api, root, state, a, b, palette } = runtime();
  a.shape = 'star'; a.x = b.x = 100; a.y = 100; a.size = 40; b.y = 32; b.size = 4;
  a.backgroundColor = '#ef4444'; b.backgroundColor = '#2563eb'; a.el.classList.add('cfx-graph-selected');
  const points = api.webGlNodePoints(state, palette, true, false, 2, 512), paints = [];
  for (let index = 0; index < points.colors.length; index += 4) paints.push('#' + Array.from(points.colors.slice(index, index + 3), channel => Math.round(channel * 255).toString(16).padStart(2,'0')).join(''));
  const context = new Proxy({}, {
    get(target, key) { return target[key] || (() => { if (key === 'fill') paints.push(target.fillStyle); }); },
    set(target, key, value) { target[key] = value; return true; }
  });
  api.drawCanvasNodes(context, root, state.nodes, true, false, points.nodes);
  assert.deepEqual(paints, ['#ef4444','#2563eb']);
});
test('mixed and driver-limited marks preserve authored body order before all status details', () => {
  const { api, root, state, a, b, palette } = runtime();
  a.backgroundColor = '#ef4444'; b.backgroundColor = '#2563eb';
  a.el.setAttribute('data-cfx-status', 'warning'); b.el.setAttribute('data-cfx-status', 'healthy');
  a.x = b.x = 100; a.y = b.y = 100;
  for (const [shape, moving, limit] of [['box', false, 1024], ['diamond', true, 1024], ['circle', false, 100]]) {
    a.shape = shape; a.size = shape === 'circle' ? 40 : 12;
    for (const nodes of [[a,b], [b,a]]) {
      state.nodes = nodes;
      const points = api.webGlNodePoints(state, palette, true, moving, 1, limit);
      const paints = Array.from(points.colors.filter((_, index) => index % 4 === 0), (_, index) => Array.from(points.colors.slice(index * 4, index * 4 + 3), channel => Math.round(channel * 255)));
      const context = new Proxy({}, {
        get(target, key) { return target[key] || (() => { if (key === 'fill') paints.push(target.fillStyle); }); },
        set(target, key, value) { target[key] = value; return true; }
      });
      api.drawCanvasNodes(context, root, nodes, true, moving, points.nodes);
      assert.deepEqual(paints, nodes.map(node => node.backgroundColor).concat(nodes.map(node => node === a ? '#f59e0b' : '#22c55e')));
    }
  }
});
test('context loss and restoration retain the canonical graph state through Canvas fallback', () => {
  const script = fs.readFileSync(path.join(assets, 'graph-explorer.04-webgl.js'), 'utf8');
  const handlers = new Map(), state = {}, paints = [];
  const canvas = { addEventListener: (name, action) => handlers.set(name, action) };
  const root = { dataset: { cfxGraphRendererActive: 'webgl' }, __cfxGraphState: state, querySelector: () => canvas, getAttribute: () => 'webgl' };
  const bind = new Function('setGraphRenderer', 'drawCanvas', 'graphState', 'hasFeature', 'applyLod', 'attr', 'webGlAvailable',
    script.slice(script.indexOf('  const bindWebGlHitTesting')) + '\nreturn bindWebGlHitTesting;')(
    (target, renderer) => target.dataset.cfxGraphRendererActive = renderer,
    (target, actual) => { assert.equal(actual, state); paints.push(target.dataset.cfxGraphRendererActive); },
    () => { throw new Error('Discarded live graph state'); }, () => false, () => {}, (target, name) => target.getAttribute(name), () => true);
  bind(root);
  let prevented = false;
  handlers.get('webglcontextlost')({ preventDefault: () => prevented = true });
  assert.ok(prevented); assert.equal(root.__cfxGraphWebGl, false); assert.equal(root.dataset.cfxGraphRendererActive, 'canvas');
  handlers.get('webglcontextrestored')();
  assert.deepEqual(paints, ['canvas', 'webgl']); assert.equal(root.__cfxGraphState, state);
});

test('Canvas and GPU draw both directed arrowheads and retain explicit edge paint', () => {
  const { api, root, edge, state, palette } = runtime(), fills = [], strokes = [], dashes = [];
  edge.sourceArrow = true; edge.directed = true; edge.targetArrow = false; edge.dashed = true;
  const context = new Proxy({}, {
    get(target, key) { return target[key] || ((...args) => {
      if (key === 'stroke') strokes.push({ width: target.lineWidth, color: target.strokeStyle });
      if (key === 'fill') fills.push(target.fillStyle);
      if (key === 'setLineDash') dashes.push(Array.from(args[0]));
    }); },
    set(target, key, value) { target[key] = value; return true; }
  });
  api.drawCanvasEdge(context, root, edge, api.graphEdgePaint(root, edge, state.byId, palette, false, false), palette);
  assert.equal(fills.length, 2); assert.ok(fills.every(color => color === '#0f766e'));
  assert.deepEqual(strokes[0], { width: 5, color: '#0f766e' });
  assert.deepEqual(dashes, [[9, 5], []]);
  const withArrows = api.webGlEdgeMesh(root, state, state.byId, palette, false, false, 1).positions.length;
  const withoutArrows = api.webGlEdgeMesh(root, state, state.byId, palette, false, true, 1).positions.length;
  assert.equal(withArrows - withoutArrows, 12);
});
test('Canvas marks render serialized fills, borders and selected emphasis', () => {
  const { api, root, a } = runtime(), paints = [];
  a.backgroundColor = '#7c3aed'; a.borderColor = '#c4b5fd';
  const context = new Proxy({}, {
    get(target, key) { return target[key] || (() => {
      if (key === 'fill') paints.push(target.fillStyle);
      if (key === 'stroke') paints.push([target.strokeStyle, target.lineWidth]);
    }); },
    set(target, key, value) { target[key] = value; return true; }
  });
  api.drawNodeMark(context, a, false, false, root, false);
  assert.deepEqual(paints, ['#7c3aed', ['#c4b5fd', 3]]);
  paints.length = 0;
  api.drawNodeMark(context, a, true, true, root, false);
  assert.deepEqual(paints, ['#7c3aed', ['#f59e0b', 5]]);
});

test('theme defaults stay derived while generic authored card colours survive theme changes', () => {
  const { api, root, a, b, state } = runtime(), nodes = state.nodes;
  for (const node of nodes) { node.card = true; node.shape = 'box'; node.el.setAttribute('data-node-card', 'true'); }
  b.el.setAttribute('data-node-background-color', '#7c3aed');
  a.vx = 3; a.x = 140;
  for (const [theme, authored] of [['dark', '#7c3aed'], ['light', '#ffffff'], ['dark', '#ffffff']]) {
    b.el.setAttribute('data-node-background-color', authored);
    root.setAttribute('data-cfx-graph-theme-active', theme);
    root.__cfxGraphEdgeMesh = {};
    api.syncGraphThemeState(root, state);
    assert.equal(state.nodes, nodes); assert.equal(state.nodes[0], a); assert.equal(a.x, 140); assert.equal(a.vx, 3);
    assert.equal(a.backgroundColor, api.graphThemePalette(root).card);
    assert.equal(a.el.getAttribute('data-node-background-color'), null);
    assert.equal(b.el.getAttribute('data-node-background-color'), authored);
    assert.equal(b.backgroundColor, authored); assert.equal(root.__cfxGraphEdgeMesh, null);
    for (const node of nodes) {
      const colours = api.graphReadableNodeColors(root, node.el, api.graphThemePalette(root));
      assert.equal(colours.halo, node.backgroundColor);
      assert.ok(api.graphColorContrast(colours.label, node.backgroundColor) >= 4.5);
    }
  }
});

test('Canvas retains visible dash patterns and bounds fully covered subpixel round dashes at physical zoom', () => {
  const { api, root, edge, state, palette } = runtime(), strokes = [];
  let scale = 1, current = [];
  const context = new Proxy({ lineCap: 'round', getTransform: () => ({ a: scale, b: 0, c: 0, d: scale }),
    setLineDash: value => current = Array.from(value), stroke: () => strokes.push(current) }, {
    get: (target, key) => target[key] || (() => {}), set: (target, key, value) => (target[key] = value, true)
  });
  edge.dashed = true; edge.label = '';
  const draw = () => api.drawCanvasEdge(context, root, edge, api.graphEdgePaint(root, edge, state.byId, palette, false, false), palette);
  edge.dashPattern = [.0001, .0001]; draw();
  edge.dashPattern = [9, 5]; draw();
  edge.dashPattern = [.1, .1]; scale = 4; draw();
  edge.dashPattern = [.1]; scale = 3; draw();
  edge.dashPattern = [.0001, .0001]; context.lineCap = 'butt'; draw();
  assert.deepEqual(strokes, [[], [9, 5], [.1, .1], [.1], [.0001, .0001]]);
});
test('renderer switches transfer surface focus and preserve focus on host controls', () => {
  const { api, root } = runtime(), doc = { activeElement: null };
  const surfaces = ['canvas', 'webgl', 'scene'].map(name => api.graphVirtualElement('graph-' + name, {}, []));
  surfaces.forEach(element => element.focus = () => doc.activeElement = element);
  root.dataset = { cfxGraphRendererActive: 'webgl' }; root.ownerDocument = doc;
  root.setAttribute('data-cfx-graph-renderer', 'webgl'); root.setAttribute('data-cfx-graph-features', 'Selection');
  root.setAttribute('data-cfx-graph-accelerated-markup', 'true');
  root.contains = element => surfaces.includes(element);
  root.querySelectorAll = selector => surfaces.filter(element => api.graphVirtualMatches(element, selector));
  root.querySelector = selector => root.querySelectorAll(selector)[0] || null;
  doc.activeElement = surfaces[1];
  for (const [renderer, target] of [['canvas', surfaces[0]], ['svg', surfaces[2]], ['webgl', surfaces[1]]]) {
    api.setGraphRenderer(root, renderer);
    assert.equal(doc.activeElement, target); assert.equal(target.getAttribute('aria-hidden'), 'false');
    assert.equal(target.getAttribute('tabindex'), '0');
  }
  const toolbar = api.graphVirtualElement('graph-toolbar', {}, []); doc.activeElement = toolbar;
  api.setGraphRenderer(root, 'canvas'); assert.equal(doc.activeElement, toolbar);
});

test('dense motion retains subpixel stroke coverage and restores precise ribbons on zoom or settlement', () => {
  const { api, root, edge, state, palette } = runtime();
  edge.dashed = false; edge.strokeWidth = 5;
  const moving = api.webGlEdgeMesh(root, state, state.byId, palette, true, true, .1);
  assert.equal(moving.positions.length, 4); assert.equal(moving.triangleVertices, 0); assert.equal(moving.lineVertices, 2);
  close(moving.colors[3], .28 * 5 * .1);
  edge.strokeWidth = 2.5; state.edges = [{ ...edge }];
  const narrower = api.webGlEdgeMesh(root, state, state.byId, palette, true, true, .1);
  close(narrower.colors[3], moving.colors[3] / 2);
  edge.strokeWidth = 5; state.edges = [edge];
  root.setAttribute('data-cfx-viewport-scale', '4');
  const zoomed = api.webGlEdgeMesh(root, state, state.byId, palette, true, true, .1);
  assert.notEqual(zoomed, moving); assert.ok(zoomed.positions.length > moving.positions.length);
  root.setAttribute('data-cfx-viewport-scale', '1');
  const settled = api.webGlEdgeMesh(root, state, state.byId, palette, true, false, .1);
  assert.ok(settled.positions.length > moving.positions.length);
});
