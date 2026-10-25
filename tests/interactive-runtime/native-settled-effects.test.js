'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { reflowFixture } = require('./native-reflow-fixture');
// Synthetic transport/reference routing. NativeSettledMotionSeamTests and the real browser sibling retain actual generated E01 artifacts.
function style(section, prefix, offset = 0, filterKind = 'surface-shadow') {
  const svg = section.matches('svg') ? section : section.querySelector('svg'), document = svg.ownerDocument, ns = svg.namespaceURI;
  const defs = document.createElementNS(ns, 'defs'), filter = document.createElementNS(ns, 'filter'), mask = document.createElementNS(ns, 'mask');
  filter.id = prefix + '-' + filterKind + '-' + (8 + offset); mask.id = prefix + '-retained-mask-' + offset;
  filter.setAttribute('filterUnits', 'userSpaceOnUse'); mask.setAttribute('maskUnits', 'userSpaceOnUse');
  defs.append(filter, mask); svg.insertBefore(defs, svg.firstChild);
  const point = svg.querySelector('[data-cfx-role=point]'); point.setAttribute('filter', 'url(#' + filter.id + ')'); point.setAttribute('mask', 'url(#' + mask.id + ')');
  return section;
}
function capture(f, filterKind = 'surface-shadow') {
  const source = style(f.makeReflow(false), 'synthetic-source', 0, filterKind), destination = style(f.makeReflow(true), 'synthetic-destination', 0, filterKind);
  f.controller.update(source); f.changes.length = 0;
  const payload = f.captureReflow(); payload.destination = destination.outerHTML;
  for (const frame of payload.drawings.wide.frames) {
    const svg = new f.window.DOMParser().parseFromString(frame.svg, 'image/svg+xml').documentElement;
    frame.svg = style(svg, frame.prefix, Number(frame.nativeOpacity) < 1 ? 1 : 0, filterKind).outerHTML;
  }
  return payload;
}
function save(f, name, payload, error) {
  const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return;
  fs.mkdirSync(output, { recursive: true });
  fs.writeFileSync(path.join(output, 'settled-effect-' + name + '.observed.json'), JSON.stringify({ revision: f.controller.sceneRevision, changes: f.changes, payload, error: error?.message }, null, 2));
  fs.writeFileSync(path.join(output, 'settled-effect-' + name + '.transport.html'), f.root.outerHTML);
}
test('exact typed filter mask resources include native index0 and shift once only behind the proven Reflow wrapper', () => {
  const f = reflowFixture(); try {
    const payload = capture(f); let error;
    try { f.controller.update(payload.destination, { sceneRevision: f.controller.sceneRevision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, 'typed-resource-offset', payload, error); assert.equal(error, undefined); assert.equal(f.changes.length, 1); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
  } finally { f.close(); }
});
const invalid = {
  duplicate: svg => svg.replace('</defs>', '<filter id="synthetic-reflow-1-surface-shadow-9"/></defs>'),
  wrongKind: svg => svg.replace('<filter ', '<path ').replace('</filter>', '</path>'),
  dangling: svg => svg.replace('url(#synthetic-reflow-1-retained-mask-1)', 'url(#synthetic-reflow-1-retained-mask-99)'),
  wrongReferenceKind: svg => svg.replace('mask="url(#synthetic-reflow-1-retained-mask-1)"', 'mask="url(#synthetic-reflow-1-surface-shadow-9)"'),
  foreignPrefix: svg => svg.replaceAll('synthetic-reflow-1-retained-mask-1', 'foreign-retained-mask-1'),
  lookalike: svg => svg.replaceAll('synthetic-reflow-1-retained-mask-1', 'synthetic-reflow-1-retained-mask-01'),
  zeroWrapped: svg => svg.replaceAll('synthetic-reflow-1-retained-mask-1', 'synthetic-reflow-1-retained-mask-0'),
  negative: svg => svg.replaceAll('synthetic-reflow-1-retained-mask-1', 'synthetic-reflow-1-retained-mask--1')
};
for (const [name, mutate] of Object.entries(invalid)) test('typed native effect resource ' + name + ' rejects before acceptance or presentation mutation', () => {
  const f = reflowFixture(); try {
    const payload = capture(f), before = f.root.outerHTML, revision = f.controller.sceneRevision; payload.drawings.wide.frames[1].svg = mutate(payload.drawings.wide.frames[1].svg);
    let error; try { f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, name, payload, error); assert.ok(error instanceof f.window.TypeError); assert.equal(f.root.outerHTML, before); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});

for (const kind of ['retained-outline', 'retained-bloom', 'retained-shadow']) {
  test(`ordered appearance ${kind} retains the exact generated filter identity behind the captured Reflow wrapper`, () => {
    const f = reflowFixture();
    try {
      const payload = capture(f, kind);
      f.controller.update(payload.destination, { sceneRevision: f.controller.sceneRevision,
        transition: JSON.stringify(payload), motion: { duration: 1000 } });
      save(f, kind, payload);
      assert.equal(f.changes.length, 1);
      assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
    } finally { f.close(); }
  });
  test(`ordered appearance ${kind} rejects a wrong generated definition kind before acceptance`, () => {
    const f = reflowFixture();
    try {
      const payload = capture(f, kind), revision = f.controller.sceneRevision, before = f.root.outerHTML;
      payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg
        .replace('<filter ', '<path ').replace('</filter>', '</path>');
      save(f, kind + '-wrong-kind', payload);
      assert.throws(() => f.controller.update(payload.destination, { sceneRevision: revision,
        transition: JSON.stringify(payload), motion: { duration: 1000 } }), f.window.TypeError);
      assert.equal(f.root.outerHTML, before);
      assert.equal(f.controller.sceneRevision, revision);
      assert.equal(f.changes.length, 0);
    } finally { f.close(); }
  });
}

test('exact retained inner-shadow filter identities use the existing native frame equivalence boundary', () => {
  const f = reflowFixture(); try {
    const payload = capture(f, 'retained-inner-shadow'); let error;
    try { f.controller.update(payload.destination, { sceneRevision: f.controller.sceneRevision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, 'inner-resource-offset', payload, error); assert.equal(error, undefined); assert.equal(f.changes.length, 1);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
  } finally { f.close(); }
});
test('retained inner-shadow dangling references cannot advance acceptance or presentation', () => {
  const f = reflowFixture(); try {
    const payload = capture(f, 'retained-inner-shadow'), before = f.root.outerHTML, revision = f.controller.sceneRevision;
    payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replaceAll(
      'url(#synthetic-reflow-1-retained-inner-shadow-9)', 'url(#synthetic-reflow-1-retained-inner-shadow-99)');
    let error; try { f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, 'inner-dangling-resource', payload, error); assert.ok(error instanceof f.window.TypeError);
    assert.equal(f.root.outerHTML, before); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});
