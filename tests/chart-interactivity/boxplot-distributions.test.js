const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source');
const names = ['00-bootstrap-targets', '00-bootstrap-tooltip', '01-annotation-identity', '05-prepared-targets', '35-series-state',
  '40-selection-hover-sync', '42-host-commands', '85-chart-update'];
const source = names.map(name => fs.readFileSync(path.join(assets, name + '.js'), 'utf8')).join('\n').replace(/^\(\(\) => \{/, '');
const runtime = new Function('window', 'document', source + '\nreturn { pointTargetId, targetIdentity, boxPlotTooltipRows, seriesTarget, matchesLocalSeriesTarget, inHoverUnit, durableChartTarget, matchesTargetIdentity, resolveSeriesTarget, prepareChartTargets };')({}, {});
const key = name => name.replace(/^data-/, '').replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
function fixture() {
  const root = { dataset: {}, attributes: {}, nodes: [], getAttribute(name) { return this.attributes[name] ?? null; },
    querySelector(selector) { return selector === '.cfx-stage svg' ? svg : null; }, querySelectorAll(selector) { return svg.querySelectorAll(selector); } };
  const svg = { getAttribute() { return null; }, setAttribute() {}, querySelectorAll(selector) {
    return root.nodes.filter(node => selector.split(',').some(part => [...part.matchAll(/\[([^=\]]+)(?:="([^"]*)")?\]/g)]
      .every(([, name, value]) => node.dataset[key(name)] !== undefined && (value === undefined || node.dataset[key(name)] === value))));
  } };
  const make = data => {
    const node = { dataset: { cfxSeries: '0', cfxSeriesKey: 's:"quoted"', cfxSeriesName: 'Latency', cfxSeriesScope: 'shared',
      cfxRole: 'point', cfxKind: 'BoxPlot', cfxPoint: '0', cfxX: '1', cfxCategoryLabel: 'Service A', ...data }, attributes: [], children: [], parentElement: null,
      closest(selector) {
        if (selector === 'svg') return svg;
        if (selector === '.cfx-interactive-chart') return root;
        const label = selector.match(/^\[data-cfx-label-([^\]]+)\]$/);
        return label && root.attributes['data-cfx-label-' + label[1]] !== undefined ? root : null;
      }, getAttribute(name) { return this.dataset[key(name)] ?? null; }, setAttribute(name, value) { this.dataset[key(name)] = String(value); },
      hasAttribute(name) { return this.dataset[key(name)] !== undefined; }, contains(other) { return other === this; } };
    root.nodes.push(node); return node;
  };
  return { root, svg, make };
}
const summary = extra => ({ cfxDerived: 'five-number-summary', cfxGroupIndex: '0', cfxGroupId: 'same:"escaped"', cfxSourceCount: '10',
  cfxMinimum: '0', cfxQ1: '2.25', cfxMedian: '4.5', cfxQ3: '6.75', cfxMaximum: '8', cfxWhiskerPolicy: 'Iqr',
  cfxQuartileEstimator: 'LinearInterpolated', cfxIqrMultiplier: '1.5', cfxSourceMinimum: '0', cfxSourceMaximum: '100',
  cfxInlierMinimum: '0', cfxInlierMaximum: '8', cfxOutlierCount: '1', ...extra });

test('raw group summary and outlier use distinct escaped identity namespaces and truthful provenance', () => {
  const { make } = fixture();
  const group = make(summary());
  const outlier = make({ cfxKind: 'BoxPlotOutlier', cfxPoint: '11', cfxSourcePoint: '9', cfxObservationId: 'same:"escaped"',
    cfxGroupIndex: '0', cfxGroupId: 'same:"escaped"', cfxValue: '100' });
  const groupTarget = runtime.targetIdentity(group), sampleTarget = runtime.targetIdentity(outlier);
  assert.equal(groupTarget.targetId, JSON.stringify(['s:"quoted"', 'five-number-summary', 'same:"escaped"']));
  assert.equal(sampleTarget.targetId, JSON.stringify(['s:"quoted"', 'same:"escaped"']));
  assert.equal(groupTarget.groupIndex, 0); assert.equal(groupTarget.sourceCount, 10);
  assert.equal(groupTarget.sourcePoint, undefined); assert.equal(groupTarget.sourcePoints, undefined); assert.equal(groupTarget.observationId, undefined);
  assert.equal(sampleTarget.sourcePoint, '9'); assert.equal(sampleTarget.groupId, groupTarget.groupId); assert.equal(sampleTarget.derived, undefined);
  assert.equal(runtime.matchesTargetIdentity(outlier, groupTarget), false);
  assert.equal(runtime.matchesTargetIdentity(group, sampleTarget), false);
  const anonymous = make(summary({ cfxGroupId: undefined, cfxPoint: '2', cfxGroupIndex: '2' }));
  assert.equal(runtime.pointTargetId(anonymous), 's:"quoted":derived:five-number-summary:2');
  assert.equal(runtime.pointTargetId(make({ cfxPoint: '1', cfxObservationId: 'supplied' })), JSON.stringify(['s:"quoted"', 'supplied']));
});

test('IQR readouts distinguish drawable whiskers, source extrema, actual inliers and no-inlier state', () => {
  const { root, make } = fixture();
  root.attributes['data-cfx-label-lower-whisker'] = 'Dolny wąs';
  root.attributes['data-cfx-label-source-minimum'] = 'Minimum próbek';
  const sparse = make(summary({ cfxMinimum: '75', cfxQ1: '75', cfxMedian: '100.5', cfxQ3: '101.25', cfxMaximum: '102',
    cfxSourceCount: '4', cfxSourceMaximum: '102', cfxInlierMinimum: '100', cfxInlierMaximum: '102' }));
  const rows = runtime.boxPlotTooltipRows(sparse);
  assert.ok(rows.some(row => row.name === 'Dolny wąs' && row.value === '75'));
  assert.ok(rows.some(row => row.name === 'Minimum próbek' && row.value === '0'));
  assert.ok(rows.some(row => row.name === 'Inlier minimum' && row.value === '100'));
  assert.ok(rows.some(row => row.name === 'Sample count' && row.value === '4'));
  assert.ok(rows.some(row => row.name === 'Quartile estimator' && row.value === 'LinearInterpolated'));
  assert.ok(rows.some(row => row.name === 'IQR multiplier' && row.value === '1.5'));
  assert.equal(rows.some(row => row.name === 'Minimum' && row.value === '75'), false);
  const none = runtime.boxPlotTooltipRows(make(summary({ cfxInlierMinimum: undefined, cfxInlierMaximum: undefined })));
  assert.ok(none.some(row => row.name === 'Inlier minimum' && row.value === 'None'));
  assert.ok(none.some(row => row.name === 'Inlier maximum' && row.value === 'None'));
  const outlier = runtime.boxPlotTooltipRows(make({ cfxKind: 'BoxPlotOutlier', cfxValue: '100', cfxFormattedValue: '100 ms' }));
  assert.deepEqual(outlier, [{ name: 'Category', value: 'Service A' }, { name: 'Outlier', value: '100 ms' }]);
  const supplied = runtime.boxPlotTooltipRows(make({ cfxMinimum: '1', cfxQ1: '2', cfxMedian: '3', cfxQ3: '4', cfxMaximum: '5' }));
  assert.deepEqual(supplied.map(row => row.name), ['Category', 'Minimum', 'Q1', 'Median', 'Q3', 'Maximum']);
});

test('BoxPlot rows preserve present empty captions and consume captured extrema without formatting the IQR factor as units', () => {
  const { make } = fixture();
  const summaryRows = runtime.boxPlotTooltipRows(make(summary({
    cfxCategoryLabel: '', cfxFormattedQ1: '', cfxFormattedSourceMinimum: '0,10 ms',
    cfxFormattedSourceMaximum: '100,12 ms', cfxFormattedInlierMinimum: '', cfxFormattedInlierMaximum: '8,00 ms',
    cfxIqrMultiplier: '0.10000000000000001', cfxFormattedIqrMultiplier: '0.1',
    cfxMinimum: '0.25', cfxInlierMinimum: '0.1', cfxMaximum: '8.5', cfxInlierMaximum: '8'
  })));
  assert.deepEqual(summaryRows[0], { name: 'Category', value: '' });
  assert.ok(summaryRows.some(row => row.name === 'Q1' && row.value === ''));
  assert.ok(summaryRows.some(row => row.name === 'Source minimum' && row.value === '0,10 ms'));
  assert.ok(summaryRows.some(row => row.name === 'Source maximum' && row.value === '100,12 ms'));
  assert.ok(summaryRows.some(row => row.name === 'Inlier minimum' && row.value === ''));
  assert.ok(summaryRows.some(row => row.name === 'Inlier maximum' && row.value === '8,00 ms'));
  assert.ok(summaryRows.some(row => row.name === 'IQR multiplier' && row.value === '0.1'));
  const outlierRows = runtime.boxPlotTooltipRows(make({ cfxKind: 'BoxPlotOutlier', cfxCategoryLabel: '', cfxValue: '100.123', cfxFormattedValue: '' }));
  assert.deepEqual(outlierRows, [{ name: 'Category', value: '' }, { name: 'Outlier', value: '' }]);
  const partial = runtime.boxPlotTooltipRows(make({ cfxMinimum: '1', cfxQ1: '2', cfxFormattedQ1: '', cfxMedian: '3' }));
  assert.deepEqual(partial, [{ name: 'Category', value: 'Service A' }, { name: 'Minimum', value: '1' },
    { name: 'Q1', value: '' }, { name: 'Median', value: '3' }]);
});

test('group legend emphasis and muting cover only its summary and outliers while sample selection stays exact', () => {
  const { make } = fixture();
  const group = make(summary());
  const outlier = make({ cfxKind: 'BoxPlotOutlier', cfxPoint: '11', cfxSourcePoint: '9', cfxGroupIndex: '0', cfxGroupId: 'same:"escaped"' });
  const other = make(summary({ cfxGroupIndex: '1', cfxGroupId: 'other', cfxPoint: '1' }));
  const legend = make(summary({ cfxRole: 'legend-item' })), target = runtime.seriesTarget(legend);
  assert.equal(runtime.matchesLocalSeriesTarget(group, target), true);
  assert.equal(runtime.matchesLocalSeriesTarget(outlier, target), true);
  assert.equal(runtime.matchesLocalSeriesTarget(other, target), false);
  assert.equal(runtime.inHoverUnit(outlier, runtime.targetIdentity(legend), true, group), true);
  assert.equal(runtime.inHoverUnit(other, runtime.targetIdentity(legend), true, group), false);
  assert.equal(runtime.seriesTarget(outlier).targetId, target.targetId);
  assert.equal(runtime.matchesTargetIdentity(group, runtime.targetIdentity(outlier)), false);
});

test('prepared replacement retains keyed group or rendered keyed sample and clears anonymous and vanished targets', () => {
  const { root, make } = fixture();
  const group = make(summary()), target = runtime.targetIdentity(group);
  const outlier = make({ cfxKind: 'BoxPlotOutlier', cfxPoint: '11', cfxSourcePoint: '9', cfxObservationId: 'sample-9', cfxGroupIndex: '0', cfxGroupId: 'same:"escaped"' });
  assert.equal(runtime.durableChartTarget(target), true);
  assert.equal(runtime.durableChartTarget(runtime.targetIdentity(outlier)), true);
  assert.equal(runtime.durableChartTarget(runtime.targetIdentity(make(summary({ cfxGroupId: undefined })))), false);
  assert.equal(runtime.durableChartTarget(runtime.targetIdentity(make({ cfxKind: 'BoxPlotOutlier', cfxGroupIndex: '0', cfxGroupId: 'same:"escaped"' }))), false);
  assert.equal(runtime.durableChartTarget({ ...target, seriesScope: 'local' }), false);
  group.dataset.cfxPoint = '1'; group.dataset.cfxGroupIndex = '1';
  assert.equal(runtime.matchesTargetIdentity(group, target), true);
  const sampleTarget = runtime.targetIdentity(outlier);
  root.nodes = root.nodes.filter(node => node !== outlier);
  assert.equal(root.nodes.some(node => runtime.matchesTargetIdentity(node, sampleTarget)), false);
  assert.equal(runtime.resolveSeriesTarget(root, runtime.seriesTarget(group)).groupId, target.groupId);
});

test('prepared targets preserve native outlier kind and give group legends only group identities', () => {
  const { root, make } = fixture();
  root.dataset.cfxPreparedChart = JSON.stringify({ series: [{ name: 'Latency', key: 'raw', local: false, state: 'none', kind: 'boxplot', indices: [] }], regions: [], xLabels: [] });
  const group = make(summary({ cfxSeriesKey: 'raw', cfxSourceId: 'series-0-point-0' }));
  const legend = make({ cfxRole: 'legend-entry', cfxSeriesKey: 'raw', cfxSourceId: 'legend-series-0-point-0', cfxKind: undefined });
  const outlier = make({ cfxKind: 'BoxPlotOutlier', cfxPoint: '11', cfxSourcePoint: '9', cfxObservationId: 'sample-9', cfxGroupIndex: '0', cfxGroupId: 'same:"escaped"' });
  runtime.prepareChartTargets(root);
  assert.equal(outlier.dataset.cfxKind, 'BoxPlotOutlier');
  assert.equal(legend.dataset.cfxDerived, 'five-number-summary'); assert.equal(legend.dataset.cfxGroupId, group.dataset.cfxGroupId);
  assert.equal(legend.dataset.cfxObservationId, undefined); assert.equal(group.dataset.cfxSourcePoint, undefined);
  assert.equal(runtime.pointTargetId(legend), runtime.pointTargetId(group));
});
