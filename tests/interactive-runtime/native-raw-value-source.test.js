 'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { fixture } = require('./native-value-fixture');
// Synthetic transport/catalog/preflight only. Real native GetSlices, font, layout, SVG/PNG and browser ink have separate contracts.
function setup() {
  const f = fixture('radial-value'), structure = 'c'.repeat(64);
  const make = (value, hidden = false, reflow = false) => {
    const section = f.make(value), metadata = JSON.parse(section.dataset.cfxPreparedChart), group = section.querySelector('[data-cfx-role=radial-point]');
    metadata.series[0].kind = 'pie'; metadata.series[0].observationIds = [{ index: 0, id: 'value' }]; metadata.series[0].indices = hidden ? [] : [0];
    if (value === 0 || hidden) group.remove();
    const paint = group.isConnected || group.parentNode ? Object.fromEntries([...group.attributes].filter(row => row.name.startsWith('data-cfx-')).map(row => [row.name, row.value])) : null;
    const key = (value === 20 ? 'a' : value === 40 ? 'b' : 'd').repeat(64);
    metadata.nativeValueSource = { kind: 'radial', sourceKey: key, structureKey: structure, readings: [{ seriesKey: 'current', observationId: 'value', value }],
      slices: hidden ? [] : [{ seriesKey: 'current', pointIndex: 0, sourceIndices: [0], paintedMetadata: paint }] };
    if (reflow) metadata.visibility = { layout: 'reflow', sourceKey: key, labelLayering: 'preserveOverlay', hiddenSeries: [],
      targetCatalog: [{ kind: 'series', seriesKey: 'current' }, { kind: 'observation', seriesKey: 'current', observationId: 'value' }],
      hiddenTargets: hidden ? [{ kind: 'observation', seriesKey: 'current', observationId: 'value' }] : [] };
    section.dataset.cfxPreparedChart = JSON.stringify(metadata); return section;
  };
  const capture = (before = 20, after = 40, reflow = false) => {
    const from = make(before, false, reflow), to = make(after, reflow, reflow), facts = section => JSON.parse(section.dataset.cfxPreparedChart).nativeValueSource;
    const frames = reflow ? [[from,'source','1'],[from,'source','0.5'],[to,'destination','0'],[to,'destination','0.5'],[to,'destination','1']].map(([section,basis,opacity],index) => {
      const svg = section.querySelector('svg').cloneNode(true);
      if (opacity !== '1') { const group = f.window.document.createElementNS(svg.namespaceURI,'g'); group.setAttribute('data-cfx-role','native-transition-opacity');group.setAttribute('opacity',opacity);while(svg.firstChild)group.appendChild(svg.firstChild);svg.appendChild(group); }
      return { prefix:'synthetic-raw-'+index,svg:svg.outerHTML,basis,nativeOpacity:opacity,nativeValueSource:facts(section) };
    }) : [before,30,after].map((value,index) => { const section=make(value);return {prefix:'synthetic-raw-'+index,svg:section.querySelector('svg').outerHTML,nativeValueSource:facts(section)}; });
    return { kind:'native-transition',effect:reflow?'source-reflow':'values',sourceKind:reflow?'native-source-reflow':'radial-raw-value',destination:to.outerHTML,
      readings:[{seriesKey:'current',observationId:'value',fromValue:before,toValue:after,minimum:0,maximum:Number.MAX_VALUE}],
      ...(reflow?{visibility:{layout:'reflow',labelLayering:'preserveOverlay',targetCatalog:JSON.parse(from.dataset.cfxPreparedChart).visibility.targetCatalog,fromHiddenTargets:[],toHiddenTargets:JSON.parse(to.dataset.cfxPreparedChart).visibility.hiddenTargets}}:{}),
      drawings:{wide:{capturePngText:false,sourcePrefix:'synthetic-source',destinationPrefix:'synthetic-destination',frames}} };
  };
  const save = (name,payload,error) => {const output=process.env.CFX_BROWSER_CAPTURE_DIRECTORY;if(!output)return;fs.mkdirSync(output,{recursive:true});fs.writeFileSync(path.join(output,'raw-node-'+name+'.json'),JSON.stringify({payload,error:error?.message,revision:f.controller.sceneRevision,events:f.changes,metadata:JSON.parse(f.root.dataset.cfxPreparedChart)},null,2));fs.writeFileSync(path.join(output,'raw-node-'+name+'.html'),f.root.outerHTML);};
  f.controller.update(make(20)); f.changes.length=0; return {...f,make,capture,save};
}
for (const initial of [false,true]) test('raw native source uses one existing '+(initial?'initial':'accepted update')+' lifetime without fabricated zero paint',()=>{
  const f=setup();try{const payload=f.capture(0,40);payload.effect=initial?'initial-values':'values';f.controller.update(f.make(initial?40:0));f.changes.length=0;const revision=f.controller.sceneRevision;
    if(initial)f.controller.presentInitial(JSON.stringify(payload),{sceneRevision:revision,motion:{duration:1000}});else f.controller.update(payload.destination,{sceneRevision:revision,transition:JSON.stringify(payload),motion:{duration:1000}});
    f.save('accepted-'+initial,payload);assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,1);assert.equal(f.changes.length,initial?0:1);assert.equal(revision===f.controller.sceneRevision,initial);assert.equal(JSON.parse(f.root.dataset.cfxPreparedChart).nativeValueSource.readings[0].value,40);
    f.controller.dispose();assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,0);
  }finally{f.close();}
});
const invalid={
  duplicateRawRow:p=>p.drawings.wide.frames[1].nativeValueSource.readings.push({...p.drawings.wide.frames[1].nativeValueSource.readings[0]}),
  negativeRaw:p=>p.drawings.wide.frames[1].nativeValueSource.readings[0].value=-1,
  foreignObservation:p=>p.drawings.wide.frames[1].nativeValueSource.readings[0].observationId='foreign',
  wrongStructure:p=>p.drawings.wide.frames[1].nativeValueSource.structureKey='e'.repeat(64),
  duplicateContributor:p=>p.drawings.wide.frames[1].nativeValueSource.slices[0].sourceIndices.push(0),
  inventedPaint:p=>p.drawings.wide.frames[1].nativeValueSource.slices[0].paintedMetadata=null,
  malformedLast:p=>p.drawings.wide.frames[2].svg='<svg><',
  wrongDomain:p=>p.readings[0].maximum=100,
  foreignSourceKey:p=>p.drawings.wide.frames[1].nativeValueSource.sourceKey='unknown'
};
for(const[name,mutate]of Object.entries(invalid))test('raw all-candidate preflight rejects '+name+' before acceptance/revision/events',()=>{const f=setup();try{const p=f.capture(),before=f.root.outerHTML,revision=f.controller.sceneRevision;mutate(p);let error;try{f.controller.update(p.destination,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}});}catch(failure){error=failure;}f.save(name,p,error);assert.ok(error instanceof f.window.TypeError);assert.equal(f.root.outerHTML,before);assert.equal(f.controller.sceneRevision,revision);assert.equal(f.changes.length,0);}finally{f.close();}});
test('source Reflow transports changed raw values and absolute visibility through old/new native snapshots only',()=>{const f=setup();try{f.controller.update(f.make(20,false,true));f.changes.length=0;const p=f.capture(20,40,true),revision=f.controller.sceneRevision;f.controller.update(p.destination,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}});f.save('source-reflow',p);assert.equal(f.changes.length,1);assert.equal(JSON.parse(f.root.dataset.cfxPreparedChart).nativeValueSource.readings[0].value,40);assert.equal(f.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=radial-point]').length,0);assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,1);f.controller.resize();assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,0);}finally{f.close();}});
test('bounded repeated raw updates keep one transient owner and reject stale revision without event',()=>{const f=setup();try{for(let index=0;index<100;index++){const before=index%2?40:20,after=index%2?20:40;const p=f.capture(before,after);f.controller.update(p.destination,{sceneRevision:f.controller.sceneRevision,transition:JSON.stringify(p),motion:{duration:1000}});assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,1);}const p=f.capture(),revision=f.controller.sceneRevision;let error;try{f.controller.update(p.destination,{sceneRevision:'stale',transition:JSON.stringify(p)});}catch(failure){error=failure;}f.save('sustained',p,error);assert.ok(error);assert.equal(f.controller.sceneRevision,revision);assert.equal(f.changes.length,100);}finally{f.close();}});

