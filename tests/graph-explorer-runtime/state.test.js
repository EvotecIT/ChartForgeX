const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const source = names => names.map(name => fs.readFileSync(path.join(assets, `graph-explorer.${name}.js`), 'utf8')).join('\n');
function element(role, id, parent = '') {
  const attributes = {'data-cfx-role':role,[role === 'graph-edge' ? 'data-edge-id' : role === 'graph-cluster' ? 'data-cluster-id' : 'data-node-id']:id,'data-node-parent':parent};
  const classes = new Set();
  return {attributes,getAttribute:name=>attributes[name]??null,setAttribute:(name,value)=>{attributes[name]=String(value);},querySelector:()=>null,classList:{contains:name=>classes.has(name),toggle:(name,selected)=>selected?classes.add(name):classes.delete(name),add:name=>classes.add(name),remove:name=>classes.delete(name)}};
}
const attr = (element, name) => element?.getAttribute(name) || '';
const items = (root, selector) => root.elements.filter(item => selector.split(',').some(part => {
  const role = part.match(/data-cfx-role="([^"]+)"/);
  return (!role || attr(item,'data-cfx-role') === role[1]) && (!part.includes('.cfx-graph-selected') || item.classList.contains('cfx-graph-selected'));
}));
function fixture() {
  const node = element('graph-node','shared'), peer = element('graph-node','db'), edge = element('graph-edge','shared');
  edge.attributes['data-source-node-id']='shared'; edge.attributes['data-target-node-id']='db';
  return {node,peer,edge,root:{...element('graph-root','fixture'),elements:[node,peer,edge],dataset:{},state:{nodes:[{id:'shared',x:10,y:20,el:node},{id:'db',x:40,y:20,el:peer}]}}};
}

test('snapshots preserve role-qualified selection and ignore selected decorative layers', () => {
  const {node,edge,root}=fixture(); node.classList.add('cfx-graph-selected');
  const decoration=element('graph-node-details',''); decoration.classList.add('cfx-graph-selected'); root.elements.push(decoration);
  const runtime=new Function('items','attr','viewport','graphFocusSnapshot','hasFeature','graphState','syncSelectedEdgeLabels','emit','syncSelectionTooltip',source(['10-layout','14-state-history'])+'\nreturn {captureGraphInteractionState,restoreGraphSelection,selectedItems};')(
    items,attr,()=>({x:0,y:0,scale:1}),()=>null,()=>false,root=>root.state,()=>{},()=>{},()=>{});
  assert.deepEqual(runtime.selectedItems(root).map(({role,id})=>({role,id})),[{role:'graph-node',id:'shared'}]);
  const snapshot=runtime.captureGraphInteractionState(root);
  node.classList.remove('cfx-graph-selected'); runtime.restoreGraphSelection(root,snapshot.selection);
  assert.equal(node.classList.contains('cfx-graph-selected'),true);
  assert.equal(edge.classList.contains('cfx-graph-selected'),false);
  runtime.restoreGraphSelection(root,['shared','db']);
  assert.equal(runtime.selectedItems(root).length,1);
  assert.equal(runtime.selectedItems(root)[0].id,'db');
});

