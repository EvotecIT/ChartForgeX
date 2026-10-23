(function () {
  "use strict";
  const current=document.currentScript?.parentElement;
  if(window.ChartForgeXResponsive){window.ChartForgeXResponsive.mount(current);return;}
  async function initialize(host){
    const compressed=host.querySelector('[data-cfx-spec-gzip]');
    let payload=host.querySelector('[data-cfx-spec]')?.textContent;
    if(compressed){
      if(typeof DecompressionStream!=='function')return;
      const bytes=Uint8Array.from(atob(compressed.textContent),c=>c.charCodeAt(0));
      payload=await new Response(new Blob([bytes]).stream().pipeThrough(new DecompressionStream('gzip'))).text();
    }
    if(!host.isConnected){delete host.cfxResponsiveReady;return;}
    await document.fonts?.ready;
    if(!host.isConnected){delete host.cfxResponsiveReady;return;}
    const spec=JSON.parse(payload),drawing=window.ChartForgeXLayout.create(host,spec);
    let timer=0,disposed=false,printing=matchMedia('print').matches;
    host.style.minWidth=printing?'0':spec.minimum+'px';
    const layout=(width,forPrint=printing)=>{if(!disposed)drawing.layout(width,forPrint);};
    const schedule=()=>{clearTimeout(timer);timer=setTimeout(()=>layout(),60);};
    const observer=new ResizeObserver(schedule);observer.observe(host);
    const print=()=>{printing=true;host.style.minWidth='0';clearTimeout(timer);layout(host.getBoundingClientRect().width||spec.printWidth);};
    const afterPrint=()=>{printing=false;host.style.minWidth=spec.minimum+'px';layout();};
    const media=matchMedia('print'),changed=event=>event.matches?print():afterPrint();
    addEventListener('beforeprint',print);addEventListener('afterprint',afterPrint);media.addEventListener('change',changed);
    host.cfxResponsive={layout,dispose:()=>{disposed=true;clearTimeout(timer);observer.disconnect();removeEventListener('beforeprint',print);removeEventListener('afterprint',afterPrint);media.removeEventListener('change',changed);delete host.cfxResponsive;delete host.cfxResponsiveReady;}};
    layout();
    document.fonts?.ready.then(()=>{if(!disposed)layout();});
  }
  function hosts(scope){if(!scope?.querySelectorAll)return[];return[...(scope.matches?.('[data-cfx-responsive-chart]')?[scope]:[]),...scope.querySelectorAll('[data-cfx-responsive-chart]')];}
  function mount(scope=document){return Promise.all(hosts(scope).map(host=>host.cfxResponsiveReady ||= initialize(host)));}
  window.ChartForgeXResponsive={mount};
  const changes=new MutationObserver(records=>{for(const record of records){for(const node of record.addedNodes)mount(node);for(const node of record.removedNodes)for(const host of hosts(node))if(!host.isConnected)host.cfxResponsive?.dispose();}});
  changes.observe(document.documentElement,{childList:true,subtree:true});
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',()=>mount(document),{once:true});
  mount(current||document);
})();
