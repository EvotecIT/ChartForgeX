'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { fixture } = require('./native-transition-fixture');
function captureFade(f) {
  const destination = f.make(.8), svg = destination.querySelector('svg');
  const frames = [0, .5, 1].map((opacity, index) => {
    const copy = svg.cloneNode(true);
    if (opacity !== 1) {
      const group = copy.ownerDocument.createElementNS('http://www.w3.org/2000/svg', 'g');
      group.setAttribute('data-cfx-role', 'native-transition-opacity'); group.setAttribute('opacity', String(opacity));
      for (const child of [...copy.childNodes]) group.appendChild(child); copy.appendChild(group);
    }
    return { prefix: 'synthetic-fade-' + index, nativeOpacity: String(opacity), svg: copy.outerHTML };
  });
  return { kind: 'native-transition', effect: 'initial-fade', sourceKind: 'native-visual-opacity', readings: [], destination: destination.outerHTML,
    drawings: { wide: { capturePngText: false, sourcePrefix: 'synthetic-source', destinationPrefix: 'synthetic-destination', frames } } };
}
function save(f, name, payload) {
  const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return;
  fs.mkdirSync(output, { recursive: true });
  fs.writeFileSync(path.join(output, name + '.observed.json'), JSON.stringify({ source: f.source(), revision: f.controller.sceneRevision, changes: f.changes, payload }, null, 2));
  fs.writeFileSync(path.join(output, name + '.transport.html'), f.root.outerHTML);
}
test('whole-visual initial fade keeps accepted values revision events targets and existing lifetime', () => {
  const f = fixture('progress-value');
  try {
    const payload = captureFade(f); f.controller.update(payload.destination);
    const revision = f.controller.sceneRevision, changes = f.changes.length, targets = f.controller.resolveTargets().length;
    f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } }); save(f, 'initial-fade-lifetime', payload);
    assert.equal(f.source(), .8); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, changes); assert.equal(f.controller.resolveTargets().length, targets);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    assert.equal(f.root.querySelector('.cfx-native-motion-frame [data-cfx-role="native-transition-opacity"]').getAttribute('opacity'), '0');
    f.controller.resize(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
    f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } }); f.controller.dispose();
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
const invalid = {
  opacityMissing: payload => delete payload.drawings.wide.frames[1].nativeOpacity,
  opacityUnknown: payload => { payload.drawings.wide.frames[1].nativeOpacity = 'unknown'; },
  opacityMismatch: payload => { payload.drawings.wide.frames[1].nativeOpacity = '.4'; },
  primitiveOutside: payload => { const row = payload.drawings.wide.frames[1]; row.svg = row.svg.replace('</svg>', '<rect x="1" y="1" width="2" height="2"/></svg>'); },
  firstVisible: payload => { const row = payload.drawings.wide.frames[0]; row.svg = row.svg.replace('opacity="0"', 'opacity="1"'); },
  duplicate: payload => { const row = payload.drawings.wide.frames[1]; row.svg = row.svg.replace('</svg>', '<path id="synthetic-fade-1-node-99"/><path id="synthetic-fade-1-node-99"/></svg>'); },
  foreignSeries: payload => { const row = payload.drawings.wide.frames[1]; row.svg = row.svg.replace('data-cfx-series="0"', 'data-cfx-series="999"'); },
  finalPaint: payload => { const row = payload.drawings.wide.frames[2]; row.svg = row.svg.replace('#2468ac', '#ff0000'); }
};
for (const [name, mutate] of Object.entries(invalid)) test(`initial fade all-candidate preflight rejects ${name} without presentation or acceptance mutation`, () => {
  const f = fixture('progress-value');
  try {
    const payload = captureFade(f); f.controller.update(payload.destination); const revision = f.controller.sceneRevision, changes = f.changes.length, original = f.root.outerHTML;
    mutate(payload); save(f, 'initial-fade-invalid-' + name, payload);
    assert.throws(() => f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } }), f.window.TypeError);
    assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, changes); assert.equal(f.root.outerHTML, original); assert.equal(f.source(), .8);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
test('stale initial fade and use as an update intent reject before old source or final exports can change', () => {
  const f = fixture('progress-value');
  try {
    const payload = captureFade(f); f.controller.update(payload.destination); const revision = f.controller.sceneRevision, changes = f.changes.length;
    save(f, 'initial-fade-stale', payload);
    assert.throws(() => f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: 'stale' }), /sceneRevision/);
    assert.throws(() => f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload) }), /captured intent/);
    assert.equal(f.source(), .8); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, changes);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
