'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { fixture } = require('./native-value-fixture');
// Synthetic transport and lifetime only. Native C#/browser contracts own relationship values, captions, layout and pixels.
function save(f, name, payload, error) {
  const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return;
  fs.mkdirSync(output, { recursive: true }); fs.writeFileSync(path.join(output, 'hierarchy-node-' + name + '.facts.json'), JSON.stringify({ revision: f.controller.sceneRevision, events: f.changes, payload, error: error?.message }, null, 2));
  fs.writeFileSync(path.join(output, 'hierarchy-node-' + name + '.transport.html'), f.root.outerHTML);
}
function setup() { const f = fixture('hierarchy-value'); f.controller.update(f.make(20)); f.changes.length = 0; return f; }
function capture(f, before = 20, after = 40) {
  const legacy = JSON.parse(f.transition(before, after)); return { kind: 'native-transition', effect: 'values', sourceKind: 'hierarchy-value', destination: f.make(after).outerHTML,
    readings: [{ seriesKey: 'current', nodeId: 'value', fromValue: before, toValue: after, minimum: 0, maximum: Number.MAX_VALUE }], drawings: {
      wide: { capturePngText: false, sourcePrefix: 'synthetic-hierarchy-source', destinationPrefix: 'synthetic-hierarchy-destination', frames: legacy.wide.map((svg, index) => ({ prefix: 'synthetic-hierarchy-frame-' + index, svg })) } } };
}
for (const legacy of [false, true]) test('native hierarchy ' + (legacy ? 'one-edit' : 'multi-reading') + ' keeps node identity and accepted endpoint while using one motion layer', () => {
  const f = setup(); try { const payload = legacy ? JSON.parse(f.transition(20, 40)) : capture(f); const revision = f.controller.sceneRevision;
    f.controller.update(f.make(40), { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); save(f, 'accepted-' + legacy, payload);
    assert.equal(f.source(), 40); assert.notEqual(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 1); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    f.controller.resize(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
const malformed = {
  observationMasquerade: payload => { payload.readings[0].observationId = 'value'; delete payload.readings[0].nodeId; },
  duplicateNode: payload => { payload.readings.push({ ...payload.readings[0] }); },
  wrongRawValue: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-authored-value="30"', 'data-cfx-authored-value="-1"'); },
  foreignNode: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-node="value"', 'data-cfx-node="other"'); },
  missingAuthoredSize: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-authored-value="30"', ''); },
  foreignSeries: payload => { payload.drawings.wide.frames[2].svg = payload.drawings.wide.frames[2].svg.replace('data-cfx-series="0"', 'data-cfx-series="99"'); }
};
for (const [name, mutate] of Object.entries(malformed)) test('all-candidate hierarchy validation rejects ' + name + ' before acceptance', () => {
  const f = setup(); try { const payload = capture(f), original = f.root.outerHTML, revision = f.controller.sceneRevision; mutate(payload); let error;
    try { f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, name, payload, error); assert.ok(error instanceof f.window.TypeError); assert.equal(f.root.outerHTML, original); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});
test('hierarchy explicit initial presentation preserves revision and stale captures cannot install work', () => {
  const f = setup(); try { f.controller.update(f.make(40)); f.changes.length = 0; const payload = capture(f); payload.effect = 'initial-values'; const revision = f.controller.sceneRevision;
    f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } }); save(f, 'initial', payload);
    assert.equal(f.controller.sceneRevision, revision); assert.equal(f.source(), 40); assert.equal(f.changes.length, 0);
    f.controller.update(f.make(50)); let error; try { f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision }); } catch (failure) { error = failure; }
    save(f, 'stale', payload, error); assert.ok(error); assert.equal(f.source(), 50); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
test('bounded sustained hierarchy updates use the incumbent cancellation and disposal owner', () => {
  const f = setup(); try { for (let index = 0; index < 100; index++) { const payload = capture(f, index % 2 ? 40 : 20, index % 2 ? 20 : 40);
      f.controller.update(payload.destination, { sceneRevision: f.controller.sceneRevision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1); }
    save(f, 'sustained', { updates: 100 }); assert.equal(f.changes.length, 100); f.controller.dispose(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame,[data-cfx-update-motion]').length, 0);
  } finally { f.close(); }
});
