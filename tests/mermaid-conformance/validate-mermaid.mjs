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
const folders = process.argv[2] ? [process.argv[2]] : ['fixtures', 'recognition'];
const files = [];
for (const folder of folders) {
  const fixtures = join(root, folder);
  const fixtureFiles = new Set(await readdir(fixtures));
  for (const file of [...fixtureFiles].filter(file => file.endsWith('.mmd')).sort()) {
    files.push({ folder, file, hasExpected: fixtureFiles.has(file.replace(/\.mmd$/, '.expected.json')) });
  }
}

if (files.length === 0) {
  throw new Error('No Mermaid conformance fixtures found.');
}

mermaid.initialize({
  startOnLoad: false,
  deterministicIds: true,
  securityLevel: 'strict'
});

const failures = [];
for (const { folder, file, hasExpected } of files) {
  const fixtures = join(root, folder);
  const source = await readFile(join(fixtures, file), 'utf8');
  try {
    await mermaid.parse(source, { suppressErrors: false });
    const expectedFile = file.replace(/\.mmd$/, '.expected.json');
    if (hasExpected) {
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
      if (expected.taskTimestamps) {
        const pad = (value, length = 2) => String(value).padStart(length, '0');
        const timestamp = value => `${pad(value.getFullYear(), 4)}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}.${pad(value.getMilliseconds(), 3)}`;
        assert.deepEqual(diagram.db.getTasks().map(task => [task.id, timestamp(task.startTime), timestamp(task.endTime)]), expected.taskTimestamps);
      }
      if (expected.taskRenderEnds) {
        const day = value => [value.getFullYear(), String(value.getMonth() + 1).padStart(2, '0'), String(value.getDate()).padStart(2, '0')].join('-');
        assert.deepEqual(diagram.db.getTasks().map(task => day(task.renderEndTime || task.endTime)), expected.taskRenderEnds);
      }
    }
  } catch (error) {
    failures.push(`${folder}/${file}: ${error?.message ?? error}`);
  }
}

if (failures.length > 0) {
  throw new Error(`Mermaid.js rejected ${failures.length} fixture(s):\n${failures.join('\n')}`);
}

console.log(`Mermaid.js accepted ${files.length} fixture(s).`);
