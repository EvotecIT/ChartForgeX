const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { graphRuntimeSource, graphMessageHelpers } = require('../helpers/graph-runtime-source');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const source = names => names.map(name => fs.readFileSync(path.join(assets, `graph-explorer.${name}.js`), 'utf8')).join('\n');
function element(role, id) {
  const attributes = {'data-cfx-role':role,[role === 'graph-edge' ? 'data-edge-id' : 'data-node-id']:id};
  const classes = new Set();
  return {getAttribute:name=>attributes[name]??null,setAttribute:(name,value)=>{attributes[name]=String(value);},removeAttribute:name=>{delete attributes[name];},classList:{contains:name=>classes.has(name),toggle:(name,selected)=>selected?classes.add(name):classes.delete(name),add:name=>classes.add(name),remove:name=>classes.delete(name)}};
}
const attr = (element, name) => element?.getAttribute(name) || '';
const items = (root, selector) => root.elements.filter(item => selector.split(',').some(part => {
  const role = part.match(/data-cfx-role="([^"]+)"/);
  return (!role || attr(item,'data-cfx-role') === role[1]) && (!part.includes('.cfx-graph-selected') || item.classList.contains('cfx-graph-selected'));
}));
function fixture() {
  const visible = new Function('document', 'window', graphRuntimeSource('return visible;'))(
    { readyState: 'loading', addEventListener() {} }, {});
  const node = element('graph-node','shared'), peer = element('graph-node','peer'), edge = element('graph-edge','shared');
  edge.setAttribute('data-source-node-id','shared'); edge.setAttribute('data-target-node-id','peer');
  const events=[],announcer={textContent:''};
  const root={getAttribute:name=>name==='data-cfx-graph-id'?'fixture':null,elements:[node,peer,edge],dataset:{},querySelector:selector=>selector.includes('announcer')?announcer:null,state:{nodes:[],edges:[],clusters:[]}};
  const runtime=new Function('uiText','items','attr','visible','graphKeyboardItemKey','hasFeature','graphState','drawCanvas','syncSelectedEdgeLabels','emit', source(['10-layout','36-host-commands'])+'\nreturn {selectGraphItems,highlightGraphItems,graphHover,selectedItems};')(
    graphMessageHelpers().uiText,items,attr,visible,item=>`${attr(item,'data-cfx-role')}:${attr(item,'data-node-id') || attr(item,'data-edge-id')}`,()=>true,root=>root.state,()=>{},()=>{},(_root,name,detail)=>events.push({name,detail}));
  return {node,peer,edge,root,runtime,events,announcer};
}
test('silent host selection is role-qualified and supports replacement, addition and removal',()=>{
  const {node,peer,edge,root,runtime,events}=fixture();
  runtime.selectGraphItems(root,[{role:'graph-node',id:'shared'}]);
  assert.equal(node.classList.contains('cfx-graph-selected'),true);
  assert.equal(edge.classList.contains('cfx-graph-selected'),false);
  runtime.selectGraphItems(root,[{role:'graph-edge',id:'shared'}],{mode:'add'});
  assert.equal(runtime.selectedItems(root).length,2);
  runtime.selectGraphItems(root,[{role:'graph-node',id:'shared'}],{mode:'remove'});
  assert.deepEqual(runtime.selectedItems(root).map(item=>item.role),['graph-edge']);
  runtime.selectGraphItems(root,[{role:'graph-node',id:'peer'}]);
  assert.equal(peer.classList.contains('cfx-graph-selected'),true);
  assert.equal(edge.classList.contains('cfx-graph-selected'),false);
  assert.equal(events.length,0);
  assert.equal(runtime.selectGraphItems(root,[],{mode:'unknown'}),false);
  assert.equal(runtime.selectedItems(root).length,1);
});
test('independent highlight and dim state survive selection clearing and clear without echo',()=>{
  const {node,peer,root,runtime,events,announcer}=fixture();
  runtime.selectGraphItems(root,[{role:'graph-node',id:'shared'}]);
  runtime.highlightGraphItems(root,[{role:'graph-node',id:'peer'}],{dim:true});
  assert.equal(peer.getAttribute('data-cfx-highlighted'),'true');
  assert.equal(node.classList.contains('cfx-graph-link-dim'),true);
  assert.equal(node.classList.contains('cfx-graph-selected'),true);
  assert.equal(announcer.textContent,'1 items highlighted.');
  runtime.selectGraphItems(root,[]);
  assert.equal(peer.getAttribute('data-cfx-highlighted'),'true');
  runtime.highlightGraphItems(root,[]);
  assert.equal(peer.getAttribute('data-cfx-highlighted'),null);
  assert.equal(node.classList.contains('cfx-graph-link-dim'),false);
  assert.equal(events.length,0);
});
test('explicit host emission reports one complete set with edge endpoints',()=>{
  const {root,runtime,events}=fixture();
  runtime.selectGraphItems(root,[{role:'graph-node',id:'peer'},{role:'graph-edge',id:'shared'}],{emit:true});
  assert.equal(events.length,1); assert.equal(events[0].name,'cfxgraphselection');
  assert.equal(events[0].detail.count,2);
  const edge=events[0].detail.items.find(item=>item.role==='graph-edge');
  assert.equal(edge.sourceNodeId,'shared'); assert.equal(edge.targetNodeId,'peer');
});
test('hover publishes each identity transition once and clearing retains the last identity',()=>{
  const {node,root,runtime,events}=fixture();
  runtime.graphHover(root,node); runtime.graphHover(root,node); runtime.graphHover(root,null); runtime.graphHover(root,null);
  assert.deepEqual(events.map(item=>item.name),['cfxgraphhover','cfxgraphhoverclear']);
  assert.equal(events[1].detail.id,'shared'); assert.equal(events[1].detail.role,'graph-node');
});
