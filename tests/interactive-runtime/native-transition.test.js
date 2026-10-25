'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { fixture, capture, flush } = require('./native-transition-fixture');
function save(f, name, payload) {
  const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return;
  fs.mkdirSync(output, { recursive: true });
  fs.writeFileSync(path.join(output, name + '.observed.json'), JSON.stringify({ revision: f.controller.sceneRevision, source: f.source(), changes: f.changes, payload }, null, 2));
  fs.writeFileSync(path.join(output, name + '.transport.html'), f.root.outerHTML);
}
for (const effect of ['values', 'value-flash']) test(`${effect} uses the same accepted scene, passive scheduler and disposal owner`, () => {
  const f = fixture('progress-value');
  try {
    const revision = f.controller.sceneRevision, payload = capture(f, .2, .8, effect);
    f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } });
    save(f, 'native-generic-' + effect, payload);
    assert.equal(f.source(), .8); assert.equal(f.changes.length, 1); assert.notEqual(f.controller.sceneRevision, revision);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    assert.equal(f.root.querySelector('.cfx-native-motion-frame').getAttribute('inert'), '');
    f.controller.dispose(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
test('explicit initial presentation retains accepted endpoint, revision and event history', async () => {
  const f = fixture('progress-value', true);
  try {
    const payload = capture(f, .2, .8, 'initial-values'); f.controller.update(payload.destination, { basis: 'authoritative-final' });
    const revision = f.controller.sceneRevision, changes = f.changes.length, targets = f.controller.resolveTargets().length;
    f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } });
    save(f, 'native-initial-presentation', payload);
    assert.equal(f.source(), .8); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, changes);
    assert.equal(f.controller.resolveTargets().length, targets); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    f.controller.resize(); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
    assert.throws(() => f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: 'stale' }), /sceneRevision/);
    assert.throws(() => f.controller.update(payload.destination, { basis: 'bad', sceneRevision: revision, transition: JSON.stringify(payload) }), /supported captured intent/);
    await flush(); assert.equal(f.controller.sceneRevision, revision);
  } finally { f.close(); }
});
const tamper = {
  duplicate: svg => svg.replace('</svg>', '<path id="synthetic-frame-1-node-0"/><path id="synthetic-frame-1-node-0"/></svg>'),
  dangling: svg => svg.replace('</svg>', '<g clip-path="url(#synthetic-frame-1-clip-99)"/></svg>'),
  wrongKind: svg => svg.replace('</svg>', '<path id="synthetic-frame-1-clip-9"/><g clip-path="url(#synthetic-frame-1-clip-9)"/></svg>'),
  lookalike: svg => svg.replace('</svg>', '<path id="synthetic-frame-1lookalike-node-0"/></svg>'),
  semantic: svg => svg.replace('data-cfx-observation-id="value"', 'data-cfx-observation-id="changed"'),
  series: svg => svg.replace('data-cfx-series="0"', 'data-cfx-series="999"')
};
for (const [kind, mutate] of Object.entries(tamper)) test(`all candidate preflight rejects ${kind} before source, revision or events change`, () => {
  const f = fixture('progress-value');
  try {
    const revision = f.controller.sceneRevision, payload = capture(f, .2, .8), original = f.root.innerHTML;
    payload.drawings.wide.frames[1].svg = mutate(payload.drawings.wide.frames[1].svg); save(f, 'native-invalid-' + kind, payload);
    assert.throws(() => f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }), f.window.TypeError);
    assert.equal(f.controller.sceneRevision, revision); assert.equal(f.root.innerHTML, original); assert.equal(f.source(), .2); assert.equal(f.changes.length, 0);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
for (const field of ['data-cfx-source-id', 'geometry', 'paint', 'text', 'authored-link', 'glyph-matrix']) test(`exact endpoint comparison preserves ${field} rather than erasing identities`, () => {
  const f = fixture('progress-value');
  try {
    const payload = capture(f, .2, .8), frame = payload.drawings.wide.frames[2];
    if (field === 'data-cfx-source-id') frame.svg = frame.svg.replace('<rect ', '<rect data-cfx-source-id="changed" ');
    if (field === 'geometry') frame.svg = frame.svg.replace('width="200"', 'width="201"');
    if (field === 'paint') frame.svg = frame.svg.replace('#2468ac', '#ff0000');
    if (field === 'text') frame.svg = frame.svg.replace('</svg>', '<text>Changed</text></svg>');
    if (field === 'authored-link') frame.svg = frame.svg.replace('</svg>', '<a href="#host-detail"><text>Changed</text></a></svg>');
    if (field === 'glyph-matrix') frame.svg = frame.svg.replace('</svg>', '<defs><path id="synthetic-frame-2-png-glyph-0" d="M0 0L1 1"/></defs><text data-cfx-png-glyphs="synthetic-frame-2-png-glyph-0;matrix(1 0 0 1 999 999)">X</text></svg>');
    const revision = f.controller.sceneRevision; save(f, 'native-endpoint-' + field, payload);
    assert.throws(() => f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload) }), /endpoint/);
    assert.equal(f.controller.sceneRevision, revision); assert.equal(f.source(), .2);
  } finally { f.close(); }
});
test('initial endpoint rejection and supersession leave no presentation work or acceptance event', () => {
  const f = fixture('progress-value');
  try {
    const payload = capture(f, .2, .8, 'initial-values'); f.controller.update(payload.destination); const revision = f.controller.sceneRevision;
    const invalid = structuredClone(payload); invalid.drawings.wide.frames[2].svg = invalid.drawings.wide.frames[2].svg.replace('width="200"', 'width="201"');
    save(f, 'native-initial-endpoint-reject', invalid);
    assert.throws(() => f.controller.presentInitial(JSON.stringify(invalid), { sceneRevision: revision, motion: { duration: 1000 } }), /endpoint/);
    f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision, motion: { duration: 1000 } }); const layer = f.root.querySelector('.cfx-native-motion-frame');
    f.controller.update(f.make(.9).outerHTML); assert.equal(layer.isConnected, false); assert.equal(f.source(), .9);
    assert.throws(() => f.controller.presentInitial(JSON.stringify(payload), { sceneRevision: revision }), /sceneRevision/);
  } finally { f.close(); }
});
