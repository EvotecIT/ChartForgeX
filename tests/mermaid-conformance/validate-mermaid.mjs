import { readdir, readFile } from 'node:fs/promises';
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
  } catch (error) {
    failures.push(`${file}: ${error?.message ?? error}`);
  }
}

if (failures.length > 0) {
  throw new Error(`Mermaid.js rejected ${failures.length} fixture(s):\n${failures.join('\n')}`);
}

console.log(`Mermaid.js accepted ${files.length} fixture(s).`);
