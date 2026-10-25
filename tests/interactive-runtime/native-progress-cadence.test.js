'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const runtime = ['adapter-native-text-export.js', 'interactive.js'].map(name => fs.readFileSync(path.join(assets, name), 'utf8')).join('\n');
const match = { seriesKey: 'delivery', observationId: 'reviewed' };
// Only browser time and the native host's generated-section transport are substitutes.
// Native frame paint is qualified separately through real PrepareSection and the captured IR.
function fixture(cadence = 100) {
  const dom = new JSDOM('<!doctype html><body></body>', { runScripts: 'outside-only', pretendToBeVisual: true, url: 'https://example.test/' });
  const { window } = dom, timers = new Map(), pending = [], changes = [], errors = [];
  let now = 0, sequence = 0;
  Object.defineProperty(window.performance, 'now', { value: () => now });
  window.setTimeout = (callback, delay = 0) => { const id = ++sequence; timers.set(id, { at: now + delay, callback }); return id; };
  window.clearTimeout = id => timers.delete(id);
  window.SVGElement.prototype.getBBox = () => ({ x: 40, y: 40, width: 20, height: 20 });
  const make = (value = .5, notes = [], other = .25) => {
    const section = window.document.createElement('section'); section.className = 'cfx-interactive-chart';
    Object.assign(section.dataset, { cfxChartId: 'progress-cadence', cfxProgressEditing: 'true', cfxInteractionFeatures: 'Selection,KeyboardNavigation' });
    const axis = { minimum: 0, maximum: 1, fullMinimum: 0, fullMaximum: 1, transformedMinimum: 0, transformedMaximum: 1,
      transformedFullMinimum: 0, transformedFullMaximum: 1, scale: 'linear', threshold: 1, reversed: false,
      plotBounds: { x: 20, y: 20, width: 300, height: 200 } };
    section.dataset.cfxPreparedChart = JSON.stringify({ series: [{ name: 'Delivery', key: 'delivery', local: false,
      kind: 'Progress', state: 'None', indices: [0, 1] }], xLabels: [], regions: [], annotations: notes.map(id => ({ id, sourceId: 'note-' + id, kind: 'Note' })),
      xProjection: axis, yProjection: axis, dataProjection: { plotBounds: axis.plotBounds, horizontal: false, x: axis, y: axis } });
    section.innerHTML = '<button data-cfx-reset>Reset</button><div class="cfx-stage"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 260">'
      + '<g data-cfx-role="progress-bar-chart" data-cfx-series="0" data-cfx-series-key="delivery" data-cfx-series-scope="shared" data-cfx-maximum="1">'
      + ['reviewed', 'other'].map((id, index) => '<g data-cfx-role="progress-row" data-cfx-series="0" data-cfx-point="' + index
        + '" data-cfx-series-key="delivery" data-cfx-series-scope="shared" data-cfx-observation-id="' + id
        + '" data-cfx-label="' + id + '" data-cfx-value="' + (index ? other : value) + '" data-cfx-min="0" data-cfx-max="1">'
        + '<rect data-cfx-role="progress-track" x="40" y="40" width="200" height="20"/><rect data-cfx-role="progress-fill" x="40" y="40" width="100" height="20"/></g>').join('')
      + '</g>' + notes.map(id => '<g data-cfx-role="annotation" data-cfx-annotation-id="' + id + '" data-cfx-source-id="note-' + id
        + '" data-cfx-kind="Note" data-cfx-label="' + id + '" data-cfx-source-x="0.4" data-cfx-source-y="0.4" data-cfx-y-axis="Primary"/>').join('')
      + '</svg></div><div class="cfx-tooltip" hidden></div>'; return section;
  };
  const root = make(); window.document.body.append(root); window.eval(runtime);
  const controller = window.ChartForgeXCharts.get(root);
  root.addEventListener('cfxprogresschange', event => changes.push(event.detail));
  root.addEventListener('cfxprogresserror', event => errors.push(event.detail));
  const callback = (request, signal) => new Promise((resolve, reject) => pending.push({ request, signal, resolve, reject }));
  const connect = (extra = {}) => controller.connectNative(callback, { basis: 'initial', axes: null, progressEditing: true, progressCadenceMs: cadence, ...extra });
  connect();
  const flush = async () => { for (let turn = 0; turn < 16; turn++) await Promise.resolve(); };
  const advance = async amount => {
    const end = now + amount;
    for (;;) {
      const next = [...timers].filter(([, timer]) => timer.at <= end).sort((a, b) => a[1].at - b[1].at)[0];
      if (!next) break; now = next[1].at; timers.delete(next[0]); next[1].callback(); await flush();
    }
    now = end; await flush();
  };
  const reply = async (index, basis = 'accepted', value = pending[index].request.value) => {
    const primary = Number(root.querySelector('[data-cfx-observation-id=reviewed]').dataset.cfxValue);
    pending[index].resolve({ html: (pending[index].request.observationId === 'other' ? make(primary, [], value) : make(value)).outerHTML, basis }); await flush();
  };
  return { window, root, controller, pending, changes, errors, make, connect, callback, flush, advance, reply,
    value: () => Number(root.querySelector('[data-cfx-observation-id=reviewed]').dataset.cfxValue),
    close() { controller.dispose(); dom.window.close(); } };
}

