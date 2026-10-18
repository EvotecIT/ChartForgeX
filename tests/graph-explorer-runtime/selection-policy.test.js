const assert = require('node:assert/strict');
const test = require('node:test');
const { adapterLifetimes } = require('../helpers/adapter-lifetime');

const { graphRuntimeSource } = require('../helpers/graph-runtime-source');
const source = graphRuntimeSource('return { graphVirtualElement, graphState, selectedItems, bindGraphItemSelection };');

function fixture(features = 'Selection') {
  const lifetimes = adapterLifetimes();
  test.after(() => lifetimes.dispose());
  const window = lifetimes.window;
  const events = [];
  const announcer = { textContent: '' };
  const tooltip = { hidden: false };
  const runtime = new Function('document', 'window', 'CustomEvent', source)(
    { readyState: 'loading', addEventListener() {} }, window,
    class { constructor(type, options) { this.type = type; this.detail = options.detail; } });
  const node = runtime.graphVirtualElement('graph-node', { 'data-node-id': 'shared' });
  const peer = runtime.graphVirtualElement('graph-node', { 'data-node-id': 'peer' });
  const edge = runtime.graphVirtualElement('graph-edge', {
    'data-edge-id': 'shared', 'data-source-node-id': 'shared', 'data-target-node-id': 'peer'
  });
  const cluster = runtime.graphVirtualElement('graph-cluster', { 'data-cluster-id': 'group' });
  const root = runtime.graphVirtualElement('graph-root', { 'data-cfx-graph-id': 'fixture', 'data-cfx-graph-features': features });
  root.dataset = {};
  root.__cfxGraphVirtualItems = [node, peer, edge, cluster];
  root.matches = selector => selector === '.cfx-graph-explorer';
  root.querySelectorAll = () => [];
  root.querySelector = selector => selector.includes('announcer') ? announcer : selector === '.cfx-graph-tooltip' ? tooltip : null;
  root.dispatchEvent = event => { events.push(event); return true; };
  lifetimes.mount(root);
  root.__cfxGraphState = runtime.graphState(root);
  const activate = (item, type, modifiers = {}) => {
    const handlers = new Map();
    // Ordinary clicks target a DOM-backed mark; model-only items use the shared accelerated surface.
    item.__cfxVirtual = false;
    item.addEventListener = (name, handler) => handlers.set(name, handler);
    runtime.bindGraphItemSelection(root, item);
    const event = { key: type === 'keydown' ? ' ' : undefined, preventDefault() {}, ...modifiers };
    handlers.get(type)(event);
  };
  return { root, node, peer, edge, cluster, events, announcer, tooltip, activate, bind: item => runtime.bindGraphItemSelection(root, item), api: window.ChartForgeXGraphExplorer,
    selected: () => runtime.selectedItems(root).map(({ role, id }) => ({ role, id })) };
}

const identity = (role, id) => ({ role, id });

test('host selection replaces with one stable role-qualified match when MultiSelection is disabled', () => {
  const { api, root, node, peer, edge, events, announcer, tooltip, selected, bind } = fixture();
  let subscriptions = 0;
  node.addEventListener = () => subscriptions++;
  bind(node);
  assert.equal(subscriptions, 0, 'Model-only virtual nodes do not own DOM event subscriptions');
  // Graph order gives a deterministic primary even when a host supplies several identities.
  const replacement = api.select(root, [identity('graph-edge', 'shared'), identity('graph-node', 'peer'), identity('graph-node', 'shared')]);
  assert.deepEqual(replacement.map(({ role, id }) => ({ role, id })), [identity('graph-node', 'shared')]);
  assert.deepEqual(selected(), [identity('graph-node', 'shared')]);
  assert.equal(events.length, 0);

  const addition = api.select(root, [identity('graph-node', 'peer'), identity('graph-edge', 'shared')], { mode: 'add', emit: true });
  assert.deepEqual(addition.map(({ role, id }) => ({ role, id })), [identity('graph-node', 'peer')]);
  assert.deepEqual(selected(), [identity('graph-node', 'peer')]);
  assert.equal(node.getAttribute('aria-pressed'), 'false');
  assert.equal(peer.getAttribute('aria-pressed'), 'true');
  assert.equal(edge.getAttribute('aria-pressed'), 'false');
  assert.equal(root.dataset.cfxGraphSelectionCount, '1');
  assert.equal(root.dataset.cfxGraphSelectionPrimary, 'peer');
  assert.equal(announcer.textContent, '1 items selected.');
  assert.equal(tooltip.hidden, true);
  assert.equal(events.length, 1);
  assert.equal(events[0].type, 'cfxgraphselection');
  assert.equal(events[0].detail.count, 1);
  assert.deepEqual(events[0].detail.items.map(({ role, id }) => ({ role, id })), selected());
});

