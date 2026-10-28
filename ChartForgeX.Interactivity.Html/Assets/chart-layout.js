(function () {
  "use strict";
  if(window.ChartForgeXLayout)return;
  let instance=0;
  function namespace(svg){
    const nodes=[svg,...svg.querySelectorAll('*')], ids=new Map(),prefix='c'+(++instance).toString(36)+'-';
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
    if(spec.schema!=="chartforgex/layout-1" && spec.schema!=="chartforgex/layout-2")throw new Error('Unsupported ChartForgeX layout specification.');
    let svg;
    if(spec.schema==="chartforgex/layout-1"){
      const parsed=new DOMParser().parseFromString(spec.svg,'image/svg+xml');
      if(parsed.querySelector('parsererror'))throw new Error('Invalid ChartForgeX paint specification.');
      svg=document.importNode(parsed.documentElement,true);host.querySelector(':scope > svg').replaceWith(svg);
    }else {
      svg=host.querySelector(':scope > svg');
      if(svg?.cfxBaseSvg){const base=svg.cfxBaseSvg, fresh=base.cloneNode(true);fresh.cfxBaseSvg=base;svg.replaceWith(fresh);svg=fresh;}
      else if(svg)svg.cfxBaseSvg=svg.cloneNode(true);
    }
    if(!svg)throw new Error('Missing ChartForgeX paint specification.');
    namespace(svg);
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
      for(const label of source.rotatedLabels)label.style.display='';
      const projection=window.ChartForgeXLayoutGeometry.project(source,spec,width,plot);
      // Tick captions are measured in their token font at the actual width and thin at one regular stride from the
      // first caption, keeping the native caption gap. The last caption stays only when it falls on the stride.
      const labels=source.labels.map(label=>({...label,center:projection.x(label.center),width:label.text.getComputedTextLength()}));
      let keep=labels.map((_,i)=>i);
      const fits=indices=>indices.every((i,n)=>!n || labels[i].center-labels[i].width/2>=labels[indices[n-1]].center+labels[indices[n-1]].width/2+8);
      for(let stride=2;!fits(keep) && stride<=labels.length;stride++)keep=labels.map((_,i)=>i).filter(i=>i%stride===0);
      const shown=new Set(keep);
      labels.forEach((label,index)=>{
        label.group.style.display=shown.has(index)?'':'none';
        label.text.setAttribute('x',Math.max(0,Math.min(width-label.width,label.center-label.width/2)));
      });
      svg.setAttribute('width',width);svg.setAttribute('height',spec.height);svg.setAttribute('viewBox',`0 0 ${width} ${spec.height}`);
      svg.style.cssText=`display:block;width:${width}px;height:${spec.height}px;max-width:none`;
      // Measure in the current viewport, including reversed columns, and keep a regular source stride.
      const rotated=source.rotatedLabels.map(group=>({group,box:group.getBoundingClientRect()}));
      let rotatedKeep=rotated.map((_,i)=>i);
      const rotatedFits=indices=>{const boxes=indices.map(i=>rotated[i].box).sort((a,b)=>a.left-b.left);return boxes.every((box,n)=>!n||box.left>=boxes[n-1].right+8);};
      for(let stride=2;!rotatedFits(rotatedKeep)&&stride<=rotated.length;stride++)rotatedKeep=rotated.map((_,i)=>i).filter(i=>i%stride===0);
      const rotatedShown=new Set(rotatedKeep);rotated.forEach((label,index)=>label.group.style.display=rotatedShown.has(index)?'':'none');
      fitText(width);
      host.dataset.cfxLayoutWidth=String(width);host.dataset.cfxLayoutVersion=spec.schema==='chartforgex/layout-2'?'spec-2':'spec-1';lastWidth=width;
    }
    // Headings and lane captions keep their size and truncate with an ellipsis; the complete text stays in a <title>.
    let captions;
    function fitText(width){
      captions ||= [...svg.querySelectorAll('[data-cfx-role^="frame-heading"] text,[data-cfx-role="schedule-row-label"] text,[data-cfx-role="lane-summary"] text,[data-cfx-role="state-timeline-group"] text,[data-cfx-role="gantt-lane-group"] text')]
        .map(text=>({text,full:nativeTitle(text) ?? text.textContent,clip:clipRect(text),right:!!text.closest('[data-cfx-role="schedule-row-label"],[data-cfx-role="lane-summary"]')}));
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
    // Text the server already shortened carries its complete value in an enclosing native tooltip.
    function nativeTitle(text){
      // The tooltip wraps the caption's clip and role groups, so it sits at most three levels up.
      for(let node=text.parentElement,depth=0;node && node!==svg && depth<4;node=node.parentElement,depth++){
        const title=node.querySelector(':scope > title');if(title)return title.textContent;
      }
      return null;
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
