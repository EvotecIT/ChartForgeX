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
    catalog(seriesKey, ids) {
      const metadata = JSON.parse(root.dataset.cfxPreparedChart);
      metadata.series.find(series => series.key === seriesKey).observationIds = ids.map((id, index) => ({ id, index }));
      root.dataset.cfxPreparedChart = JSON.stringify(metadata);
    },
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
  f.catalog('local', ['reading']);
  const explicitlyScoped = f.project({ seriesKey: 'primary', observationIds: ['reading'] });
  f.current([target('primary', 'reading'), target('reference', 'reading')]);
  assert.deepEqual(f.resolve(explicitlyScoped).map(item => item.seriesKey), ['primary']);
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

test('an unpainted authored observation retains its catalog series across replacement', () => {
  const f = fixture();
  f.catalog('primary', ['kept', 'omitted']);
  for (const match of [{ observationIds: ['omitted'] }, { targets: [{ observationId: 'omitted' }] }]) {
    f.current([target('primary', 'kept')]);
    const projected = f.project(match);
    f.current([target('primary', 'omitted'), target('reference', 'omitted')]);
    assert.deepEqual(f.resolve(projected).map(item => item.seriesKey), ['primary']);
  }
});

test('an unpainted local observation cannot become unscoped shared intent', () => {
  const f = fixture();
  f.catalog('local', ['kept', 'omitted']);
  f.current([target('local', 'kept', 'local')]);
  assert.equal(f.project({ observationIds: ['omitted'] }), null);
  assert.equal(f.project({ targets: [{ observationId: 'omitted', seriesScope: 'shared' }] }), null);
  const mixed = f.project({ observationIds: ['omitted', 'future'] });
  f.current([target('primary', 'omitted'), target('primary', 'future')]);
  assert.deepEqual(f.resolve(mixed).map(item => item.observationId), ['future']);
});
