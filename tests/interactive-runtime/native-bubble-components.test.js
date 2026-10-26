 'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { fixture } = require('./native-value-fixture');
// Synthetic component transport/preflight only. Real typed tuples, size mapping and SVG/PNG ink have native/browser proof.
function setup() {
  const f = fixture('cartesian-value');
  const make = (y, size) => {
    const section = f.make(y), metadata = JSON.parse(section.dataset.cfxPreparedChart), point = section.querySelector('[data-cfx-role=point]');
    metadata.series[0].kind = 'bubble'; metadata.series[0].observationIds = [{ index: 0, id: 'value' }];
    point.dataset.cfxSize = String(size); point.dataset.cfxSizeMapping = 'sqrt-radius-range';
    metadata.nativeValueSource = { kind: 'bubble-components', sourceKey: (y === 20 ? 'a' : y === 40 ? 'b' : 'c').repeat(64), structureKey: 'd'.repeat(64), mapping: 'sqrt-radius-range', reversed: false,
      readings: [{ seriesKey: 'current', observationId: 'value', component: 'y', value: y, minimum: 0, maximum: 100 },
        { seriesKey: 'current', observationId: 'value', component: 'size', value: size, minimum: 0, maximum: 100 }] };
    section.dataset.cfxPreparedChart = JSON.stringify(metadata); return section;
  };
  const capture = (flash = false) => {
    const to = make(40, 180), frames = (flash ? [[40,180],[40,180],[40,180]] : [[20,.25],[30,90.125],[40,180]]).map(([y,size],index) => {
      const section = make(y,size); return { prefix: 'synthetic-component-' + index, svg: section.querySelector('svg').outerHTML, nativeValueSource: JSON.parse(section.dataset.cfxPreparedChart).nativeValueSource };
    });
    return { kind: 'native-transition', effect: flash ? 'value-flash' : 'values', sourceKind: 'cartesian-component-value', destination: to.outerHTML,
      readings: [{ seriesKey: 'current', observationId: 'value', component: 'y', fromValue: 20, toValue: 40, minimum: 0, maximum: 100 },
        { seriesKey: 'current', observationId: 'value', component: 'size', fromValue: .25, toValue: 180, minimum: 0, maximum: 100 }],
      drawings: { wide: { capturePngText: false, sourcePrefix: 'synthetic-source', destinationPrefix: 'synthetic-destination', frames } } };
  };
  const save = (name,payload,error) => { const output=process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if(!output)return;fs.mkdirSync(output,{recursive:true});
    fs.writeFileSync(path.join(output,'bubble-node-'+name+'.json'),JSON.stringify({payload,error:error?.message,revision:f.controller.sceneRevision,events:f.changes,metadata:JSON.parse(f.root.dataset.cfxPreparedChart)},null,2));
    fs.writeFileSync(path.join(output,'bubble-node-'+name+'.html'),f.root.outerHTML); };
  f.controller.update(make(20,.25));f.changes.length=0;return{...f,make,capture,save};
}
for(const effect of ['values','initial-values','value-flash'])test('native Bubble component '+effect+' retains one accepted catalog and the existing lifetime',()=>{const f=setup();try{
  const p=f.capture(effect==='value-flash');p.effect=effect;if(effect==='initial-values')f.controller.update(f.make(40,180));f.changes.length=0;const revision=f.controller.sceneRevision;
  let error;try{if(effect==='initial-values')f.controller.presentInitial(JSON.stringify(p),{sceneRevision:revision,motion:{duration:1000}});else f.controller.update(p.destination,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}});}catch(failure){error=failure;}
  f.save(effect,p,error);assert.equal(error,undefined);assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,1);assert.equal(f.changes.length,effect==='initial-values'?0:1);
  assert.equal(revision===f.controller.sceneRevision,effect==='initial-values');const rows=JSON.parse(f.root.dataset.cfxPreparedChart).nativeValueSource.readings;
  assert.equal(rows[0].value,40);assert.equal(rows[1].value,180);assert.equal(rows[1].maximum,100);f.controller.dispose();assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,0);
}finally{f.close();}});
const malformed={
  duplicateComponent:p=>p.readings[1].component='y',
  missingComponent:p=>delete p.readings[0].component,
  inventedComponent:p=>p.readings[1].component='radius',
  foreignObservation:p=>p.drawings.wide.frames[1].nativeValueSource.readings[0].observationId='foreign',
  zeroSize:p=>p.drawings.wide.frames[1].nativeValueSource.readings[1].value=0,
  wrongSizeDomain:p=>p.drawings.wide.frames[1].nativeValueSource.readings[1].maximum=200,
  wrongMapping:p=>p.drawings.wide.frames[1].nativeValueSource.mapping='linear-area',
  wrongReverse:p=>p.drawings.wide.frames[1].nativeValueSource.reversed=true,
  wrongStructure:p=>p.drawings.wide.frames[1].nativeValueSource.structureKey='e'.repeat(64),
  valuePaintMismatch:p=>p.drawings.wide.frames[1].svg=p.drawings.wide.frames[1].svg.replace('data-cfx-size="90.125"','data-cfx-size="91"'),
  malformedLast:p=>p.drawings.wide.frames[2].svg='<svg><',
  wrongYDomain:p=>p.readings[0].maximum=200,
  foreignRow:p=>p.drawings.wide.frames[1].nativeValueSource.readings.push({...p.drawings.wide.frames[1].nativeValueSource.readings[0]}),
  mixedJoint:p=>p.effect='values-and-visibility'
};
for(const[name,mutate]of Object.entries(malformed))test('Bubble all-candidate preflight rejects '+name+' before acceptance/events/revision',()=>{const f=setup();try{
  const p=f.capture(),before=f.root.outerHTML,revision=f.controller.sceneRevision;mutate(p);let error;try{f.controller.update(p.destination,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}});}catch(failure){error=failure;}
  f.save(name,p,error);assert.ok(error instanceof f.window.TypeError);assert.equal(f.root.outerHTML,before);assert.equal(f.controller.sceneRevision,revision);assert.equal(f.changes.length,0);
}finally{f.close();}});
test('Bubble component stale revision and bounded repeated replacement keep one transient owner',()=>{const f=setup();try{
  for(let index=0;index<24;index++){f.controller.update(f.make(20,.25));const p=f.capture();f.controller.update(p.destination,{sceneRevision:f.controller.sceneRevision,transition:JSON.stringify(p),motion:{duration:1000}});assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,1);}
  const p=f.capture(),revision=f.controller.sceneRevision;let error;try{f.controller.update(p.destination,{sceneRevision:'stale',transition:JSON.stringify(p)});}catch(failure){error=failure;}f.save('sustained-stale',p,error);
  assert.ok(error);assert.equal(f.controller.sceneRevision,revision);assert.equal(JSON.parse(f.root.dataset.cfxPreparedChart).nativeValueSource.readings[1].value,180);
}finally{f.close();}});