for(const [name,mutate] of Object.entries({oldLegend:p=>p.effect='legend-visibility',foreignBasis:p=>p.drawings.wide.frames[1].basis='foreign',foreignRawBasis:p=>p.drawings.wide.frames[1].nativeValueSource={...p.drawings.wide.frames[1].nativeValueSource,sourceKey:'f'.repeat(64)}}))test('source Reflow rejects '+name+' without reopening acceptance or changing the full source',()=>{const f=setup();try{f.controller.update(f.make(20,false,true));f.changes.length=0;const p=f.capture(20,40,true),before=f.root.outerHTML,revision=f.controller.sceneRevision;mutate(p);let error;try{f.controller.update(p.destination,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}});}catch(failure){error=failure;}f.save('source-reflow-'+name,p,error);assert.ok(error instanceof f.window.TypeError);assert.equal(f.root.outerHTML,before);assert.equal(f.controller.sceneRevision,revision);assert.equal(f.changes.length,0);}finally{f.close();}});

for(const malformed of [false,true])test('raw value flash '+(malformed?'rejects foreign accepted raw frame facts':'retains accepted raw source through the existing flash owner'),()=>{const f=setup();try{const p=f.capture(),to=f.make(40);p.effect='value-flash';p.drawings.wide.frames=p.drawings.wide.frames.map((frame,index)=>({...frame,svg:to.querySelector('svg').outerHTML.replace('#2468ac',index===1?'#ffc000':'#2468ac'),nativeValueSource:JSON.parse(to.dataset.cfxPreparedChart).nativeValueSource}));if(malformed)p.drawings.wide.frames[1].nativeValueSource={...p.drawings.wide.frames[1].nativeValueSource,readings:[{seriesKey:'current',observationId:'value',value:39}]};const revision=f.controller.sceneRevision,before=f.root.outerHTML;let error;try{f.controller.update(p.destination,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}});}catch(failure){error=failure;}f.save('owned-flash-'+malformed,p,error);if(malformed){assert.ok(error instanceof f.window.TypeError);assert.equal(f.controller.sceneRevision,revision);assert.equal(f.root.outerHTML,before);assert.equal(f.changes.length,0);}else{assert.equal(error,undefined);assert.equal(f.changes.length,1);assert.equal(JSON.parse(f.root.dataset.cfxPreparedChart).nativeValueSource.readings[0].value,40);assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length,1);}}finally{f.close();}});
