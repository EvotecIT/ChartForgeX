const assert = require('node:assert/strict');
const test = require('node:test');
const { adapterLifetimes } = require('../helpers/adapter-lifetime');
const { graphRuntimeSource } = require('../helpers/graph-runtime-source');
const source = graphRuntimeSource('return { graphVirtualElement, graphState, applyLayout, selectedItems, bindCanvasHitTesting };');

test('Canvas applies distinct clicks, clear/reselect and modified toggles without dropping rapid input', t => {
  const lifetimes = adapterLifetimes();
  t.after(() => lifetimes.dispose());
  let now = 1000;
  const runtime = new Function('document', 'window', 'CustomEvent', 'Date', source)(
    { readyState: 'loading', addEventListener() {} }, lifetimes.window,
    class { constructor(type, options) { this.type = type; this.detail = options.detail; } }, { now: () => now });
  const handlers = new Map(), rect = { left: 0, top: 0, width: 960, height: 560 };
  const canvas = { width: 960, height: 560, getBoundingClientRect: () => rect, getContext: () => null,
    addEventListener: (name, handler) => handlers.set(name, handler), removeEventListener() {} };
  const svg = { getAttribute: name => name === 'viewBox' ? '0 0 960 560' : '', getBoundingClientRect: () => rect };
  const root = runtime.graphVirtualElement('graph-root', {
    'data-cfx-graph-id': 'rapid-input', 'data-cfx-graph-features': 'Selection,MultiSelection'
  });
  root.dataset = { cfxGraphRendererActive: 'canvas' };
  root.matches = selector => selector === '.cfx-graph-explorer';
  root.classList.add('cfx-graph-render-canvas');
  root.ownerDocument = { activeElement: null };
  const nodes = [['a', 220], ['b', 580]].map(([id, x]) => runtime.graphVirtualElement('graph-node', {
    'data-node-id': id, 'data-node-label': id, 'data-node-size': '30', 'data-node-x': String(x), 'data-node-y': '180'
  }));
  root.__cfxGraphVirtualItems = nodes;
  root.querySelectorAll = () => [];
  root.querySelector = selector => selector.includes('graph-scene') ? svg : selector.includes('graph-canvas') ? canvas : null;
  root.dispatchEvent = () => true;
  root.contains = () => false;
  lifetimes.mount(root);
  runtime.applyLayout(root, runtime.graphState(root));
  runtime.bindCanvasHitTesting(root);
  const selected = () => runtime.selectedItems(root).map(item => item.id);
  const click = (id, ctrlKey = false) => {
    const node = nodes.find(item => item.getAttribute('data-node-id') === id);
    handlers.get('click')({ clientX: Number(node.getAttribute('data-node-x')), clientY: 180, ctrlKey, preventDefault() {} });
    now += 50;
  };
  click('a');
  assert.deepEqual(selected(), ['a']);
  click('b');
  assert.deepEqual(selected(), ['b'], 'A later distinct click must replace the earlier selection');
  lifetimes.window.ChartForgeXGraphExplorer.clearSelection(root);
  assert.deepEqual(selected(), []);
  click('a');
  assert.deepEqual(selected(), ['a'], 'A click after Clear must select the visible node immediately');
  click('a', true);
  assert.deepEqual(selected(), [], 'A modified repeated click must toggle the current node off');
});
