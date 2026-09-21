const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const source = ['10-neighborhood-plan', '10-neighborhood', '14-state-history'].map(name => fs.readFileSync(path.join(assets, `graph-explorer.${name}.js`), 'utf8')).join('\n');
const runtime = new Function(`
  const attr = (element, name) => element.attributes[name] || '';
  const num = (element, name, fallback) => name in element.attributes ? Number(element.attributes[name]) : fallback;
  const hasFeature = (root, name) => !root.disabledFeatures?.has(name), graphState = root => root.state;
  const items = (root, selector) => root.elements.filter(item => selector.split(',').some(part => {
    if (part[0] === '.') return item.classList.contains(part.slice(1));
    const role = part.match(/data-cfx-role="([^"]+)"/);
    return role ? item.attributes['data-cfx-role'] === role[1] : false;
  }));
  const viewport = root => root.viewport, setViewport = (root, value) => { root.viewport = { ...value }; };
  const selectedItems = root => root.selection.map(value => typeof value === 'string' ? {id:value, role:'graph-node'} : value);
  const updateSelectionState = root => { root.selection = root.elements.filter(item => item.classList.contains('cfx-graph-selected')).map(item => ({id:attr(item,'data-node-id')||attr(item,'data-edge-id')||attr(item,'data-cluster-id'), role:attr(item,'data-cfx-role')})); return root.selection; };
  const clearHiddenSelections = root => { root.selection = selectedItems(root).filter(value => { const item=root.elements.find(element => (attr(element,'data-node-id')||attr(element,'data-edge-id')||attr(element,'data-cluster-id'))===value.id && attr(element,'data-cfx-role')===value.role); return item && !item.classList.contains('cfx-graph-neighborhood-hidden'); }); return true; };
  const syncSelectionTooltip = () => {}, syncGraphItemTabStops = () => {}, syncNodeDetailLayers = () => {}, drawCanvas = root => { root.paintedSelection = JSON.parse(JSON.stringify(root.selection)); };
  const applyLod = root => { root.lodChanges = (root.lodChanges || 0) + 1; };
  const pausePhysics = root => { root.paused = true; }, fitViewport = root => { root.viewport = {x:10,y:20,scale:2}; };
  const emit = (root, name, detail) => root.events.push({name, detail});
  ${source}
  return { applyNeighborhoodFocus, clearNeighborhoodFocus, pageGraphNeighborhood, backGraphNeighborhood, graphFocusSnapshot };
`)();
function element(attributes = {}) {
  const classes = new Set();
  return { attributes, setAttribute(name,value){this.attributes[name]=String(value);}, getAttribute(name){return this.attributes[name]||null;}, classList: { add: (...names) => names.forEach(name => classes.add(name)), remove: (...names) => names.forEach(name => classes.delete(name)), contains: name => classes.has(name), toggle(name, enabled) { if (enabled) classes.add(name); else classes.delete(name); } } };
}
function hub() {
  const nodes = Array.from({length:26}, (_, i) => { const id=`n${String(i).padStart(2,'0')}`; return {id, x:i*20, y:i*10, fixed:i===0, el:element({'data-cfx-role':'graph-node','data-node-id':id})}; });
  const edges = nodes.slice(1).map(node => ({id:`e${node.id}`, source:nodes[0], target:node, el:element({'data-cfx-role':'graph-edge','data-edge-id':`e${node.id}`})}));
  return { ...element(), dataset:{}, state:{nodes,edges,clusters:[],byId:new Map(nodes.map(node=>[node.id,node]))},
    elements:[...nodes.map(node=>node.el),...edges.map(edge=>edge.el)], querySelector:()=>null,
    selection:[{id:'n00',role:'graph-node'}], viewport:{x:27,y:63,scale:.8}, events:[] };
}
test('paging and drill-down preserve geometry and restore overview viewport and selection', () => {
  const root=hub(), geometry=JSON.stringify(root.state.nodes.map(({id,x,y,fixed})=>({id,x,y,fixed})));
  root.__cfxGraphViewportTouched = false;
  assert.equal(runtime.applyNeighborhoodFocus(root,'n00'),true);
  assert.equal(root.__cfxGraphViewportTouched,true);
  assert.equal(root.state.nodes.filter(node=>!node.el.classList.contains('cfx-graph-neighborhood-hidden')).length,13);
  assert.equal(runtime.pageGraphNeighborhood(root,-1),false);
  runtime.pageGraphNeighborhood(root,1);
  assert.equal(root.__cfxGraphNeighborhood.neighborOffset,12);
  runtime.applyNeighborhoodFocus(root,'n13');
  assert.equal(root.__cfxGraphNeighborhoodView.nodeIds.size,2);
  runtime.backGraphNeighborhood(root);
  assert.equal(root.dataset.cfxGraphFocusNode,'n00'); assert.equal(root.__cfxGraphNeighborhood.neighborOffset,12);
  runtime.pageGraphNeighborhood(root,1);
  assert.equal(root.__cfxGraphNeighborhoodView.nodeIds.size,2); assert.equal(runtime.pageGraphNeighborhood(root,1),false);
  runtime.clearNeighborhoodFocus(root);
  assert.deepEqual(root.viewport,{x:27,y:63,scale:.8}); assert.deepEqual(root.selection,[{id:'n00',role:'graph-node'}]);
  assert.equal(root.__cfxGraphViewportTouched,false);
  assert.equal(root.elements.some(item=>item.classList.contains('cfx-graph-neighborhood-hidden')),false);
  assert.equal(JSON.stringify(root.state.nodes.map(({id,x,y,fixed})=>({id,x,y,fixed}))),geometry);
});
test('missing roots are rejected without changing view and snapshots detach navigation state', () => {
  const root=hub(); assert.equal(runtime.applyNeighborhoodFocus(root,'absent'),false); assert.equal(root.events.length,0);
  runtime.applyNeighborhoodFocus(root,'n00'); runtime.applyNeighborhoodFocus(root,'n01');
  const snapshot=runtime.graphFocusSnapshot(root);
  snapshot.overview.viewport.x=999; snapshot.history.length=0;
  assert.equal(root.__cfxGraphNeighborhoodOverview.viewport.x,27); assert.equal(root.__cfxGraphNeighborhoodHistory.length,1);
});

