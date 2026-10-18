const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { Point, identitySvg, getComputedStyle } = require('../helpers/identity-svg');
const source = name => fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source', name + '.js'), 'utf8');

function readouts() {
  return new Function('isDataTarget', 'targetCategory', 'seriesLabel', 'renderedTargetKind', 'renderedTargetId', 'seriesKey',
    'contributorObservationIds', 'targetRawX', 'targetDataRange', 'sourcePointIndex', 'sourcePointCollection', 'dataTargets', 'timeTarget',
    source('00-bootstrap-tooltip') + '\n' + source('10-legend-summary') + '\nreturn {text,targetIdentity,compareItems,tooltipRows,comparisonLegendRows};')(
    () => true, node => node.dataset.cfxXLabel, node => node.dataset.cfxSeriesName, () => 'point', node => node.dataset.cfxPoint,
    node => node.dataset.cfxSeriesKey, () => undefined, node => Number(node.dataset.cfxX), node => node.range,
    node => Number(node.dataset.cfxPoint), () => undefined, root => root.nodes, node => node.dataset.cfxXScale === 'time');
}
function mark(dataset, range) {
  return {dataset, range, attributes: [], getAttribute: () => null, classList: {contains: name => name === 'cfx-selected'}};
}
test('target readouts publish captured bin labels and retain exact closed logic endpoints', () => {
  const runtime = readouts();
  const node = mark({cfxPoint:'0', cfxSeriesName:'Duration', cfxKind:'bar', cfxY:'1', cfxX:'50',
    cfxDisplayLabel:'40 – 60', cfxRangeLabel:'40 – 60'}, [40, 59.99999999999999]);
  const target = runtime.targetIdentity(node);
  assert.equal(target.label, '40 – 60'); assert.equal(target.rangeLabel, '40 – 60');
  assert.deepEqual(target.range, [40, 59.99999999999999]);
  assert.equal(runtime.compareItems({nodes:[node]})[0].label, '40 – 60');
});
test('time and numeric tooltip titles use captured axis text while compare chips identify observations or series', () => {
  const runtime = readouts();
  const time = mark({cfxPoint:'0',cfxSeriesName:'Requests',cfxKind:'line',cfxX:'46086',cfxY:'27',
    cfxXScale:'time',cfxDisplayLabel:'2026-03-05',cfxFormattedX:'2026-03-05'});
  const scatter = mark({cfxPoint:'1',cfxSeriesName:'Samples',cfxKind:'scatter',cfxX:'4.242394913100822',cfxY:'8.8888888888889',
    cfxDisplayLabel:'4,2',cfxFormattedX:'4,2'});
  assert.equal(runtime.text(time, true), '2026-03-05'); assert.equal(runtime.text(scatter, true), '4,2');
  assert.deepEqual(runtime.compareItems({nodes:[time,scatter]}).map(item => item.label), ['Requests · 2026-03-05','Samples']);
  time.dataset.cfxAuthoredLabel = '';
  assert.equal(runtime.text(time, true), ''); assert.equal(runtime.compareItems({nodes:[time]})[0].label, '');
});
test('compare chips distinguish equal dates across series', () => {
  const runtime = readouts();
  const api = mark({cfxPoint:'0',cfxSeries:'0',cfxSeriesName:'API',cfxKind:'line',cfxXScale:'time',cfxDisplayLabel:'2026-03-04'});
  const worker = mark({cfxPoint:'0',cfxSeries:'1',cfxSeriesName:'Worker',cfxKind:'line',cfxXScale:'time',cfxDisplayLabel:'2026-03-04'});
  assert.deepEqual(runtime.compareItems({nodes:[api,worker]}).map(item => item.label), ['API · 2026-03-04','Worker · 2026-03-04']);
});
test('scatter tooltip and selection use captured display values while numericValue remains exact', () => {
  const runtime = readouts(), value = 2.6130852767327268;
  const scatter = mark({cfxPoint:'0',cfxSeriesName:'Samples',cfxKind:'scatter',cfxX:'66.09458950219',cfxY:String(value),
    cfxFormattedX:'66.09',cfxFormattedValue:'2.61',cfxDisplayLabel:'66.09'});
  assert.deepEqual(runtime.tooltipRows(scatter).map(row=>row.value), ['Samples','66.09','2.61']);
  assert.equal(runtime.targetIdentity(scatter).value, '2.61');
  assert.equal(runtime.targetIdentity(scatter).numericValue, value);
  assert.equal(scatter.dataset.cfxY, String(value));
});
test('box plot targets expose their median and state timeline targets expose their named state', () => {
  const runtime = readouts();
  const box = runtime.targetIdentity(mark({cfxKind:'boxplot',cfxY:'1',cfxMinimum:'1',cfxMedian:'3',cfxMaximum:'5'}));
  assert.equal(box.numericValue, 3); assert.equal(box.value, '3');
  const state = runtime.targetIdentity(mark({cfxKind:'statetimeline',cfxStateLabel:'Available',cfxEnd:'46083.25'}));
  assert.equal(state.numericValue, undefined); assert.equal(state.value, 'Available');
});
test('comparison tooltip rows use captured value-axis bounds and preserve their raw facts', () => {
  const runtime = readouts();
  const range = mark({cfxKind:'rangebar',cfxCategoryLabel:'Category 0',cfxStart:'10.25',cfxEnd:'24.75',
    cfxFormattedStart:'10,25 ms',cfxFormattedEnd:'24,75 ms'});
  assert.deepEqual(runtime.tooltipRows(range).map(row=>row.value),['Category 0','10,25 ms','24,75 ms']);
  const waterfall = mark({cfxKind:'waterfall',cfxWaterfallKind:'delta',cfxValue:'14.5',cfxStart:'10.25',cfxEnd:'24.75',
    cfxFormattedValue:'14,50 ms',cfxFormattedStart:'10,25 ms',cfxFormattedEnd:'24,75 ms'});
  const values = runtime.tooltipRows(waterfall).map(row=>row.value);
  assert.deepEqual(values,['14,50 ms','10,25 ms','24,75 ms']);
  const dumbbell = mark({cfxKind:'dumbbell',cfxCategoryLabel:'Category 0',cfxStart:'10.25',cfxEnd:'24.75',cfxDelta:'14.5',
    cfxFormattedStart:'10,25 ms',cfxFormattedEnd:'24,75 ms'});
  assert.deepEqual(runtime.comparisonLegendRows(dumbbell).map(row=>row.value),['Category 0','10,25 ms','24,75 ms','14.5']);
  assert.equal(range.dataset.cfxStart,'10.25'); assert.equal(waterfall.dataset.cfxEnd,'24.75');
});

