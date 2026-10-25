const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const source = name => fs.readFileSync(path.resolve(__dirname,
  '../../ChartForgeX.Interactivity.Html/Assets/interactive.source', name + '.js'), 'utf8');

function fixture() {
  const nodes = [], lifetimes = new WeakMap(), mounted = new Set(), window = {}, writes = [];
  const root = { isConnected: true, dataset: { cfxInteractionFeatures: '' },
    _cfxObservationIds: [new Map([[0, 'first'], [1, 'second'], [2, 'third']])],
    querySelectorAll: () => nodes, closest: selector => selector === '.cfx-interactive-chart' ? root : null };
  const mark = (dataset, parent = root) => {
    const classes = new Set(['cfx-selected', 'cfx-highlighted']);
    const node = { dataset, parentElement: parent, id: '',
      classList: { contains: name => classes.has(name), toggle: () => writes.push('class') },
      getAttribute: () => null,
      closest(selector) {
        if (selector === '.cfx-interactive-chart') return root;
        if (selector === 'svg') return null;
        if (selector === '[data-cfx-passive="true"]') return dataset.cfxPassive === 'true' ? node : parent.closest(selector);
        if (selector === '[data-cfx-role="legend-item"]') return dataset.cfxRole === 'legend-item' ? node : parent.closest(selector);
        if (selector.startsWith('[data-cfx-point]')) return dataset.cfxPoint !== undefined ? node : parent.closest(selector);
        return null;
      }
    };
    nodes.push(node); return node;
  };
  const aggregate = mark({ cfxRole: 'point', cfxKind: 'bar', cfxSeries: '0', cfxSeriesKey: 'durations',
    cfxPoint: '0', cfxDerived: 'histogram-bin', cfxSourcePoints: '0,1', cfxRangeStart: '0', cfxRangeEnd: '0.9999999999999999',
    cfxBinLower: '0', cfxBinUpper: '1', cfxY: '2', cfxDisplayLabel: '0 – 1' });
  const other = mark({ cfxRole: 'point', cfxKind: 'bar', cfxSeries: '0', cfxSeriesKey: 'durations',
    cfxPoint: '1', cfxObservationId: 'third', cfxSourcePoint: '2', cfxY: '1', cfxXLabel: 'Third' });
  mark({ ...aggregate.dataset }); // Repeated rendered parts have one selection readout.
  mark({ ...aggregate.dataset }, aggregate); // Nested paint is not an independent target.
  mark({ cfxRole: 'series', cfxSeries: '0', cfxSeriesKey: 'durations' });
  mark({ cfxRole: 'legend-item', cfxSeries: '0', cfxSeriesKey: 'durations' });
  mark({ ...other.dataset, cfxPassive: 'true' });
  const document = { querySelectorAll: () => [] };
  const release = owner => {
    lifetimes.get(owner).disposed = true; lifetimes.delete(owner); mounted.delete(owner);
  };
  const unchanged = () => JSON.stringify({ nodes: nodes.map(node => node.dataset), selected: nodes.map(node =>
    [node.classList.contains('cfx-selected'), node.classList.contains('cfx-highlighted')]), root: root.dataset });
  const runtime = ['00-bootstrap-targets', '00-bootstrap-tooltip', '40-selection-hover-sync', '42-host-commands', '90-chart-api']
    .map(source).join('\n');
  new Function('window', 'document', 'chartLifetimes', 'mountedCharts', 'readChartSection', 'readAdaptiveLayout',
    'nextChartSceneRevision', 'prepareAdaptiveRoot', 'bindChart', 'bindAdaptiveLayout', 'watchChartLifetimes',
    'releaseChartLifetime', 'emitHostEvent', 'emitSync', 'rememberNativeDrawingSnapshot', runtime)(window, document, lifetimes, mounted,
      () => {}, () => null, () => 'scene-1', () => {}, () => {}, () => {}, () => {}, release,
      () => writes.push('event'), () => writes.push('sync'), () => {}); // Native presentation capture is outside this target-resolution unit's synthetic DOM boundary.
  return { controller: window.ChartForgeXCharts.mount(root), root, aggregate, other, writes, unchanged };
}

