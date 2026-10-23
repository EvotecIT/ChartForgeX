const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const sourceRoot = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source');
const source = ['43-brush-projection', '86-native-x-viewport', '86-native-cartesian-viewport', '88-native-x-navigation']
  .map(name => fs.readFileSync(path.join(sourceRoot, name + '.js'), 'utf8')).join('\n');
const clamp = (value, minimum, maximum) => Math.max(minimum, Math.min(maximum, value));
const runtime = new Function('clamp', source + '\nreturn {transformedAxisBound,projectedAxisValue,projectedAxisRatio,validXProjection,validDataProjection,validSourceAxisWindow,xNavigationWindow};')(clamp);
const projection = () => ({minimum:45000,maximum:45091,fullMinimum:45000,fullMaximum:45091,
  transformedMinimum:0,transformedMaximum:3,transformedFullMinimum:0,transformedFullMaximum:3,
  scale:'time',threshold:1,reversed:false,timeSpacing:{kind:'unit',instants:[45000,45031,45060,45091]},
  plotBounds:{x:0,y:0,width:800,height:440}});

test('exclusive canonical band edges retain a single band through captured forward and inverse mappings', () => {
  for (const reversed of [false,true]) {
    const full=projection(), selected={...full,minimum:45031,maximum:45060,maximumInclusive:false,
      transformedMinimum:1,transformedMaximum:2,reversed};
    assert.equal(runtime.transformedAxisBound(selected,45060,true),2);
    assert.equal(runtime.transformedAxisBound(selected,45060,true,true),3); // Authored inclusive boundary still includes that band.
    assert.equal(runtime.projectedAxisRatio(selected,45040),.5);
    const lower=runtime.projectedAxisRatio({...full,reversed:false},45031,false);
    const upper=runtime.projectedAxisRatio({...full,reversed:false},45060,true,false);
    assert.equal(upper-lower,1/3);
    assert.equal(runtime.projectedAxisValue({...full,reversed:false},lower,false),45031);
    assert.equal(runtime.projectedAxisValue({...full,reversed:false},upper,true),45060);
  }
});

test('every physical native source payload preserves false while rejecting non-boolean boundary policies', () => {
  const axis=projection();
  const metadata=Object.fromEntries(['x','y','secondaryX','secondaryY'].map(name => [name+'Projection',axis]));
  const root={dataset:{cfxPreparedChart:JSON.stringify(metadata)}}, requests=[];
  const managed=new Function('clamp','nativePreparationBinding','requestNativePreparation',source + '\nreturn {requestNativeCartesianWindow,nativeViewportWindows,nativeViewportChanged};')(
    clamp,()=>({legacy:false,axes:['x']}),(_,channel,payload)=>{requests.push(payload);return true;});
  for (const name of ['x','y','secondaryX','secondaryY']) {
    const window={minimum:45031,maximum:45060,maximumInclusive:false};
    assert.equal(managed.requestNativeCartesianWindow(root,{[name]:window}),true);
    assert.deepEqual(requests.at(-1).sourceViewport[name],window);
    assert.equal(Object.isFrozen(requests.at(-1).sourceViewport[name]),true);
    for (const invalid of [null,0,1,'false',{},[]])
      assert.equal(managed.requestNativeCartesianWindow(root,{[name]:{...window,maximumInclusive:invalid}}),false);
    const count=requests.length;
    assert.equal(managed.requestNativeCartesianWindow(root,{[name]:{...window,maximum:45050}}),false);
    assert.equal(requests.length,count); // Interior exclusive Unit bounds fail before native transport.
  }
  for (const name of ['x','y','secondaryX','secondaryY']) metadata[name+'Projection']={...axis,maximumInclusive:false};
  root.dataset.cfxPreparedChart=JSON.stringify(metadata);
  const restored=JSON.parse(JSON.stringify(managed.nativeViewportWindows(root,true)));
  for (const name of ['x','y','secondaryX','secondaryY']) assert.equal(restored[name].maximumInclusive,false);
  assert.equal(managed.nativeViewportChanged(root),true); // Same numbers with an excluded endpoint are not a full reset.
  assert.equal(managed.requestNativeCartesianWindow(root,restored),true);
  assert.equal(requests.at(-1).sourceViewport.secondaryY.maximumInclusive,false);
});

test('captured viewport and pointer projections reject malformed maximum inclusion metadata', () => {
  const axis=projection(), data={plotBounds:axis.plotBounds,horizontal:false,x:axis};
  assert.equal(runtime.validXProjection({...axis,maximumInclusive:false}),true);
  assert.equal(runtime.validDataProjection({...data,x:{...axis,maximumInclusive:false}}),true);
  for (const invalid of [null,0,'true']) {
    assert.equal(runtime.validXProjection({...axis,maximumInclusive:invalid}),false);
    assert.equal(runtime.validDataProjection({...data,x:{...axis,maximumInclusive:invalid}}),false);
  }
  for (const instants of [null,{},[]]) {
    const malformed={...axis,maximumInclusive:false,timeSpacing:{kind:'unit',instants}};
    assert.equal(runtime.validXProjection(malformed),false);
    assert.equal(runtime.validDataProjection({...data,x:malformed}),false);
  }
});

test('navigation retains an authored inclusive full edge inside a month without inventing a partial band', () => {
  const axis=projection(), root={dataset:{cfxPreparedChart:JSON.stringify({xProjection:axis})}};
  assert.deepEqual(runtime.xNavigationWindow(root,axis,45031,45060),{minimum:45031,maximum:45060,maximumInclusive:false});
  assert.deepEqual(runtime.xNavigationWindow(root,axis,45031,45080),{minimum:45031,maximum:45080});
  assert.equal(runtime.validSourceAxisWindow(runtime.xNavigationWindow(root,axis,45031,45080),axis),true);
  assert.deepEqual(runtime.xNavigationWindow(root,axis,45031,45060,true),{minimum:45031,maximum:45060});
});
