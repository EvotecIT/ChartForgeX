const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

// Native DOM/catalog facts form the boundary; identity, projection and scenario matching use the shipped source.
function fixture() {
  const source = name => {
    const text = fs.readFileSync(path.resolve(__dirname,
      '../../ChartForgeX.Interactivity.Html/Assets/interactive.source', name + '.js'), 'utf8');
    // The next canonical fragment completes bindChart; these contracts exercise the preceding scenario owner.
    return name === '60-scenarios' ? text.slice(0, text.indexOf('  const bindChart = (root) => {')) : text;
  };
  const nodes = [], svg = { querySelectorAll: selector => {
    if (selector.includes('data-cfx-role="annotation"') || selector.includes('data-cfx-role^="annotation"')) return nodes.filter(node =>
      (selector.includes('data-cfx-role^=') ? node.dataset.cfxRole.startsWith('annotation') : node.dataset.cfxRole === 'annotation')
      || selector.includes('data-cfx-target-kind') && node.dataset.cfxTargetKind === 'annotation');
    return selector === '[data-cfx-source-id]' ? nodes.filter(node => node.dataset.cfxSourceId !== undefined) : [];
  }, getAttribute: () => null };
  const root = { dataset: {}, querySelector: () => svg, querySelectorAll: () => nodes, closest: () => null };
  const mark = (sourceId, annotationId, label = 'Limit', value = '4') => {
    const node = { id: sourceId + '-dom', parentElement: root,
      dataset: { cfxRole: 'annotation', cfxKind: 'HorizontalLine', cfxSourceId: sourceId,
        cfxLabel: label, cfxValue: value },
      getAttribute: () => null, closest: selector => selector === '.cfx-interactive-chart' ? root : selector === 'svg' ? svg : null };
    if (annotationId !== undefined) node.dataset.cfxAnnotationId = annotationId;
    nodes.push(node); return node;
  };
  const warning = mark('annotation-id-d2FybmluZw', 'warning');
  const critical = mark('annotation-id-Y3JpdGljYWw', 'critical');
  const anonymous = mark('annotation-2');
  const metadata = () => ({ series: [], xLabels: [],
    annotations: nodes.filter(node => node.dataset.cfxAnnotationId !== undefined).map(node => ({
      id: node.dataset.cfxAnnotationId, sourceId: node.dataset.cfxSourceId, kind: node.dataset.cfxKind })),
    regions: nodes.map(node => ({ id: node.dataset.cfxSourceId, role: 'annotation' })) });
  const document = { querySelectorAll: () => [] }, window = {};
  const runtime = ['00-bootstrap-targets', '00-bootstrap-tooltip', '01-annotation-identity', '05-prepared-targets',
    '40-selection-hover-sync', '42-host-commands', '60-scenarios', '85-chart-update'].map(source).join('\n');
  const api = new Function('window', 'document', 'return ' + runtime + `
    return { annotationCatalog, prepareChartTargets, targetIdentity, targetKey, matchesTargetIdentity,
      selectionTargetBatch, durableChartTarget, projectHostHighlight, resolveHostTargets, scenarioTargets };
  })();`)(window, document);
  const prepare = () => { root.dataset.cfxPreparedChart = JSON.stringify(metadata()); api.prepareChartTargets(root); };
  prepare();
  return { api, nodes, svg, root, warning, critical, anonymous, metadata, prepare };
}

test('native annotation catalogs reject missing, duplicate and mismatched bindings before changing targets', () => {
  const f = fixture(), before = JSON.stringify(f.nodes);
  assert.equal(f.api.annotationCatalog(f.svg, f.metadata()).size, 2);
  const mutate = change => { const metadata = f.metadata(); change(metadata); return metadata; };
  for (const metadata of [
    mutate(value => delete value.annotations),
    mutate(value => value.annotations.push({ ...value.annotations[0] })),
    mutate(value => value.annotations[1].id = value.annotations[0].id),
    mutate(value => value.annotations[0].id = 'unknown'),
    mutate(value => value.annotations[0].sourceId = 'annotation-0'),
    mutate(value => value.annotations[0].kind = 'Note'),
    mutate(value => value.regions.shift())
  ]) assert.throws(() => f.api.annotationCatalog(f.svg, metadata), /prepared annotation metadata/);
  assert.equal(JSON.stringify(f.nodes), before);
});

