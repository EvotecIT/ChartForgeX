(function () {
  "use strict";
  if (window.ChartForgeXLayoutGeometry) return;
  const numeric = /[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?/g;
  const tokens = /[MmLlHhVvCcSsQqTtAaZz]|[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?/g;
  const finite = value => Number.isFinite(value) ? Math.round(value * 1000000) / 1000000 : 0;
  function path(value, x, y) {
    const list = value.match(tokens) || [], out = []; let at = 0, command, px = 0, py = 0, sx = 0, sy = 0;
    const counts = { M:2,L:2,H:1,V:1,C:6,S:4,Q:4,T:2,A:7 };
    while (at < list.length) {
      if (/^[A-Za-z]$/.test(list[at])) command = list[at++];
      const kind = command.toUpperCase(), relative = command !== kind;
      if (kind === "Z") { out.push("Z"); px=sx;py=sy;command=null;continue; }
      const count=counts[kind]; if(!count || at+count>list.length) return value;
      const data=list.slice(at,at+count).map(Number);at+=count;
      if(kind==='H'){px=(relative?px:0)+data[0];out.push('H'+finite(x(px)));continue;}
      if(kind==='V'){py=(relative?py:0)+data[0];out.push('V'+finite(y(py)));continue;}
      const start=kind==='A'?5:0;
      for(let i=start;i<count;i+=2){data[i]+=(relative?px:0);data[i+1]+=(relative?py:0);}
      px=data[count-2];py=data[count-1];
      if(kind==='M'){sx=px;sy=py;}
      for(let i=start;i<count;i+=2){data[i]=finite(x(data[i]));data[i+1]=finite(y(data[i+1]));}
      out.push(kind+data.join(' ')); if(kind==='M')command=relative?'l':'L';
    }
    return out.join(' ');
  }
  function snapshot(svg, spec) {
    const nodes=[...svg.querySelectorAll('*')], bars=new Map(), excluded=new Set();
    for(const entry of svg.querySelectorAll('[data-cfx-role=legend-entry]')){
      excluded.add(entry);for(const node of entry.querySelectorAll('*'))excluded.add(node);
      for(const node of entry.querySelectorAll('[clip-path]')){const id=node.getAttribute('clip-path').match(/url\(#([^)]*)\)/)?.[1],clip=id&&svg.getElementById(id);if(clip)for(const child of clip.querySelectorAll('*'))excluded.add(child);}
    }
    const plot=spec.plot, slots=new Map(spec.bars.map(slot=>[slot.series+':'+slot.point,slot]));
    const points=[...svg.querySelectorAll('[data-cfx-role="point"]')];
    for(const point of points){
      const kind=point.closest('[data-cfx-role="series"]')?.dataset.cfxKind;
      if(kind!=='Bar')continue;
      const bar=point.querySelector('[data-cfx-role="bar"]'); if(!bar)continue;
      const slot=slots.get(point.dataset.cfxSeries+':'+point.dataset.cfxPoint);if(!slot)continue;
      const bounds=bar.getBBox(), box={center:bounds.x+bounds.width/2,width:bounds.width,
        categoryCenter:slot.anchor,count:slot.count,position:slot.position};
      for(const node of point.querySelectorAll('*'))bars.set(node,box);
      for(const node of point.querySelectorAll('[clip-path]')){
        const id=node.getAttribute('clip-path').match(/url\(#([^)]*)\)/)?.[1];
        const clip=id && svg.getElementById(id);if(clip)for(const child of clip.querySelectorAll('*'))bars.set(child,box);
      }
    }
    // Headings and the clip that frames them keep their left inset when a drawing has no plot band.
    const headings=new Set();
    for(const heading of svg.querySelectorAll('[data-cfx-role^="frame-heading"]')){
      for(const node of heading.querySelectorAll('*'))headings.add(node);
      const id=heading.closest('[clip-path]')?.getAttribute('clip-path').match(/url\(#([^)]*)\)/)?.[1],clip=id&&svg.getElementById(id);
      if(clip)for(const child of clip.querySelectorAll('*'))headings.add(child);
    }
    // Legends and caption boxes inside a plot band move with it but keep their own width: scaling them squashes the
    // colour steps and shrinks each caption's clip until one character remains.
    const rigid=new Map();
    for(const group of svg.querySelectorAll('[data-cfx-role="heatmap-scale"],[data-cfx-role="heatmap-column-label"]')){
      if(group.closest('[transform*="rotate("]'))continue;
      const box=group.getBBox(),anchor={center:box.x+box.width/2,width:box.width};
      for(const node of [group,...group.querySelectorAll('*')])rigid.set(node,anchor);
      // A caption's own clip may sit on the wrapper around it; take it only when that wrapper holds this caption alone.
      const owner=group.parentElement?.closest('[clip-path]'), own=owner && owner.querySelectorAll('[data-cfx-role]').length===1 ? [owner] : [];
      for(const node of [...own,group,...group.querySelectorAll('[clip-path]')]){const id=node.getAttribute('clip-path')?.match(/url\(#([^)]*)\)/)?.[1],clip=id&&svg.getElementById(id);if(clip)for(const child of clip.querySelectorAll('*'))rigid.set(child,anchor);}
    }
    const recipes=[];
    for(const node of nodes){
      if(node.closest('pattern,marker') || excluded.has(node))continue;
      const attributes={};
      for(const name of ['x','y','cx','cy','x1','y1','x2','y2','width','height','d','points','transform']){
        const value=node.getAttribute(name);if(value!=null && !value.includes('%'))attributes[name]=value;
      }
      if(Object.keys(attributes).length)recipes.push({node,attributes,bar:bars.get(node),rigid:rigid.get(node),heading:headings.has(node)||!!node.closest('[data-cfx-role^="frame-heading"]')});
    }
    const labels=[...svg.querySelectorAll('[data-cfx-role="axis-x-label"],[data-cfx-role="heatmap-column-label"],[data-cfx-role="schedule-tick-label"]')]
      .filter(group=>!group.matches('[data-cfx-role="heatmap-column-label"]') || !group.closest('[transform*="rotate("]')).map(group=>{
      const text=group.querySelector('text')||group, box=text.getBBox();
      return {group,text,center:box.x+box.width/2,sourceWidth:box.width};
    });
    const centers=[...new Set(spec.bars.map(slot=>slot.categoryAnchor))].sort((a,b)=>a-b);
    const pitch=Math.min(spec.barPitch ?? plot[2],centers.slice(1).reduce((p,c,i)=>Math.min(p,c-centers[i]),plot[2]));
    return {recipes,labels,pitch,plot};
  }
  function project(state, spec, width, plot) {
    const [left,top,pw,ph]=spec.plot, [nl,nt,nw,nh]=plot;
    const map=(v,low,size,newLow,newSize,totalDelta,before=value=>value)=>v<low? before(v) : v>low+size ? v+totalDelta : newLow+(v-low)*newSize/size;
    // A declared lane gutter narrows proportionally between its start and the plot; content before it stays put.
    const gutter=spec.gutter, before=!gutter||nl>=left ? v=>v : v=>v<=gutter[0] ? v : gutter[0]+(v-gutter[0])*(nl-gutter[0])/Math.max(.001,left-gutter[0]);
    const x=v=>spec.cartesian ? map(v,left,pw,nl,nw,width-spec.width,before) : v+(width-spec.width)/2, y=v=>map(v,top,ph,nt,nh,0);
    const maximumBarWidth=spec.barWidthUnbounded ? Infinity : spec.maximumBarWidth;
    for(const recipe of state.recipes){
      const {node,attributes:a,bar}=recipe;
      // Match the native category-slot contract: 68% occupied pitch, bounded gaps, independently capped bodies.
      const occupied=state.pitch*nw/pw*.68, gap=bar && bar.count>1 ? Math.min(spec.spacing/2,occupied/(bar.count*4)):0;
      const thickness=bar?Math.max(Math.min(.1,maximumBarWidth),Math.min(maximumBarWidth,(occupied-gap*(bar.count-1))/bar.count)):0;
      const center=bar?x(bar.categoryCenter)+(bar.position-(bar.count-1)/2)*(thickness+gap):0;
      // Drawings without a plot band stay centred, but their heading keeps its left inset and is fitted afterwards.
      const rigid=recipe.rigid, shift=rigid ? Math.max(rigid.width/2,Math.min(width-rigid.width/2,x(rigid.center)))-rigid.center : 0;
      const bx=bar?v=>center+(v-bar.center)*thickness/Math.max(.001,bar.width):rigid ? v=>v+shift : !spec.cartesian && recipe.heading ? v=>v : x;
      const baseX=Number(a.x||0),baseY=Number(a.y||0);
      for(const [name,value] of Object.entries(a)){
        let result=value;
        if(['x','cx','x1','x2'].includes(name))result=finite(bx(Number(value)));
        else if(['y','cy','y1','y2'].includes(name))result=finite(y(Number(value)));
        else if(name==='width')result=finite(!spec.cartesian && baseX===0 && Number(value)===spec.width ? width : bx(baseX+Number(value))-bx(baseX));
        else if(name==='height')result=finite(y(baseY+Number(value))-y(baseY));
        else if(name==='d')result=path(value,bx,y);
        else if(name==='points'){let i=0;result=value.replace(numeric,n=>finite(i++%2?y(Number(n)):bx(Number(n))));}
        else if(name==='transform')result=value.replace(/translate\(\s*([-+.\deE]+)(?:[, ]+([-+.\deE]+))?\s*\)/g,(_,a,b)=>`translate(${finite(bx(Number(a)))} ${finite(y(Number(b||0)))})`)
          .replace(/rotate\(\s*([-+.\deE]+)[, ]+([-+.\deE]+)[, ]+([-+.\deE]+)\s*\)/g,(_,a,b,c)=>`rotate(${a} ${finite(bx(Number(b)))} ${finite(y(Number(c)))})`);
        node.setAttribute(name,result);
      }
    }
    return {x,y};
  }
  window.ChartForgeXLayoutGeometry={snapshot,project};
})();
