import { readdir, readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { isSyntaxRejection } from './reference-errors.mjs';

const root = fileURLToPath(new URL('.', import.meta.url));
const manifest = JSON.parse(await readFile(join(root, 'compatibility.json'), 'utf8'));
assert.equal(manifest.schemaVersion, 1, 'Unsupported compatibility manifest schema.');
const args = process.argv.slice(2);
const referenceIndex = args.indexOf('--reference');
const referenceId = referenceIndex < 0 ? null : args.splice(referenceIndex, 2)[1];
assert.ok(args.length <= 1 && (!args[0] || ['fixtures', 'recognition'].includes(args[0])), 'Usage: node validate-mermaid.mjs [fixtures|recognition] [--reference 10|11|12]');
const folders = args[0] ? [args[0]] : ['fixtures', 'recognition'];
const references = Object.keys(manifest.references);
assert.deepEqual(references.sort(), ['10', '11', '12'], 'Declare the three qualified reference lanes.');
assert.ok(referenceIndex < 0 || references.includes(referenceId), 'Select a declared reference after --reference.');
const inventory = [];
for (const folder of ['fixtures', 'recognition']) {
  inventory.push(...(await readdir(join(root, folder))).filter(file => file.endsWith('.mmd')).map(file => `${folder}/${file}`));
}
assert.deepEqual(Object.keys(manifest.fixtures).sort(), inventory.sort(), 'Every fixture needs version metadata; remove stale entries.');
for (const [path, fixture] of Object.entries(manifest.fixtures)) {
  assert.deepEqual(Object.keys(fixture.syntax).sort(), references, `${path}: incomplete reference outcomes.`);
  for (const outcome of Object.values(fixture.syntax)) assert.ok(['accepted', 'rejected'].includes(outcome), `${path}: invalid syntax outcome.`);
  assert.equal(fixture.syntax['12'], 'accepted', `${path}: current reference must accept the corpus.`);
  assert.ok(fixture.provenance?.length > 0, `${path}: record source provenance.`);
  for (const [id, expected] of Object.entries(fixture.expectedByReference ?? {})) {
    assert.ok(references.includes(id) && fixture.syntax[id] === 'accepted', `${path}: invalid semantic reference.`);
    assert.ok(/^[a-z0-9-]+\.expected(?:\.\d+)?\.json$/.test(expected), `${path}: expected files must be siblings.`);
    assert.ok(fixture.notes?.[id], `${path}: explain version-specific semantic expectations.`);
  }
}
if (!referenceId) {
  const contracts = spawnSync(process.execPath, ['--test', fileURLToPath(new URL('reference-errors.test.mjs', import.meta.url))], {encoding: 'utf8'});
  if (contracts.error) throw contracts.error;
  if (contracts.status !== 0) throw new Error(contracts.stderr || contracts.stdout || 'Reference-error contract tests failed.');
  for (const id of references) {
    const child = spawnSync(process.execPath, [fileURLToPath(import.meta.url), ...args, '--reference', id], {encoding: 'utf8', maxBuffer: 8 * 1024 * 1024});
    if (child.error) throw child.error;
    if (child.status !== 0) throw new Error(child.stderr || child.stdout || `Reference ${id} exited ${child.status}.`);
    const summary = child.stdout.split('\n').findLast(line => line.startsWith('Mermaid.js '));
    assert.ok(summary, `Reference ${id} did not report qualification.`);
    console.log(summary);
  }
  process.exit(0);
}
assert.ok(references.includes(referenceId), `Unknown reference ${referenceId}.`);
const reference = manifest.references[referenceId];
assert.ok(['mermaid', 'mermaid-10', 'mermaid-11'].includes(reference.package), 'Unsupported reference package.');
const installed = JSON.parse(await readFile(join(root, 'node_modules', reference.package, 'package.json'), 'utf8'));
assert.equal(installed.version, reference.version, `Reference ${referenceId} must use its pinned version.`);

const { JSDOM } = await import('jsdom');
const dom = new JSDOM('<!doctype html><html><body></body></html>');
globalThis.window = dom.window;
globalThis.document = dom.window.document;
Object.defineProperty(globalThis, 'navigator', {
  value: dom.window.navigator,
  configurable: true
});

const { default: mermaid } = await import(reference.package);

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
let accepted = 0;
let rejected = 0;
let semanticVariants = 0;
for (const { folder, file, hasExpected } of files) {
  const fixtures = join(root, folder);
  const source = await readFile(join(fixtures, file), 'utf8');
  const contract = manifest.fixtures[`${folder}/${file}`];
  let syntaxError;
  let syntaxFailed = false;
  try {
    const parsed = await mermaid.parse(source, { suppressErrors: false });
    if (parsed === false) throw new Error('The reference parser returned false.');
  }
  catch (error) { syntaxFailed = true; syntaxError = error; }
  if (contract.syntax[referenceId] === 'rejected') {
    if (!syntaxFailed) failures.push(`${folder}/${file}: reference ${reference.version} unexpectedly accepts syntax marked unavailable.`);
    else if (isSyntaxRejection(syntaxError, reference.version)) rejected++;
    else failures.push(`${folder}/${file}: unexpected engine failure, not a qualified syntax rejection: ${syntaxError?.message ?? syntaxError}`);
    continue;
  }
  if (syntaxFailed) {
    failures.push(`${folder}/${file}: ${syntaxError?.message ?? syntaxError}`);
    continue;
  }
  accepted++;
  try {
    const variant = contract.expectedByReference?.[referenceId];
    const expectedFile = variant ?? file.replace(/\.mmd$/, '.expected.json');
    if (variant) semanticVariants++;
    if (hasExpected || variant) {
      const expected = JSON.parse(await readFile(join(fixtures, expectedFile), 'utf8'));
      const diagram = await mermaid.mermaidAPI.getDiagramFromText(source);
      const keys = values => values instanceof Map ? [...values.keys()] : Object.keys(values);
      if (expected.states && referenceId === '10') diagram.db.extract(diagram.db.getRootDocV2());
      if (expected.classes) assert.deepEqual(keys(diagram.db.getClasses()), expected.classes);
      if (expected.entities) assert.deepEqual(keys(diagram.db.getEntities()), expected.entities);
      if (expected.states) assert.deepEqual(keys(diagram.db.getStates()), expected.states);
      if (Object.hasOwn(expected, 'direction')) {
        assert.equal(typeof diagram.db.getDirection === 'function' ? diagram.db.getDirection() : null, expected.direction);
      }
      if (expected.configuration) {
        const configuration = mermaid.mermaidAPI.getConfig();
        for (const [path, value] of Object.entries(expected.configuration)) {
          assert.deepEqual(path.split('.').reduce((current, key) => current?.[key], configuration), value, `Configuration ${path}`);
        }
      }
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

console.log(`Mermaid.js ${reference.version}: ${accepted} accepted, ${rejected} documented syntax rejections, ${semanticVariants} explicit semantic variant(s).`);
