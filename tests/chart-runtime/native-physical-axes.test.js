const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const sourceRoot = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source');
const source = ['43-brush-projection', '86-native-cartesian-viewport'].map(name => fs.readFileSync(path.join(sourceRoot, name + '.js'), 'utf8')).join('\n');

function runtime(axes = ['x', 'y', 'secondaryY']) {
  const projection = (minimum, maximum, reversed = false) => ({minimum,maximum,fullMinimum:minimum,fullMaximum:maximum,
    transformedMinimum:minimum,transformedMaximum:maximum,transformedFullMinimum:minimum,transformedFullMaximum:maximum,
    scale:'linear',threshold:1,reversed,plotBounds:{x:10,y:20,width:400,height:200}});
  const meta = {xProjection:projection(0,10),yProjection:projection(0,100),secondaryYProjection:projection(1000,5000,true)};
  const root = {dataset:{cfxPreparedChart:JSON.stringify(meta)}};
  const binding = {axes:Object.freeze([...axes]),legacy:false}, requests = [];
  const api = new Function('nativePreparationBinding','requestNativePreparation','chartXProjection','suspendPointerReadout',
    'emitSync','getState','clamp',source + '\nreturn {nativeViewportAxes,requestNativeCartesianWindow,requestCartesianViewportZoom,requestViewportReset,nativeViewportChanged,nativeViewportWindows};')(
      () => binding, (_,channel,payload,hooks) => {requests.push({channel,payload,hooks});return true;}, () => meta.xProjection,
      () => {}, () => {}, () => ({}), (value,minimum,maximum) => Math.max(minimum,Math.min(maximum,value)));
  return {api,root,binding,meta,requests};
}

test('physical axes are exact distinct names, copied and frozen rather than concatenated strings', () => {
  const {api} = runtime();
  for (const bad of ['xy','secondaryY',[],['y','y'],['x','SecondaryY'],['x','not-an-axis']])
    assert.throws(() => api.nativeViewportAxes(bad), TypeError);
  const source = ['x','secondaryY']; const accepted = api.nativeViewportAxes(source); source.push('y');
  assert.deepEqual(accepted,['x','secondaryY']); assert.equal(Object.isFrozen(accepted),true);
});

test('a reversed secondary-Y gesture uses vertical logical bounds without opting into primary Y', () => {
  const {api,root,requests} = runtime(['secondaryY']);
  api.requestCartesianViewportZoom(root,2,210,120);
  assert.equal(requests.length,1); const payload = requests[0].payload;
  assert.deepEqual(payload.axes,['secondaryY']); assert.equal(Object.isFrozen(payload.axes),true);
  assert.equal(payload.top,70); assert.equal(payload.bottom,170);
  assert.equal('left' in payload,false); assert.equal('right' in payload,false);
});

test('secondary source windows use their own captured transform even with an ambiguous generic value projection', () => {
  const {api,root,requests} = runtime(['secondaryY']);
  root._cfxDataProjection = {x:{scale:'linear'},y:null};
  assert.equal(api.requestNativeCartesianWindow(root,{secondaryY:{minimum:2000,maximum:3000}}),true);
  const payload = requests[0].payload;
  assert.deepEqual(payload.axes,['secondaryY']); assert.deepEqual(payload.sourceViewport,{secondaryY:{minimum:2000,maximum:3000}});
  assert.equal(Object.isFrozen(payload.sourceViewport.secondaryY),true);
  assert.equal(api.requestNativeCartesianWindow(root,{y:{minimum:20,maximum:30}}),true); // Programmatic windows are independent of gesture axes.
  assert.equal(api.requestNativeCartesianWindow(root,{secondaryY:{minimum:2000,maximum:3000},unknown:{minimum:0,maximum:1}}),false);
});

test('visible-category policy and all connected physical windows are captured independently', () => {
  const {api,root,meta,requests} = runtime();
  meta.valueDomain='visibleCategories'; meta.secondaryYProjection.minimum=2000;
  root.dataset.cfxPreparedChart=JSON.stringify(meta);
  assert.equal(api.nativeViewportChanged(root),true);
  assert.equal(api.requestNativeCartesianWindow(root,{x:{minimum:2,maximum:8},valueDomain:'visibleCategories'}),true);
  assert.equal(requests[0].payload.valueDomain,'visibleCategories');
  const snapshot=api.nativeViewportWindows(root,true);
  assert.deepEqual(Object.keys(snapshot),['x','y','secondaryY','valueDomain']);
  assert.equal(snapshot.secondaryY.minimum,2000);
  assert.equal(api.requestViewportReset(root),true);
  assert.deepEqual(requests[1].payload.axes,['x','y','secondaryY']); assert.equal(requests[1].payload.valueDomain,'fullData');
});

test('generated physical projection validation rejects invalid captured scale and sign policies', () => {
  const native=fs.readFileSync(path.join(sourceRoot,'86-native-x-viewport.js'),'utf8');
  const transforms=fs.readFileSync(path.join(sourceRoot,'43-brush-projection.js'),'utf8');
  const valid=new Function(transforms+'\n'+native.slice(0,native.indexOf('  const chartXProjection'))+'\nreturn validXProjection;')();
  const {meta}=runtime();
  assert.equal(valid(meta.yProjection),true);
  assert.equal(valid({...meta.yProjection,scale:'automatic'}),false);
  assert.equal(valid({...meta.yProjection,logarithmBase:1}),false);
  assert.equal(valid({...meta.yProjection,scale:'logarithmic',logarithmicDomain:'negative'}),false);
  assert.equal(valid({...meta.yProjection,threshold:0}),false);
});
