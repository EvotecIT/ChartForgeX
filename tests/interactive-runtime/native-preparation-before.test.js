'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const { fixture } = require('./native-preparation-fixture');

for (const policy of ['omitted', 'immediate']) test(`${policy} immediate native requests retain supersession, response promises and committed-basis behavior`, async () => {
  const f = fixture(policy);
  try {
    assert.equal(f.set(1), f.controller); await f.flush();
    f.set(2); await f.flush(); f.capture('native-admission-default-before-' + policy);
    assert.equal(f.pending.length, 2); assert.equal(f.pending[0].signal.aborted, true);
    assert.equal(Object.hasOwn(f.pending[1].request, 'preparationBackpressure'), false);
    assert.equal(f.pending[1].request.basis, 'initial'); assert.equal(f.requests.length, 2);
    await f.reply(1, 'second'); await f.reply(0, 'stale');
    assert.equal(f.controller.getXWindow().minimum, 2); assert.equal(f.changes.length, 1);
    assert.equal(f.failures.length, 0); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('opt-in replacement waits for an ignored-abort physical callback instead of starting another producer', async () => {
  const f = fixture();
  try {
    f.set(1); await f.flush();
    for (let index = 0; index < 32; index++) { f.set(2 + index / 100); await f.flush(); }
    f.capture('native-admission-causal-before');
    assert.equal(f.pending.length, 1, 'one configured-connection physical callback remains unresolved');
    assert.equal(f.pending[0].signal.aborted, true); assert.equal(f.requests.length, 1);
    assert.equal(f.controller.getXWindow().minimum, 0); assert.equal(f.changes.length, 0);
    await f.reply(0, 'stale'); assert.equal(f.pending.length, 2);
    assert.equal(f.pending[1].request.sourceViewport.x.minimum, 2.31);
    assert.equal(f.pending[1].request.basis, 'initial');
    await f.reply(1, 'latest'); assert.equal(f.controller.getXWindow().minimum, 2.31);
    assert.equal(f.changes.length, 1); assert.equal(f.failures.length, 0); assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});