function brushes() {
  return new Function('DOMPoint','getComputedStyle','dataValue','clamp', source('00-svg-coordinate-precision') + '\n' + source('43-brush-projection') + '\nreturn {projectBrushSelection,validDataProjection};')(
    Point, getComputedStyle, (value,time) => time ? new Date((value-25569)*86400000).toISOString() : value,
    (value,min,max) => Math.max(min,Math.min(max,value)));
}
const axis = (minimum,maximum,scale,displayTicks) => ({minimum,maximum,transformedMinimum:minimum,transformedMaximum:maximum,
  scale,threshold:1,reversed:false,displayTicks});
const root = projection => ({_cfxDataProjection:projection,dataset:{cfxBrushAxis:'x',cfxPreparedChart:'{"xLabels":[]}'},
  querySelector:identitySvg});
test('brush labels and filtering bounds use the same enclosing prepared axis ticks', () => {
  const runtime = brushes(), minimum = 46266;
  const projection = {horizontal:false,plotBounds:{x:0,y:0,width:100,height:100},x:axis(minimum,minimum+10,'time',
    [0,2,4,6,8,10].map(value => ({value:minimum+value,text:'Date '+value})))};
  const result = runtime.projectBrushSelection(root(projection),{left:21,top:0,right:83,bottom:100}).data;
  assert.equal(result.rangeLabel,'Date 2 – Date 10'); assert.equal(result.label,result.rangeLabel);
  assert.deepEqual(result.displayRange.x,[new Date((minimum+2-25569)*86400000).toISOString(),new Date((minimum+10-25569)*86400000).toISOString()]);
  assert.deepEqual(result.range.x,result.displayRange.x); assert.deepEqual(result.categoryRange,result.range.x);
});
test('horizontal brush display uses the category axis and accepts deliberately empty captured captions', () => {
  const runtime = brushes();
  const projection = {horizontal:true,plotBounds:{x:0,y:0,width:100,height:100},
    x:axis(0,100,'linear',[{value:0,text:'0 unités'},{value:100,text:'100 unités'}]),
    y:axis(0,10,'linear',[{value:0,text:''},{value:10,text:'End'}])};
  const host = root(projection); host.dataset.cfxBrushAxis = 'xy';
  const result = runtime.projectBrushSelection(host,{left:5,top:0,right:95,bottom:100}).data;
  assert.equal(result.rangeLabel,' – End'); assert.deepEqual(result.categoryRange,[0,10]);
  assert.equal(runtime.validDataProjection(projection),true);
  projection.y.displayTicks[1].text = '';
  assert.equal(runtime.projectBrushSelection(host,{left:5,top:0,right:95,bottom:100}).data.rangeLabel,'');
  projection.y.displayTicks[0].text = null;
  assert.equal(runtime.validDataProjection(projection),false);
});