test('zero cadence retains immediate superseding dispatch with no connection API additions', async () => {
  const f = fixture(0);
  try {
    f.controller.setProgressValue(match, .6); await f.flush();
    f.controller.setProgressValue(match, .7); await f.flush();
    assert.equal(f.pending.length, 2); assert.equal(f.pending[0].signal.aborted, true);
    assert.equal(f.pending[1].request.basis, 'initial');
    await f.reply(1, 'second'); await f.reply(0, 'stale');
    assert.equal(f.value(), .7); assert.equal(f.changes.length, 1); assert.equal(f.errors.length, 0);
  } finally { f.close(); }
});

test('opt-in synchronous bursts create only the last same-row request and use each committed basis', async () => {
  const f = fixture();
  try {
    for (const value of [.6, .7, .8]) f.controller.setProgressValue(match, value);
    assert.equal(f.pending.length, 0); await f.flush();
    assert.equal(f.pending.length, 1); assert.equal(f.pending[0].request.value, .8);
    assert.equal(Object.isFrozen(f.pending[0].request), true);
    await f.reply(0, 'first'); assert.equal(f.value(), .8);
    f.controller.setProgressValue(match, .9); await f.flush(); assert.equal(f.pending.length, 1);
    await f.advance(99); assert.equal(f.pending.length, 1);
    await f.advance(1); assert.equal(f.pending.length, 2); assert.equal(f.pending[1].request.basis, 'first');
    await f.reply(1, 'second'); assert.equal(f.value(), .9); assert.equal(f.errors.length, 0);
  } finally { f.close(); }
});

test('ignored abort admits one unresolved callback plus one latest intent and stalls honestly if it never settles', async () => {
  const f = fixture();
  try {
    f.controller.setProgressValue(match, .6); await f.flush();
    for (let index = 0; index < 5000; index++) f.controller.setProgressValue(match, .7 + index / 50000);
    await f.advance(60000);
    assert.equal(f.pending.length, 1); assert.equal(f.pending[0].signal.aborted, true);
    assert.equal(f.value(), .5); assert.equal(f.changes.length, 0);
    // Settling the superseded host call releases admission, never its stale destination.
    await f.reply(0, 'must-not-commit'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.value, .7 + 4999 / 50000);
    assert.equal(f.pending[1].request.basis, 'initial'); assert.equal(f.value(), .5);
    await f.reply(1, 'latest'); assert.equal(f.value(), f.pending[1].request.value);
  } finally { f.close(); }
});

