'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const { fixture } = require('./native-preparation-fixture');

test('same-turn windows receive no request identity or event until one latest intent is admitted', async () => {
  const f = fixture();
  try {
    for (let index = 0; index < 5000; index++) f.set(1 + index / 10000);
    assert.equal(f.requests.length, 0); assert.equal(f.pending.length, 0);
    assert.equal(f.root.querySelector('[data-cfx-export]').disabled, true);
    await f.flush(); f.capture('native-admission-same-turn');
    assert.equal(f.pending.length, 1); assert.equal(f.requests.length, 1);
    assert.equal(f.pending[0].request.requestId, 'cfx-request-1');
    assert.equal(f.pending[0].request.sourceViewport.x.minimum, 1.4999);
    assert.equal(Object.isFrozen(f.pending[0].request), true);
    await f.reply(0); assert.equal(f.root.querySelector('[data-cfx-export]').disabled, false);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('an accepted change listener can immediately issue an incremental edit against the newly committed basis', async () => {
  const f = fixture();
  try {
    f.root.addEventListener('cfxviewportchange', () => f.controller.applyAnnotationChange({ operation: 'replace', id: 'note',
      drawing: { kind: 'Note', start: { x: 4, y: 40 }, label: 'After the view' } }), { once: true });
    f.set(1); await f.flush(); await f.reply(0, 'view-basis');
    assert.equal(f.pending.length, 2); assert.equal(f.pending[1].request.channel, 'annotation');
    assert.equal(f.pending[1].request.basis, 'view-basis');
    await f.reply(1, 'edit-basis', f.make({ minimum: 1, maximum: 9, note: { x: 4, y: 40, label: 'After the view' } }));
    assert.equal(f.changes.length, 2); assert.equal(f.failures.length, 0);
    assert.equal(f.root.querySelector('[data-cfx-annotation-id=note]').dataset.cfxLabel, 'After the view');
    assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('busy incremental calls and internal relative controls leave pending work, basis and events unchanged', async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush(); f.set(2); await f.flush();
    const before = [f.controller.sceneRevision, f.requests.length, f.changes.length, f.failures.length];
    assert.throws(() => f.controller.applyAnnotationChange({ operation: 'replace', id: 'note', drawing: {
      kind: 'Note', start: { x: 4, y: 40 }, label: 'Rejected while busy' } }), /Native preparation busy/);
    assert.throws(() => f.controller.setProgressValue({ seriesKey: 'unused', observationId: 'unused' }, .5), /Native preparation busy/);
    assert.throws(() => f.controller.setHierarchyView({ rootId: 'unused' }), /Native preparation busy/);
    f.root.querySelector('[data-cfx-zoom]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true }));
    f.root.querySelector('[data-cfx-export]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true }));
    await f.flush(); f.capture('native-admission-busy');
    assert.deepEqual([f.controller.sceneRevision, f.requests.length, f.changes.length, f.failures.length], before);
    assert.equal(f.pending.length, 1); assert.equal(f.controller.getXWindow().minimum, 0);
    assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('Escape clears the queued replacement but cannot reopen an ignored-abort physical slot', async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush(); f.set(2); await f.flush();
    f.root.dispatchEvent(new f.window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    f.set(3); await f.flush(); assert.equal(f.pending.length, 1);
    await f.reply(0, 'stale'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.requestId, 'cfx-request-2');
    assert.equal(f.pending[1].request.basis, 'initial');
    assert.equal(f.pending[1].request.sourceViewport.x.minimum, 3);
    await f.reply(1); assert.equal(f.controller.getXWindow().minimum, 3);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

for (const laterWindow of [false, true]) test(`busy Reset occupies the same one replacement slot${laterWindow ? ' and may be superseded by a later window' : ''}`, async () => {
  const f = fixture();
  try {
    let resets = 0; f.root.addEventListener('cfxreset', () => resets++);
    f.set(1); await f.flush(); f.set(2); await f.flush();
    assert.equal(f.root.querySelector('[data-cfx-reset]').hidden, false);
    f.root.querySelector('[data-cfx-reset]').click(); if (laterWindow) f.set(3);
    await f.flush(); f.capture('native-admission-reset-' + laterWindow);
    assert.equal(f.pending.length, 1); assert.equal(f.requests.length, 1); assert.equal(resets, 0);
    await f.reply(0, 'stale'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.kind, laterWindow ? 'window' : 'reset');
    assert.equal(f.pending[1].request.basis, 'initial');
    await f.reply(1, 'accepted');
    assert.equal(f.controller.getXWindow().minimum, laterWindow ? 3 : 0);
    assert.equal(resets, laterWindow ? 0 : 1); assert.equal(f.changes.length, 1);
    assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('external update cancels old queued input and retains admission until the old physical callback settles', async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush(); f.set(2); await f.flush();
    f.controller.update(f.make({ minimum: 3, maximum: 9 }).outerHTML, { basis: 'external' });
    f.set(4); await f.flush(); assert.equal(f.pending.length, 1);
    await f.reply(0, 'must-not-commit'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.basis, 'external');
    assert.equal(f.pending[1].request.sourceViewport.x.minimum, 4);
    await f.reply(1); assert.equal(f.controller.getXWindow().minimum, 4);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

for (const reconnect of [false, true]) test(`an abort listener's newer ${reconnect ? 'connection and window' : 'absolute window'} replaces the single queued intent`, async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush();
    f.pending[0].signal.addEventListener('abort', () => {
      if (reconnect) f.connect({ basis: 'reentrant-connection' });
      f.set(3);
    }, { once: true });
    f.set(2); await f.flush();
    assert.equal(f.pending.length, reconnect ? 2 : 1);
    await f.reply(0, 'old'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.sourceViewport.x.minimum, 3);
    assert.equal(f.pending[1].request.basis, reconnect ? 'reentrant-connection' : 'initial');
    await f.reply(1, 'latest'); assert.equal(f.controller.getXWindow().minimum, 3);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('disconnect and reconnect start a new configured-connection bound without accepting old work', async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush(); f.set(2); await f.flush();
    f.controller.connectNative(null); f.connect({ basis: 'new-connection' }); f.set(3); await f.flush();
    assert.equal(f.pending.length, 2, 'old host work may remain outside the new configured connection');
    assert.equal(f.pending[0].signal.aborted, true); assert.equal(f.pending[1].signal.aborted, false);
    assert.equal(f.pending[1].request.basis, 'new-connection');
    f.set(4); await f.flush(); assert.equal(f.pending.length, 2);
    await f.reply(0, 'old'); assert.equal(f.pending.length, 2);
    await f.reply(1, 'stale-new'); assert.equal(f.pending.length, 3);
    assert.equal(f.pending[2].request.basis, 'new-connection');
    await f.reply(2, 'new'); assert.equal(f.controller.getXWindow().minimum, 4);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('replacing the binding with a legacy callback cancels its queued managed intent without claiming a legacy flight bound', async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush(); f.set(2); await f.flush();
    f.controller.connectViewport(f.callback, { axes: ['x'] }); f.set(3); await f.flush();
    assert.equal(f.pending.length, 2); assert.equal(Object.hasOwn(f.pending[1].request, 'basis'), false);
    assert.equal(Object.hasOwn(f.pending[1].request, 'channel'), false);
    f.pending[1].resolve(f.make({ minimum: 3, maximum: 7 }).outerHTML); await f.flush();
    await f.reply(0, 'old'); assert.equal(f.controller.getXWindow().minimum, 3);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

for (const action of ['dispose', 'remove']) test(`${action} releases queued input and rejects late producer completion`, async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush(); f.set(2); await f.flush();
    const svg = f.root.querySelector('.cfx-stage').innerHTML, revision = f.controller.sceneRevision;
    if (action === 'dispose') f.controller.dispose(); else f.root.remove();
    await f.flush(); await f.reply(0, 'late');
    assert.equal(f.pending.length, 1); assert.equal(f.pending[0].signal.aborted, true);
    assert.equal(f.window.ChartForgeXCharts.get(f.root), null); assert.equal(f.controller.sceneRevision, revision);
    assert.equal(f.root.querySelector('.cfx-stage').innerHTML, svg); assert.equal(f.changes.length, 0);
    assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

for (const failure of ['throw', 'reject']) test(`${failure} releases its physical slot and retains the last committed basis`, async () => {
  const f = fixture();
  try {
    const calls = [];
    f.connect({}, request => { calls.push(request); if (failure === 'throw') throw new Error('Native declined'); return Promise.reject(new Error('Native declined')); });
    f.set(1); await f.flush(); f.set(2); await f.flush();
    assert.equal(calls.length, 2); assert.equal(f.failures.length, 2);
    assert.deepEqual(calls.map(request => request.basis), ['initial', 'initial']);
    assert.equal(f.controller.getXWindow().minimum, 0); assert.equal(f.changes.length, 0);
    assert.equal(f.root.querySelector('[data-cfx-export]').disabled, false); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('an invalid current native reply releases admission without publishing its candidate basis or changing the committed scene', async () => {
  const f = fixture();
  try {
    const revision = f.controller.sceneRevision;
    f.set(1); await f.flush(); f.pending[0].resolve({ html: '<p>Not a native section</p>', basis: 'unaccepted' }); await f.flush();
    f.capture('native-admission-invalid-reply');
    assert.equal(f.failures.length, 1); assert.equal(f.changes.length, 0); assert.equal(f.controller.sceneRevision, revision);
    assert.equal(f.root.querySelector('[data-cfx-export]').disabled, false);
    f.set(2); await f.flush(); assert.equal(f.pending[1].request.basis, 'initial');
    await f.reply(1, 'accepted'); assert.equal(f.controller.getXWindow().minimum, 2);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('invalid policy or conflicting cadence fails before changing the active binding, pending signal, scene or request events', async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush(); const html = f.root.innerHTML, revision = f.controller.sceneRevision;
    for (const preparationBackpressure of [null, false, 1, {}, 'latest', ''])
      assert.throws(() => f.connect({ preparationBackpressure }), /preparationBackpressure/);
    assert.throws(() => f.connect({ progressEditing: true, progressCadenceMs: 10 }), /cannot be combined/);
    assert.throws(() => f.connect({ axes: null }), /viewport axes or hierarchy reset owner required/);
    assert.equal(f.pending[0].signal.aborted, false); assert.equal(f.requests.length, 1);
    assert.equal(f.root.innerHTML, html); assert.equal(f.controller.sceneRevision, revision);
    await f.reply(0); assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('disposing an idle-admitted incremental callback restores owned controls before a fresh controller mounts', async () => {
  const f = fixture();
  try {
    f.controller.applyAnnotationChange({ operation: 'replace', id: 'note', drawing: {
      kind: 'Note', start: { x: 4, y: 40 }, label: 'Uncommitted' } }); await f.flush();
    assert.equal(f.pending.length, 1); assert.equal(f.root.querySelector('[data-cfx-export]').disabled, true);
    f.controller.dispose(); assert.equal(f.root.querySelector('[data-cfx-export]').disabled, false);
    const fresh = f.window.ChartForgeXCharts.mount(f.root);
    await f.reply(0, 'late');
    assert.equal(f.window.ChartForgeXCharts.get(f.root), fresh);
    assert.equal(f.root.querySelector('[data-cfx-export]').disabled, false);
    assert.equal(f.root.querySelector('[data-cfx-annotation-id=note]').dataset.cfxLabel, 'Original');
    assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

for (const replaceReset of [false, true]) test(`synthetic hierarchy plus both-axis transport keeps Reset's original channel${replaceReset ? ' until a later absolute window replaces it' : ''}`, async () => {
  // This deliberately controlled routing fixture is not a native model combination.
  // Real native hierarchy-only and numeric/typed viewport proof are separate C# rows.
  const f = fixture('latest-window', { hierarchy: true, bothAxes: true });
  try {
    // Keep the committed hierarchy at its full root while physical viewport work is pending.
    const metadata = JSON.parse(f.root.dataset.cfxPreparedChart); metadata.hierarchy.rootId = null; f.root.dataset.cfxPreparedChart = JSON.stringify(metadata);
    f.set(1); await f.flush();
    f.root.querySelector('[data-cfx-reset]').click(); f.root.querySelector('[data-cfx-reset]').click(); if (replaceReset) f.set(2);
    await f.flush(); assert.equal(f.pending.length, 1); assert.equal(f.requests.length, 1);
    await f.reply(0, 'stale'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.channel, replaceReset ? 'viewport' : 'hierarchy');
    assert.equal(f.pending[1].request.kind, replaceReset ? 'window' : 'reset');
    assert.equal(f.pending[1].request.basis, 'initial');
    await f.reply(1); assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
    assert.equal(f.controller.getHierarchyView().rootId, replaceReset ? 'team' : null);
  } finally { f.close(); }
});

test('hierarchy-only reset shares the physical slot and busy hierarchy operations reject before events', async () => {
  const f = fixture('latest-window', { hierarchy: true });
  try {
    f.controller.setHierarchyView({ rootId: 'team' }); await f.flush();
    f.controller.setHierarchyView(null); await f.flush();
    assert.throws(() => f.controller.setHierarchyView({ rootId: 'team' }), /Native preparation busy/);
    assert.equal(f.pending.length, 1); assert.equal(f.requests.length, 1);
    await f.reply(0, 'stale'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.channel, 'hierarchy'); assert.equal(f.pending[1].request.kind, 'reset');
    assert.equal(f.pending[1].request.basis, 'initial');
    await f.reply(1); assert.equal(f.controller.getHierarchyView().rootId, null);
    assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('synthetic window/hierarchy-reset alternation retains one admission microtask across channel changes', async () => {
  const f = fixture('latest-window', { hierarchy: true, bothAxes: true });
  const original = f.window.Promise.resolve; let scheduled = 0;
  try {
    // Observe the actual browser-promise scheduling boundary; do not expose queue internals.
    f.window.Promise.resolve = function (...args) {
      const promise = original.apply(this, args), then = promise.then;
      promise.then = function (...callbacks) { scheduled++; return then.apply(this, callbacks); };
      return promise;
    };
    for (let index = 0; index < 1000; index++) { f.set(1); f.root.querySelector('[data-cfx-reset]').click(); }
    assert.equal(scheduled, 1); assert.equal(f.requests.length, 0);
    f.window.Promise.resolve = original; await f.flush();
    assert.equal(f.pending.length, 1); assert.equal(f.requests.length, 1);
    assert.equal(f.pending[0].request.channel, 'hierarchy'); assert.equal(f.pending[0].request.kind, 'reset');
    await f.reply(0); assert.equal(f.changes.length, 1); assert.deepEqual(f.pageErrors, []);
  } finally { f.window.Promise.resolve = original; f.close(); }
});

for (const boundary of ['reconnect', 'dispose']) test(`synthetic combined routing ${boundary} drops a queued hierarchy reset without accepting an old callback`, async () => {
  const f = fixture('latest-window', { hierarchy: true, bothAxes: true });
  try {
    f.set(1); await f.flush(); f.root.querySelector('[data-cfx-reset]').click(); await f.flush();
    if (boundary === 'dispose') {
      f.controller.dispose(); await f.reply(0, 'old');
      assert.equal(f.window.ChartForgeXCharts.get(f.root), null); assert.equal(f.pending.length, 1); assert.equal(f.changes.length, 0);
    } else {
      f.controller.connectNative(null); f.connect({ basis: 'new-routing-connection' }); f.set(2); await f.flush();
      assert.equal(f.pending.length, 2); await f.reply(0, 'old'); assert.equal(f.changes.length, 0);
      assert.equal(f.pending[1].request.channel, 'viewport'); assert.equal(f.pending[1].request.basis, 'new-routing-connection');
      await f.reply(1, 'new'); assert.equal(f.changes.length, 1); assert.equal(f.controller.getHierarchyView().rootId, 'team');
    }
    assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});
