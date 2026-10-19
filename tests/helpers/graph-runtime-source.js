const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const adapter = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html');
const manifest = fs.readFileSync(path.join(adapter, 'HtmlGraphExplorerAssets.cs'), 'utf8');
const resources = [...manifest.matchAll(/"ChartForgeX\.Interactivity\.Html\.Assets\.([^"\r\n]+\.js)"/g)]
  .map(match => ({ name: match[1], source: fs.readFileSync(path.join(adapter, 'Assets', match[1]), 'utf8') }));

// Probe private helpers inside their actual runtime owner. Loading manifest order
// retains the shared adapter lifetime and every real helper dependency; no product
// export or replacement implementation is needed for these focused Node contracts.
function graphRuntimeSource(probe) {
  return resources.map(resource => {
    if (resource.name !== 'graph-explorer.30-bindings.js') return resource.source;
    const closing = resource.source.match(/\n}\s*$/);
    assert.ok(closing, 'The graph runtime owner must close after its binding entry point');
    return resource.source.slice(0, closing.index) + '\n' + probe + resource.source.slice(closing.index);
  }).join('\n');
}

function graphMessageHelpers() {
  return new Function('document', 'window', graphRuntimeSource('return { uiText, graphUiError, graphLabelText };'))(
    { readyState: 'loading', addEventListener() {} }, {});
}
module.exports = { graphRuntimeSource, graphMessageHelpers };