test('two admitted rows keep independent latest facts in stable order and dispatch from the first committed basis', async () => {
  const f = fixture();
  try {
    f.controller.setProgressValue(match, .6); await f.flush();
    const other = { seriesKey: 'delivery', observationId: 'other' };
    f.controller.setProgressValue(other, .8); f.controller.setProgressValue(other, .9);
    assert.equal(f.pending[0].signal.aborted, false, 'another row does not erase the active replacement fact');
    await f.reply(0, 'first'); assert.equal(f.value(), .6); assert.equal(f.pending.length, 1);
    await f.advance(100); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.observationId, 'other'); assert.equal(f.pending[1].request.value, .9);
    assert.equal(f.pending[1].request.basis, 'first'); await f.reply(1, 'second');
    assert.equal(f.value(), .6); assert.equal(Number(f.root.querySelector('[data-cfx-observation-id=other]').dataset.cfxValue), .9);
    assert.equal(f.changes.length, 2); assert.equal(f.errors.length, 0);
  } finally { f.close(); }
});

test('rejected native replies retain basis and external replacement retains the unresolved-binding admission bound', async () => {
  const f = fixture();
  try {
    f.controller.setProgressValue(match, .6); await f.flush(); await f.reply(0, 'wrong', .5);
    assert.equal(f.errors.length, 1); assert.equal(f.value(), .5);
    f.controller.setProgressValue(match, .7); await f.advance(100);
    assert.equal(f.pending[1].request.basis, 'initial');
    f.controller.update(f.make(.8).outerHTML, { basis: 'external' });
    f.controller.setProgressValue(match, .9); await f.advance(1000);
    assert.equal(f.pending.length, 2); assert.equal(f.pending[1].signal.aborted, true);
    await f.reply(1, 'stale'); assert.equal(f.value(), .8);
    assert.equal(f.pending[2].request.basis, 'external');
    await f.reply(2, 'fresh'); assert.equal(f.value(), .9);
  } finally { f.close(); }
});

test('incremental annotation commands remain immediate and are never cadence coalesced', async () => {
  const f = fixture();
  try {
    f.controller.setProgressValue(match, .6); await f.flush(); f.controller.setProgressValue(match, .7);
    const add = id => ({ operation: 'add', id, drawing: { kind: 'Note', start: { x: .4, y: .4 }, label: id, yAxis: 'Primary' } });
    f.controller.applyAnnotationChange(add('first')); await f.flush();
    f.controller.applyAnnotationChange(add('second')); await f.flush();
    assert.deepEqual(f.pending.slice(1).map(item => [item.request.channel, item.request.annotationId]), [['annotation', 'first'], ['annotation', 'second']]);
    assert.equal(f.pending[0].signal.aborted, true);
    await f.reply(0, 'stale-progress'); await f.advance(1000);
    assert.equal(f.pending.length, 3, 'the annotation command cancelled the old queued progress intent');
  } finally { f.close(); }
});

for (const boundary of ['disconnect', 'update', 'reset', 'dispose']) test(`${boundary} cancels queued cadence work and cannot publish a late progress scene`, async () => {
  const f = fixture();
  try {
    f.controller.setProgressValue(match, .6); await f.flush(); f.controller.setProgressValue(match, .7);
    const revision = f.controller.sceneRevision;
    if (boundary === 'disconnect') f.controller.connectNative(null);
    if (boundary === 'update') f.controller.update(f.make(.8).outerHTML, { basis: 'manual' });
    if (boundary === 'reset') f.root.querySelector('[data-cfx-reset]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true }));
    if (boundary === 'dispose') f.controller.dispose();
    await f.reply(0, 'late'); await f.advance(10000);
    assert.equal(f.pending.length, 1); assert.equal(f.pending[0].signal.aborted, true);
    assert.equal(f.changes.length, 0); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
    assert.equal(f.value(), boundary === 'update' ? .8 : .5);
    if (boundary !== 'update') assert.equal(f.controller.sceneRevision, revision);
  } finally { f.close(); }
});

test('invalid cadence options leave the committed section, active binding and pending request unchanged', async () => {
  const f = fixture();
  try {
    f.controller.setProgressValue(match, .6); await f.flush();
    const revision = f.controller.sceneRevision, html = f.root.innerHTML;
    for (const progressCadenceMs of [-1, 1001, NaN, Infinity, '100', true])
      assert.throws(() => f.connect({ progressCadenceMs }), /finite 0\.\.1000/);
    assert.throws(() => f.connect({ progressCadenceMs: 50, progressEditing: false }), /progressEditing required/);
    assert.equal(f.pending[0].signal.aborted, false); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.root.innerHTML, html);
    await f.reply(0, 'valid'); assert.equal(f.value(), .6);
  } finally { f.close(); }
});

