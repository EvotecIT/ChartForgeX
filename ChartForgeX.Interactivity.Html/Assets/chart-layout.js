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
    if(!/^chartforgex\/layout-[1-4]$/.test(spec.schema))throw new Error('Unsupported ChartForgeX layout specification.');
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
    if(spec.schema==='chartforgex/layout-4')return window.ChartForgeXLayoutRadial.create(host,spec,svg);
    let source,legend,sticky;
    let lastWidth=0,matrixViewport;
    function matrixOverlay(){
      if(!spec.matrix)return null;
      const ns='http://www.w3.org/2000/svg',group=document.createElementNS(ns,'g');
      group.setAttribute('data-cfx-role','matrix-sticky-labels');
      const background=document.createElementNS(ns,'rect');background.setAttribute('pointer-events','all');
      const card=svg.querySelector('[data-cfx-role="frame-card"],[data-cfx-role="frame-surface"]');
      let surface=card?.getAttribute('fill');
      if(!surface || surface==='none' || surface==='transparent'){
        for(let node=host;node;node=node.parentElement){const color=getComputedStyle(node).backgroundColor;if(color && color!=='transparent' && color!=='rgba(0, 0, 0, 0)'){surface=color;break;}}
      }
      background.setAttribute('fill',surface||'white');
      const header=document.createElementNS(ns,'rect');header.setAttribute('pointer-events','none');header.setAttribute('fill',background.getAttribute('fill'));
      group.appendChild(background);group.appendChild(header);
      for(const label of svg.querySelectorAll('[data-cfx-role="heatmap-row-label"],[data-cfx-role="heatmap-row-group"],[data-cfx-role="legend-entry"],[data-cfx-role^="frame-heading"]')){
        const wrapper=label.parentElement?.hasAttribute('clip-path')?label.parentElement:label;
        group.appendChild(wrapper);
      }
      svg.appendChild(group);
      const previousOverflow=host.style.overflowX,previousPadding=host.style.scrollPaddingLeft;
      host.style.overflowX='auto';
      const scroll=()=>group.setAttribute('transform',`translate(${host.scrollLeft} 0)`);
      const remember=()=>{
        scroll();
        // Ignore layout/clamping events; only a scroll in the current viewport changes the retained column.
        if(matrixViewport && !matrixViewport.printing && host.clientWidth===matrixViewport.width && matrixViewport.maximum>0 && Math.abs(host.scrollLeft-matrixViewport.assigned)>.5){
          matrixViewport.end=matrixViewport.maximum-host.scrollLeft<=1;
          matrixViewport.column=host.scrollLeft/matrixViewport.pitch;
          matrixViewport.assigned=host.scrollLeft;
        }
      };
      host.addEventListener('scroll',remember,{passive:true});
      return {group,background,header,scroll,dispose:()=>{host.removeEventListener('scroll',remember);host.style.overflowX=previousOverflow;host.style.scrollPaddingLeft=previousPadding;}};
    }
    function layout(requestedWidth,printing=false){
      if(matrixViewport)matrixViewport.printing=printing;
      const available=requestedWidth||host.clientWidth;
      if(!host.isConnected || !available || (typeof host.checkVisibility === "function" ? !host.checkVisibility() : !host.getClientRects().length))return;
      const hostWidth=printing ? available : Math.min(spec.maximum,Math.max(spec.minimum,available));
      const [left,top,pw,ph]=spec.plot, right=spec.width-left-pw;
      const matrixGutter=spec.matrix ? Math.min(left,Math.max(spec.matrix[2],hostWidth*.35)) : left;
      const minimumPlot=spec.matrix ? spec.matrix[0]*14+Math.max(0,spec.matrix[0]-1)*spec.matrix[1] : 0;
      const width=spec.matrix && !printing ? Math.max(hostWidth,matrixGutter+right+minimumPlot) : hostWidth;
      if(!source){sticky=matrixOverlay();source=window.ChartForgeXLayoutGeometry.snapshot(svg,spec);legend=window.ChartForgeXLayoutLegend.snapshot(svg,spec);}
      const delta=window.ChartForgeXLayoutLegend.layout(legend,spec,hostWidth);
      // Lane gutters narrow to keep 60% of the remaining width for lanes.
      const gutterLeft=spec.matrix ? matrixGutter : spec.gutter ? Math.min(left,Math.max(spec.gutter[1],spec.gutter[0]+(width-right-spec.gutter[0])*.4)) : left;
      const plot=[gutterLeft,top+Math.max(0,delta),Math.max(1,width-(spec.width-pw)+left-gutterLeft),Math.max(1,ph-Math.abs(delta))];
      if(spec.matrix?.[3] && !printing){
        const [columns,gap,,rows,headers]=spec.matrix,cellWidth=(plot[2]-gap*(columns-1))/columns;
        const pitch=Math.max(12+gap,Math.min(32,cellWidth*1.6));
        plot[3]=headers.reduce((sum,band)=>sum+band[1],0)+rows*pitch-gap;
      }
      const verticalShift=plot[1]+plot[3]-top-ph;
      // Bottom legends already account for wrapping; shift them only by the final plot movement.
      if(spec.matrix)window.ChartForgeXLayoutLegend.layout(legend,spec,hostWidth,verticalShift-Math.min(0,delta));
      for(const label of source.labels)label.group.style.display='';
      for(const label of source.rotatedLabels)label.style.display='';
      const projection=window.ChartForgeXLayoutGeometry.project(source,spec,width,plot);
      if(sticky){
        host.style.scrollPaddingLeft=gutterLeft+'px';
        sticky.background.setAttribute('x','0');sticky.background.setAttribute('y',plot[1]);
        sticky.background.setAttribute('width',Math.max(0,gutterLeft));sticky.background.setAttribute('height',Math.max(0,spec.height+verticalShift-plot[1]));
        sticky.header.setAttribute('x',0);sticky.header.setAttribute('y',0);
        sticky.header.setAttribute('width',hostWidth);sticky.header.setAttribute('height',Math.max(0,plot[1]));
        sticky.scroll();
      }
      // Thin captions at a regular stride, keeping matrix endpoints clear.
      const labels=source.labels.map(label=>{
        const size=label.text.getComputedTextLength(),projected=projection.x(label.center);
        const center=spec.matrix ? Math.max(gutterLeft+size/2,Math.min(width-size/2,projected)) : projected;
        return {...label,center,width:size};
      });
      let keep=labels.map((_,i)=>i);
      const fits=indices=>{const ordered=indices.map(i=>labels[i]).sort((a,b)=>a.center-b.center);return ordered.every((label,n)=>!n || label.center-label.width/2>=ordered[n-1].center+ordered[n-1].width/2+8);};
      for(let stride=2;!fits(keep) && stride<=labels.length;stride++){
        keep=labels.map((_,i)=>i).filter(i=>i%stride===0);
        if(spec.matrix && labels.length>1 && keep.at(-1)!==labels.length-1){
          while(keep.length>1 && Math.abs(labels.at(-1).center-labels[keep.at(-1)].center)<(labels.at(-1).width+labels[keep.at(-1)].width)/2+8)keep.pop();
          keep.push(labels.length-1);
        }
      }
      const shown=new Set(keep);
      labels.forEach((label,index)=>{
        label.group.style.display=shown.has(index)?'':'none';
        const labelX=Math.max(spec.matrix?gutterLeft:0,Math.min(width-label.width,label.center-label.width/2));
        label.text.setAttribute('x',labelX);
        if(spec.matrix){const clip=clipRect(label.text);if(clip){clip.setAttribute('x',labelX-1);clip.setAttribute('width',label.width+2);}}
      });
      let height=spec.matrix && !printing ? spec.height+verticalShift : spec.height;
      function size(){svg.setAttribute('width',width);svg.setAttribute('height',height);svg.setAttribute('viewBox',`0 0 ${width} ${height}`);
        svg.style.cssText=`display:block;width:${width}px;height:${height}px;max-width:none`;}
      size();
      // Measure in the current viewport, including reversed columns, and keep a regular source stride.
      const rotated=source.rotatedLabels.map(group=>({group,box:group.getBoundingClientRect()}));
      let rotatedKeep=rotated.map((_,i)=>i);
      const rotatedFits=indices=>{const boxes=indices.map(i=>rotated[i].box).sort((a,b)=>a.left-b.left);return boxes.every((box,n)=>!n||box.left>=boxes[n-1].right+8);};
      for(let stride=2;!rotatedFits(rotatedKeep)&&stride<=rotated.length;stride++)rotatedKeep=rotated.map((_,i)=>i).filter(i=>i%stride===0);
      const rotatedShown=new Set(rotatedKeep);rotated.forEach((label,index)=>label.group.style.display=rotatedShown.has(index)?'':'none');
      fitText(width);
      if(spec.matrix?.[3] && !printing){
        // Fit the footer to painted content.
        const origin=svg.getBoundingClientRect().top;
        const bottoms=[...svg.querySelectorAll('text,[data-cfx-role="heatmap-scale"]')]
          .filter(node=>node.getClientRects().length && getComputedStyle(node).display!=='none' && !node.closest('[style*="display: none"]'))
          .map(node=>node.getBoundingClientRect().bottom-origin);
        height=Math.ceil(Math.max(plot[1]+plot[3],...bottoms)+Math.max(12,spec.spacing));
        for(const frame of svg.querySelectorAll('[data-cfx-role="frame-card"],[data-cfx-role="frame-surface"]'))
          if(frame.tagName.toLowerCase()==='rect')frame.setAttribute('height',Math.max(0,height-Number(frame.getAttribute('y'))));
        if(sticky)sticky.background.setAttribute('height',Math.max(0,height-plot[1]));
        size();
      }
      if(sticky && !printing){
        matrixViewport ||= {end:spec.matrixWindow!=='Squeeze' && !spec.matrixReversed,column:0};
        matrixViewport.width=host.clientWidth;
        matrixViewport.maximum=Math.max(0,host.scrollWidth-host.clientWidth);
        matrixViewport.pitch=(plot[2]+spec.matrix[1])/spec.matrix[0];
        host.scrollLeft=matrixViewport.end?matrixViewport.maximum:matrixViewport.column*matrixViewport.pitch;
        matrixViewport.assigned=host.scrollLeft;
        sticky.scroll();
      }
      host.dataset.cfxLayoutWidth=String(hostWidth);host.dataset.cfxLayoutVersion=spec.schema==='chartforgex/layout-3'?'spec-3':spec.schema==='chartforgex/layout-2'?'spec-2':'spec-1';lastWidth=hostWidth;
    }
    // Truncate captions without shrinking text; retain full titles.
    let captions;
    function fitText(width){
      captions ||= [...svg.querySelectorAll('[data-cfx-role^="frame-heading"] text,[data-cfx-role="schedule-row-label"] text,[data-cfx-role="lane-summary"] text,[data-cfx-role="state-timeline-group"] text,[data-cfx-role="gantt-lane-group"] text,[data-cfx-role="heatmap-row-label"] text')]
        .map(text=>({text,full:nativeTitle(text) ?? text.textContent,clip:clipRect(text),right:!!text.closest('[data-cfx-role="schedule-row-label"],[data-cfx-role="lane-summary"],[data-cfx-role="heatmap-row-label"]')}));
      for(const caption of captions){
        const {text,full,clip,right}=caption;
        if(text.textContent!==full)text.textContent=full;
        // Re-anchor with the browser's font metrics.
        if(right && clip){text.setAttribute('text-anchor','end');text.setAttribute('x',Number(clip.getAttribute('x'))+Number(clip.getAttribute('width'))-1);}
        const anchor=text.getAttribute('text-anchor')||getComputedStyle(text).textAnchor;
        const x=Number(text.getAttribute('x'))||0, clipRight=Math.min(width-Math.max(0,spec.spacing||8),clip ? Number(clip.getAttribute('x'))+Number(clip.getAttribute('width')) : Infinity);
        // Allow two pixels for glyph overhang.
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
    return {layout,dispose:()=>sticky?.dispose(),get width(){return lastWidth;},svg};
  }
  window.ChartForgeXLayout={create};
})();
