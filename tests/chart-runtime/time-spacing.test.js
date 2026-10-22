const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const sourceRoot = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source');
const source = ['43-brush-projection','86-native-x-viewport','86-native-cartesian-viewport']
  .map(name => fs.readFileSync(path.join(sourceRoot,name + '.js'),'utf8')).join('\n');
const runtime = new Function('clamp', source + '\nreturn {projectedAxisValue,projectedAxisRatio,transformedAxisValue,transformedAxisBound,validDataProjection,validXProjection,validSourceAxisWindow};')(
  (value,minimum,maximum) => Math.max(minimum,Math.min(maximum,value)));
const axis = (kind,instants,reversed=false) => ({minimum:instants[0],maximum:instants.at(-1),fullMinimum:instants[0],fullMaximum:instants.at(-1),
  transformedMinimum:0,transformedMaximum:instants.length-1,transformedFullMinimum:0,transformedFullMaximum:instants.length-1,
  threshold:1,scale:'time',reversed,timeSpacing:{kind,instants},plotBounds:{x:0,y:0,width:800,height:440}});

for (const reversed of [false,true]) {
  test(`ordinal spacing inverts actual instants and interpolates omitted elapsed time (reversed=${reversed})`, () => {
    const projected=axis('ordinal',[45000,45001,45011],reversed);
    assert.equal(runtime.projectedAxisRatio(projected,45001),.5);
    assert.equal(runtime.projectedAxisValue(projected,.5),45001);
    for (const value of [45000,45001,45006,45011]) assert.equal(runtime.projectedAxisValue(projected,runtime.projectedAxisRatio(projected,value)),value);
    assert.equal(runtime.projectedAxisValue(projected,reversed ? .25 : .75),45006);
    assert.equal(runtime.validXProjection(projected),true);
  });
  test(`calendar bands keep missing months without using elapsed band width (reversed=${reversed})`, () => {
    const projected=axis('unit',[45000,45031,45060,45091],reversed);
    const positions=[45009,45045,45089].map(value => runtime.projectedAxisRatio(projected,value));
    assert.equal(Math.abs(positions[1]-positions[0]),Math.abs(positions[2]-positions[1]));
    assert.equal(runtime.projectedAxisValue(projected,positions[0]),45000);
    assert.equal(runtime.projectedAxisValue(projected,positions[1]),45031);
    assert.equal(runtime.projectedAxisValue(projected,positions[2]),45060);
    assert.equal(runtime.transformedAxisBound(projected,45040,false),1);
    assert.equal(runtime.transformedAxisBound(projected,45050,true),2);
    assert.equal(runtime.validSourceAxisWindow({minimum:45040,maximum:45050},projected),true); // A complete band has projected width even if both source endpoints share its center.
  });
}

test('all physical projections validate the same captured immutable-domain shape and reject ambiguous policies', () => {
  const good=axis('ordinal',[45000,45001,45011]);
  const projection={horizontal:false,plotBounds:good.plotBounds,x:good,y:good};
  assert.equal(runtime.validDataProjection(projection),true);
  for (const spacing of [{kind:'automatic',instants:[45000,45001]}, {kind:'ordinal',instants:[]},
    {kind:'unit',instants:[45000]}, {kind:'ordinal',instants:[45001,45000]}, {kind:'ordinal',instants:[45000,45000]},
    {kind:'ordinal',instants:[45000,Infinity]}, {kind:'ordinal',instants:[-1,45000]}]) {
    assert.equal(runtime.validXProjection({...good,timeSpacing:spacing}),false);
    assert.equal(runtime.validDataProjection({...projection,x:{...good,timeSpacing:spacing}}),false);
  }
  assert.equal(runtime.validXProjection({...good,scale:'linear'}),false);
  assert.equal(runtime.validXProjection({...good,timeSessions:[{minimum:45000,maximum:45011,offset:0}]}),false);
});

test('managed unit windows preserve raw source requests for each exact physical axis', () => {
  const captured=axis('unit',[45000,45031,45060,45091]);
  const metadata=Object.fromEntries(['x','y','secondaryX','secondaryY'].map(name => [name+'Projection',captured]));
  const root={dataset:{cfxPreparedChart:JSON.stringify(metadata)}}, requests=[];
  const managed = new Function('clamp','nativePreparationBinding','requestNativePreparation',source + '\nreturn {requestNativeCartesianWindow};')(
    (value,minimum,maximum)=>Math.max(minimum,Math.min(maximum,value)),()=>({legacy:false,axes:Object.freeze(['secondaryY'])}),
    (_,channel,payload)=>{requests.push({channel,payload});return true;});
  for (const name of ['x','y','secondaryX','secondaryY']) {
    assert.equal(managed.requestNativeCartesianWindow(root,{[name]:{minimum:45040,maximum:45050}}),true);
    assert.deepEqual(requests.at(-1).payload.axes,[name]);
    assert.deepEqual(requests.at(-1).payload.sourceViewport,{[name]:{minimum:45040,maximum:45050}});
  }
});
