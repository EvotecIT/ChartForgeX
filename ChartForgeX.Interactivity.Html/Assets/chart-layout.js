(function () {
  "use strict";
  if(window.ChartForgeXLayout)return;
  let instance=0;
  function namespace(svg){
    const nodes=[svg,...svg.querySelectorAll('*')], ids=new Map(),prefix='cfx-live-'+(++instance)+'-';
    for(const node of nodes)if(node.id)ids.set(node.id,prefix+node.id);
    for(const node of nodes)for(const attribute of [...node.attributes]){
      let value=attribute.value;
      if(attribute.name==='id')value=ids.get(value);
      else if(['aria-labelledby','aria-describedby'].includes(attribute.name))value=value.split(/\s+/).map(id=>ids.get(id)||id).join(' ');
      else {value=value.replace(/url\(#([^)]*)\)/g,(_,id)=>'url(#'+(ids.get(id)||id)+')');if(value.startsWith('#')&&ids.has(value.slice(1)))value='#'+ids.get(value.slice(1));}
      if(value!==attribute.value)node.setAttribute(attribute.name,value);
    }
  }
  function create(host,spec){
    if(spec.schema!=="chartforgex/layout-1")throw new Error('Unsupported ChartForgeX layout specification.');
    const parsed=new DOMParser().parseFromString(spec.svg,'image/svg+xml');
    if(parsed.querySelector('parsererror'))throw new Error('Invalid ChartForgeX paint specification.');
    const svg=document.importNode(parsed.documentElement,true);namespace(svg);host.querySelector(':scope > svg').replaceWith(svg);
    let source,legend;
    let lastWidth=0;
    function layout(requestedWidth,printing=false){
      const available=requestedWidth||host.getBoundingClientRect().width;
      if(!host.isConnected || !available || !host.checkVisibility())return;
      const width=printing ? available : Math.min(spec.maximum,Math.max(spec.minimum,available));
      if(!source){source=window.ChartForgeXLayoutGeometry.snapshot(svg,spec);legend=window.ChartForgeXLayoutLegend.snapshot(svg,spec);}
      const delta=window.ChartForgeXLayoutLegend.layout(legend,spec,width);
      const [left,top,pw,ph]=spec.plot, plot=[left,top+Math.max(0,delta),Math.max(1,width-(spec.width-pw)),Math.max(1,ph-Math.abs(delta))];
      for(const label of source.labels)label.group.style.display='';
      const projection=window.ChartForgeXLayoutGeometry.project(source,spec,width,plot);
      // Tick captions are measured in their token font at the actual width. Keep endpoints and thin at a regular stride.
      const labels=source.labels.map(label=>({...label,center:projection.x(label.center),width:label.text.getComputedTextLength()}));
      let keep=labels.map((_,i)=>i);
      const fits=indices=>indices.every((i,n)=>!n || labels[i].center-labels[i].width/2>=labels[indices[n-1]].center+labels[indices[n-1]].width/2+8);
      for(let stride=2;!fits(keep) && stride<=labels.length;stride++){
        keep=labels.map((_,i)=>i).filter(i=>i%stride===0);
        if(keep.at(-1)!==labels.length-1)keep.push(labels.length-1);
        // Endpoints are mandatory; omit the adjacent stride tick if that final gap is too short.
        if(keep.length>2 && !fits(keep.slice(-2)))keep.splice(keep.length-2,1);
      }
      const shown=new Set(keep);
      labels.forEach((label,index)=>{
        label.group.style.display=shown.has(index)?'':'none';
        label.text.setAttribute('x',Math.max(0,Math.min(width-label.width,label.center-label.width/2)));
      });
      svg.setAttribute('width',width);svg.setAttribute('height',spec.height);svg.setAttribute('viewBox',`0 0 ${width} ${spec.height}`);
      svg.style.cssText=`display:block;width:${width}px;height:${spec.height}px;max-width:none`;
      host.dataset.cfxLayoutWidth=String(width);host.dataset.cfxLayoutVersion='spec-1';lastWidth=width;
    }
    return {layout,get width(){return lastWidth;},svg};
  }
  window.ChartForgeXLayout={create};
})();