test('anonymous annotation groups and native children cannot claim authored namespaces through any target ID', () => {
  for (const role of ['annotation', 'annotation-line', 'annotation-band', 'annotation-label', 'annotation-label-backplate', 'annotation-label-leader']) {
    for (const field of ['cfxSourceId', 'cfxId', 'cfxTargetId']) {
      for (const registered of [true, false]) {
        const f = fixture(), node = role === 'annotation' ? f.anonymous
          : { ...f.anonymous, id: '', dataset: { cfxRole: role }, parentElement: f.anonymous };
        if (node !== f.anonymous) f.nodes.push(node);
        node.dataset[field] = registered ? f.warning.dataset.cfxSourceId : 'annotation-id-unknown';
        const before = JSON.stringify(f.nodes), binding = node._cfxAnnotationId;
        assert.throws(() => f.api.prepareChartTargets(f.root), /prepared annotation metadata/);
        assert.equal(JSON.stringify(f.nodes), before);
        assert.equal(node._cfxAnnotationId, binding);
        assert.equal(f.warning._cfxAnnotationId, 'warning');
      }
    }
  }
});

test('resolved authored DTOs retain identity after refreshed facts and serialized selection while anonymous targets stay local', () => {
  const f = fixture(), first = f.api.resolveHostTargets(f.root).find(target => target.annotationId === 'warning');
  assert.equal(first.seriesScope, 'shared'); assert.equal(first.seriesKey, '');
  const captured = JSON.parse(JSON.stringify(first)), key = f.api.targetKey(first);
  f.warning.dataset.cfxLabel = 'Translated limit'; f.warning.dataset.cfxValue = '6';
  f.nodes.reverse(); f.prepare();
  assert.equal(f.api.targetKey(f.api.targetIdentity(f.warning)), key);
  assert.equal(f.api.matchesTargetIdentity(f.warning, captured), true);
  assert.deepEqual(f.api.selectionTargetBatch(f.root).resolve(captured), [f.warning]);
  assert.equal(f.api.durableChartTarget(f.api.targetIdentity(f.warning)), true);
  const local = f.api.targetIdentity(f.anonymous);
  assert.equal(local.annotationId, undefined); assert.equal(local.seriesScope, 'local');
  assert.equal(f.api.durableChartTarget(local), false);
  assert.equal(f.api.resolveHostTargets(f.root, { targets: [captured] })[0].numericValue, 6);
  first.annotationId = 'edited by caller';
  assert.equal(f.api.targetIdentity(f.warning).annotationId, 'warning');
});

test('unknown authored identities and caller shared hints cannot select or project anonymous or caption peers', () => {
  const f = fixture(), local = f.api.targetIdentity(f.anonymous), real = f.api.targetIdentity(f.warning);
  for (const target of [
    { ...local, annotationId: 'warning', seriesScope: 'shared' },
    { ...real, annotationId: 'unknown', label: 'Limit', value: '4' },
    { targetKind: 'annotation', annotationId: 'unknown', role: 'annotation', label: 'Limit' }
  ]) {
    assert.deepEqual(f.api.resolveHostTargets(f.root, { targets: [target] }), []);
    assert.deepEqual(f.api.selectionTargetBatch(f.root).resolve(target), []);
    assert.equal(f.api.projectHostHighlight(f.root, { match: { targets: [target] } }), null);
  }
  const state = { match: { targets: [real] }, dim: true }, before = JSON.stringify(state);
  const projected = f.api.projectHostHighlight(f.root, state);
  assert.equal(JSON.stringify(state), before);
  assert.equal(projected.match.targets[0].annotationId, 'warning');
  projected.match.targets[0].annotationId = 'caller edit';
  assert.equal(f.api.targetIdentity(f.warning).annotationId, 'warning');
});

test('authored scenario namespace stays exact when a target is removed and a caption resembles its ID', () => {
  const f = fixture(), id = f.warning.dataset.cfxSourceId;
  const route = { steps: [{ targetKind: 'annotation', targetId: id }] };
  f.critical.dataset.cfxLabel = id;
  assert.deepEqual([...f.api.scenarioTargets(f.root, route)], [f.warning]);
  f.nodes.splice(f.nodes.indexOf(f.warning), 1); f.prepare();
  assert.deepEqual([...f.api.scenarioTargets(f.root, route)], []);
  assert.deepEqual([...f.api.scenarioTargets(f.root, { steps: [{ targetKind: 'annotation', targetId: 'Limit' }] })], [f.anonymous]);
});
