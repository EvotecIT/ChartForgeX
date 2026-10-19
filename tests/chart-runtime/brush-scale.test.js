const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

// Exercise the real pointer-to-payload owner; the DOM boundary is an identity SVG transform.
const source = ['00-svg-coordinate-precision', '43-brush-projection'].map(name =>
  fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source', name + '.js'), 'utf8')).join('\n');
const { Point, identitySvg, getComputedStyle } = require('../helpers/identity-svg');
const { projectBrushSelection, finishBrushSelection } = new Function('DOMPoint', 'getComputedStyle', 'dataValue', 'clamp', 'stagePoint',
  source + '\nreturn {projectBrushSelection,finishBrushSelection};')(
  Point, getComputedStyle, (value, time) => time ? new Date((value - 25569) * 86400000).toISOString() : value,
  (value, min, max) => Math.max(min, Math.min(max, value)), (_, point) => point);
const selectionSource = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source/40-selection-hover-sync.js'), 'utf8');
const hostSource = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source/42-host-commands.js'), 'utf8');
const dataValue = (value,time) => time ? new Date((Number(value)-25569)*86400000).toISOString() : Number(value);
const { overlapsRange } = new Function(hostSource.slice(hostSource.indexOf('  const intervalNumber'), hostSource.indexOf('  const matchesHostTarget')) + '\nreturn {overlapsRange};')();
const selectTargetsInBox = new Function('hasFeature', 'dataTargets', 'clearSelections', 'setNodeSelected', 'targetIdentity', 'syncResetControl', 'syncSelectionAppearance',
  'keyboardTargetAvailable', 'overlapsRange', 'tooltipNumber', 'dataValue',
  selectionSource.slice(selectionSource.indexOf('  const nodeCenterInRect'), selectionSource.indexOf('  const applySync')) + '\nreturn selectTargetsInBox;')(
  () => true, root => root.targets, root => root.targets.forEach(node => node.selected = false),
  (node, selected) => node.selected = selected, node => ({id:node.id,targetKind:'point',x:node.dataset.cfxXInstant ?? dataValue(node.dataset.cfxX,node.time)}), () => {}, () => {},
  node => node.available !== false, overlapsRange, value => value !== undefined && value !== '' && Number.isFinite(Number(value)), dataValue);

function brush(axis, from, to, horizontal = false) {
  const category = {minimum:0, maximum:1, transformedMinimum:0, transformedMaximum:1, scale:'linear', threshold:1, reversed:false};
  const projection = {horizontal, plotBounds:{x:0, y:0, width:100, height:100},
    x:horizontal ? category : axis, ...(horizontal ? {y:axis} : {})};
  const root = {_cfxDataProjection:projection, dataset:{cfxBrushAxis:horizontal ? 'xy' : 'x', cfxPreparedChart:'{"xLabels":[]}'},
    querySelector:identitySvg};
  return projectBrushSelection(root, horizontal
    ? {left:0, right:100, top:100 * (1 - to), bottom:100 * (1 - from)}
    : {left:100 * from, right:100 * to, top:0, bottom:100}).data;
}

for (const reversed of [false, true]) {
  test(`logarithmic brush ranges enclose the gesture in the same painted ticks as their captions (reversed=${reversed})`, () => {
    const axis = {minimum:10, maximum:100, transformedMinimum:1, transformedMaximum:2,
      scale:'logarithmic', threshold:1, reversed, displayTicks:[{value:10, text:'10 units'}, {value:100, text:'100 units'}]};
    const result = brush(axis, .45, .55);
    assert.deepEqual(result.displayRange.x, [10, 100]);
    assert.equal(result.rangeLabel, '10 units – 100 units');
    assert.deepEqual(result.range.x, [10, 100]);
    assert.deepEqual(result.categoryRange, result.range.x);
    assert.deepEqual(result.range.x, result.displayRange.x);
  });
}

for (const horizontal of [false, true]) {
  test(`symmetric logarithmic brush captions use the painted negative and positive tick intervals (horizontal=${horizontal})`, () => {
    const axis = {minimum:-198, maximum:198, transformedMinimum:-2, transformedMaximum:2,
      scale:'symmetriclogarithmic', threshold:2, reversed:false,
      displayTicks:[{value:-18, text:'−18'}, {value:0, text:'zero'}, {value:18, text:'18'}]};
    const result = brush(axis, .36, .64, horizontal);
    const direction = horizontal ? 'y' : 'x';
    assert.deepEqual(result.displayRange[direction], [-18, 18]);
    assert.equal(result.rangeLabel, '−18 – 18');
    assert.deepEqual(result.range[direction], [-18, 18]);
    assert.deepEqual(result.categoryRange, result.range[direction]);
    const nearZero = brush(axis, .40, .60, horizontal);
    assert.deepEqual(nearZero.displayRange[direction], [-18, 18]);
    assert.equal(nearZero.rangeLabel, '−18 – 18');
  });
}

test('linear brush captions and filtering ranges use enclosing ticks even for an interior singleton gesture', () => {
  const axis = {minimum:10, maximum:100, transformedMinimum:10, transformedMaximum:100,
    scale:'linear', threshold:1, reversed:false, displayTicks:[{value:10, text:'10'}, {value:100, text:'100'}]};
  const result = brush(axis, .45, .55);
  assert.deepEqual(result.displayRange.x, [10, 100]);
  assert.deepEqual(result.range.x, [10, 100]);
  assert.deepEqual(brush(axis, .5, .5).displayRange.x, [10, 100]);
});

function gesture(axis, from, to, values, labels = [], horizontal = false, paintOffsets = {}, metadata) {
  const unit = {minimum:0, maximum:100, transformedMinimum:0, transformedMaximum:100, scale:'linear', threshold:1, reversed:false,
    displayTicks:[{value:0,text:'0'},{value:100,text:'100'}]};
  const root = {_cfxDataProjection:{horizontal, plotBounds:{x:0,y:0,width:100,height:100}, x:horizontal ? unit : axis, ...(horizontal ? {y:axis} : {})},
    dataset:{cfxBrushAxis:horizontal ? 'xy' : 'x', cfxPreparedChart:JSON.stringify(metadata ?? {xLabels:labels, ...(labels.length ? {categoryLabels:labels} : {})})},
    querySelector:identitySvg,
    targets:values.map(value => {
      const ratio = (value - axis.minimum) / (axis.maximum - axis.minimum), position = 100 * (axis.reversed ? 1 - ratio : ratio) + (paintOffsets[value] || 0);
      return {id:value,dataset:{cfxX:String(value),cfxY:'50'},time:axis.scale==='time',closest:() => null,getBoundingClientRect:() => ({left:horizontal ? 49 : position - 1,
        top:horizontal ? 99 - position : 49, width:2,height:2})};
    })};
  const original = horizontal ? {left:0,top:100*(1-to),right:100,bottom:100*(1-from)} : {left:100*from,top:0,right:100*to,bottom:100};
  const brush = {style:{left:original.left+'px',top:original.top+'px',width:original.right-original.left+'px',height:original.bottom-original.top+'px'},
    getBoundingClientRect() { const left=parseFloat(this.style.left),top=parseFloat(this.style.top);
      return {left,top,right:left+parseFloat(this.style.width),bottom:top+parseFloat(this.style.height)}; }};
  const data = finishBrushSelection(root, {}, brush);
  const selected = selectTargetsInBox(root, brush.getBoundingClientRect(), false, data).map(target => target.id);
  return {data,selected,rect:brush.getBoundingClientRect()};
}

for (const reversed of [false, true]) {
  test(`numeric brush caption payload visible box and selected marks share the enclosing interval (reversed=${reversed})`, () => {
    const axis = {minimum:0,maximum:100,transformedMinimum:0,transformedMaximum:100,scale:'linear',threshold:1,reversed,
      displayTicks:[0,20,40,60,80,100].map(value => ({value,text:String(value)}))};
    const values=[10,20,21,22,40,60,68,69,80,90], result=gesture(axis,.22,.68,values);
    assert.deepEqual(result.data.range.x,[20,80]);
    assert.deepEqual(result.data.displayRange.x,result.data.range.x);
    assert.equal(result.data.rangeLabel,'20 – 80');
    assert.deepEqual(result.selected,values.filter(value=>value>=20 && value<=80));
    assert.ok(Math.abs(result.rect.left-20)<1e-10);
    assert.ok(Math.abs(result.rect.right-80)<1e-10);
    assert.equal(result.rect.top,0); assert.equal(result.rect.bottom,100);
  });
}

test('coarse time brushes publish and select the full labelled week boundaries', () => {
  const minimum=46266, axis={minimum,maximum:minimum+28,transformedMinimum:minimum,transformedMaximum:minimum+28,
    scale:'time',threshold:1,reversed:false,displayTicks:[0,7,14,21,28].map(day=>({value:minimum+day,text:'Day '+day}))};
  const values=[0,5.3,6,7,14,15.4,21,22,28].map(day=>minimum+day), result=gesture(axis,6.01/28,15.37/28,values);
  assert.equal(result.data.rangeLabel,'Day 0 – Day 21');
  assert.deepEqual(result.data.range.x,[new Date((minimum-25569)*86400000).toISOString(),new Date((minimum+21-25569)*86400000).toISOString()]);
  assert.deepEqual(result.data.range.x,result.data.displayRange.x);
  assert.deepEqual(result.selected,values.filter(value=>value<=minimum+21));
  assert.deepEqual(result.rect,{left:0,top:0,right:75,bottom:100});
});

test('plot edges between painted ticks use captured canonical boundary captions', () => {
  const axis={minimum:3,maximum:97,transformedMinimum:3,transformedMaximum:97,scale:'linear',threshold:1,reversed:false,
    displayTicks:[20,40,60,80].map(value=>({value,text:value+' ms'})),displayBounds:[{value:3,text:'3 ms'},{value:97,text:'97 ms'}]};
  const result=gesture(axis,.01,.99,[3,10,20,80,90,97]);
  assert.deepEqual(result.data.range.x,[3,97]);
  assert.equal(result.data.rangeLabel,'3 ms – 97 ms');
  assert.deepEqual(result.selected,[3,10,20,80,90,97]);
  assert.deepEqual(result.rect,{left:0,top:0,right:100,bottom:100});
});

test('committed range includes a rounded edge mark and excludes an adjacent outside coordinate', () => {
  const axis={minimum:0,maximum:100,transformedMinimum:0,transformedMaximum:100,scale:'linear',threshold:1,reversed:false,
    displayTicks:[0,20,40,60,80,100].map(value=>({value,text:String(value)}))};
  const result=gesture(axis,.22,.68,[20,40,80,80.000001],[],false,{20:-.02,80:.02,80.000001:-.02});
  assert.deepEqual(result.data.range.x,[20,80]);
  assert.deepEqual(result.selected,[20,40,80]);
});

for (const horizontal of [false,true]) {
  test(`xy point membership uses both committed coordinates and preserves availability (horizontal=${horizontal})`, () => {
    const axis={minimum:0,maximum:100,transformedMinimum:0,transformedMaximum:100,scale:'linear',threshold:1,reversed:false};
    const range={x:[20,80],y:[20,80]},node=(id,x,y,available=true)=>({id,available,dataset:{cfxX:String(x),cfxY:String(y)},closest:()=>null,
      getBoundingClientRect:()=>({left:49,top:49,width:2,height:2})});
    const root={_cfxDataProjection:{horizontal,x:axis,y:axis},dataset:{},targets:[node('inside',20,80),node('category-outside',80.000001,40),
      node('value-outside',40,80.000001),node('hidden',40,40,false)]};
    assert.deepEqual(selectTargetsInBox(root,{left:20,top:20,right:80,bottom:80},false,{range}).map(target=>target.id),['inside']);
  });

  test(`explicit x-only brush ignores a rounded painted centre on the unconstrained y edge (horizontal=${horizontal})`, () => {
    const axis={minimum:0,maximum:100,transformedMinimum:0,transformedMaximum:100,scale:'linear',threshold:1,reversed:false};
    const node={id:'y-edge',dataset:{cfxX:'50',cfxY:'50'},closest:()=>null,
      getBoundingClientRect:()=>({left:49,top:99+1e-12,width:2,height:2})};
    const root={_cfxDataProjection:{horizontal,x:axis,y:axis},dataset:{cfxBrushAxis:'x'},targets:[node]};
    const rect={left:20,top:0,right:80,bottom:100};
    assert.deepEqual(selectTargetsInBox(root,rect,false,{range:{x:[20,80]}}).map(target=>target.id),['y-edge']);
    root.dataset.cfxBrushAxis='xy';
    assert.deepEqual(selectTargetsInBox(root,rect,false,{range:{x:[20,80]}}),[]);
  });
}

test('finite axes spanning an overflowing subtraction retain finite snapped geometry', () => {
  const axis={minimum:-1e308,maximum:1e308,transformedMinimum:-1e308,transformedMaximum:1e308,scale:'linear',threshold:1,reversed:false,
    displayTicks:[{value:-1e308,text:'Lower'},{value:1e308,text:'Upper'}]};
  const result=gesture(axis,.25,.75,[]);
  assert.deepEqual(result.data.range.x,[-1e308,1e308]);
  assert.deepEqual(result.data.range.x,result.data.displayRange.x);
  assert.deepEqual(result.rect,{left:0,top:0,right:100,bottom:100});
});

for (const horizontal of [false,true]) {
  test(`category brush names exactly the included categories without expanding its rectangle (horizontal=${horizontal})`, () => {
    const labels=['North','South','East','West','Central'].map((text,value)=>({value,text}));
    const axis={minimum:0,maximum:4,transformedMinimum:0,transformedMaximum:4,scale:'linear',threshold:1,reversed:false,displayTicks:labels};
    const result=gesture(axis,.2,.8,[0,1,2,3,4],labels,horizontal);
    assert.deepEqual(result.data.categories,['South','East','West']);
    assert.equal(result.data.rangeLabel,'South, East, West');
    assert.deepEqual(result.data.displayRange[horizontal?'y':'x'],[1,3]);
    assert.deepEqual(result.selected,[1,2,3]);
    assert.deepEqual(result.data.categoryRange,result.data.range[horizontal?'y':'x']);
    assert.ok(Math.abs((horizontal?result.rect.top:result.rect.left)-20)<1e-10);
    assert.ok(Math.abs((horizontal?result.rect.bottom:result.rect.right)-80)<1e-10);
  });
}

test('box-category singleton and empty brushes never borrow excluded category names', () => {
  const labels=['API','Worker','Queue'].map((text,value)=>({value,text}));
  const axis={minimum:0,maximum:2,transformedMinimum:0,transformedMaximum:2,scale:'linear',threshold:1,reversed:false,displayTicks:labels};
  const result=gesture(axis,.3,.7,[0,1,2],labels);
  assert.deepEqual(result.selected,[1]); assert.deepEqual(result.data.categories,['Worker']);
  assert.equal(result.data.rangeLabel,'Worker'); assert.deepEqual(result.data.displayRange.x,[1,1]);
  const empty=gesture(axis,.1,.2,[0,1,2],labels);
  assert.deepEqual(empty.selected,[]); assert.deepEqual(empty.data.categories,[]);
  assert.equal(empty.data.rangeLabel,''); assert.equal(empty.data.label,'');
});

for (const horizontal of [false, true]) {
  test(`partial numeric aliases retain interval semantics instead of an empty category filter (horizontal=${horizontal})`, () => {
    const aliases=[{value:0,text:'Low'},{value:100,text:'High'}];
    const axis={minimum:0,maximum:100,transformedMinimum:0,transformedMaximum:100,scale:'linear',threshold:1,reversed:false,
      displayTicks:aliases};
    const result=gesture(axis,.22,.68,[20,40,60,80],aliases,horizontal,{}, {xLabels:aliases});
    const direction=horizontal?'y':'x';
    assert.equal(result.data.categories,undefined);
    assert.deepEqual(result.data.range[direction],[0,100]);
    assert.deepEqual(result.data.displayRange[direction],result.data.range[direction]);
    assert.equal(result.data.rangeLabel,'Low – High');
    assert.deepEqual(result.selected,[20,40,60,80]);
  });
}

// These exact values are produced by public ChartPoint(DateTime,double) at the
// native 100-microsecond resolution; JavaScript Date would discard the remainder.
const preciseStops=[
  {value:46266.00000000463,text:'Start',instant:'2026-09-01T00:00:00.0004000Z'},
  {value:46266.00011574653,text:'End',instant:'2026-09-01T00:00:10.0005000Z'}
];
const unitAxis={minimum:0,maximum:100,transformedMinimum:0,transformedMaximum:100,scale:'linear',threshold:1,reversed:false,
  displayTicks:[{value:0,text:'0'},{value:100,text:'100'}]};
function preciseRoot(horizontal, timeOnCategory, stops) {
  const timeAxis={minimum:preciseStops[0].value,maximum:preciseStops[1].value,transformedMinimum:preciseStops[0].value,
    transformedMaximum:preciseStops[1].value,scale:'time',threshold:1,reversed:false,...stops};
  const timeDirection=(horizontal===timeOnCategory)?'y':'x';
  const target=(id,instant)=>({id,time:timeOnCategory,dataset:timeOnCategory
    ? {cfxX:String(preciseStops[1].value),cfxXInstant:instant,cfxY:'50'}
    : {cfxX:'50',cfxY:String(preciseStops[1].value),cfxValueInstant:instant},
    closest:()=>null,getBoundingClientRect:()=>({left:49,top:49,width:2,height:2})});
  return {_cfxDataProjection:{horizontal,plotBounds:{x:0,y:0,width:100,height:100},x:timeDirection==='x'?timeAxis:unitAxis,
    y:timeDirection==='y'?timeAxis:unitAxis},dataset:{cfxBrushAxis:'xy',cfxPreparedChart:'{"xLabels":[]}'},
    querySelector:identitySvg,
    targets:[target('last',preciseStops[1].instant),target('outside','2026-09-01T00:00:10.0006000Z')]};
}
for (const horizontal of [false,true]) {
  for (const bounds of [false,true]) {
    test(`time brush preserves native endpoint precision in ${bounds?'bounds':'ticks'} (horizontal=${horizontal})`,()=>{
      const root=preciseRoot(horizontal,true,bounds?{displayBounds:preciseStops}:{displayTicks:preciseStops});
      const rect={left:0,top:0,right:100,bottom:100},selection=projectBrushSelection(root,rect);
      const range=selection.data.range[horizontal?'y':'x'];
      assert.deepEqual(range,preciseStops.map(stop=>stop.instant));
      assert.equal(selection.data.rangeLabel,'Start – End');
      assert.deepEqual(selectTargetsInBox(root,rect,false,selection.data).map(target=>target.id),['last']);
    });
  }
  test(`time-valued scalar membership uses the captured native value instant (horizontal=${horizontal})`,()=>{
    const root=preciseRoot(horizontal,false,{displayTicks:preciseStops});
    const rect={left:0,top:0,right:100,bottom:100},selection=projectBrushSelection(root,rect);
    assert.deepEqual(selection.data.range[horizontal?'x':'y'],preciseStops.map(stop=>stop.instant));
    assert.deepEqual(selectTargetsInBox(root,rect,false,selection.data).map(target=>target.id),['last']);
  });
}