for (const boundary of ['hidden', 'reduced', 'completed']) test(`captured native presentation releases on ${boundary} while the accepted scene remains final`, async () => {
  const f = fixture(0);
  try {
    let sequence = 0; const frames = new Map(), mediaListeners = new Set();
    // JSDOM has no layout. Supply only the browser viewport-measurement boundary;
    // native paint comes from separate C# and real-browser contracts.
    f.window.HTMLElement.prototype.getBoundingClientRect = () => ({ left: 10, top: 20, width: 360, height: 260 });
    Object.defineProperty(f.window.HTMLElement.prototype, 'offsetWidth', { configurable: true, get: () => 360 });
    Object.defineProperty(f.window.HTMLElement.prototype, 'offsetHeight', { configurable: true, get: () => 260 });
    f.window.SVGElement.prototype.getBoundingClientRect = () => ({ left: 24, top: 34, width: 320, height: 220 });
    f.window.requestAnimationFrame = callback => { const id = ++sequence; frames.set(id, callback); return id; };
    f.window.cancelAnimationFrame = id => frames.delete(id);
    f.window.matchMedia = () => ({ matches: false, addEventListener: (_, callback) => mediaListeners.add(callback), removeEventListener: (_, callback) => mediaListeners.delete(callback) });
    f.connect({ motion: { duration: 1000 } });
    f.controller.setProgressValue(match, .6); await f.flush();
    const native = value => f.make(value).querySelector('svg').outerHTML;
    // Synthetic transport qualifies presentation lifetime only. Actual geometry/text
    // observations come from the separate C# native and real-host browser rows.
    f.pending[0].resolve({ html: f.make(.6).outerHTML, basis: 'final', transition: JSON.stringify({
      kind: 'progress-value', seriesKey: 'delivery', observationId: 'reviewed',
      fromValue: .5, toValue: .6, minimum: 0, maximum: 1, wide: [.5, .55, .6].map(native)
    }) }); await f.flush();
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    const targets = f.controller.resolveTargets().length;
    assert.equal(f.value(), .6); assert.equal(f.root.querySelector('.cfx-stage > svg').style.opacity, '0');
    const run = time => { const callbacks = [...frames.values()]; frames.clear(); for (const callback of callbacks) callback(time); };
    // Commit/style mutation schedules the keyboard owner's one-shot availability refresh.
    // Give the full runtime its first browser frame before testing cancellation of the
    // still-pending presentation tick; keyboard support and every cleanup assertion stay active.
    run(0); await f.flush();
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    assert.equal(frames.size, 1, 'only the pending presentation tick remains after the initial availability pass');
    if (boundary === 'hidden') {
      Object.defineProperty(f.window.document, 'hidden', { configurable: true, value: true });
      f.window.document.dispatchEvent(new f.window.Event('visibilitychange'));
    } else if (boundary === 'reduced') {
      for (const callback of [...mediaListeners]) callback({ matches: true });
    } else {
      run(500);
      assert.equal(f.root.querySelector('.cfx-native-motion-frame').dataset.cfxUpdateFrame, '1');
      run(1000);
    }
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
    assert.equal(frames.size, 0, [...frames.values()].map(callback => callback.toString()).join('\n'));
    assert.equal(mediaListeners.size, 0);
    assert.equal(f.root.querySelector('.cfx-stage > svg').style.opacity, '');
    assert.equal(f.value(), .6); assert.equal(f.controller.resolveTargets().length, targets);
    assert.equal(f.changes.length, 1); assert.equal(f.errors.length, 0);
  } finally { f.close(); }
});
