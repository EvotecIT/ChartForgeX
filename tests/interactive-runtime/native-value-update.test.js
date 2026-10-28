'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const { fixture } = require('./native-value-fixture');
for (const managed of [false, true]) test(`direct native transition ${managed ? 'with' : 'without'} managed basis rejects missing and stale scene revisions before mutation`, () => {
  const f = fixture('layered-radial-value', managed);
  try {
    const revision = f.controller.sceneRevision, html = f.root.innerHTML, next = f.make(80).outerHTML, transition = f.transition(20, 80);
    assert.throws(() => f.controller.update(next, { basis: 'bad', transition, motion: { duration: 1000 } }), /current source sceneRevision/);
    assert.equal(f.root.innerHTML, html); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
    // Values and native paint deliberately remain identical, so this isolates the observation guard.
    f.controller.update(f.make(20).outerHTML, { basis: 'external' }); const current = f.controller.sceneRevision, installed = f.root.innerHTML;
    assert.notEqual(current, revision);
    assert.throws(() => f.controller.update(next, { basis: 'stale', sceneRevision: revision, transition }), /current source sceneRevision/);
    assert.throws(() => f.controller.update(next, { basis: 'malformed', sceneRevision: current, transition: '{' }), /invalid JSON/);
    assert.equal(f.root.innerHTML, installed); assert.equal(f.controller.sceneRevision, current);
    f.controller.update(next, { basis: 'fresh', sceneRevision: current, transition, motion: { duration: 1000 } });
    assert.equal(f.source(), 80); assert.equal(f.changes.length, 2); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
  } finally { f.close(); }
});
for (const kind of ['progress-value', 'layered-radial-value', 'circle-value', 'progress-ring-value', 'gauge-value', 'bullet-value']) test(`${kind} uses the same passive frame and commit owner through direct update, supersession and disposal`, () => {
  const f = fixture(kind);
  try {
    const first = .2 * f.scale, second = .8 * f.scale, third = .9 * f.scale;
    const update = (before, after) => f.controller.update(f.make(after).outerHTML, { sceneRevision: f.controller.sceneRevision,
      transition: f.transition(before, after), motion: { duration: 1000 } });
    update(first, second); const old = f.root.querySelector('.cfx-native-motion-frame');
    assert.equal(old.getAttribute('aria-hidden'), 'true'); assert.equal(old.hasAttribute('inert'), true);
    assert.equal(old.dataset.cfxPassive, 'true'); assert.equal(f.source(), second);
    const targetCount = f.controller.resolveTargets().length;
    update(second, third); assert.equal(old.isConnected, false); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    assert.equal(f.controller.resolveTargets().length, targetCount); assert.equal(f.source(), third);
    f.controller.resize(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
    const pending = { html: f.make(second).outerHTML, sceneRevision: f.controller.sceneRevision, transition: f.transition(third, second) };
    f.controller.dispose(); assert.throws(() => f.controller.update(pending.html, pending), /disposed/);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0); assert.equal(f.source(), third);
  } finally { f.close(); }
});
