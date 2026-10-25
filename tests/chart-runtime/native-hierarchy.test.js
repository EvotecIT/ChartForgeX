'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source/87-native-hierarchy.js'), 'utf8');
function fixture() {
  const model = { kind: 'treemap', seriesKey: 'work', rootId: null, path: [] };
  const root = { dataset: {}, querySelector: () => null, querySelectorAll: () => [ { dataset: { cfxNode: 'team' } } ] };
  const requests = []; const binding = {};
  const api = new Function('capturedCartesianMetadata', 'hasFeature', 'nativePreparationBinding', 'copyHostValue',
    'chartLifetimes', 'uiText', 'interactiveTargets', 'requestNativePreparation', 'selectionSignature', 'emitHostEvent',
    'publishResetSelection', 'publishInteractionState', 'syncResetControl', 'listenChart', 'nativeLegendConnected', source + '\nreturn {requestNativeHierarchy,hierarchySnapshot};')(
    target => ({ hierarchy: target.model || model }), () => true, () => binding, value => JSON.parse(JSON.stringify(value)),
    new WeakMap([[root, {}]]), (_, __, fallback) => fallback, () => [],
    (target, channel, payload, hooks) => { requests.push({ target, channel, payload, hooks }); return true; },
    () => '', () => {}, () => {}, () => {}, () => {}, () => {}, () => false); // This isolated hierarchy unit keeps native legend preparation disconnected.
  return { root, model, requests, api };
}
test('hierarchy requests validate source identity and use the existing managed transaction seam', () => {
  const { root, requests, api } = fixture();
  for (const view of [{ rootId: 'absent' }, { rootId: '' }, { rootId: 'team', copiedNodes: [] }, 'team'])
    assert.equal(api.requestNativeHierarchy(root, view), false);
  assert.equal(requests.length, 0);
  assert.equal(api.requestNativeHierarchy(root, { rootId: 'team' }), true);
  assert.deepEqual(requests[0].payload, { kind: 'root', rootId: 'team' }); assert.equal(requests[0].channel, 'hierarchy');
  requests[0].hooks.validate({ model: { kind: 'treemap', seriesKey: 'work', rootId: 'team' } });
  for (const model of [{ kind: 'sunburst', seriesKey: 'work', rootId: 'team' },
    { kind: 'treemap', seriesKey: 'different', rootId: 'team' }, { kind: 'treemap', seriesKey: 'work', rootId: null }])
    assert.throws(() => requests[0].hooks.validate({ model }), TypeError);
  assert.equal(api.requestNativeHierarchy(root, null), true); assert.deepEqual(requests[1].payload, { kind: 'reset', rootId: null });
});
test('committed root observations are detached from host mutation', () => {
  const { root, model, api } = fixture(); model.rootId = 'team'; model.path.push({ id: 'team', label: 'Team' });
  const snapshot = api.hierarchySnapshot(root); snapshot.path[0].label = 'Changed';
  assert.equal(api.hierarchySnapshot(root).path[0].label, 'Team'); assert.equal(model.rootId, 'team');
});
