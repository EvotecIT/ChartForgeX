// Product-specific test assets only; browser installation and sessions belong to HtmlTinkerX.
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const test = require('node:test');
const { setTimeout: delay } = require('node:timers/promises');

function start(root, addressFile) {
  const child = spawn(process.execPath, [path.join(__dirname, 'Serve-BrowserGallery.cjs'), root, addressFile], {
    stdio: ['ignore', 'pipe', 'pipe']
  });
  const output = { stdout: '', stderr: '' };
  child.stdout.on('data', bytes => { output.stdout += bytes; });
  child.stderr.on('data', bytes => { output.stderr += bytes; });
  const closed = new Promise((resolve, reject) => {
    child.once('error', reject);
    child.once('close', (code, signal) => resolve({ code, signal }));
  });
  return { child, closed, output };
}

async function bounded(promise, description) {
  let timer;
  try {
    return await Promise.race([promise, new Promise((_, reject) => {
      timer = setTimeout(() => reject(new Error(description + ' timed out')), 10000);
    })]);
  } finally { clearTimeout(timer); }
}

async function stop(server) {
  if (server.child.exitCode === null && server.child.signalCode === null) server.child.kill();
  return bounded(server.closed, 'Gallery server termination');
}

async function ready(server, addressFile) {
  const deadline = Date.now() + 10000;
  // This is the parent protocol: existence polling never opens a pending file.
  while (!fs.existsSync(addressFile)) {
    if (server.child.exitCode !== null) throw new Error('Gallery server exited before readiness: ' + server.output.stderr);
    if (Date.now() >= deadline) throw new Error('Gallery server readiness timed out');
    await delay(10);
  }
  const address = fs.readFileSync(addressFile, 'utf8');
  assert.match(address, /^http:\/\/127\.0\.0\.1:[1-9][0-9]*\/catalog\.html$/);
  return address;
}

test('Readiness publishes a complete address and the real gallery server serves and terminates', async () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'ChartForgeX-gallery-server-'));
  const gallery = path.join(root, 'gallery');
  fs.mkdirSync(gallery);
  const contents = '<!doctype html><title>Gallery readiness contract</title>';
  fs.writeFileSync(path.join(gallery, 'catalog.html'), contents);
  fs.writeFileSync(path.join(gallery, 'chart.svg'), '<svg xmlns="http://www.w3.org/2000/svg"/>');
  const addressFile = path.join(root, 'ready.txt');
  const server = start(gallery, addressFile);
  try {
    const address = await ready(server, addressFile);
    const response = await fetch(address, { signal: AbortSignal.timeout(5000) });
    assert.equal(response.status, 200);
    assert.match(response.headers.get('content-type'), /^text\/html/);
    assert.equal(await response.text(), contents);
    const head = await fetch(address, { method: 'HEAD', signal: AbortSignal.timeout(5000) });
    assert.equal(head.headers.get('content-length'), String(Buffer.byteLength(contents)));
    assert.equal(await head.text(), '');
    const svg = await fetch(new URL('chart.svg', address), { signal: AbortSignal.timeout(5000) });
    assert.equal(svg.headers.get('content-type'), 'image/svg+xml');
    await svg.text();
    const missing = await fetch(new URL('missing.svg', address), { signal: AbortSignal.timeout(5000) });
    assert.equal(missing.status, 404);
    await missing.text();
    const post = await fetch(address, { method: 'POST', signal: AbortSignal.timeout(5000) });
    assert.equal(post.status, 405);
    await post.text();
    assert.deepEqual(fs.readdirSync(root).sort(), ['gallery', 'ready.txt']);
    await stop(server);
    await assert.rejects(fetch(address, { signal: AbortSignal.timeout(1000) }));
    assert.equal(server.output.stderr, '');
  } finally {
    await stop(server);
    fs.rmSync(root, { recursive: true });
  }
});

test('A stale readiness path is rejected without changing its contents or reporting a new server', async () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'ChartForgeX-gallery-stale-'));
  const addressFile = path.join(root, 'ready.txt');
  const stale = 'http://127.0.0.1:1/catalog.html';
  fs.writeFileSync(addressFile, stale);
  const server = start(root, addressFile);
  try {
    const result = await bounded(server.closed, 'Stale readiness rejection');
    assert.notEqual(result.code, 0);
    assert.match(server.output.stderr, /readiness file must not already exist/);
    assert.equal(server.output.stdout, '');
    assert.equal(fs.readFileSync(addressFile, 'utf8'), stale);
  } finally {
    await stop(server);
    fs.rmSync(root, { recursive: true });
  }
});

test('A readiness publication failure exits without publishing a partial or successful address', async () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'ChartForgeX-gallery-failed-'));
  const addressFile = path.join(root, 'missing', 'ready.txt');
  const server = start(root, addressFile);
  try {
    const result = await bounded(server.closed, 'Failed readiness publication');
    assert.notEqual(result.code, 0);
    assert.match(server.output.stderr, /ENOENT/);
    assert.equal(server.output.stdout, '');
    assert.equal(fs.existsSync(addressFile), false);
  } finally {
    await stop(server);
    fs.rmSync(root, { recursive: true });
  }
});
