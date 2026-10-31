const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/cfx-link.js'), 'utf8');
function load() { const window = {}; new Function('window', source)(window); return window.CfxLink; }

// Any object with the LinkedView methods; events are dispatched on its element.
function view(name, chart) {
  const el = new EventTarget(); el.nodeType = 1; el.id = name;
  const calls = [];
  const api = {
    view: name, label: name, calls, selection: ['kept'],
    setExternalFilter: (source, filter) => { calls.push(['filter', source, filter]); return Promise.resolve(true); },
    clearExternalFilters: () => Promise.resolve(true), getExternalFilters: () => [],
    setHighlight: (keys, options) => { calls.push(['highlight', keys, options]); return Promise.resolve(keys.length); },
    clearHighlight: () => { calls.push(['clearHighlight']); return Promise.resolve(0); }, getHighlight: () => [],
    startLinkEvents: () => { calls.push(['start']); return Promise.resolve(); },
    clearSelection: options => { calls.push(['clearSelection', options]); }
  };
  if (chart) api.svg = el; else api.host = el;
  return { api, el };
}
function emit(el, type, detail) { el.dispatchEvent(new CustomEvent(type, { detail })); }

test('a chart selection filters a CanopyX-shaped grid by values, and a range by an inclusive range', () => {
  const CfxLink = load(), chart = view('activity', true), grid = view('records');
  CfxLink.connect(chart.api, grid.api, { field: 'day', sourceLabel: 'Activity chart' });
  assert.deepEqual(grid.api.calls.shift(), ['start']);
  assert.equal(chart.api.selectOnClick, true);
  emit(chart.el, 'cfx:linkchange', { view: 'activity', action: 'select', scope: 'select', keys: ['r1'], values: { day: ['2026-07-14T00:00:00.000Z'], kind: ['Notes'] }, label: 'Tue 14 Jul · Notes' });
  assert.deepEqual(grid.api.calls.pop(), ['filter', 'activity', { field: 'day', values: ['2026-07-14T00:00:00.000Z'], label: 'Tue 14 Jul · Notes', sourceLabel: 'Activity chart' }]);
  emit(chart.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: [], values: {}, label: '1 – 3 Oct', range: { column: 'day', from: '2026-10-01T00:00:00.000Z', to: '2026-10-04T00:00:00.000Z' } });
  assert.deepEqual(grid.api.calls.pop()[2].range, ['2026-10-01T00:00:00.000Z', '2026-10-03T23:59:59.999Z']);
  emit(chart.el, 'cfx:linkchange', { action: 'clear', scope: 'select', keys: [], values: {}, label: '' });
  assert.deepEqual(grid.api.calls.pop(), ['filter', 'activity', null]);
});

test('grid selections highlight the chart, grid conditions filter it, and a removed chip clears the selection silently', () => {
  const CfxLink = load(), chart = view('activity', true), grid = view('records');
  CfxLink.connect(chart.api, grid.api, { field: 'day', targetLabel: 'Records' });
  emit(grid.el, 'canopyx:linkchange', { view: 'records', action: 'select', scope: 'select', keys: ['a', 'b'], values: {}, label: '' });
  assert.deepEqual(chart.api.calls.pop(), ['highlight', ['a', 'b'], { source: 'Records' }]);
  emit(grid.el, 'canopyx:linkchange', { view: 'records', action: 'filter', scope: 'filter', keys: [], values: { kind: ['Notes'], empty: [] }, label: '' });
  assert.deepEqual(chart.api.calls.pop(), ['filter', 'records', [{ field: 'kind', values: ['Notes'], sourceLabel: 'Records' }]]);
  emit(grid.el, 'canopyx:linkchange', { view: 'records', action: 'clear', scope: 'select', keys: [], values: {}, label: '' });
  assert.deepEqual(chart.api.calls.pop(), ['clearHighlight']);
  // An api-driven filter change never clears the chart; the reader removing the chart's chip does.
  emit(grid.el, 'canopyx:externalfilter', { filters: [], reason: 'api' });
  assert.equal(chart.api.calls.length, 0);
  emit(grid.el, 'canopyx:externalfilter', { filters: [{ source: 'other' }], reason: 'user' });
  assert.deepEqual(chart.api.calls.pop(), ['clearSelection', { silent: true }]);
});

