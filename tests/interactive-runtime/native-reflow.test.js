'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { reflowFixture, flush } = require('./native-reflow-fixture');
function save(f, name, payload, error = null) {
  const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return;
  fs.mkdirSync(output, { recursive: true }); fs.writeFileSync(path.join(output, 'reflow-' + name + '.observed.json'), JSON.stringify({ revision: f.controller.sceneRevision, changes: f.changes, payload, error: error?.message }, null, 2));
  fs.writeFileSync(path.join(output, 'reflow-' + name + '.transport.html'), f.root.outerHTML);
}
test('source and destination frame headers retain different real domains while only destination is accepted', () => {
  const f = reflowFixture(); try {
    const payload = f.captureReflow(), revision = f.controller.sceneRevision;
    f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); save(f, 'basis', payload);
    assert.notEqual(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 1); assert.equal(JSON.parse(f.root.dataset.cfxPreparedChart).yProjection.maximum, 40);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1); assert.equal(f.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=point]').length, 1);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame [data-cfx-role=point]').length, 2); f.controller.resize(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
const invalid = {
  unknownBasis: payload => { payload.drawings.wide.frames[1].basis = 'foreign'; },
  earlyDestination: payload => { payload.drawings.wide.frames[1].basis = 'destination'; },
  missingOpacity: payload => { delete payload.drawings.wide.frames[1].nativeOpacity; },
  wrongOpacity: payload => { payload.drawings.wide.frames[1].nativeOpacity = '.4'; },
  sourceInk: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-y="80"', 'data-cfx-y="81"'); },
  destinationInk: payload => { payload.drawings.wide.frames[3].svg = payload.drawings.wide.frames[3].svg.replace('data-cfx-y="20"', 'data-cfx-y="80"'); },
  foreignSeries: payload => { payload.drawings.wide.frames[2].svg = payload.drawings.wide.frames[2].svg.replace('data-cfx-series="0"', 'data-cfx-series="999"'); },
  duplicateId: payload => { payload.drawings.wide.frames[3].svg = payload.drawings.wide.frames[3].svg.replace('</svg>', '<path id="synthetic-reflow-3-node-9"/><path id="synthetic-reflow-3-node-9"/></svg>'); },
  wrongTargetNamespace: payload => { payload.visibility.toHiddenTargets = [{ kind: 'boxPlotGroup', seriesKey: 'current', observationId: 'other' }]; },
  wrongCatalogOrder: payload => { payload.visibility.targetCatalog = [...payload.visibility.targetCatalog].reverse(); },
  changedSourceKey: payload => { const template = fakedDestination(payload); const metadata = JSON.parse(template.dataset.cfxPreparedChart); metadata.visibility.sourceKey = 'b'.repeat(64); template.dataset.cfxPreparedChart = JSON.stringify(metadata); payload.destination = template.outerHTML; },
  wrapperTransform: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('opacity="0.5"', 'opacity="0.5" transform="translate(10 0)"'); },
  zeroNativeSlot: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('synthetic-reflow-1-node-2', 'synthetic-reflow-1-node-0'); },
  unprovenNativeSlot: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-source-id="series-0-point-0"', ''); },
  danglingGradient: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('fill="#2468ac"', 'fill="url(#synthetic-reflow-1-gradient-2)"'); },
  wrongKindClip: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('</svg>', '<defs><path id="synthetic-reflow-1-clip-2"/></defs></svg>'); },
  negativeNativeSlot: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('synthetic-reflow-1-node-2', 'synthetic-reflow-1-node--1'); },
  lookalikeNativeSlot: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('synthetic-reflow-1-node-2', 'synthetic-reflow-1-node-02'); },
  foreignLayout: payload => { payload.visibility.layout = 'preserve'; }
};
let activeWindow;
function fakedDestination(payload) { const template = activeWindow.document.createElement('template'); template.innerHTML = payload.destination; return template.content.firstElementChild; }
for (const [name, mutate] of Object.entries(invalid)) test('all-candidate Reflow preflight rejects ' + name + ' before basis revision history or events', () => {
  const f = reflowFixture(); try {
    activeWindow = f.window; const payload = f.captureReflow(), original = f.root.outerHTML, revision = f.controller.sceneRevision; mutate(payload);
    let error; try { f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, 'invalid-' + name, payload, error); assert.ok(error instanceof f.window.TypeError); assert.equal(f.root.outerHTML, original); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { activeWindow = null; f.close(); }
});
test('retained point legend keeps authored hidden identity and reset uses the existing absolute descriptor', async () => {
  const f = reflowFixture(); try {
    const calls = []; f.controller.connectNative((request, signal) => new Promise(resolve => calls.push({ request, signal, resolve })), { basis: 'source', axes: ['x'], legend: true, motion: { duration: 1000 } });
    const item = () => f.root.querySelector('[data-cfx-role=legend-item][data-cfx-observation-id=other]'); item().dispatchEvent(new f.window.MouseEvent('click', { bubbles: true })); await flush();
    save(f, 'legend-request', calls[0]?.request); assert.equal(calls.length, 1); assert.deepEqual(JSON.parse(JSON.stringify(calls[0].request.sourceVisibility.hiddenTargets)), [{ kind: 'observation', seriesKey: 'current', observationId: 'other' }]);
    calls[0].resolve({ html: f.makeReflow(true).outerHTML, basis: 'hidden', transition: JSON.stringify(f.captureReflow()) }); await flush();
    save(f, 'legend-accepted', calls[0].request); assert.equal(item().dataset.cfxObservationId, 'other'); assert.equal(item().getAttribute('aria-pressed'), 'false');
    f.root.querySelector('[data-cfx-reset]').click(); await flush(); save(f, 'reset-request', calls[1]?.request); assert.equal(calls[1].request.kind, 'reset'); assert.equal(calls[1].request.basis, 'hidden'); assert.equal(calls[1].request.sourceVisibility.hiddenTargets.length, 0);
  } finally { f.close(); }
});
test('stale and disposed Reflow captures cannot install transient frames', () => {
  const f = reflowFixture(); try {
    const payload = f.captureReflow(), revision = f.controller.sceneRevision; let error;
    try { f.controller.update(payload.destination, { sceneRevision: 'stale', transition: JSON.stringify(payload) }); } catch (failure) { error = failure; }
    save(f, 'stale', payload, error); assert.ok(error); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
    f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); f.controller.dispose(); save(f, 'disposed', payload);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame,[data-cfx-update-motion]').length, 0);
  } finally { f.close(); }
});
test('bounded sustained absolute Reflow updates reuse one native motion layer and no extra source queue', () => {
  const f = reflowFixture(); try {
    for (let index = 0; index < 100; index++) { const payload = f.captureReflow(index % 2 === 0); f.controller.update(payload.destination, { sceneRevision: f.controller.sceneRevision, transition: JSON.stringify(payload), motion: { duration: 1000 } });
      assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1); }
    save(f, 'sustained', { updates: 100 }); assert.equal(f.changes.length, 100); f.controller.dispose(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});

test('queued absolute reset retains typed Reflow intent across ignored abort and reconnect disposal', async () => {
  const f = reflowFixture(); try {
    const calls = []; const callback = (request, signal) => new Promise(resolve => calls.push({ request, signal, resolve }));
    f.controller.connectNative(callback, { basis: 'source', axes: ['x'], legend: true, preparationBackpressure: 'latest-window' });
    f.root.querySelector('[data-cfx-role=legend-item][data-cfx-observation-id=other]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true })); await flush();
    f.root.querySelector('[data-cfx-reset]').click(); await flush(); save(f, 'queued-reset', calls.map(row => row.request));
    assert.equal(calls.length, 1); assert.equal(calls[0].signal.aborted, true);
    calls[0].resolve({ html: f.makeReflow(true).outerHTML, basis: 'ignored' }); await flush(); save(f, 'admitted-reset', calls.map(row => row.request));
    assert.equal(calls.length, 2); assert.equal(calls[1].request.kind, 'reset'); assert.equal(calls[1].request.basis, 'source'); assert.equal(calls[1].request.sourceVisibility.layout, 'reflow'); assert.equal(calls[1].request.sourceVisibility.hiddenTargets.length, 0);
    f.controller.connectNative(null); f.controller.connectNative(callback, { basis: 'reconnected', axes: ['x'], legend: true, preparationBackpressure: 'latest-window' });
    f.root.querySelector('[data-cfx-role=legend-item][data-cfx-observation-id=other]').dispatchEvent(new f.window.MouseEvent('click', { bubbles: true })); await flush();
    const revision = f.controller.sceneRevision; f.controller.dispose(); calls[1].resolve({ html: f.makeReflow(false).outerHTML, basis: 'old' }); calls[2].resolve({ html: f.makeReflow(true).outerHTML, basis: 'new' }); await flush();
    save(f, 'reconnect-dispose', calls.map(row => row.request)); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});

test('one proven outer native group shifts node clip gradient scene indices once while other facts stay exact', () => {
  const f = reflowFixture(); try {
    const style = (section, prefix, offset) => {
      const svg = section.matches('svg') ? section : section.querySelector('svg'), ns = svg.namespaceURI;
      const defs = svg.ownerDocument.createElementNS(ns, 'defs'), clip = svg.ownerDocument.createElementNS(ns, 'clipPath'), gradient = svg.ownerDocument.createElementNS(ns, 'linearGradient');
      clip.id = prefix + '-clip-' + (5 + offset); const rect = svg.ownerDocument.createElementNS(ns, 'rect'); for (const [key, value] of Object.entries({ x: 0, y: 0, width: 360, height: 260 })) rect.setAttribute(key, value); clip.appendChild(rect);
      gradient.id = prefix + '-gradient-' + (7 + offset); const stop = svg.ownerDocument.createElementNS(ns, 'stop'); stop.setAttribute('offset', '0'); stop.setAttribute('stop-color', '#2468ac'); gradient.appendChild(stop); defs.append(clip, gradient); svg.insertBefore(defs, svg.firstChild);
      const mark = svg.querySelector('[data-cfx-role=point] rect'); mark.setAttribute('clip-path', 'url(#' + clip.id + ')'); mark.setAttribute('fill', 'url(#' + gradient.id + ')'); return section;
    };
    const source = style(f.makeReflow(false), 'synthetic-source', 0), destination = style(f.makeReflow(true), 'synthetic-destination', 0); f.controller.update(source); f.changes.length = 0;
    const payload = f.captureReflow(); payload.destination = destination.outerHTML;
    for (const frame of payload.drawings.wide.frames) { const parsed = new f.window.DOMParser().parseFromString(frame.svg, 'image/svg+xml').documentElement; frame.svg = style(parsed, frame.prefix, Number(frame.nativeOpacity) < 1 ? 1 : 0).outerHTML; }
    f.controller.update(payload.destination, { sceneRevision: f.controller.sceneRevision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); save(f, 'native-resource-offset', payload);
    assert.equal(f.changes.length, 1); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
  } finally { f.close(); }
});
