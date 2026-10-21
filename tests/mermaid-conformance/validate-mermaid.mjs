import { readdir, readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
import { isAbsolute, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { isReferenceRejection } from './reference-errors.mjs';
import { gitGraphFacts, flowchartStyleFacts } from './reference-facts.mjs';
import { useReferenceClock } from './reference-clock.mjs';
import { classFacts, stateFacts, erFacts, sequenceLifetimeFacts } from './reference-notation.mjs';
import { timeFormat } from 'd3-time-format';
import * as calendar from 'd3-time';

// Reference calendar facts use the same deterministic UTC wall-clock interpretation as native Gantt dates.
process.env.TZ = 'UTC';

const root = fileURLToPath(new URL('.', import.meta.url));
const manifest = JSON.parse(await readFile(join(root, 'compatibility.json'), 'utf8'));
assert.equal(manifest.schemaVersion, 1, 'Unsupported compatibility manifest schema.');
const args = process.argv.slice(2);
const peerIndex = args.indexOf('--peer');
const includePeer = peerIndex >= 0;
if (includePeer) args.splice(peerIndex, 1);
const outputIndex = args.indexOf('--output');
const output = outputIndex < 0 ? null : args.splice(outputIndex, 2)[1];
assert.ok(!includePeer || output && isAbsolute(output), 'Optional peer output requires --peer --output <absolute task directory>.');
assert.ok(includePeer || outputIndex < 0, '--output applies only to the optional peer lane.');
const referenceIndex = args.indexOf('--reference');
const referenceId = referenceIndex < 0 ? null : args.splice(referenceIndex, 2)[1];
const corpusFolders = ['fixtures', 'recognition', 'invalid'];
assert.ok(args.length <= 1 && (!args[0] || corpusFolders.includes(args[0])), 'Usage: node validate-mermaid.mjs [fixtures|recognition|invalid] [--reference 10|11|12] [--peer --output <absolute task directory>]');
const folders = args[0] ? [args[0]] : corpusFolders;
const references = Object.keys(manifest.references);
assert.deepEqual(references.sort(), ['10', '11', '12'], 'Declare the three qualified reference lanes.');
assert.ok(referenceIndex < 0 || references.includes(referenceId), 'Select a declared reference after --reference.');
assert.ok(!includePeer || !referenceId, 'The peer lane runs once after all official references.');
const inventory = [];
for (const folder of corpusFolders) {
  inventory.push(...(await readdir(join(root, folder))).filter(file => file.endsWith('.mmd')).map(file => `${folder}/${file}`));
}
assert.deepEqual(Object.keys(manifest.fixtures).sort(), inventory.sort(), 'Every fixture needs version metadata; remove stale entries.');
for (const [path, fixture] of Object.entries(manifest.fixtures)) {
  assert.deepEqual(Object.keys(fixture.syntax).sort(), references, `${path}: incomplete reference outcomes.`);
  for (const outcome of Object.values(fixture.syntax)) assert.ok(['accepted', 'rejected'].includes(outcome), `${path}: invalid syntax outcome.`);
  const malformed = path.startsWith('invalid/');
  assert.equal(fixture.syntax['12'], malformed ? 'rejected' : 'accepted',
    `${path}: current reference must ${malformed ? 'reject malformed sources' : 'accept the positive corpus'}.`);
  assert.ok(fixture.provenance?.length > 0, `${path}: record source provenance.`);
  for (const [id, expected] of Object.entries(fixture.expectedByReference ?? {})) {
    assert.ok(references.includes(id) && fixture.syntax[id] === 'accepted', `${path}: invalid semantic reference.`);
    assert.ok(/^[a-z0-9-]+\.expected(?:\.\d+)?\.json$/.test(expected), `${path}: expected files must be siblings.`);
    assert.ok(fixture.notes?.[id], `${path}: explain version-specific semantic expectations.`);
  }
}
if (!referenceId) {
  const contracts = spawnSync(process.execPath, ['--test', fileURLToPath(new URL('reference-errors.test.mjs', import.meta.url)), fileURLToPath(new URL('reference-clock.test.mjs', import.meta.url))], {encoding: 'utf8'});
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
  if (includePeer) {
    const peer = spawnSync('dotnet', ['run', '--project', join(root, 'peer', 'MermaiderPeer.csproj'), '-c', 'Release',
      '--artifacts-path', join(output, 'peer-build'), '-p:RestoreLockedMode=true', '--', root, join(output, 'peer')],
      { encoding: 'utf8', timeout: 180000, maxBuffer: 8 * 1024 * 1024 });
    if (peer.error) throw peer.error;
    if (peer.status !== 0) throw new Error(peer.stderr || peer.stdout || `Optional peer runner exited ${peer.status}.`);
    console.log(peer.stdout.trim());
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
  const restoreClock = useReferenceClock(contract.referenceClockUtc, dom.window);
  try {
  let syntaxError;
  let syntaxFailed = false;
  try {
    const parsed = await mermaid.parse(source, { suppressErrors: false });
    if (parsed === false) throw new Error('The reference parser returned false.');
  }
  catch (error) { syntaxFailed = true; syntaxError = error; }
  if (contract.syntax[referenceId] === 'rejected') {
    if (!syntaxFailed) failures.push(`${folder}/${file}: reference ${reference.version} unexpectedly accepts syntax marked rejected.`);
    else if (isReferenceRejection(syntaxError, reference.version, /^gitGraph\b/m.test(source) ? 'gitGraph'
      : /^(?:flowchart(?:-elk)?|graph)\b/m.test(source) ? 'flowchart'
      : /^packet(?:-beta)?\b/m.test(source) ? 'packet' : '')) rejected++;
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
      if (Object.hasOwn(expected, 'todayMarker')) {
        assert.equal(diagram.db.getTodayMarker(), expected.todayMarker);
      }
      if (expected.markerFacts) {
        assert.deepEqual(diagram.db.getTasks().map(task => [task.id, !!task.vert, task.order]), expected.markerFacts);
      }
      if (expected.states && referenceId === '10') diagram.db.extract(diagram.db.getRootDocV2());
      if (expected.classes) assert.deepEqual(keys(diagram.db.getClasses()), expected.classes);
      if (expected.entities) assert.deepEqual(keys(diagram.db.getEntities()), expected.entities);
      if (expected.states) assert.deepEqual(keys(diagram.db.getStates()), expected.states);
      if (expected.classFacts) assert.deepEqual(classFacts(diagram.db), expected.classFacts);
      if (expected.stateFacts) assert.deepEqual(stateFacts(diagram.db), expected.stateFacts);
      if (expected.erFacts) assert.deepEqual(erFacts(diagram.db), expected.erFacts);
      if (expected.sequenceLifetimes) assert.deepEqual(sequenceLifetimeFacts(diagram.db), expected.sequenceLifetimes);
      if (expected.participants) {
        assert.deepEqual(keys(diagram.db.getActors()), expected.participants);
        assert.deepEqual(diagram.db.getMessages().filter(message => message.from && message.to && message.placement === undefined)
          .map(message => [message.from, message.to, message.message]), expected.messages);
      }
      if (Object.hasOwn(expected, 'direction')) {
        assert.equal(typeof diagram.db.getDirection === 'function' ? diagram.db.getDirection() : null, expected.direction);
      }
      if (expected.configuration) {
        const configuration = mermaid.mermaidAPI.getConfig();
        for (const [path, value] of Object.entries(expected.configuration)) {
          assert.deepEqual(path.split('.').reduce((current, key) => current?.[key], configuration), value, `Configuration ${path}`);
        }
      }
      if (expected.packetRows) {
        // Inclusive ranges determine visible field widths. The auxiliary bits value
        // differs from end-start+1 for split fields in both accepting pinned engines.
        assert.deepEqual(diagram.db.getPacket().map(row => row.map(block => [block.start, block.end, block.label])), expected.packetRows);
      }
      if (expected.configurationAbsent) {
        const configuration = mermaid.mermaidAPI.getConfig();
        for (const path of expected.configurationAbsent)
          assert.equal(path.split('.').reduce((current, key) => current?.[key], configuration), undefined, `Configuration ${path} is unavailable`);
      }
      if (expected.flowchartStyles) assert.deepEqual(flowchartStyleFacts(diagram.db), expected.flowchartStyles);
      if (expected.groups) assert.deepEqual(diagram.db.getSubGraphs().map(group => ({
        id: group.id, title: group.title, members: [...group.nodes].sort()
      })), expected.groups);
      if (expected.gitgraph) assert.deepEqual(gitGraphFacts(diagram.db, expected.gitgraph), expected.gitgraph);
      if (expected.nodes) {
        const vertices = diagram.db.getVertices();
        assert.deepEqual(vertices instanceof Map ? [...vertices.keys()] : Object.keys(vertices), expected.nodes);
        if (expected.nodeLabels) {
          const labels = vertices instanceof Map ? [...vertices.values()] : Object.values(vertices);
          assert.deepEqual(Object.fromEntries(labels.map(vertex => [vertex.id, vertex.text])), expected.nodeLabels);
        }
        if (expected.referenceNodeShapes) assert.deepEqual((vertices instanceof Map ? [...vertices.values()] : Object.values(vertices)).map(vertex => vertex.type), expected.referenceNodeShapes);
        assert.deepEqual(diagram.db.getEdges().map(edge => [edge.start, edge.end]), expected.edges);
        if (expected.referenceEdgeTypes) assert.deepEqual(diagram.db.getEdges().map(edge => edge.type), expected.referenceEdgeTypes);
        if (expected.edgeLabels) assert.deepEqual(diagram.db.getEdges().map(edge => edge.text), expected.edgeLabels);
        if (expected.minimumRankSpans) assert.deepEqual(diagram.db.getEdges().map(edge => edge.length), expected.minimumRankSpans);
      }
      if (expected.sankey) {
        const graph = diagram.db.getGraph();
        assert.deepEqual(graph.nodes.map(node => node.id), expected.sankey.nodes);
        assert.deepEqual(graph.links.map(link => [link.source, link.target, link.value]), expected.sankey.links);
      }
      if (expected.tasks) {
        const day = value => [value.getFullYear(), String(value.getMonth() + 1).padStart(2, '0'), String(value.getDate()).padStart(2, '0')].join('-');
        assert.deepEqual(diagram.db.getTasks().map((task, index) => [expected.tasks[index][0] === null ? null : task.id, day(task.startTime), day(task.endTime)]), expected.tasks);
      }
      if (expected.treemap) {
        const nodes = diagram.db.getNodes();
        const indexes = new Map(nodes.map((node, index) => [node, index]));
        const parents = new Map();
        for (const [index, node] of nodes.entries()) for (const child of node.children ?? []) parents.set(child, index);
        assert.deepEqual(nodes.map(node => [node.name, node.value ?? null, parents.get(node) ?? null]), expected.treemap.nodes);
        assert.equal(indexes.size, nodes.length, 'Treemap source nodes retain distinct identities.');
      }
      if (expected.mindmap) {
        assert.deepEqual(mindmapFacts(diagram.db, Object.hasOwn(expected.mindmap, 'sections')), expected.mindmap);
      }
      if (expected.radar) {
        assert.deepEqual(diagram.db.getAxes().map(axis => [axis.name, axis.label]), expected.radar.axes);
        assert.deepEqual(diagram.db.getCurves().map(curve => [curve.name, curve.label, curve.entries]), expected.radar.curves);
        assert.deepEqual(diagram.db.getOptions(), expected.radar.options);
      }
      if (expected.quadrant) {
        const data = diagram.db.getQuadrantData();
        assert.deepEqual(data.quadrants.map(region => region.text.text), expected.quadrant.labels);
        const [one, two, three, four] = data.quadrants;
        assert.ok(one.x > two.x && one.y === two.y && three.x === two.x && three.y > two.y
          && four.x === one.x && four.y === three.y, 'Quadrants preserve top-right, top-left, bottom-left, bottom-right order.');
        const points = data.points.map(point => [point.text.text,
          (point.x - two.x) / (two.width * 2), 1 - (point.y - two.y) / (two.height * 2)])
          .sort((a, b) => a[0].localeCompare(b[0]));
        const expectedPoints = [...expected.quadrant.points].sort((a, b) => a[0].localeCompare(b[0]));
        assert.deepEqual(points.map(point => point[0]), expectedPoints.map(point => point[0]));
        points.forEach((point, index) => {
          assert.ok(Math.abs(point[1] - expectedPoints[index][1]) < 1e-9);
          assert.ok(Math.abs(point[2] - expectedPoints[index][2]) < 1e-9);
        });
      }
      if (expected.taskTimestamps) {
        const pad = (value, length = 2) => String(value).padStart(length, '0');
        const timestamp = value => `${pad(value.getFullYear(), 4)}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}.${pad(value.getMilliseconds(), 3)}`;
        assert.deepEqual(diagram.db.getTasks().map(task => [task.id, timestamp(task.startTime), timestamp(task.endTime)]), expected.taskTimestamps);
      }
      if (expected.axisFormats) {
        assert.equal(diagram.db.getAxisFormat(), expected.axisFormat);
        const tasks = diagram.db.getTasks();
        for (const contract of expected.axisFormats) {
          const task = tasks.find(value => value.id === contract.id);
          assert.ok(task, `Axis format task ${contract.id}`);
          for (const [format, label] of contract.values) assert.equal(timeFormat(format)(task.startTime), label, `Axis format ${format}`);
        }
      }
      if (expected.tickCases) {
        assert.equal(diagram.db.getTickInterval(), expected.tickInterval);
        assert.equal(diagram.db.getWeekday(), expected.weekday);
        const format = timeFormat('%Y-%m-%dT%H:%M:%S.%L');
        for (const contract of expected.tickCases) {
          const interval = calendar[contract.unit === 'Week' ? 'time' + contract.weekday : 'time' + contract.unit].every(contract.count);
          const values = interval.range(new Date(contract.start + 'Z'), new Date(+new Date(contract.end + 'Z') + 1)).map(format);
          assert.deepEqual(values, contract.values, `Calendar ticks ${contract.unit}/${contract.count}/${contract.weekday}`);
        }
      }
      if (expected.taskRenderEnds) {
        const day = value => [value.getFullYear(), String(value.getMonth() + 1).padStart(2, '0'), String(value.getDate()).padStart(2, '0')].join('-');
        assert.deepEqual(diagram.db.getTasks().map(task => day(task.renderEndTime || task.endTime)), expected.taskRenderEnds);
      }
      if (expected.ganttInputDates) {
        for (const [format, start, end, expectedStart, expectedEnd] of expected.ganttInputDates) {
          const input = await mermaid.mermaidAPI.getDiagramFromText(`gantt\ndateFormat ${format}\nTask :task,${start},${end}`);
          const task = input.db.getTasks()[0];
          assert.deepEqual([task.startTime.toISOString().replace('Z', ''), task.endTime.toISOString().replace('Z', '')],
            [expectedStart, expectedEnd], `Input date ${format}/${start}/${end}`);
        }
      }
    }
  } catch (error) {
    failures.push(`${folder}/${file}: ${error?.message ?? error}`);
  }
  } finally { restoreClock(); }
}

dom.window.close();

if (failures.length > 0) {
  throw new Error(`Mermaid.js rejected ${failures.length} fixture(s):\n${failures.join('\n')}`);
}

console.log(`Mermaid.js ${reference.version}: ${accepted} accepted, ${rejected} documented source rejections, ${semanticVariants} explicit semantic variant(s).`);

// The parsed hierarchy is available in every pinned engine. Color sections become
// model facts in 11/12; the 10 expectations omit those renderer-only facts.
function mindmapFacts(db, includeSections) {
  const facts = { nodes: [] };
  const sections = includeSections
    ? new Map(db.getData().nodes.map(node => [String(node.id), node.section ?? null])) : null;
  if (sections) facts.sections = [];
  function visit(node, parent) {
    const index = facts.nodes.length;
    facts.nodes.push([node.descr, parent]);
    if (sections) facts.sections.push(sections.get(String(node.id)));
    for (const child of node.children ?? []) visit(child, index);
  }
  const root = db.getMindmap();
  if (root) visit(root, null);
  return facts;
}
