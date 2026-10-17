const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const fragment = name => fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source', name), 'utf8');

function element(classes = [], attributes = {}) {
  const values = new Set(classes);
  return {
    nodeType: 1, dataset: {}, parent: null, children: [],
    classList: {
      contains: value => values.has(value),
      toggle: (value, enabled) => enabled ? values.add(value) : values.delete(value),
      add: value => values.add(value), remove: value => values.delete(value)
    },
    getAttribute: name => name === 'class' ? [...values].join(' ') : attributes[name] ?? null,
    setAttribute: (name, value) => { attributes[name] = value; },
    contains(node) { return node === this || this.children.some(child => child.contains(node)); },
    matches(selector) { return selector.split(',').some(value => value.startsWith('.')
      ? values.has(value.slice(1)) : this.selector === value); },
    closest(selector) { return this.matches(selector) ? this : this.parent?.closest(selector) || null; }
  };
}
function append(parent, child) { parent.children.push(child); child.parent = parent; return child; }
const highlight = new Function(fragment('06-highlight-paint.js') + '\nreturn syncCartesianLinkedPaint;')();
const availability = new Function(fragment('30-keyboard-navigation.js') + '\nreturn keyboardAvailabilityMutation;')();

test('Cartesian legends follow foreground observations while only connected geometry gains series emphasis', () => {
  for (const kind of ['line', 'area', 'stepline', 'steparea', 'bar', 'scatter']) {
    const connected = !['bar', 'scatter'].includes(kind);
    const owner = element(); owner.selector = '[data-cfx-role="series"]'; owner.dataset.cfxKind = kind; owner.dataset.cfxSeries = '0';
    const selected = append(owner, element(['cfx-selected']));
    const sibling = append(owner, element(['cfx-selection-dim']));
    const otherOwner = element(); otherOwner.selector = owner.selector; otherOwner.dataset.cfxKind = kind; otherOwner.dataset.cfxSeries = '1';
    const other = append(otherOwner, element(['cfx-selection-dim']));
    const legend = element(), otherLegend = element(), unrelatedLegend = element();
    legend.dataset.cfxSeries = '0'; otherLegend.dataset.cfxSeries = '1'; unrelatedLegend.dataset.cfxSeries = '2';
    const root = { querySelectorAll: selector => selector.includes('legend-item') ? [legend, otherLegend, unrelatedLegend] : [owner, otherOwner] };
    highlight(root, [selected, sibling, other]);
    assert.equal(owner.classList.contains('cfx-linked-series-active'), connected, kind);
    assert.equal(owner.classList.contains('cfx-linked-series-dim'), false, kind);
    assert.equal(otherOwner.classList.contains('cfx-linked-series-dim'), connected, kind);
    assert.equal(legend.classList.contains('cfx-linked-series-active'), true, kind);
    assert.equal(otherLegend.classList.contains('cfx-linked-series-dim'), true, kind);
    assert.equal(unrelatedLegend.classList.contains('cfx-linked-series-dim'), false, kind);
    assert.equal(legend.classList.contains('cfx-selected'), false, kind);
    assert.equal(sibling.classList.contains('cfx-selected'), false, kind);
    selected.classList.remove('cfx-selected');
    selected.classList.add('cfx-highlighted');
    highlight(root, [selected, sibling, other]);
    assert.equal(owner.classList.contains('cfx-linked-series-active'), connected, kind);
    assert.equal(legend.classList.contains('cfx-linked-series-active'), true, kind);
    owner.classList.add('cfx-series-isolated-out');
    legend.setAttribute('data-cfx-muted', 'true');
    highlight(root, [selected, sibling, other]);
    assert.equal(legend.classList.contains('cfx-series-isolated-out'), true, kind);
    assert.equal(legend.getAttribute('data-cfx-muted'), 'true', kind);
    assert.equal(selected.classList.contains('cfx-highlighted'), true, kind);
    owner.classList.remove('cfx-series-isolated-out');
    highlight(root, [selected, sibling, other]);
    assert.equal(legend.classList.contains('cfx-series-isolated-out'), false, kind);
    selected.classList.remove('cfx-highlighted');
    sibling.classList.remove('cfx-selection-dim');
    other.classList.remove('cfx-selection-dim');
    highlight(root, [selected, sibling, other]);
    assert.equal(owner.classList.contains('cfx-linked-series-active'), false, kind);
    assert.equal(otherOwner.classList.contains('cfx-linked-series-dim'), false, kind);
    assert.equal(legend.classList.contains('cfx-linked-series-active'), false, kind);
    assert.equal(otherLegend.classList.contains('cfx-linked-series-dim'), false, kind);
  }
});

test('Cartesian point legends retain their own foreground and isolation without adopting selected peers', () => {
  const owner = element(); owner.selector = '[data-cfx-role="series"]'; owner.dataset.cfxKind = 'bar'; owner.dataset.cfxSeries = '0';
  const selected = append(owner, element(['cfx-selected'])); selected.dataset.cfxPoint = '0';
  const sibling = append(owner, element(['cfx-selection-dim', 'cfx-series-isolated-out'])); sibling.dataset.cfxPoint = '1';
  const first = element(), second = element();
  first.dataset.cfxSeries = second.dataset.cfxSeries = '0'; first.dataset.cfxPoint = '0'; second.dataset.cfxPoint = '1';
  const root = { querySelectorAll: selector => selector.includes('legend-item') ? [first, second] : [owner] };
  highlight(root, [selected, sibling]);
  assert.equal(first.classList.contains('cfx-linked-series-active'), true);
  assert.equal(second.classList.contains('cfx-linked-series-active'), false);
  assert.equal(second.classList.contains('cfx-linked-series-dim'), true);
  assert.equal(second.classList.contains('cfx-series-isolated-out'), true);
  assert.equal(first.classList.contains('cfx-selected'), false);
  sibling.classList.remove('cfx-series-isolated-out'); selected.classList.remove('cfx-selected'); sibling.classList.remove('cfx-selection-dim');
  highlight(root, [selected, sibling]);
  assert.equal(first.classList.contains('cfx-linked-series-active'), false);
  assert.equal(second.classList.contains('cfx-linked-series-dim'), false);
  assert.equal(second.classList.contains('cfx-series-isolated-out'), false);
});

