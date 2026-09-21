const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const context = vm.createContext({ sceneSize: () => ({ width: 600, height: 400, centerX: 300, centerY: 200 }) });
vm.runInContext("const attr = (element, key) => element?.[key] || '';\n" +
  fs.readFileSync(path.join(assets, 'graph-explorer.02-geometry.js'), 'utf8') + '\n' +
  fs.readFileSync(path.join(assets, 'graph-explorer.25-layout-metrics.js'), 'utf8') + '\n' +
  fs.readFileSync(path.join(assets, 'graph-explorer.25-layout-quality.js'), 'utf8') +
  '\nthis.assess = assessOverlaps; this.radius = nodeRadius; this.metrics = layoutQualityMetrics; this.expand = expandDenseLayout;', context);
const node = (id, x, y, size = 8) => ({ id, x, y, size });
test('assessment includes collisions after the first 900 nodes', () => {
  const nodes = Array.from({ length: 1000 }, (_, i) => node(String(i), i * 100, 0));
  nodes[999].x = nodes[998].x;
  const result = context.assess(nodes);
  assert.equal(result.overlaps, 1);
  assert.equal(result.complete, true);
  assert.equal(result.nodeCount, 1000);
  assert.ok(result.comparisons < 1000);
});
test('sweep agrees with brute force for varying sizes and labels', () => {
  const nodes = Array.from({ length: 80 }, (_, i) => node('label-' + i, (i * 73) % 600, (i * 37) % 400, 4 + i % 50));
  for (const labels of [false, true]) {
    let expected = 0;
    for (let i = 0; i < nodes.length; i++) for (let j = i + 1; j < nodes.length; j++) {
      if (Math.hypot(nodes[i].x - nodes[j].x, nodes[i].y - nodes[j].y) < context.radius(nodes[i], labels) + context.radius(nodes[j], labels)) expected++;
    }
    const result = context.assess(nodes, labels);
    assert.equal(result.overlaps, expected);
    assert.equal(result.complete, true);
  }
});
test('pathological overlap reports a bounded lower count rather than complete coverage', () => {
  const result = context.assess(Array.from({ length: 1000 }, (_, i) => node(String(i), 0, 0)), false, 123);
  assert.equal(result.comparisons, 123);
  assert.equal(result.overlaps, 123);
  assert.equal(result.complete, false);
});
test('empty and tangent geometry do not report overlaps', () => {
  assert.equal(context.assess([]).overlaps, 0);
  assert.equal(context.assess([node('a', 0, 0), node('b', 26, 0)], false).overlaps, 0);
});

test('a centered scene with unresolved collisions is still marked for review', () => {
  const root = { dataset: {} };
  context.metrics(root, { nodes: [node('a', 300, 200), node('b', 300, 200)] });
  assert.equal(root.dataset.cfxGraphLayoutOverlapCoverage, 'complete');
  assert.equal(root.dataset.cfxGraphLayoutOverlapCount, '1');
  assert.equal(root.dataset.cfxGraphLayoutQuality, 'needs-review');
});

test('adaptive sweep reaches dense regions after a tall sparse prefix', () => {
  const nodes = Array.from({ length: 1000 }, (_, i) => ({ ...node(String(i), 0, i < 900 ? i * 100 : 100000), vx: 0, vy: 0 }));
  const root = { dataset: {} };
  const assessment = context.assess(nodes);
  assert.equal(assessment.complete, true);
  assert.equal(assessment.overlaps, 4950);
  context.expand(root, { nodes });
  assert.equal(root.dataset.cfxGraphLayoutDensityCoverage, 'complete');
  assert.ok(Number(root.dataset.cfxGraphLayoutDensityExpansion) > 1);
});

test('a tall collision-free chain is neither marked dense nor expanded', () => {
  const nodes = Array.from({ length: 1000 }, (_, i) => ({ ...node(String(i), 0, i * 100), vx: 0, vy: 0 }));
  const root = { dataset: {} };
  const before = nodes.map(node => [node.x, node.y]);
  context.expand(root, { nodes });
  assert.equal(root.dataset.cfxGraphLayoutDensityCoverage, 'complete');
  assert.equal(root.dataset.cfxGraphLayoutDensityDecision, 'sparse');
  assert.equal(root.dataset.cfxGraphLayoutDensityExpansion, undefined);
  assert.deepEqual(nodes.map(node => [node.x, node.y]), before);
});

test('unresolved sparse coverage does not invent density or move coordinates', () => {
  const nodes = Array.from({ length: 2000 }, (_, i) => ({
    ...node(String(i), i < 1000 ? 0 : (i - 999) * 100, i < 1000 ? (i + 1) * 100 : 0), vx: 0, vy: 0
  }));
  const root = { dataset: {} };
  const before = nodes.map(node => [node.x, node.y]);
  context.expand(root, { nodes });
  assert.equal(root.dataset.cfxGraphLayoutDensityCoverage, 'budget-limited');
  assert.equal(root.dataset.cfxGraphLayoutDensityDecision, 'unresolved');
  assert.equal(root.dataset.cfxGraphLayoutDensityExpansion, undefined);
  assert.deepEqual(nodes.map(node => [node.x, node.y]), before);
});