test('filter refresh keeps the root and resets a page that no longer has eligible neighbors', () => {
  const root=hub(); runtime.applyNeighborhoodFocus(root,'n00'); runtime.pageGraphNeighborhood(root,1);
  for (const node of root.state.nodes) if (!['n00','n01'].includes(node.id)) node.el.classList.add('cfx-graph-hidden');
  runtime.applyNeighborhoodFocus(root,'n00',root.__cfxGraphNeighborhood,{refresh:true,fit:false});
  assert.equal(root.__cfxGraphNeighborhood.neighborOffset,0);
  assert.deepEqual([...root.__cfxGraphNeighborhoodView.nodeIds],['n00','n01']);
});
test('bounded focus labels survive compact rendering but omitted nodes draw no labels', () => {
  const details = fs.readFileSync(path.join(assets,'graph-explorer.03-canvas-details.js'),'utf8');
  const draw = new Function(`
    const visible = element => !element.classList.contains('cfx-graph-neighborhood-hidden');
    const attr = (element,name) => element.attributes[name] || '';
    const graphThemePalette = () => ({}), graphReadableNodeColors = () => ({halo:'white',label:'black'});
    ${details}\nreturn drawCanvasNodeDetails;
  `)();
  const text=[], context={save(){},restore(){},strokeText(){},fillText(label){text.push(label);}};
  const node={id:'a',label:'Neighbor',x:20,y:20,size:10,shape:'circle',el:element()};
  const root=element();
  draw(context,root,node,true,false); assert.deepEqual(text,[]);
  node.el.classList.add('cfx-graph-neighborhood-related');
  draw(context,root,node,true,false); assert.deepEqual(text,['Neighbor']);
  node.el.classList.add('cfx-graph-neighborhood-hidden');
  draw(context,root,node,true,false); assert.deepEqual(text,['Neighbor']);
});

test('restored focus pauses active physics even when navigation history is retained', () => {
  const root = hub();
  root.paused = false;
  runtime.applyNeighborhoodFocus(root, 'n00', {}, {refresh:true, fit:false});
  assert.equal(root.paused, true);
});
test('Back paints restored selection after leaving a drilled neighbor', () => {
  const root = hub();
  runtime.applyNeighborhoodFocus(root, 'n00');
  root.selection = [{id:'n01',role:'graph-node'}];
  runtime.applyNeighborhoodFocus(root, 'n01');
  runtime.backGraphNeighborhood(root);
  assert.deepEqual(root.selection, [{id:'n00',role:'graph-node'}]);
  assert.deepEqual(root.paintedSelection, [{id:'n00',role:'graph-node'}]);
});

test('bounded views re-evaluate rendering LOD and remove hidden selections', () => {
  const root = hub();
  root.selection.push({id:'n20',role:'graph-node'});
  runtime.applyNeighborhoodFocus(root, 'n00');
  assert.equal(root.lodChanges, 1);
  assert.deepEqual(root.selection, [{id:'n00',role:'graph-node'}]);
  runtime.clearNeighborhoodFocus(root);
  assert.equal(root.lodChanges, 2);
});

test('Back does not create selection when Selection is disabled', () => {
  const root = hub();
  root.disabledFeatures = new Set(['Selection']);
  root.selection = [];
  runtime.applyNeighborhoodFocus(root, 'n00');
  runtime.applyNeighborhoodFocus(root, 'n01');
  runtime.backGraphNeighborhood(root);
  assert.deepEqual(root.selection, []);
});

test('overview restores the selected role when node and edge IDs match', () => {
  const root = hub();
  const duplicate = root.state.edges[0];
  duplicate.id = 'n00';
  duplicate.el.attributes['data-edge-id'] = 'n00';
  runtime.applyNeighborhoodFocus(root, 'n00');
  runtime.clearNeighborhoodFocus(root);
  assert.deepEqual(root.selection, [{id:'n00',role:'graph-node'}]);
  assert.equal(duplicate.el.classList.contains('cfx-graph-selected'), false);
});