function mutationFixture() {
  const host = element(['host']), root = append(host, element(['cfx-interactive-chart']));
  const stage = append(root, element(['cfx-stage'])), svg = append(stage, element());
  const mark = append(svg, element(['native-mark', 'cfx-hovered']));
  stage.querySelector = () => svg;
  return { host, root, stage, svg, mark, ancestry: { nodes: [root, host] } };
}
test('keyboard availability ignores adapter emphasis but observes simultaneous host classes and native geometry', () => {
  const { host, stage, mark, ancestry } = mutationFixture();
  const classChange = { type: 'attributes', attributeName: 'class', target: mark, oldValue: 'native-mark' };
  assert.equal(availability(classChange, stage, ancestry), false);
  mark.classList.add('hidden-by-host');
  assert.equal(availability(classChange, stage, ancestry), true);
  assert.equal(availability({ type: 'attributes', attributeName: 'width', target: mark }, stage, ancestry), true);
  assert.equal(availability({ type: 'attributes', attributeName: 'style', target: host }, stage, ancestry), true);
});
test('keyboard availability ignores brush and guide overlays while observing native replacement and host removal', () => {
  const { host, root, stage, svg, ancestry } = mutationFixture();
  const guide = append(stage, element(['cfx-crosshair']));
  assert.equal(availability({ type: 'attributes', attributeName: 'style', target: guide }, stage, ancestry), false);
  assert.equal(availability({ type: 'attributes', attributeName: 'hidden', target: guide }, stage, ancestry), false);
  const label = append(guide, element());
  assert.equal(availability({ type: 'childList', target: guide, addedNodes: [label], removedNodes: [] }, stage, ancestry), false);
  const native = element();
  assert.equal(availability({ type: 'childList', target: svg, addedNodes: [native], removedNodes: [] }, stage, ancestry), true);
  const outer = element();
  assert.equal(availability({ type: 'childList', target: outer, addedNodes: [], removedNodes: [host] }, stage, ancestry), true);
  assert.equal(root.parent, host);
});
test('keyboard availability retains native opacity and style changes outside owned highlight paint', () => {
  const { stage, svg, ancestry } = mutationFixture();
  const paint = append(svg, element([], { style: '--cfx-highlight-opacity: .3;' }));
  paint.selector = '[data-cfx-highlight-paint]';
  const change = { type: 'attributes', attributeName: 'style', target: paint, oldValue: '--cfx-highlight-opacity: 1;' };
  assert.equal(availability(change, stage, ancestry), false);
  paint.setAttribute('style', '--cfx-highlight-opacity: .3; display: none;');
  assert.equal(availability(change, stage, ancestry), true);
  assert.equal(availability({ ...change, attributeName: 'opacity' }, stage, ancestry), true);
  const outline = append(paint, element()); outline.selector = '[data-cfx-highlight-outline]';
  assert.equal(availability({ type: 'childList', target: paint, addedNodes: [outline], removedNodes: [] }, stage, ancestry), false);
  assert.equal(availability({ type: 'attributes', attributeName: 'd', target: outline }, stage, ancestry), false);
});

test('keyboard native availability changes coalesce to one frame and disposal cancels queued work', () => {
  const { root, stage, mark, ancestry } = mutationFixture();
  root.isConnected = true; root.querySelector = () => stage;
  let changed, frame, refreshes = 0, disconnected = 0, canceled = 0, cleanup;
  const window = { removeEventListener() {} };
  class Observer { constructor(callback) { changed = callback; } observe() {} disconnect() { disconnected++; } }
  const bind = new Function('MutationObserver', 'Element', 'window', 'chartLifetimes', 'composedAncestry', 'paintAvailabilityAttributes',
    'listenChart', 'ownChartWork', 'requestAnimationFrame', 'cancelAnimationFrame', 'refreshRootBranchHighlightAvailability', 'hasFeature',
    fragment('30-keyboard-navigation.js') + '\nreturn bindKeyboardNavigationAvailability;')(
      Observer, Object, window, { get: () => ({ disposed: false }) }, () => ({ ...ancestry, trees: [] }), ['class', 'width'],
      () => {}, (_root, release) => { cleanup = release; }, callback => { frame = callback; return 1; }, () => { canceled++; },
      () => { refreshes++; }, () => false);
  bind(root);
  const nativeChange = [{ type: 'attributes', target: mark, attributeName: 'width' }];
  changed(nativeChange); const firstFrame = frame;
  changed(nativeChange); changed(nativeChange);
  assert.equal(frame, firstFrame);
  assert.equal(refreshes, 0);
  frame();
  assert.equal(refreshes, 1);
  changed(nativeChange); cleanup();
  assert.equal(canceled, 1);
  assert.ok(disconnected >= 1);
});
