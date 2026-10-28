'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const { fixture } = require('./native-value-fixture');
for (const kind of ['progress-value', 'circle-value', 'bullet-value']) test(`ordinary legacy ${kind} retains current direct update payload and cleanup`, () => {
  const f = fixture(kind);
  try {
    const revision = f.controller.sceneRevision, destination = f.make(.8 * f.scale).outerHTML;
    f.controller.update(destination, { sceneRevision: revision, transition: f.transition(.2 * f.scale, .8 * f.scale), motion: { duration: 1000 } });
    assert.equal(f.source(), .8 * f.scale); assert.notEqual(f.controller.sceneRevision, revision);
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1); f.controller.resize();
    assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 0);
  } finally { f.close(); }
});
