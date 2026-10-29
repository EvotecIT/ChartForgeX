(function () {
  "use strict";
  if(window.ChartForgeXLayoutLegend)return;
  function snapshot(svg,spec){
    const entries=[...svg.querySelectorAll('[data-cfx-role="legend-entry"]')].map(node=>{
      const caption=node.querySelector('[data-cfx-role="legend-label"]'),label=caption?.querySelector('text')||caption,columns=[...node.querySelectorAll('[data-cfx-role="legend-value"],[data-cfx-role="legend-percentage"]')].map(node=>({node,transform:node.getAttribute('transform')}));
      return {node,box:node.getBBox(),label,full:label?.textContent||'',labelX:label?.getAttribute('x'),anchor:label?.getAttribute('text-anchor'),columns};
    });
    // The original lane no longer bounds the reflowed rows. Each entry is fitted before it receives a slot.
    for(const entry of entries)for(let node=entry.node.parentElement;node && node!==svg;node=node.parentElement)node.removeAttribute('clip-path');
    const top=entries.length?Math.min(...entries.map(e=>e.box.y)):0;
    const bottom=entries.length?Math.max(...entries.map(e=>e.box.y+e.box.height)):0;
    const summary=entries[0]?.label?.cloneNode(true);
    if(summary){summary.textContent='';summary.setAttribute('data-cfx-role','legend-overflow');summary.removeAttribute('id');(svg.querySelector('[data-cfx-role=matrix-sticky-labels]')||svg).appendChild(summary);}
    return {entries,summary,top,bottom,above:bottom<=spec.plot[1]+1,below:top>=spec.plot[1]+spec.plot[3]-1};
  }
  function fit(entry,available){
    const {node,label,full,columns}=entry;
    node.removeAttribute('transform');node.style.display='';
    if(!label)return {...entry,size:entry.box.width};
    label.textContent=full;label.setAttribute('x',entry.labelX);if(entry.anchor)label.setAttribute('text-anchor',entry.anchor);else label.removeAttribute('text-anchor');
    for(const column of columns){if(column.transform)column.node.setAttribute('transform',column.transform);else column.node.removeAttribute('transform');}
    const original=label.getComputedTextLength(),room=Math.max(0,available-(entry.box.width-original));
    if(original>room){
      let low=0,high=full.length;
      while(low<high){const middle=Math.ceil((low+high)/2);label.textContent=full.slice(0,middle)+'\u2026';if(label.getComputedTextLength()<=room)low=middle;else high=middle-1;}
      label.textContent=low?full.slice(0,low)+'\u2026':'';
      label.setAttribute('text-anchor','start');label.setAttribute('x',entry.box.x+18);
      for(const column of columns)column.node.setAttribute('transform',`translate(${label.getComputedTextLength()-original} 0) ${column.transform||''}`);
      let title=node.querySelector(':scope > title');if(!title){title=document.createElementNS('http://www.w3.org/2000/svg','title');node.prepend(title);}title.textContent=full;
    }
    return {...entry,size:Math.min(available,node.getBBox().width)};
  }
  function layout(state,spec,width,verticalShift=0){
    if(!state.entries.length)return 0;
    const gap=spec.spacing,padding=Math.min(24,width/10),available=width-2*padding;
    const line=Math.max(...state.entries.map(e=>e.box.height))+8,maxRows=Math.max(1,Math.floor(spec.height*.35/line));
    const rows=[[]];let used=0,kept=0;
    for(const raw of state.entries){
      const entry=fit(raw,available);
      if(used && used+gap+entry.size>available){rows.push([]);used=0;}
      if(rows.length>maxRows){rows.pop();break;}
      rows.at(-1).push(entry);used+=(used?gap:0)+entry.size;kept++;
    }
    let omitted=state.entries.length-kept;
    if(omitted){const last=rows.at(-1).pop();if(last){kept--;omitted++;}}
    state.entries.forEach((entry,index)=>entry.node.style.display=index<kept?'':'none');
    if(state.summary){
      state.summary.textContent=omitted?'+'+omitted+' more':'';state.summary.style.display=omitted?'':'none';
      if(omitted){let title=state.summary.querySelector('title');if(!title){title=document.createElementNS('http://www.w3.org/2000/svg','title');state.summary.appendChild(title);}title.textContent=state.entries.slice(kept).map(e=>e.full).join(', ');}
    }
    const height=rows.length*line-8,delta=Math.max(0,height-(state.bottom-state.top));let y=state.below?state.top-delta+verticalShift:state.top;
    for(let i=0;i<rows.length;i++){
      const row=rows[i],summaryWidth=omitted && i===rows.length-1?state.summary.getComputedTextLength()+gap:0;
      const occupied=row.reduce((n,e)=>n+e.size,0)+gap*Math.max(0,row.length-1)+summaryWidth;
      let x=spec.legendPosition.endsWith('Right')?width-padding-occupied:['Top','Bottom'].includes(spec.legendPosition)?(width-occupied)/2:padding;
      for(const entry of row){entry.node.setAttribute('transform',`translate(${x-entry.box.x} ${y-entry.box.y})`);x+=entry.size+gap;}
      if(summaryWidth){state.summary.setAttribute('x',x);state.summary.setAttribute('y',y+line-8);}
      y+=line;
    }
    return state.above?delta:state.below?-delta:0;
  }
  window.ChartForgeXLayoutLegend={snapshot,layout};
})();