test('successful host document updates clear stale undo snapshots and rejected updates retain history', () => {
  const {root}=fixture();
  const events=[];
  const apiSource=source(['40-api']);
  const patchSource=apiSource.slice(apiSource.indexOf('  const applyGraphRuntimePatch ='),apiSource.indexOf('  const graphExplorerApi ='));
  const ignored=['stopWorkerPhysics','stopMainPhysics','upsertGraphCluster','upsertGraphEdge','syncSvgThemeColors','performanceGate','applyLayout','syncGraphItemTabStops'];
  let observeHostUpdate=false;
  const environment={...Object.fromEntries(ignored.map(name=>[name,()=>{}])),graphApiRoot:target=>target,
    items,attr,setGraphAttribute:(item,name,value)=>{ if(value===undefined || value===null || value==='') delete item.attributes[name]; else item.setAttribute(name,value); },
    idList:value=>value.split(',').filter(Boolean),hasFeature:(_root,name)=>['IncrementalUpdates','History'].includes(name),
    graphState:root=>({...root.state,edges:[],clusters:[]}),
    upsertGraphNode:(_root,node)=>{ if(!root.elements.some(item=>attr(item,'data-node-id')===node.id)) root.elements.push(element('graph-node',node.id)); },
    applyFilters:()=>{ if(observeHostUpdate) assert.equal(runtime.traverseGraphHistory(root,'undo'),false); },
    emit:(_root,name,detail)=>{ events.push({name,detail}); if(observeHostUpdate && name==='cfxgraphpatch') assert.equal(runtime.traverseGraphHistory(root,'undo'),false); }};
  const runtime=new Function(...Object.keys(environment),source(['14-state-history','39-patch-validation'])+patchSource+'\nreturn {applyGraphRuntimePatch,traverseGraphHistory};')(...Object.values(environment));
  const update=patch=>runtime.applyGraphRuntimePatch(root,patch,{hostUpdate:true});
  root.__cfxGraphHistory={undo:[{state:{document:{nodes:[]}}}],redo:[{}],applying:false};
  assert.throws(()=>update({upsertNodes:[{id:'shared',parentId:'shared'}]}),/parent cycle/);
  assert.equal(root.__cfxGraphHistory.undo.length,1);
  update({fit:true}); assert.equal(root.__cfxGraphHistory.undo.length,1);
  events.length=0; observeHostUpdate=true;
  update({upsertNodes:[{id:'host-data'}]});
  assert.equal(root.__cfxGraphHistory.undo.length,0); assert.equal(root.__cfxGraphHistory.redo.length,0);
  assert.equal(events.find(event=>event.name==='cfxgraphhistory').detail.action,'host-update');
  assert.equal(root.elements.some(item=>attr(item,'data-node-id')==='host-data'),true);
});

test('export preserves inherited edge width and explicit widths', () => {
  const {edge,root}=fixture();
  const exportJson=new Function('items','attr','metadataDetail','viewport','graphFocusSnapshot','exportGraphPerformance','routePoints','idList','graphState','document',source(['30-bindings'])+'\nreturn exportGraphJson;')(
    items,attr,()=>({}),()=>({}),()=>null,()=>({}),()=>[],value=>value.split(',').filter(Boolean),root=>root.state,{readyState:'loading',addEventListener(){}});
  assert.equal(exportJson(root).edges[0].style.width,null);
  edge.setAttribute('data-edge-width',2.5); assert.equal(exportJson(root).edges[0].style.width,2.5);
});

test('node and cluster parent cycles and invalid edge widths are rejected before mutation', () => {
  const {root}=fixture();
  const validate=new Function('items','attr','idList',source(['39-patch-validation'])+'\nreturn validateGraphPatch;')(items,attr,value=>value.split(',').filter(Boolean));
  const before=JSON.stringify(root.elements.map(item=>item.attributes));
  for (const patch of [
    {upsertNodes:[{id:'shared',parentId:'db'},{id:'db',parentId:'shared'}]},
    {upsertNodes:[{id:'shared',parentId:'shared'}]},
    {upsertClusters:[{id:'a',parentClusterId:'b'},{id:'b',parentClusterId:'a'}]},
    {upsertClusters:[{id:'a',parentClusterId:'a'}]}
  ]) assert.throws(()=>validate(root,patch),/parent cycle/);
  for (const width of [0,-1,Infinity,'bad']) assert.throws(()=>validate(root,{upsertEdges:[{id:'shared',source:'shared',target:'db',style:{width}}]}),/width/);
  validate(root,{upsertNodes:[{id:'db',parentId:'shared'}],upsertEdges:[{id:'shared',source:'shared',target:'db',style:{width:null}}]});
  assert.equal(JSON.stringify(root.elements.map(item=>item.attributes)),before);
});

test('descendant traversal deduplicates malformed cycles rather than repeating them to the depth limit', () => {
  const visible=new Function(source(['13-hierarchy'])+'\nreturn hierarchyVisibleIds;')();
  const children=new Map([['a',['b']],['b',['a']]]);
  assert.deepEqual([...visible('a',10000,children)],['a','b']);
});