test('host add follows ordinary modified click and keyboard replacement in single-selection mode', () => {
  const { api, root, node, peer, edge, activate, selected } = fixture();
  activate(node, 'click');
  activate(peer, 'click', { ctrlKey: true });
  assert.deepEqual(selected(), [identity('graph-node', 'peer')]);
  activate(edge, 'keydown', { shiftKey: true });
  assert.deepEqual(selected(), [identity('graph-edge', 'shared')]);
  api.select(root, [identity('graph-node', 'peer')], { mode: 'add' });
  assert.deepEqual(selected(), [identity('graph-node', 'peer')]);
  // A repeated additive request follows an ordinary activation rather than toggling off.
  api.select(root, [identity('graph-node', 'peer')], { mode: 'add' });
  assert.deepEqual(selected(), [identity('graph-node', 'peer')]);
});

test('single-selection removal and unmatched addition preserve unrelated selection and independent highlight', () => {
  const { api, root, node, peer, selected } = fixture();
  api.select(root, [identity('graph-node', 'shared')]);
  api.highlight(root, [identity('graph-node', 'peer')], { dim: true });
  api.select(root, [identity('graph-node', 'missing')], { mode: 'add' });
  assert.deepEqual(selected(), [identity('graph-node', 'shared')]);
  api.select(root, [], { mode: 'add' });
  assert.deepEqual(selected(), [identity('graph-node', 'shared')]);
  api.select(root, [identity('graph-edge', 'shared')], { mode: 'remove' });
  assert.deepEqual(selected(), [identity('graph-node', 'shared')]);
  api.select(root, [identity('graph-node', 'shared')], { mode: 'remove' });
  assert.deepEqual(selected(), []);
  api.select(root, [identity('graph-node', 'shared')]);
  assert.deepEqual(api.clearSelection(root), []);
  assert.equal(peer.getAttribute('data-cfx-highlighted'), 'true');
  assert.equal(node.classList.contains('cfx-graph-link-dim'), true);
  api.clearHighlight(root);
  assert.equal(peer.getAttribute('data-cfx-highlighted'), null);
  assert.equal(node.classList.contains('cfx-graph-link-dim'), false);
});

test('MultiSelection permits role-qualified node edge and cluster sets with additive and removal commands', () => {
  const { api, root, selected } = fixture('Selection,MultiSelection');
  api.select(root, [identity('graph-node', 'shared'), identity('graph-edge', 'shared'), identity('graph-cluster', 'group')]);
  assert.deepEqual(selected(), [identity('graph-node', 'shared'), identity('graph-edge', 'shared'), identity('graph-cluster', 'group')]);
  api.select(root, [identity('graph-node', 'peer')], { mode: 'add' });
  assert.deepEqual(selected(), [identity('graph-node', 'shared'), identity('graph-node', 'peer'), identity('graph-edge', 'shared'), identity('graph-cluster', 'group')]);
  api.select(root, [identity('graph-edge', 'shared')], { mode: 'remove' });
  assert.deepEqual(selected(), [identity('graph-node', 'shared'), identity('graph-node', 'peer'), identity('graph-cluster', 'group')]);
});

test('disabled Selection rejects host commands while independent highlight accepts several identities', () => {
  const { api, root, node, peer, edge, events, selected } = fixture('');
  assert.equal(api.select(root, [identity('graph-node', 'shared')], { emit: true }), false);
  assert.equal(api.clearSelection(root, { emit: true }), false);
  assert.deepEqual(selected(), []);
  const highlighted = api.highlight(root, [identity('graph-node', 'shared'), identity('graph-edge', 'shared')], { dim: true });
  assert.deepEqual(highlighted.map(({ role, id }) => ({ role, id })), [identity('graph-node', 'shared'), identity('graph-edge', 'shared')]);
  assert.equal(node.getAttribute('data-cfx-highlighted'), 'true');
  assert.equal(edge.getAttribute('data-cfx-highlighted'), 'true');
  assert.equal(peer.classList.contains('cfx-graph-link-dim'), true);
  assert.deepEqual(selected(), []);
  assert.equal(events.length, 0);
});
