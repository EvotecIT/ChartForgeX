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
      if(!host.isConnected || !available || (typeof host.checkVisibility === "function" ? !host.checkVisibility() : !host.getClientRects().length))return;
      const width=printing ? available : Math.min(spec.maximum,Math.max(spec.minimum,available));
      if(!source){source=window.ChartForgeXLayoutGeometry.snapshot(svg,spec);legend=window.ChartForgeXLayoutLegend.snapshot(svg,spec);}
      const delta=window.ChartForgeXLayoutLegend.layout(legend,spec,width);
      const [left,top,pw,ph]=spec.plot;
      // A lane gutter narrows toward its minimum so lanes keep about 60% of the width left beside fixed right gutters.
      const right=spec.width-left-pw;
      const gutterLeft=spec.gutter ? Math.min(left,Math.max(spec.gutter[1],spec.gutter[0]+(width-right-spec.gutter[0])*.4)) : left;
      const plot=[gutterLeft,top+Math.max(0,delta),Math.max(1,width-(spec.width-pw)+left-gutterLeft),Math.max(1,ph-Math.abs(delta))];
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
      // Two endpoints that still collide leave only the first one; overlapping captions are never drawn.
      if(keep.length===2 && labels.length>1 && !fits(keep))keep=[0];
      const shown=new Set(keep);
      labels.forEach((label,index)=>{
        label.group.style.display=shown.has(index)?'':'none';
        label.text.setAttribute('x',Math.max(0,Math.min(width-label.width,label.center-label.width/2)));
      });
      svg.setAttribute('width',width);svg.setAttribute('height',spec.height);svg.setAttribute('viewBox',`0 0 ${width} ${spec.height}`);
      svg.style.cssText=`display:block;width:${width}px;height:${spec.height}px;max-width:none`;
      fitText(width);
      host.dataset.cfxLayoutWidth=String(width);host.dataset.cfxLayoutVersion='spec-1';lastWidth=width;
    }
    // Headings and lane captions keep their size and truncate with an ellipsis; the complete text stays in a <title>.
    let captions;
    function fitText(width){
      captions ||= [...svg.querySelectorAll('[data-cfx-role^="frame-heading"] text,[data-cfx-role="schedule-row-label"] text,[data-cfx-role="lane-summary"] text,[data-cfx-role="state-timeline-group"] text,[data-cfx-role="gantt-lane-group"] text')]
        .map(text=>({text,full:text.textContent,clip:clipRect(text),right:!!text.closest('[data-cfx-role="schedule-row-label"],[data-cfx-role="lane-summary"]')}));
      for(const caption of captions){
        const {text,full,clip,right}=caption;
        if(text.textContent!==full)text.textContent=full;
        // Native right alignment is baked with server metrics; re-anchor it so the browser's own font stays aligned.
        if(right && clip){text.setAttribute('text-anchor','end');text.setAttribute('x',Number(clip.getAttribute('x'))+Number(clip.getAttribute('width'))-1);}
        const anchor=text.getAttribute('text-anchor')||getComputedStyle(text).textAnchor;
        const x=Number(text.getAttribute('x'))||0, clipRight=Math.min(width-Math.max(0,spec.spacing||8),clip ? Number(clip.getAttribute('x'))+Number(clip.getAttribute('width')) : Infinity);
        // Glyph boxes extend slightly past their advance, so keep two pixels of slack inside the clip.
        const limit=(anchor==='end' ? (clip ? Number(clip.getAttribute('width')) : x) : anchor==='middle' ? 2*Math.min(x,width-x) : clipRight-x)-2;
        if(limit<=0 || text.getComputedTextLength()<=limit+.5)continue;
        let low=0,high=full.length;
        while(low<high){const mid=Math.ceil((low+high)/2);text.textContent=full.slice(0,mid).trimEnd()+'…';if(text.getComputedTextLength()<=limit+.5)low=mid;else high=mid-1;}
        text.textContent=low>0 ? full.slice(0,low).trimEnd()+'…' : '';
        const title=document.createElementNS('http://www.w3.org/2000/svg','title');title.textContent=full;text.appendChild(title);
      }
    }
    function clipRect(node){
      for(let current=node;current && current!==svg;current=current.parentElement){
        const id=current.getAttribute?.('clip-path')?.match(/url\(#([^)]*)\)/)?.[1], clip=id && svg.getElementById(id);
        const rect=clip?.querySelector('rect');if(rect)return rect;
      }
      return null;
    }
    return {layout,get width(){return lastWidth;},svg};
  }
  window.ChartForgeXLayout={create};
})();
