const assert = require('node:assert/strict');
const test = require('node:test');
const { graphRuntimeSource } = require('../helpers/graph-runtime-source');

// Minimap bounds run during camera/physics updates; font and SVG layout reads
// belong to explicit fitting rather than the interactive paint loop.
test('ordinary overview bounds avoid painted-text reads while fitting includes them', () => {
  let measurements = 0;
  const source = graphRuntimeSource('return { graphVirtualElement, contentBounds };');
  const runtime = new Function('document', 'window', source)({ readyState: 'loading', addEventListener() {},
    createElement: () => ({ getContext: () => ({ measureText: () => { measurements++; return { width: 900 }; } }) }) }, {});
  const root = runtime.graphVirtualElement('graph-root', {});
  const el = runtime.graphVirtualElement('graph-node', { 'data-node-id': 'service', 'data-node-label': 'Complete service caption' });
  const node = { el, id: 'service', label: 'Complete service caption', shape: 'circle', size: 8, x: 480, y: 280,
    detailsEl: { getBBox() { measurements++; return { x: -450, y: 16, width: 900, height: 15 }; } } };
  const state = { nodes: [node], edges: [], clusters: [], byId: new Map([['service', node]]) };
  const overview = runtime.contentBounds(root, state);
  assert.equal(measurements, 0);
  const fitted = runtime.contentBounds(root, state, true);
  assert.ok(measurements > 0);
  assert.ok(fitted.maxX - fitted.minX >= 900);
  assert.ok(overview.maxX - overview.minX < fitted.maxX - fitted.minX);
});
