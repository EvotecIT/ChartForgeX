(function () {
  "use strict";
  const current=document.currentScript?.parentElement;
  if(window.ChartForgeXResponsive){window.ChartForgeXResponsive.mount(current);return;}
  const warned=new WeakSet();
  // A host without usable layout data keeps its static drawing. Warn once and
  // allow a later specification node to retry instead of caching the failure.
  function unavailable(host,reason){
    delete host.cfxResponsiveReady;
    // A specification that arrived while this attempt was still running gets its own attempt.
    if(host.cfxResponsiveRetry){delete host.cfxResponsiveRetry;queueMicrotask(()=>mount(host));}
    if(!warned.has(host)){warned.add(host);console.warn('ChartForgeX responsive chart kept its static drawing: '+reason);}
  }
  // The newest specification node wins, so a host can replace an unusable one by appending another.
  async function readSpecification(host){
    const nodes=host.querySelectorAll('[data-cfx-spec],[data-cfx-spec-gzip]'),node=nodes[nodes.length-1];
    if(!node||!node.hasAttribute('data-cfx-spec-gzip'))return node?.textContent;
    if(typeof DecompressionStream!=='function')return null;
    const bytes=Uint8Array.from(atob(node.textContent.trim()),c=>c.charCodeAt(0));
    return await new Response(new Blob([bytes]).stream().pipeThrough(new DecompressionStream('gzip'))).text();
  }
  async function initialize(host){
    // Parser-added hosts can appear before their inline specification is complete.
    if(document.readyState==='loading')await new Promise(resolve=>document.addEventListener('DOMContentLoaded',resolve,{once:true}));
    let payload,spec;
    try{payload=await readSpecification(host);}catch{return unavailable(host,'the compressed layout data could not be read.');}
    if(payload===null)return unavailable(host,'this browser cannot decompress the layout data.');
    if(!payload?.trim())return unavailable(host,'the layout data is missing.');
    try{spec=JSON.parse(payload);}catch{return unavailable(host,'the layout data is not valid JSON.');}
    if(!spec||typeof spec!=='object'||!window.ChartForgeXLayout)return unavailable(host,'the layout data or layout runtime is unavailable.');
    if(!host.isConnected){delete host.cfxResponsiveReady;return;}
    await document.fonts?.ready;
    if(!host.isConnected){delete host.cfxResponsiveReady;return;}
    const fallback=host.querySelector(':scope > svg');let drawing;
    try{drawing=window.ChartForgeXLayout.create(host,spec);}catch{return unavailable(host,'the layout data does not match this drawing.');}
    let timer=0,disposed=false,printing=matchMedia('print').matches;
    host.style.minWidth=printing?'0':spec.minimum+'px';
    // Every layout, including later resize, print and font passes, falls back to the static drawing on failure.
    const fail=()=>{host.cfxResponsive?.dispose();disposed=true;if(fallback&&drawing.svg.isConnected)drawing.svg.replaceWith(fallback);unavailable(host,'the layout data could not be applied.');};
    const layout=(width,forPrint=printing)=>{if(disposed)return;try{drawing.layout(width,forPrint);}catch{fail();}};
    const schedule=()=>{clearTimeout(timer);timer=setTimeout(()=>layout(),60);};
    const observer=new ResizeObserver(schedule);observer.observe(host);
    const print=()=>{printing=true;host.style.minWidth='0';clearTimeout(timer);layout(host.getBoundingClientRect().width||spec.printWidth);};
    const afterPrint=()=>{printing=false;host.style.minWidth=spec.minimum+'px';layout();};
    const media=matchMedia('print'),changed=event=>event.matches?print():afterPrint();
    addEventListener('beforeprint',print);addEventListener('afterprint',afterPrint);media.addEventListener('change',changed);
    host.cfxResponsive={layout,dispose:()=>{disposed=true;clearTimeout(timer);observer.disconnect();removeEventListener('beforeprint',print);removeEventListener('afterprint',afterPrint);media.removeEventListener('change',changed);delete host.cfxResponsive;delete host.cfxResponsiveReady;}};
    layout();
    document.fonts?.ready.then(()=>layout());
  }
  function hosts(scope){if(!scope?.querySelectorAll)return[];return[...(scope.matches?.('[data-cfx-responsive-chart]')?[scope]:[]),...scope.querySelectorAll('[data-cfx-responsive-chart]')];}
  function mount(scope=document){return Promise.all(hosts(scope).map(host=>host.cfxResponsiveReady ||= initialize(host)));}
  window.ChartForgeXResponsive={mount};
  const owner=node=>node?.nodeType===1&&node.matches('[data-cfx-spec],[data-cfx-spec-gzip]')?node.closest('[data-cfx-responsive-chart]'):null;
  const changes=new MutationObserver(records=>{for(const record of records){for(const node of record.addedNodes){mount(node);const host=owner(node);if(host&&!host.cfxResponsive){if(host.cfxResponsiveReady)host.cfxResponsiveRetry=true;else mount(host);}}for(const node of record.removedNodes)for(const host of hosts(node))if(!host.isConnected)host.cfxResponsive?.dispose();}});
  changes.observe(document.documentElement,{childList:true,subtree:true});
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',()=>mount(document),{once:true});
  mount(current||document);
})();
