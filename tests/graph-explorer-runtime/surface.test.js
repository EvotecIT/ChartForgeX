const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/graph-explorer.00-core.js'), 'utf8');
const close = (actual, expected) => assert.ok(Math.abs(actual - expected) < 1e-8, `${actual} != ${expected}`);
function fixture(width, height, ratio, renderer = 'canvas') {
  const transforms = [], rect = {left:17, top:31, width, height};
  const context = {setTransform(...values) { transforms.push(values); }};
  const canvas = {width:0, height:0, getBoundingClientRect:()=>rect, getContext:()=>context};
  const svg = {getAttribute:name=>name==='viewBox'?'0 0 960 560':'', getBoundingClientRect:()=>rect};
  const values = {'data-cfx-viewport-x':'35','data-cfx-viewport-y':'-21','data-cfx-viewport-scale':'1.7'};
  const root = {dataset:{cfxGraphRendererActive:renderer}, getAttribute:name=>values[name]||'',
    querySelector:selector=>selector.includes('graph-scene')?svg:selector.includes('graph-canvas')||selector.includes('graph-webgl')?canvas:null};
  const runtime = new Function('window', source+'\nreturn { graphSurfaceFit, scenePoint, canvasContext };')({devicePixelRatio:ratio});
  return {runtime, root, canvas, transforms, rect};
}
test('painting and pointer inversion share centered aspect-preserving geometry at every viewport and pixel ratio', () => {
  for (const [width,height] of [[390,600],[1200,440],[960,560],[389.5,613.25]]) {
    for (const ratio of [1,1.25,2,3]) for (const renderer of ['canvas','svg','webgl']) {
      const {runtime,root,canvas,transforms,rect} = fixture(width,height,ratio,renderer);
      const fit = runtime.graphSurfaceFit({width:960,height:560},width,height);
      for (const point of [{x:0,y:0},{x:480,y:280},{x:800,y:430}]) {
        const screenX=(point.x*1.7+35)*fit.scale+fit.offsetX;
        const screenY=(point.y*1.7-21)*fit.scale+fit.offsetY;
        const actual=runtime.scenePoint(root,{clientX:rect.left+screenX,clientY:rect.top+screenY});
        close(actual.x,point.x); close(actual.y,point.y);
      }
      const surface=runtime.canvasContext(root), [a,, ,d,e,f]=transforms.at(-1);
      close(a*width/canvas.width,fit.scale); close(d*height/canvas.height,fit.scale);
      close(e*width/canvas.width,fit.offsetX); close(f*height/canvas.height,fit.offsetY);
      close(surface.bounds.x*a+e,0); close(surface.bounds.y*d+f,0);
      close(surface.bounds.width*a,canvas.width); close(surface.bounds.height*d,canvas.height);
    }
  }
});
test('hidden export surfaces use scene dimensions and remain finite', () => {
  const {runtime,root,canvas,transforms}=fixture(0,0,2);
  runtime.canvasContext(root);
  assert.equal(canvas.width,1920); assert.equal(canvas.height,1120);
  assert.deepEqual(transforms.at(-1),[2,0,0,2,0,0]);
});
test('resizing a touched or fixed viewport redraws without resetting it and coalesces callbacks', () => {
  const frames = new Map(), painted = [], stage = {}, state = {};
  let callback, next = 0, disconnected = false;
  const viewportSource = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/graph-explorer.05-viewport.js'), 'utf8');
  const bind = new Function('ResizeObserver','requestAnimationFrame','cancelAnimationFrame','drawCanvas','graphState','hasFeature', viewportSource+'\nreturn bindGraphSurfaceResize;')(
    class { constructor(action) { callback=action; } observe(value) { assert.equal(value,stage); } disconnect() { disconnected=true; } },
    action=>{frames.set(++next,action);return next;}, id=>frames.delete(id), (root, actual)=>{assert.equal(actual,state);painted.push(root);}, ()=>{throw new Error('Rebuilt stale attributes');}, root=>root.viewportEnabled);
  const root={__cfxGraphState:state,querySelector:()=>stage,__cfxGraphViewportTouched:true,viewportEnabled:true};
  bind(root); callback(); callback(); assert.equal(frames.size,1);
  const flush=()=>{const pending=[...frames.values()];frames.clear();pending.forEach(action=>action());};
  flush(); assert.deepEqual(painted,[root]);
  root.viewportEnabled=false;root.__cfxGraphViewportTouched=false;callback();flush();assert.equal(painted.length,2);
  root.isConnected=false;callback();flush();assert.equal(painted.length,2);assert.equal(disconnected,true);
});