test('highlight mode and chart-to-chart links use the same neutral protocol; disconnect stops both directions', () => {
  const CfxLink = load(), a = view('trend', true), b = view('activity', true);
  const disconnect = CfxLink.connect(a.api, b.api, { mode: 'highlight' });
  emit(a.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: ['k1'], values: {}, label: 'x' });
  assert.deepEqual(b.api.calls.pop(), ['highlight', ['k1'], { source: 'trend' }]);
  // The second chart reports through cfx:linkchange, not a CanopyX event.
  emit(b.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: ['k2'], values: {}, label: 'y' });
  assert.deepEqual(a.api.calls.pop(), ['highlight', ['k2'], { source: 'activity' }]);
  disconnect();
  assert.equal(a.api.selectOnClick, undefined);
  emit(a.el, 'cfx:linkchange', { action: 'clear', scope: 'select', keys: [], values: {}, label: '' });
  emit(b.el, 'cfx:linkchange', { action: 'clear', scope: 'select', keys: [], values: {}, label: '' });
  assert.equal(a.api.calls.length + b.api.calls.filter(c => c[0] !== 'start').length, 0);
});

test('disconnect restores native click mode only after the last connection and preserves an authored selection mode', () => {
  const CfxLink = load(), chart = view('activity', true), first = view('first'), second = view('second');
  const offFirst = CfxLink.connect(chart.api, first.api, { field: 'day' });
  const offSecond = CfxLink.connect(chart.api, second.api, { field: 'day' });
  offFirst(); offFirst();
  assert.equal(chart.api.selectOnClick, true);
  offSecond();
  assert.equal(chart.api.selectOnClick, undefined);
  chart.api.selectOnClick = true;
  const offAuthored = CfxLink.connect(chart.api, first.api, { field: 'day' });
  offAuthored();
  assert.equal(chart.api.selectOnClick, true);
});

test('several fields filter by each, labelled with the texts a reader sees', () => {
  const CfxLink = load(), chart = view('week', true), grid = view('records');
  CfxLink.connect(chart.api, grid.api, { field: ['day', 'kind'], sourceLabel: 'Changes this week' });
  emit(chart.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: ['r1'], label: 'Thu 29 · Updates',
    values: { day: ['2026-10-29T00:00:00.000Z'], kind: ['Update'] }, texts: { day: ['Thu 29'], kind: ['Updates'] } });
  assert.deepEqual(grid.api.calls.pop(), ['filter', 'week', [
    { field: 'day', values: ['2026-10-29T00:00:00.000Z'], label: 'Thu 29', sourceLabel: 'Changes this week' },
    { field: 'kind', values: ['Update'], label: 'Updates', sourceLabel: 'Changes this week' }]]);
  // A field the selection does not define is left out; none at all clears the source's filter.
  emit(chart.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: ['r2'], label: 'x', values: { kind: ['Note'] } });
  assert.deepEqual(grid.api.calls.pop(), ['filter', 'week', { field: 'kind', values: ['Note'], sourceLabel: 'Changes this week' }]);
  emit(chart.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: [], label: 'x', values: {} });
  assert.deepEqual(grid.api.calls.pop(), ['filter', 'week', null]);
});

test('a page element holding a CanopyX host links once the grid mounts', () => {
  const CfxLink = load(), chart = view('week', true), grid = view('records');
  const host = grid.el; host.hasAttribute = name => name === 'data-canopyx-host';
  const slot = new EventTarget(); slot.nodeType = 1; slot.hasAttribute = () => false; slot.querySelector = () => host;
  CfxLink.connect(chart.api, slot, { field: 'day' });
  emit(chart.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: ['r1'], label: 'x', values: { day: ['d'] } });
  assert.equal(grid.api.calls.length, 0);
  host.canopyx = grid.api;
  emit(host, 'canopyx:ready', {});
  assert.deepEqual(grid.api.calls.shift(), ['start']);
  emit(chart.el, 'cfx:linkchange', { action: 'select', scope: 'select', keys: ['r1'], label: 'Thu 29', values: { day: ['d'] } });
  assert.deepEqual(grid.api.calls.pop(), ['filter', 'week', { field: 'day', values: ['d'], label: 'Thu 29', sourceLabel: 'week' }]);
});
