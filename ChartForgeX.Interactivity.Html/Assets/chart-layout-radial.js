(function(){
  'use strict';
  if(window.ChartForgeXLayoutRadial)return;
  const ns='http://www.w3.org/2000/svg';
  const textOf=node=>node?.matches('text')?node:node?.querySelector('text');
  const number=(node,name)=>Number(node.getAttribute(name))||0;
  function unclipped(node,svg){for(let p=node.parentElement;p&&p!==svg;p=p.parentElement)p.removeAttribute('clip-path');}
  function fit(text,full,width,ellipsis=true){
    text.textContent=full;
    if(text.getComputedTextLength()<=width+.5)return full;
    let low=0,high=full.length;
    while(low<high){const mid=Math.ceil((low+high)/2);text.textContent=full.slice(0,mid).trimEnd()+(ellipsis?'…':'');if(text.getComputedTextLength()<=width+.5)low=mid;else high=mid-1;}
    return low?full.slice(0,low).trimEnd()+(ellipsis?'…':''):'';
  }
  function create(host,spec,svg){
    const r=spec.radial,gap=spec.spacing,[pl,pt,pr,pb]=r.padding,[sx,sy,sw,sh]=r.plot,sourceX=sx+sw/2,sourceY=sy+sh/2;
    let lastWidth=0;
    const headings=[...svg.querySelectorAll('[data-cfx-role=frame-heading-source]')].map((node,i)=>{
      unclipped(node,svg);return {node,template:textOf(node).cloneNode(true),data:r.headings[i]};
    });
    const legend=r.entries.map(data=>{
      const node=[...svg.querySelectorAll('[data-cfx-role=legend-entry],[data-cfx-role=legend-item]')].find(e=>e.dataset.cfxSourceId===data[0]);
      if(!node)throw new Error('Missing native radial legend entry.');
      unclipped(node,svg);
      const label=textOf(node.querySelector('[data-cfx-role=legend-label]')),value=textOf(node.querySelector('[data-cfx-role=legend-value]')),percentage=textOf(node.querySelector('[data-cfx-role=legend-percentage]'));
      return {node,data,label,value,percentage,x:number(label,'x')-18,baseline:number(label,'y')};
    });
    const groups=[...svg.querySelectorAll('[data-cfx-role=coordinate-system] > g[clip-path]')].map(node=>{
      const point=node.querySelector('[data-cfx-role=radial-point]'),center=node.querySelector('[data-cfx-role=donut-center]'),overlay=node.querySelector('[data-cfx-radial-callout=overlay]');
      const index=point?Number(point.dataset.cfxSeries):overlay?Number(overlay.dataset.cfxSeries):Number(center?.dataset.cfxSourceId?.match(/series-(\d+)/)?.[1]);
      const id=node.getAttribute('clip-path')?.match(/url\(#([^)]*)\)/)?.[1],clip=id&&svg.getElementById(id)?.querySelector('rect');
      const labels=[...node.querySelectorAll('[data-cfx-role=data-label]')].map(owner=>{
        const text=textOf(owner),source=owner.closest('[data-cfx-full-label]'),slice=r.slices.find(s=>s[0]===source?.dataset.cfxLabelFor);
        const previous=source?.previousElementSibling,connector=source?.querySelector('[data-cfx-role=data-label-connector]')||(previous?.matches('[data-cfx-role=data-label-connector]')?previous:null),halo=source?.querySelector('[data-cfx-role=data-label-connector-halo]');
        const leftOverlay=source?.dataset.cfxRadialCallout==='overlay'&&Number(source.dataset.cfxLabelLeft)+Number(source.dataset.cfxLabelWidth)<sourceX;
        return {owner,text,slice,series:Number(source?.dataset.cfxSeries??slice?.[0]?.match(/series-(\d+)/)?.[1]),connector,halo,path:connector?.getAttribute('d'),x:number(text,'x')+(leftOverlay?Number(source.dataset.cfxLabelWidth):0),y:number(text,'y'),anchor:leftOverlay?'end':text.getAttribute('text-anchor'),font:number(text,'font-size'),full:source?.dataset.cfxFullLabel||text.textContent};
      });
      for(const mark of node.querySelectorAll('[stroke]'))mark.setAttribute('vector-effect','non-scaling-stroke');
      const patterns=new Map();
      for(const mark of node.querySelectorAll('[fill]')){
        const id=mark.getAttribute('fill')?.match(/url\(#([^)]*)\)/)?.[1],pattern=id&&svg.getElementById(id);
        if(pattern?.localName!=='pattern')continue;
        if(!patterns.has(id)){const clone=pattern.cloneNode(true);clone.id=id+'-r'+index;pattern.parentElement.appendChild(clone);patterns.set(id,{node:clone,transform:clone.getAttribute('patternTransform')||''});}
        mark.setAttribute('fill','url(#'+patterns.get(id).node.id+')');
      }
      return {node,index,clip,labels,center,patterns,overlay:!!overlay};
    }).filter(group=>Number.isInteger(group.index));
    function layout(requestedWidth,printing=false){
      const available=requestedWidth||host.clientWidth;
      if(!host.isConnected||!available||!host.getClientRects().length)return;
      const width=printing?available:Math.min(spec.maximum,Math.max(spec.minimum,available));
      let top=pt;
      for(let h=0;h<headings.length;h++){
        const {node,template,data}=headings[h];if(!data)continue;
        node.replaceChildren();let remaining=data[0];
        if(h)top+=gap/6;
        for(let line=0;remaining&&line<2;line++){
          const text=template.cloneNode(true);node.appendChild(text);
          let caption=fit(text,remaining,width-pl-pr,line===1);
          if(line===0&&caption.length<remaining.length){const space=caption.lastIndexOf(' ');if(space>0)caption=caption.slice(0,space);}
          text.textContent=caption;text.setAttribute('data-cfx-role',line?'frame-heading-continuation':'frame-heading');
          const anchor=text.getAttribute('text-anchor');text.setAttribute('x',anchor==='middle'?(pl+width-pr)/2:anchor==='end'?width-pr:pl);
          text.setAttribute('y',top+data[2]);top+=data[1];
          remaining=caption.endsWith('…')?'':remaining.slice(caption.length).trimStart();
        }
      }
      if(headings.length)top+=gap*5/6;
      const position=spec.legendPosition,side=position==='Left'||position==='Right',above=position.startsWith('Top');
      const contentWidth=Math.max(1,width-pl-pr),legendWidth=side?Math.min(contentWidth*.32,180):contentWidth;
      const rows=[];let row=[],used=0;
      for(const entry of legend){
        const [,full,labelWidth,,valueWidth,,percentWidth]=entry.data;
        entry.width=Math.min(legendWidth,28+labelWidth+(valueWidth?gap+valueWidth+(percentWidth?percentWidth+gap*2/3:0):0));
        if(row.length&&(side||used+gap+entry.width>legendWidth)){rows.push(row);row=[];used=0;}
        row.push(entry);used+=(used?gap:0)+entry.width;
        entry.label.textContent=fit(entry.label,full,Math.max(0,entry.width-22-(valueWidth?gap+valueWidth+(percentWidth?percentWidth+gap*2/3:0):0)));
        let title=entry.node.querySelector(':scope > title');
        if(entry.label.textContent!==full){if(!title){title=document.createElementNS(ns,'title');entry.node.prepend(title);}title.textContent=full;}
        if(entry.value)entry.value.textContent=entry.data[3];if(entry.percentage)entry.percentage.textContent=entry.data[5];
      }
      if(row.length)rows.push(row);
      const legendHeight=rows.length*r.line,legendBand=legendHeight?legendHeight+gap:0;
      const left=pl+(side&&position==='Left'?legendWidth+gap:0),plotWidth=Math.max(1,contentWidth-(side?legendWidth+gap:0));
      const plotTop=top+(above?legendBand:0),bottomBand=!side&&!above?legendBand:0;
      const minimumPlot=Math.min(plotWidth,2*(r.labelSize*3+gap));
      let plotHeight=Math.max(minimumPlot,r.height-pb-plotTop-bottomBand,side?legendHeight:0);
      const radii=r.frames.map(([horizontal,vertical,extent])=>Math.max(0,(Math.min(plotWidth*horizontal,plotHeight*vertical)/2-gap)/extent));
      if(!printing)plotHeight=Math.max(side?legendHeight:0,...r.frames.map((frame,i)=>2*(radii[i]*frame[2]+gap)/frame[1]));
      const height=plotTop+plotHeight+bottomBand+pb,cx=left+plotWidth/2,cy=plotTop+plotHeight/2;
      let legendTop=side||above?top:plotTop+plotHeight+gap;
      for(const entries of rows){
        const occupied=entries.reduce((sum,e)=>sum+e.width,0)+Math.max(0,entries.length-1)*gap;
        let x=position.endsWith('Right')?width-pr-occupied:['Top','Bottom'].includes(position)?pl+(contentWidth-occupied)/2:pl;
        if(side)x=position==='Right'?width-pr-legendWidth:pl;
        for(const entry of entries){
          const {node,label,value,percentage,data}=entry;
          node.setAttribute('transform',`translate(${x-entry.x} ${legendTop+r.ascent-entry.baseline})`);
          label.setAttribute('x',entry.x+18);label.setAttribute('text-anchor','start');
          let end=entry.x+entry.width-4;
          if(percentage){percentage.setAttribute('x',end);percentage.setAttribute('text-anchor','end');end-=data[6]+gap*2/3;}
          if(value){value.setAttribute('x',end);value.setAttribute('text-anchor','end');}
          for(const hit of node.querySelectorAll('[data-cfx-browser-hit-area]')){hit.setAttribute('x',entry.x);hit.setAttribute('y',entry.baseline-r.ascent);hit.setAttribute('width',entry.width);hit.setAttribute('height',r.line);}
          x+=entry.width+gap;
        }
        legendTop+=r.line;
      }
      for(const group of groups){
        const frame=r.frames[group.index],radius=radii[group.index],scale=radius/frame[3];if(!Number.isFinite(scale)||scale<=0)continue;
        group.node.setAttribute('transform',`translate(${cx} ${cy}) scale(${scale}) translate(${-sourceX} ${-sourceY})`);
        if(group.clip){group.clip.setAttribute('x',sourceX+(left-cx)/scale);group.clip.setAttribute('y',sourceY+(plotTop-cy)/scale);group.clip.setAttribute('width',plotWidth/scale);group.clip.setAttribute('height',plotHeight/scale);}
        for(const pattern of group.patterns.values())pattern.node.setAttribute('patternTransform',pattern.transform+` scale(${1/scale})`);
        if(group.overlay)for(const series of new Set(group.labels.map(label=>label.series)))
          layoutLabels({...group,labels:group.labels.filter(label=>label.series===series)},r.frames[series],scale,left,plotTop,plotWidth,plotHeight,cx,cy);
        else layoutLabels(group,frame,scale,left,plotTop,plotWidth,plotHeight,cx,cy);
        if(group.center){
          const value=textOf(group.center.querySelector('[data-cfx-role=donut-total-label]')),caption=textOf(group.center.querySelector('[data-cfx-role=donut-title]'));
          const inner=radius*frame[4];
          value.setAttribute('font-size',Math.min(r.centerSize,inner*.44)/scale);caption.setAttribute('font-size',Math.min(r.labelSize,inner*.25)/scale);
          if(frame[9]){
            const maximum=Math.min(r.centerSize,inner*.44),minimum=Math.min(10,maximum),room=inner*1.6/scale;
            value.textContent=frame[9][0];let low=minimum,high=maximum;
            for(let i=0;i<16;i++){const size=(low+high)/2;value.setAttribute('font-size',size/scale);if(value.getComputedTextLength()<=room)low=size;else high=size;}
            value.setAttribute('font-size',low/scale);
            if(value.getComputedTextLength()>room)value.textContent=frame[9][1];
            if(value.getComputedTextLength()>room)value.textContent='';
          }else value.textContent=fit(value,group.center.dataset.cfxCenterValue,inner*1.6/scale);
          caption.textContent=fit(caption,group.center.dataset.cfxCenterCaption,inner*1.6/scale);
          for(const text of [value,caption]){text.setAttribute('x',sourceX);text.setAttribute('y',0);text.setAttribute('text-anchor','middle');}
          const vb=value.getBBox(),cb=caption.getBBox(),space=Math.min(gap,inner*.1)/scale,begin=sourceY-(vb.height+space+cb.height)/2;
          value.setAttribute('y',begin-vb.y);caption.setAttribute('y',begin+vb.height+space-cb.y);
        }
      }
      for(const owner of svg.querySelectorAll('[data-cfx-role=no-data]')){const text=textOf(owner);text.setAttribute('x',cx);text.setAttribute('y',cy);}
      for(const surface of svg.querySelectorAll('[data-cfx-role=background],[data-cfx-role=frame-card],[data-cfx-role=frame-surface],[data-cfx-role=content-surface]')){
        if(surface.tagName.toLowerCase()!=='rect')continue;
        if(surface.dataset.cfxRole==='content-surface'){surface.setAttribute('x',left);surface.setAttribute('y',plotTop);surface.setAttribute('width',plotWidth);surface.setAttribute('height',plotHeight);}
        else {surface.setAttribute('width',Math.max(0,width-2*number(surface,'x')));surface.setAttribute('height',Math.max(0,height-2*number(surface,'y')));}
      }
      svg.setAttribute('width',width);svg.setAttribute('height',height);svg.setAttribute('viewBox',`0 0 ${width} ${height}`);svg.style.cssText=`display:block;width:${width}px;height:${height}px;max-width:none`;
      host.dataset.cfxLayoutWidth=String(width);host.dataset.cfxLayoutVersion='spec-4';lastWidth=width;
    }
    function layoutLabels(group,frame,scale,left,top,width,height,cx,cy){
      const placement=frame[6],side=['Outside','Left','Right'].includes(placement),vertical=['Above','Below'].includes(placement);
      const toSourceX=x=>sourceX+(x-cx)/scale,toSourceY=y=>sourceY+(y-cy)/scale;
      const baseline=(label,y)=>y+(r.labelAscent-r.labelLine/2)*(label.font/r.labelSize)/scale;
      for(const label of group.labels){
        label.owner.style.display='';if(label.connector)label.connector.style.display='';if(label.halo)label.halo.style.display='';
        label.text.setAttribute('font-size',label.font/scale);label.text.setAttribute('text-anchor',label.anchor||'start');label.text.setAttribute('x',label.x);label.text.setAttribute('y',label.y);label.text.textContent=label.full;
      }
      const hide=label=>{label.owner.style.display='none';if(label.connector)label.connector.style.display='none';if(label.halo)label.halo.style.display='none';};
      const caption=(label,room)=>{label.text.textContent=fit(label.text,label.full,Math.max(0,room)/scale);if(!label.text.textContent)hide(label);};
      const route=(label,x,y)=>{
        if(!label.connector||!label.path)return;
        const tokens=label.path.match(/[A-Za-z]|[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:e[-+]?\d+)?/gi),last=tokens.length-1,sign=label.anchor==='end'?-1:1;
        const endX=x-sign*4/scale,dx=endX-Number(tokens[last-1]);
        tokens[last-1]=endX;tokens[last]=y;
        if(r.connector==='Curve'){tokens[last-3]=Number(tokens[last-3])+dx;tokens[last-2]=y;}
        else if(r.connector==='Elbow'&&tokens.length>=12){tokens[last-4]=endX-sign*8/scale;tokens[last-3]=y;}
        label.connector.setAttribute('d',tokens.join(' '));
        if(label.halo)label.halo.setAttribute('d',tokens.join(' '));
      };
      if(side){
        for(const anchor of ['end','start']){
          let lane=group.labels.filter(l=>l.anchor===anchor).sort((a,b)=>a.y-b.y);
          if(!lane.length)continue;
          const step=Math.max(...lane.map(l=>r.labelLine*l.font/r.labelSize))+gap,capacity=Math.floor(height/step);
          const kept=new Set([...lane].sort((a,b)=>(b.slice?.[6]||0)-(a.slice?.[6]||0)).slice(0,capacity));
          lane.forEach(l=>{if(!kept.has(l))hide(l);});lane=lane.filter(l=>kept.has(l));
          let previous=top-step/2;
          for(const label of lane){
            const originalY=label.path?Number(label.path.match(/[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:e[-+]?\d+)?/gi).at(-1)):label.y;
            label.laneY=Math.max(previous+step,Math.min(top+height-step/2,cy+(originalY-sourceY)*scale));previous=label.laneY;
          }
          const overflow=lane.length?Math.max(0,lane.at(-1).laneY-(top+height-step/2)):0;
          for(const label of lane){
            const x=cx+(label.x-sourceX)*scale,y=label.laneY-overflow,room=anchor==='end'?x-left-4:left+width-x-4;
            caption(label,room);label.text.setAttribute('y',baseline(label,toSourceY(y)));route(label,label.x,toSourceY(y));
          }
        }
      }else if(vertical){
        const extent=frame[3]*scale*frame[2],above=placement==='Above',begin=above?top:cy+extent+gap,end=above?cy-extent-gap:top+height;
        const ordered=[...group.labels].sort((a,b)=>a.x-b.x),slot=width/Math.max(1,ordered.length);
        for(let i=0;i<ordered.length;i++){
          const label=ordered[i];if(end-begin<r.labelLine*label.font/r.labelSize){hide(label);continue;}
          caption(label,slot-gap);const x=toSourceX(left+slot*(i+.5)),y=toSourceY((begin+end)/2);
          label.text.setAttribute('x',x);label.text.setAttribute('y',baseline(label,y));route(label,x,y);
        }
      }else{
        for(const label of group.labels){
          const slice=label.slice;if(!slice)continue;
          const [,px,py,outer,inner,start,sweep]=slice,mid=start+sweep/2,distance=frame[7]!=null?outer*frame[7]:inner>0?(inner+outer)/2:outer*.66;
          const x=px+Math.cos(mid)*distance,y=py+Math.sin(mid)*distance,targetX=cx+(x-sourceX)*scale;
          const maximum=Math.min(outer*.9*scale,Math.max(0,Math.min(targetX-left,left+width-targetX)*2));
          caption(label,maximum);label.text.setAttribute('x',x);label.text.setAttribute('y',baseline(label,y));
          if(label.text.getComputedTextLength()>2*distance*Math.sin(Math.min(Math.PI/2,sweep/2))+.5){hide(label);continue;}
          const bounded=frame[8]||frame[7]!=null||outer<frame[3]*frame[5]-.01;
          if(bounded){const b=label.text.getBBox();if([[b.x,b.y],[b.x+b.width,b.y],[b.x,b.y+b.height],[b.x+b.width,b.y+b.height]].some(([x,y])=>{
            const distance=Math.hypot(x-px,y-py),angle=(Math.atan2(y-py,x-px)-start+Math.PI*4)%(Math.PI*2);
            return distance<inner||distance>outer||angle>sweep+1e-6;
          }))hide(label);}
        }
      }
    }
    return {layout,dispose(){},get width(){return lastWidth;},svg};
  }
  window.ChartForgeXLayoutRadial={create};
})();
