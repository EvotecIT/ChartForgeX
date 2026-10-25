'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { fixture } = require('./native-value-fixture');
// Synthetic identity/transport/preflight contracts. C# native and real browser rows own geometry, glyph and pixel proof.
function save(f, name, payload, error) {
  const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return;
  fs.mkdirSync(output, { recursive: true });
  fs.writeFileSync(path.join(output, 'radial-node-' + name + '.observed.json'), JSON.stringify({ revision: f.controller.sceneRevision, events: f.changes, payload, error: error?.message }, null, 2));
  fs.writeFileSync(path.join(output, 'radial-node-' + name + '.transport.html'), f.root.outerHTML);
}
function capture(f, before = 20, after = 40) {
  const destination = f.make(after), legacy = JSON.parse(f.transition(before, after));
  return { kind: 'native-transition', effect: 'values', sourceKind: 'radial-value', destination: destination.outerHTML,
    readings: [{ seriesKey: 'current', observationId: 'value', fromValue: before, toValue: after, minimum: 0, maximum: Number.MAX_VALUE }],
    drawings: { wide: { capturePngText: false, sourcePrefix: 'synthetic-radial-source', destinationPrefix: 'synthetic-radial-destination',
      frames: legacy.wide.map((svg, index) => ({ prefix: 'synthetic-radial-frame-' + index, svg })) } } };
}
function setup() { const f = fixture('radial-value'); f.controller.update(f.make(20)); f.changes.length = 0; return f; }
for (const legacy of [true, false]) test('existing radial ' + (legacy ? 'one-edit' : 'multi-reading') + ' transport keeps accepted final source and one transient layer', () => {
  const f = setup(); try {
    const payload = legacy ? JSON.parse(f.transition(20, 40)) : capture(f), revision = f.controller.sceneRevision;
    f.controller.update(f.make(40), { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); save(f, 'accepted-' + legacy, payload);
    assert.notEqual(f.controller.sceneRevision, revision); assert.equal(f.source(), 40); assert.equal(f.changes.length, 1);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    f.controller.resize(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
const invalid = {
  foreignIdentity: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-observation-id="value"', 'data-cfx-observation-id="foreign"'); },
  foreignSeries: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-series="0"', 'data-cfx-series="999"'); },
  wrongDomain: payload => { payload.readings[0].maximum = 100; },
  negativeValue: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-value="30"', 'data-cfx-value="-1"'); },
  duplicateReading: payload => { payload.readings.push({ ...payload.readings[0] }); },
  malformedLastFrame: payload => { payload.drawings.wide.frames[2].svg = '<svg><'; }
};
for (const [name, mutate] of Object.entries(invalid)) test('all-candidate radial preparation rejects ' + name + ' before accepted basis or events', () => {
  const f = setup(); try {
    const payload = capture(f), html = f.root.outerHTML, revision = f.controller.sceneRevision; mutate(payload); let error;
    try { f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, name, payload, error); assert.ok(error instanceof f.window.TypeError); assert.equal(f.root.outerHTML, html); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});
test('radial initial presentation leaves accepted revision and events unchanged and stale replay cannot replace it', () => {
  const f = setup(); try {
    f.controller.update(f.make(40)); f.changes.length = 0; const payload = capture(f); payload.effect = 'initial-values'; const revision = f.controller.sceneRevision;
    f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } }); save(f, 'initial', payload);
    assert.equal(f.controller.sceneRevision, revision); assert.equal(f.source(), 40); assert.equal(f.changes.length, 0); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    f.controller.update(f.make(50)); let error; try { f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision }); } catch (failure) { error = failure; }
    save(f, 'stale-initial', payload, error); assert.ok(error); assert.equal(f.source(), 50); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
test('bounded sustained radial replacements retain one incumbent layer and disposal cancels its work', () => {
  const f = setup(); try {
    for (let index = 0; index < 100; index++) { const before = index % 2 ? 40 : 20, after = index % 2 ? 20 : 40, payload = capture(f, before, after);
      f.controller.update(payload.destination, { sceneRevision: f.controller.sceneRevision, transition: JSON.stringify(payload), motion: { duration: 1000 } });
      assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1); }
    save(f, 'sustained', { count: 100 }); assert.equal(f.changes.length, 100); f.controller.dispose(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame,[data-cfx-update-motion]').length, 0);
  } finally { f.close(); }
});

function bandFixture() {
  const f = fixture('radial-value');
  const make = (inner = 20, outer = 45) => {
    const root = f.make(inner), svg = root.querySelector('svg'), first = svg.querySelector('[data-cfx-role=radial-point]'), next = first.cloneNode(true);
    Object.assign(next.dataset, { cfxSeries: '1', cfxSeriesKey: 'outer', cfxValue: String(outer) }); svg.appendChild(next);
    const metadata = JSON.parse(root.dataset.cfxPreparedChart); metadata.series.push({ ...metadata.series[0], key: 'outer', name: 'Outer' }); root.dataset.cfxPreparedChart = JSON.stringify(metadata); return root;
  };
  const capture = (legacy = false) => {
    const frames = [[20,45],[30,50],[40,55]].map(values => make(...values).querySelector('svg').outerHTML);
    if (legacy) return { kind: 'radial-value', seriesKey: 'outer', observationId: 'value', fromValue: 45, toValue: 55, minimum: 0, maximum: Number.MAX_VALUE,
      wide: [[20,45],[20,50],[20,55]].map(values => make(...values).querySelector('svg').outerHTML) };
    return { kind: 'native-transition', sourceKind: 'radial-value', effect: 'values', destination: make(40,55).outerHTML,
      readings: [{ seriesKey: 'current', observationId: 'value', fromValue: 20, toValue: 40, minimum: 0, maximum: Number.MAX_VALUE }, { seriesKey: 'outer', observationId: 'value', fromValue: 45, toValue: 55, minimum: 0, maximum: Number.MAX_VALUE }],
      drawings: { wide: { capturePngText: false, sourcePrefix: 'band-source', destinationPrefix: 'band-destination', frames: frames.map((svg,index) => ({ prefix: 'band-frame-' + index, svg })) } } };
  };
  f.controller.update(make()); f.changes.length = 0; return { ...f, makeBands: make, captureBands: capture };
}
for (const legacy of [true,false]) test('multiband radial ' + (legacy ? 'one-edit' : 'multi-reading') + ' keeps duplicate observation IDs scoped by the actual series', () => {
  const f = bandFixture(); try { const payload = f.captureBands(legacy), destination = legacy ? f.makeBands(20,55) : f.makeBands(40,55), revision = f.controller.sceneRevision;
    f.controller.update(destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); save(f, 'bands-' + legacy, payload);
    const rows = [...f.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=radial-point]')]; assert.equal(rows.length, 2);
    assert.deepEqual(rows.map(row => [row.dataset.cfxSeriesKey,row.dataset.cfxObservationId,row.dataset.cfxValue]), [['current','value',legacy ? '20' : '40'],['outer','value','55']]);
    assert.equal(f.changes.length, 1); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1); f.controller.resize(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
for (const corruption of ['duplicateReading','foreignBand','lastFrame']) test('multiband all-frame preparation rejects ' + corruption + ' before source revision or events change', () => {
  const f = bandFixture(); try { const payload = f.captureBands(), original = f.root.outerHTML, revision = f.controller.sceneRevision;
    if (corruption === 'duplicateReading') payload.readings[1].seriesKey = 'current';
    else if (corruption === 'foreignBand') payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-series-key="outer"', 'data-cfx-series-key="foreign"');
    else payload.drawings.wide.frames[2].svg = '<svg><';
    let error; try { f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, 'bands-invalid-' + corruption, payload, error); assert.ok(error instanceof f.window.TypeError); assert.equal(f.root.outerHTML, original); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});
test('multiband initial values preserve accepted destination and revision through the one presentation lifetime', () => {
  const f = bandFixture(); try { const payload = f.captureBands(); payload.effect = 'initial-values'; f.controller.update(payload.destination); f.changes.length = 0; const revision = f.controller.sceneRevision;
    f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } }); save(f, 'bands-initial', payload);
    assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    f.controller.dispose(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame,[data-cfx-update-motion]').length, 0);
  } finally { f.close(); }
});
