import { readdir, readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const { JSDOM } = await import('jsdom');
const dom = new JSDOM('<!doctype html><html><body></body></html>');
globalThis.window = dom.window;
globalThis.document = dom.window.document;
Object.defineProperty(globalThis, 'navigator', {
  value: dom.window.navigator,
  configurable: true
});

const { default: mermaid } = await import('mermaid');

const root = fileURLToPath(new URL('.', import.meta.url));
const fixtures = join(root, 'fixtures');
const files = (await readdir(fixtures)).filter((file) => file.endsWith('.mmd')).sort();
const fixtureFiles = new Set(await readdir(fixtures));

if (files.length === 0) {
  throw new Error('No Mermaid conformance fixtures found.');
}

mermaid.initialize({
  startOnLoad: false,
  deterministicIds: true,
  securityLevel: 'strict'
});

const failures = [];
for (const file of files) {
  const source = await readFile(join(fixtures, file), 'utf8');
  try {
    await mermaid.parse(source, { suppressErrors: false });
    const expectedFile = file.replace(/\.mmd$/, '.expected.json');
    if (fixtureFiles.has(expectedFile)) {
      const expected = JSON.parse(await readFile(join(fixtures, expectedFile), 'utf8'));
      const diagram = await mermaid.mermaidAPI.getDiagramFromText(source);
      if (expected.nodes) {
        const vertices = diagram.db.getVertices();
        assert.deepEqual(vertices instanceof Map ? [...vertices.keys()] : Object.keys(vertices), expected.nodes);
        assert.deepEqual(diagram.db.getEdges().map(edge => [edge.start, edge.end]), expected.edges);
      }
      if (expected.tasks) {
        const day = value => [value.getFullYear(), String(value.getMonth() + 1).padStart(2, '0'), String(value.getDate()).padStart(2, '0')].join('-');
        assert.deepEqual(diagram.db.getTasks().map((task, index) => [expected.tasks[index][0] === null ? null : task.id, day(task.startTime), day(task.endTime)]), expected.tasks);
      }
      if (expected.radar) {
        assert.deepEqual(diagram.db.getAxes().map(axis => [axis.name, axis.label]), expected.radar.axes);
        assert.deepEqual(diagram.db.getCurves().map(curve => [curve.name, curve.label, curve.entries]), expected.radar.curves);
        assert.deepEqual(diagram.db.getOptions(), expected.radar.options);
      }
    }
  } catch (error) {
    failures.push(`${file}: ${error?.message ?? error}`);
  }
}

if (failures.length > 0) {
  throw new Error(`Mermaid.js rejected ${failures.length} fixture(s):\n${failures.join('\n')}`);
}

console.log(`Mermaid.js accepted ${files.length} fixture(s).`);
