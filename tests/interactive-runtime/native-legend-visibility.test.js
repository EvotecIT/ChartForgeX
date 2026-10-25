'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const { fixture } = require('./native-preparation-fixture');
// Synthetic routing only. The native/browser companions use actual Line/Bar scenes; the combined hierarchy+axes case is not a native chart configuration.
function setup(policy = 'immediate', options = {}, native = true) {
  const f = fixture(policy, options);
  const make = (hidden = [], changes = {}) => {
    const root = f.make(changes), metadata = JSON.parse(root.dataset.cfxPreparedChart);
    metadata.series = [{ name: 'Reviewed', key: 'reviewed', local: false, kind: 'line', state: 'none', indices: [0], observationIds: [{ index: 0, id: 'current' }] }];
    metadata.visibility = { hiddenSeries: [...hidden], labelLayering: 'preserveOverlay' }; root.dataset.cfxPreparedChart = JSON.stringify(metadata);
    root.dataset.cfxInteractionFeatures += ',LegendToggles';
    root.querySelector('svg').insertAdjacentHTML('beforeend', '<g data-cfx-role="legend-entry" data-cfx-source-id="legend-series-0" data-cfx-series-key="reviewed" aria-label="Reviewed"><rect x="5" y="5" width="10" height="10"/></g>'
      + '<g data-cfx-role="series" data-cfx-series="0" data-cfx-series-key="reviewed" data-cfx-series-scope="shared" data-cfx-kind="Line" opacity="' + (hidden.length ? 0 : 1) + '"' + (hidden.length ? ' data-cfx-visible="false"' : '') + '>'
      + '<g data-cfx-role="point" data-cfx-source-id="point-native" data-cfx-series="0" data-cfx-point="0" data-cfx-observation-id="current"><circle cx="40" cy="40" r="5"/></g></g>');
    return root;
  };
  f.controller.update(make().outerHTML, { basis: 'native-view' });
  for (const suffix of ['request', 'change', 'error']) f.root.addEventListener('cfxseries' + suffix, event => (suffix === 'request' ? f.requests : suffix === 'change' ? f.changes : f.failures).push(event.detail));
  f.connect({ basis: 'native-view', ...(native ? { legend: true } : {}) });
  const click = () => f.root.querySelector('[data-cfx-role="legend-item"]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true }));
  const hidden = () => JSON.parse(f.root.dataset.cfxPreparedChart).visibility.hiddenSeries;
  return { ...f, makeView: make, click, hidden };
}
test('omitted native legend retains the incumbent local mute and emits no preparation request', async () => {
  const f = setup('immediate', {}, false);
  try {
    f.click(); await f.flush(); f.capture('legacy-legend-default');
    assert.equal(f.pending.length, 0); assert.equal(f.root.querySelectorAll('.cfx-series-muted').length > 0, true); assert.deepEqual(f.hidden(), []);
  } finally { f.close(); }
});
test('opted native legend sends one absolute view and accepts only the matching native reply without optimistic CSS mutation', async () => {
  const f = setup();
  try {
    const revision = f.controller.sceneRevision; f.click(); await f.flush(); f.capture('native-legend-pending');
    assert.equal(f.pending.length, 1); assert.equal(f.pending[0].request.channel, 'legend'); assert.equal(f.pending[0].request.kind, 'visibility');
    assert.deepEqual([...f.pending[0].request.sourceVisibility.hiddenSeries], ['reviewed']); assert.equal(f.pending[0].request.basis, 'native-view');
    assert.deepEqual(f.hidden(), []); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.root.querySelectorAll('.cfx-series-muted').length, 0);
    f.pending[0].resolve({ html: f.makeView(['reviewed']).outerHTML, basis: 'hidden' }); await f.flush(); f.capture('native-legend-accepted');
    assert.deepEqual(f.hidden(), ['reviewed']); assert.notEqual(f.controller.sceneRevision, revision);
    assert.equal(f.root.querySelector('[data-cfx-role="legend-item"]').getAttribute('aria-pressed'), 'false');
    assert.equal(f.controller.resolveTargets().some(target => target.observationId === 'current'), false); assert.equal(f.failures.length, 0);
  } finally { f.close(); }
});
for (const options of [{}, { bothAxes: true }, { hierarchy: true, bothAxes: true }]) test(`busy absolute reset retains existing ${options.hierarchy ? 'synthetic hierarchy-priority' : options.bothAxes ? 'both-axis' : 'X-axis'} channel and one physical slot with native visibility descriptor`, async () => {
  const f = setup('latest-window', options);
  try {
    f.controller.update(f.makeView(['reviewed']).outerHTML, { basis: 'native-view' }); f.set(1); await f.flush();
    assert.equal(f.pending.length, 1); const revision = f.controller.sceneRevision, requests = f.requests.length;
    assert.equal(f.root.querySelector('[data-cfx-role="legend-item"]').getAttribute('aria-disabled'), 'true');
    f.click(); await f.flush(); assert.equal(f.pending.length, 1); assert.equal(f.requests.length, requests); assert.deepEqual(f.hidden(), ['reviewed']);
    f.root.querySelector('[data-cfx-reset]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true })); await f.flush(); f.capture('native-legend-queued-reset-' + (options.hierarchy ? 'synthetic-hierarchy' : options.bothAxes ? 'both-axes' : 'x-axis'));
    assert.equal(f.pending[0].signal.aborted, true); assert.equal(f.pending.length, 1); assert.equal(f.requests.length, requests); assert.equal(f.controller.sceneRevision, revision);
    f.pending[0].resolve({ html: f.makeView(['reviewed']).outerHTML, basis: 'ignored' }); await f.flush();
    assert.equal(f.pending.length, 2); const reset = f.pending[1].request;
    assert.equal(reset.kind, 'reset'); assert.equal(reset.channel, options.hierarchy ? 'hierarchy' : 'viewport'); assert.equal(reset.basis, 'native-view');
    assert.deepEqual([...reset.sourceVisibility.hiddenSeries], []); assert.equal(reset.sourceVisibility.labelLayering, 'preserveOverlay');
    f.pending[1].resolve({ html: f.makeView([], options.hierarchy ? { hierarchyRootId: null } : {}).outerHTML, basis: 'after-reset' }); await f.flush(); f.capture('native-legend-reset-accepted-' + (options.hierarchy ? 'synthetic-hierarchy' : options.bothAxes ? 'both-axes' : 'x-axis'));
    assert.equal(f.root.isConnected, true); assert.equal(f.window.ChartForgeXCharts.get(f.root), f.controller);
    assert.equal(f.controller.getXWindow().minimum, 0); assert.equal(f.controller.getXWindow().maximum, 10);
    assert.deepEqual(f.hidden(), []); assert.equal(f.failures.length, 0); assert.equal(f.pageErrors.length, 0);
  } finally { f.close(); }
});
for (const failure of ['throw', 'reject', 'missing-view']) test(`native legend ${failure} keeps accepted source and clears logical pending`, async () => {
  const f = setup();
  try {
    const revision = f.controller.sceneRevision;
    f.connect({ basis: 'native-view', legend: true }, failure === 'throw' ? () => { throw new Error('sync failed'); }
      : failure === 'reject' ? () => Promise.reject(new Error('async failed')) : () => Promise.resolve({ html: f.make().outerHTML, basis: 'bad' }));
    f.click(); await f.flush(); f.capture('native-legend-' + failure);
    assert.equal(f.controller.sceneRevision, revision); assert.deepEqual(f.hidden(), []); assert.equal(f.failures.length, 1); assert.equal(f.pageErrors.length, 0);
  } finally { f.close(); }
});
test('disconnect, replacement and disposal discard queued visibility reset without reopening ignored-abort physical work', async () => {
  const f = setup('latest-window');
  try {
    f.controller.update(f.makeView(['reviewed']).outerHTML, { basis: 'native-view' }); f.set(1); await f.flush();
    f.root.querySelector('[data-cfx-reset]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true })); await f.flush();
    f.controller.connectNative(null); f.connect({ basis: 'reconnected', legend: true });
    f.pending[0].resolve({ html: f.makeView().outerHTML, basis: 'stale' }); await f.flush(); f.capture('native-legend-reconnected');
    assert.equal(f.pending.length, 1); assert.deepEqual(f.hidden(), ['reviewed']);
    f.click(); await f.flush(); assert.equal(f.pending.length, 2); assert.equal(f.pending[1].request.basis, 'reconnected');
    f.controller.dispose(); f.pending[1].resolve({ html: f.makeView().outerHTML, basis: 'disposed' }); await f.flush();
    assert.deepEqual(f.hidden(), ['reviewed']); assert.equal(f.pageErrors.length, 0);
  } finally { f.close(); }
});

test('omitted legend catalog retains native hide state and declines unsupported isolate through existing overflow controls', async () => {
  const f = setup();
  try {
    const view = f.makeView(); view.querySelector('[data-cfx-role="legend-entry"]').setAttribute('data-cfx-role', 'legend-entry-omitted');
    f.controller.update(view.outerHTML, { basis: 'native-overflow' });
    const mute = f.root.querySelector('[data-cfx-legend-action="mute"]'), isolate = f.root.querySelector('[data-cfx-legend-action="isolate"]');
    assert.equal(isolate.disabled, true); mute.click(); await f.flush(); assert.equal(f.pending.length, 1);
    assert.deepEqual([...f.pending[0].request.sourceVisibility.hiddenSeries], ['reviewed']);
    const hidden = f.makeView(['reviewed']); hidden.querySelector('[data-cfx-role="legend-entry"]').setAttribute('data-cfx-role', 'legend-entry-omitted');
    f.pending[0].resolve({ html: hidden.outerHTML, basis: 'hidden-overflow' }); await f.flush(); f.capture('native-legend-overflow');
    assert.equal(f.root.querySelector('[data-cfx-legend-action="mute"]').getAttribute('aria-pressed'), 'true');
    assert.equal(f.root.querySelector('[data-cfx-legend-action="isolate"]').disabled, true);
    f.controller.connectNative(null); assert.equal(f.root.querySelector('[data-cfx-legend-action="isolate"]').disabled, false);
    assert.equal(f.pageErrors.length, 0);
  } finally { f.close(); }
});

// These focused owner controls load the real admission/lifetime helpers without DOM geometry.
function section(source, start, end) {
  const first = source.indexOf(start), last = source.indexOf(end, first + start.length);
  assert.ok(first >= 0 && last > first, 'actual owner boundary must remain present'); return source.slice(first, last);
}
function leaseOwners(state = 'active') {
  const fs = require('node:fs'), path = require('node:path');
  const read = name => fs.readFileSync(path.join(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source', name), 'utf8');
  const root = { isConnected: state !== 'removed' }, lifetime = { disposed: state === 'disposed', cleanups: new Set() };
  const lifetimes = new WeakMap(state === 'missing' ? [] : [[root, lifetime]]), attributes = new Map([['aria-pressed', 'original']]);
  const node = { dataset: {}, getAttribute: name => attributes.get(name) ?? null, setAttribute: (name, value) => attributes.set(name, value), removeAttribute: name => attributes.delete(name) };
  let connected = true, overflow = 0, animations = 0, cancelled = 0, captured = 0;
  const svg = { dataset: {}, animate: () => { animations++; return { cancel: () => cancelled++, finished: { then() {} } }; } };
  root.querySelector = () => svg;
  const own = section(read('07-chart-lifetime.js'), '  const ownChartWork =', '  const listenChart =');
  const visibility = section(read('85-native-visibility.js'), '  const syncNativeSeriesVisibility =', '  const requestNativeSeriesVisibility =');
  const motion = read('85-update-motion.js');
  const api = new Function('chartLifetimes', 'nativeLegendConnected', 'capturedNativeVisibility', 'syncLegendOverflow',
    'interactiveTargets', 'seriesKey', 'seriesTarget', 'nativePreparationBackpressureBusy', 'document', 'window', 'beginCapturedValueMotion',
    own + visibility + motion + '\nreturn { syncNativeSeriesVisibility, beginChartUpdateMotion };')(
      lifetimes, () => connected, () => ({ seriesKeys: ['reviewed'], hiddenSeries: ['reviewed'] }), () => overflow++,
      () => [node], () => 'reviewed', () => ({ targetKind: 'series' }), () => false,
      { hidden: false, addEventListener() {}, removeEventListener() {} }, { matchMedia: () => null }, () => captured++);
  return { api, root, lifetime, attributes, connect: value => { connected = value; },
    counters: () => ({ overflow, animations, cancelled, captured }) };
}
for (const state of ['missing', 'disposed', 'removed']) test('native legend sync does not reacquire a lease for a ' + state + ' lifetime', () => {
  const f = leaseOwners(state); assert.doesNotThrow(() => f.api.syncNativeSeriesVisibility(f.root));
  assert.equal(f.lifetime.cleanups.size, 0); assert.equal(f.attributes.get('aria-pressed'), 'original'); assert.equal(f.lifetime.nativeLegendRelease, undefined);
});
test('active native legend lease restores held attributes before disposed reentrant sync and still handles disconnect', () => {
  const f = leaseOwners(); f.api.syncNativeSeriesVisibility(f.root); assert.equal(f.lifetime.cleanups.size, 1); assert.equal(f.attributes.get('aria-pressed'), 'false');
  f.lifetime.disposed = true; for (const cleanup of [...f.lifetime.cleanups]) cleanup();
  assert.equal(f.attributes.get('aria-pressed'), 'original'); assert.equal(f.lifetime.nativeLegendRelease, null);
  assert.doesNotThrow(() => f.api.syncNativeSeriesVisibility(f.root)); assert.equal(f.lifetime.cleanups.size, 0);
  f.lifetime.disposed = false; f.api.syncNativeSeriesVisibility(f.root); f.connect(false); f.api.syncNativeSeriesVisibility(f.root);
  assert.equal(f.lifetime.cleanups.size, 0); assert.equal(f.attributes.get('aria-pressed'), 'original'); assert.ok(f.counters().overflow > 0);
});
for (const state of ['missing', 'disposed', 'removed']) test('native motion admits neither captured nor WAAPI work for a ' + state + ' lifetime', () => {
  const f = leaseOwners(state); assert.doesNotThrow(() => f.api.beginChartUpdateMotion(f.root, { duration: 1000 }));
  assert.doesNotThrow(() => f.api.beginChartUpdateMotion(f.root, { duration: 1000 }, {}));
  assert.equal(f.counters().animations, 0); assert.equal(f.counters().captured, 0); assert.equal(f.lifetime.cleanups.size, 0); assert.equal(f.lifetime.updateMotionRelease, undefined);
});
test('active native motion retains its existing captured and WAAPI cleanup owner', () => {
  const f = leaseOwners(); f.api.beginChartUpdateMotion(f.root, { duration: 1000 }, {}); assert.equal(f.counters().captured, 1);
  f.api.beginChartUpdateMotion(f.root, { duration: 1000 }); assert.equal(f.counters().animations, 1); assert.equal(f.lifetime.cleanups.size, 1);
  f.lifetime.updateMotionRelease(); assert.equal(f.counters().cancelled, 1); assert.equal(f.lifetime.cleanups.size, 0); assert.equal(f.lifetime.updateMotionRelease, null);
});
for (const boundary of ['update', 'reconnect', 'dispose', 'remove']) test('queued native legend reset teardown retains its real lifetime boundary through ' + boundary, async () => {
  const f = setup('latest-window');
  try {
    f.controller.update(f.makeView(['reviewed']).outerHTML, { basis: 'hidden' }); f.set(1); await f.flush();
    f.root.querySelector('[data-cfx-reset]').click(); await f.flush(); f.captureRaw('native-legend-teardown-' + boundary + '-before');
    let error; try {
      if (boundary === 'update') f.controller.update(f.makeView().outerHTML, { basis: 'external' });
      else if (boundary === 'reconnect') { f.controller.connectNative(null); f.connect({ basis: 'replacement', legend: true }); }
      else if (boundary === 'dispose') f.controller.dispose();
      else f.root.remove();
    } catch (failure) { error = failure; }
    await f.flush(); f.pending[0].resolve({ html: f.makeView().outerHTML, basis: 'late' }); await f.flush();
    f.captureRaw('native-legend-teardown-' + boundary + '-after', { actionError: error?.message });
    assert.equal(error, undefined); assert.equal(f.pending.length, 1); assert.equal(f.changes.length, 0); assert.deepEqual(f.pageErrors, []);
    if (boundary === 'dispose' || boundary === 'remove') {
      assert.equal(f.window.ChartForgeXCharts.get(f.root), null); assert.throws(() => f.controller.getXWindow(), /disposed/);
    } else {
      assert.equal(f.window.ChartForgeXCharts.get(f.root), f.controller); assert.equal(f.controller.getXWindow().minimum, 0);
      assert.deepEqual(f.hidden(), boundary === 'update' ? [] : ['reviewed']);
    }
  } finally { f.close(); }
});
