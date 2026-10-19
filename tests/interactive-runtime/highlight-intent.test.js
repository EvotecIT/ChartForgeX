const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

// The DOM boundary supplies detached target identities; projection and host matching use the actual runtime.
function fixture() {
  let identities = [];
  const root = { dataset: { cfxPreparedChart: JSON.stringify({ series: [
    { key: 'primary', local: false }, { key: 'reference', local: false }, { key: 'local', local: true }
  ] }) } };
  const source = fs.readFileSync(path.resolve(__dirname,
    '../../ChartForgeX.Interactivity.Html/Assets/interactive.source/42-host-commands.js'), 'utf8');
  const runtime = new Function('interactiveTargets', 'isInteractiveTarget', 'renderedTargetKind', 'targetIdentity',
    'matchesTargetIdentity', 'durableChartTarget', 'targetKey', source + '\nreturn { projectHostHighlight, resolveHostTargets };')(
      () => identities.map(identity => ({ dataset: {}, identity })), () => true, () => 'point', node => node.identity,
      (node, target) => (!target.seriesKey || target.seriesKey === node.identity.seriesKey)
        && (target.observationId !== undefined
          ? node.identity.observationId === target.observationId || node.identity.observationIds?.includes(target.observationId)
          : target.targetId === node.identity.targetId),
      target => target.seriesScope === 'shared', target => JSON.stringify([target.targetKind, target.targetId, target.seriesKey]));
  const state = match => ({ match, dim: true, source: 'host' });
  return {
    current(values) { identities = values; },
    project(match) { const input = state(match), before = JSON.stringify(input);
      const projected = runtime.projectHostHighlight(root, input);
      assert.equal(JSON.stringify(input), before); return projected; },
    resolve(projected) { return projected ? runtime.resolveHostTargets(root, projected.match) : []; }
  };
}
const target = (seriesKey, observationId, seriesScope = 'shared') =>
  ({ seriesKey, observationId, seriesScope, targetKind: 'point', targetId: seriesKey + ':' + observationId });

test('unmatched observation intent survives until a value arrives only in the captured shared series', () => {
  const f = fixture();
  const projected = f.project({ observationIds: ['future'] });
  f.current([target('primary', 'future'), target('local', 'future', 'local')]);
  assert.deepEqual(f.resolve(projected).map(item => item.seriesKey), ['primary']);
});

test('unmatched observation descriptors retain a real shared-series scope and ignore a caller scope hint', () => {
  const f = fixture();
  const projected = f.project({ targets: [{ seriesKey: 'primary', observationId: 'future' }] });
  f.current([target('primary', 'future'), target('reference', 'future')]);
  assert.deepEqual(f.resolve(projected).map(item => item.seriesKey), ['primary']);
  assert.equal(f.project({ targets: [{ seriesKey: 'local', observationId: 'future', seriesScope: 'shared' }] }), null);
});

test('an observed local identity does not become an unmatched shared identity during replacement', () => {
  const f = fixture(); f.current([target('local', 'reading', 'local')]);
  assert.equal(f.project({ observationIds: ['reading'] }), null);
  assert.equal(f.project({ targets: [{ observationId: 'reading', seriesScope: 'shared' }] }), null);
});

test('known shared contributor intent follows its new aggregate while retaining its original series', () => {
  const f = fixture();
  f.current([{ seriesKey: 'primary', seriesScope: 'shared', targetKind: 'point', targetId: 'old-bin', observationIds: ['a', 'b'] }]);
  for (const match of [{ observationIds: ['a'] }, { targets: [{ observationId: 'a' }] }]) {
    const projected = f.project(match);
    f.current([{ seriesKey: 'primary', seriesScope: 'shared', targetKind: 'point', targetId: 'new-bin', observationIds: ['a'] }, target('reference', 'a')]);
    assert.deepEqual(f.resolve(projected).map(item => item.targetId), ['new-bin']);
    f.current([{ seriesKey: 'primary', seriesScope: 'shared', targetKind: 'point', targetId: 'old-bin', observationIds: ['a', 'b'] }]);
  }
});
