// Test-only static assets. Browser installation and sessions remain owned by HtmlTinkerX.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(process.argv[2]);
const addressFile = process.argv[3];
const types = {
  '.html': 'text/html; charset=utf-8', '.svg': 'image/svg+xml', '.png': 'image/png',
  '.json': 'application/json', '.ttf': 'font/ttf', '.css': 'text/css; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8', '.txt': 'text/plain; charset=utf-8'
};
if (!addressFile || !fs.statSync(root).isDirectory()) throw new Error('Provide the gallery directory and readiness file.');

const server = http.createServer((request, response) => {
  if (!['GET', 'HEAD'].includes(request.method)) { response.writeHead(405); response.end(); return; }
  let file;
  try { file = path.resolve(root, '.' + decodeURIComponent(new URL(request.url, 'http://127.0.0.1').pathname)); }
  catch { response.writeHead(400); response.end(); return; }
  if (!file.startsWith(root + path.sep)) { response.writeHead(403); response.end(); return; }
  fs.stat(file, (error, stat) => {
    if (error || !stat.isFile()) { response.writeHead(404); response.end(); return; }
    response.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Content-Length': stat.size });
    if (request.method === 'HEAD') { response.end(); return; }
    const stream = fs.createReadStream(file);
    stream.on('error', () => response.destroy());
    response.on('close', () => stream.destroy());
    stream.pipe(response);
  });
});
server.listen(0, '127.0.0.1', () => {
  const address = 'http://127.0.0.1:' + server.address().port + '/catalog.html';
  fs.writeFileSync(addressFile, address, 'utf8');
  console.log('Browser gallery ready: ' + address);
});
