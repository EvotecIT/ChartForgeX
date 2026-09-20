const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const source = ['10-neighborhood-plan', '10-neighborhood'].map(name => fs.readFileSync(path.join(assets, `graph-explorer.${name}.js`), 'utf8')).join('\n');
const runtime = new Function(`
  const attr = (element, name) => element.attributes[name] || '';
  const num = (element, name, fallback) => name in element.attributes ? Number(element.attributes[name]) : fallback;
  const hasFeature = () => true, graphState = root => root.state;
  const items = (root, selector) => root.elements.filter(item => selector.split(',').some(part => part[0] === '.' ? item.classList.contains(part.slice(1)) : false));
  const viewport = root => root.viewport, setViewport = (root, value) => { root.viewport = { ...value }; };
  const selectedItems = root => root.selection.map(id => ({id})), restoreGraphSelection = (root, ids) => { root.selection = [...ids]; };
  const clearHiddenSelections = () => {}, syncGraphItemTabStops = () => {}, syncNodeDetailLayers = () => {}, drawCanvas = (root, state) => { root.paintedState = state; root.paintedSelection = [...root.selection]; };
  const pausePhysics = root => { root.paused = true; }, fitViewport = root => { root.viewport = {x:10,y:20,scale:2}; };
  const emit = (root, name, detail) => root.events.push({name, detail});
  ${source}
  return { applyNeighborhoodFocus, clearNeighborhoodFocus, pageGraphNeighborhood, backGraphNeighborhood, graphFocusSnapshot };
`)();
function element(attributes = {}) {
  const classes = new Set();
  return { attributes, classList: { add: (...names) => names.forEach(name => classes.add(name)), remove: (...names) => names.forEach(name => classes.delete(name)), contains: name => classes.has(name), toggle(name, enabled) { if (enabled) classes.add(name); else classes.delete(name); } } };
}
function hub() {
  const nodes = Array.from({length:26}, (_, i) => ({id:`n${String(i).padStart(2,'0')}`, x:i*20, y:i*10, fixed:i===0, el:element()}));
  const edges = nodes.slice(1).map(node => ({id:`e${node.id}`, source:nodes[0], target:node, el:element({'data-edge-id':`e${node.id}`})}));
  return { ...element(), dataset:{}, state:{nodes,edges,clusters:[],byId:new Map(nodes.map(node=>[node.id,node]))},
    elements:[...nodes.map(node=>node.el),...edges.map(edge=>edge.el)], querySelector:()=>null,
    selection:['n00'], viewport:{x:27,y:63,scale:.8}, events:[] };
}
test('paging and drill-down preserve geometry and restore overview viewport and selection', () => {
  const root=hub(), geometry=JSON.stringify(root.state.nodes.map(({id,x,y,fixed})=>({id,x,y,fixed})));
  assert.equal(runtime.applyNeighborhoodFocus(root,'n00'),true);
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
  assert.deepEqual(root.viewport,{x:27,y:63,scale:.8}); assert.deepEqual(root.selection,['n00']);
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
  root.selection = ['n01'];
  runtime.applyNeighborhoodFocus(root, 'n01');
  runtime.backGraphNeighborhood(root);
  assert.deepEqual(root.selection, ['n00']);
  assert.deepEqual(root.paintedSelection, ['n00']);
});

test('neighborhood presentation preserves live accelerated coordinates across navigation', () => {
  const root = hub();
  root.__cfxGraphState = {...root.state, nodes:root.state.nodes.map(node=>({...node,x:node.x+500}))};
  runtime.applyNeighborhoodFocus(root,'n00');
  assert.equal(root.paintedState,root.__cfxGraphState);
  runtime.applyNeighborhoodFocus(root,'n01');
  runtime.backGraphNeighborhood(root);
  assert.equal(root.paintedState,root.__cfxGraphState);
  runtime.clearNeighborhoodFocus(root);
  assert.equal(root.paintedState,root.__cfxGraphState);
});