test('public target resolution enumerates canonical data targets without Selection and distinguishes empty matches', () => {
  const { controller } = fixture();
  const targets = controller.resolveTargets();
  assert.equal(targets.length, 2);
  assert.deepEqual(targets.map(target => target.targetKind), ['point', 'point']);
  assert.equal(targets[0].targetId, 'durations:derived:histogram-bin:0:1:false:count:value');
  assert.deepEqual(targets[0].observationIds, ['first', 'second']);
  assert.equal(targets[1].observationId, 'third');
  assert.deepEqual(controller.resolveTargets(undefined), targets);
  for (const match of [{}, { targets: [] }, { observationIds: ['missing'] }]) assert.deepEqual(controller.resolveTargets(match), []);
});

test('independent contributor filters intersect the same aggregate identity and preserve native OR matching', () => {
  const { controller } = fixture();
  const first = controller.resolveTargets({ sourcePoints: [{ seriesKey: 'durations', index: 0 }] });
  const second = controller.resolveTargets({ sourcePoints: [{ seriesKey: 'durations', index: 1 }] });
  assert.equal(first.length, 1); assert.equal(second.length, 1);
  assert.equal(first[0].targetId, second[0].targetId);
  assert.deepEqual(first[0].sourcePoints, [0, 1]);
  assert.equal(first[0].observationId, undefined);
  const selected = new Set(second.map(target => JSON.stringify([target.targetKind, target.targetId, target.seriesKey])));
  assert.deepEqual(first.filter(target => selected.has(JSON.stringify([target.targetKind, target.targetId, target.seriesKey]))), first);
  assert.deepEqual(controller.resolveTargets({ observationIds: ['second'] }), first);
  assert.deepEqual(controller.resolveTargets({ targets: first }), first);
  assert.equal(controller.resolveTargets({ categories: ['Third'], observationIds: ['first'] }).length, 2);
  assert.deepEqual(controller.resolveTargets({ ranges: [[0, .5]] }), first);
  assert.equal(controller.resolveTargets({ observationIds: ['first'], seriesKey: 'other' }).length, 0);
});

test('target resolution returns detached snapshots without changing classes, selection, highlights or host events', () => {
  const { controller, writes, unchanged } = fixture();
  const before = unchanged(), selection = controller.getSelection();
  const match = { observationIds: ['first'] }, originalMatch = JSON.stringify(match);
  const target = controller.resolveTargets(match)[0];
  target.observationIds[0] = 'changed'; target.sourcePoints.push(99); target.range[0] = -100; target.targetId = 'changed';
  assert.equal(unchanged(), before);
  assert.equal(JSON.stringify(match), originalMatch);
  assert.deepEqual(controller.getSelection(), selection);
  const fresh = controller.resolveTargets(match)[0];
  assert.deepEqual(fresh.observationIds, ['first', 'second']);
  assert.deepEqual(fresh.sourcePoints, [0, 1]);
  assert.equal(fresh.range[0], 0); assert.notEqual(fresh.targetId, 'changed');
  assert.deepEqual(writes, []);
});

test('target resolution applies native matcher validation and mounted controller lifetime', () => {
  const { controller, root, writes } = fixture();
  for (const match of [null, [], 'all', { targets: {} }, { sourcePoints: 0 }, { ranges: 'range' }])
    assert.throws(() => controller.resolveTargets(match), TypeError);
  assert.deepEqual(writes, []);
  root.isConnected = false;
  assert.throws(() => controller.resolveTargets(), /disposed/);
  root.isConnected = true;
  assert.throws(() => controller.resolveTargets({}), /disposed/);
  const second = fixture().controller;
  second.dispose();
  assert.throws(() => second.resolveTargets(), /disposed/);
});
