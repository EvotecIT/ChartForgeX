const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/graph-explorer.10-neighborhood-plan.js'), 'utf8');
const plan = new Function(source + '\nreturn planGraphNeighborhood;')();

test('10,000-node hub has bounded pages, ordinal order, retained root, and exact omissions', () => {
  const nodes = Array.from({length: 10000}, (_, i) => ({id: String(i).padStart(5, '0')}));
  const edges = nodes.slice(1).map(node => ({id: node.id, sourceId: '00000', targetId: node.id}));
  const first = plan(nodes, edges, '00000');
  const second = plan(nodes, edges, '00000', {neighborOffset: 12});
  assert.equal(first.nodeIds.size, 13); assert.equal(first.edgeIds.size, 12);
  assert.deepEqual([...first.nodeIds].slice(1), nodes.slice(1, 13).map(node => node.id));
  assert.deepEqual([...second.nodeIds].slice(1), nodes.slice(13, 25).map(node => node.id));
  assert.equal(first.hiddenNodeCount, 9987); assert.equal(first.boundaryEdgeCount, 9987);
  assert.equal(first.scopeNodeCount, 10000); assert.equal(first.hasNext, true);
  assert.equal(second.hasPrevious, true);
});
test('hidden intermediate objects are traversed without consuming visible budgets', () => {
  const nodes = [{id:'root'}, {id:'hidden', visible:false}, {id:'leaf'}];
  const edges = [{id:'hidden', sourceId:'root', targetId:'hidden', visible:false}, {id:'bridge', sourceId:'hidden', targetId:'leaf'}];
  const view = plan(nodes, edges, 'root', {hops:2, maximumNodes:2});
  assert.deepEqual([...view.nodeIds], ['root', 'leaf']); assert.equal(view.scopeNodeCount, 2);
  assert.equal(view.edgeIds.size, 0); assert.equal(view.boundaryEdgeCount, 2);
  assert.equal(plan(nodes, edges, 'hidden'), null);
});
test('edge budgets prefer nearest relationships and reject invalid numeric options', () => {
  const nodes = ['r', 'a', 'b'].map(id => ({id}));
  const edges = [{id:'z',sourceId:'r',targetId:'a'}, {id:'a',sourceId:'a',targetId:'b'}, {id:'x',sourceId:'r',targetId:'b'}];
  assert.deepEqual([...plan(nodes, edges, 'r', {maximumEdges:1}).edgeIds], ['x']);
  assert.equal(plan(nodes, edges, 'r', {maximumEdges:0}).edgeIds.size, 0);
  for (const options of [{hops:NaN}, {maximumNodes:1}, {neighborOffset:-1}, {maximumEdges:2.5}]) assert.throws(() => plan(nodes, edges, 'r', options), RangeError);
});
