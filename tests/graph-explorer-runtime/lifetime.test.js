const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/adapter-lifetime.js'), 'utf8');

test('unrelated host mutations do not scan mounted adapter listeners; local removals release them', async () => {
  const dom = new JSDOM('<main><section id="graph"></section><aside id="host"></aside></main>', { runScripts: 'outside-only' });
  const { window } = dom;
  try {
    window.eval(source);
    const root = window.document.querySelector('#graph'), host = window.document.querySelector('#host');
    const nodes = Array.from({ length: 100 }, () => root.appendChild(window.document.createElement('button')));
    const targets = new Set(nodes), contains = root.contains.bind(root);
    let listenerChecks = 0, activations = 0;
    root.contains = target => { if (targets.has(target)) listenerChecks++; return contains(target); };
    const lifetime = window[Symbol.for('ChartForgeX.adapterLifetimes')].create(root, 'graph');
    for (const node of nodes) lifetime.listen(node, 'click', () => activations++);
    const mutations = () => new Promise(resolve => window.setTimeout(resolve, 0));
    await mutations(); listenerChecks = 0;
    host.appendChild(window.document.createElement('span'));
    await mutations();
    assert.equal(listenerChecks, 0, 'Host additions must not walk the graph listener targets.');
    host.firstChild.remove();
    await mutations();
    assert.equal(listenerChecks, 0, 'Host removals must not walk another root\'s listener targets.');
    root.appendChild(window.document.createElement('span'));
    await mutations();
    assert.equal(listenerChecks, 0, 'Additions cannot detach a listener target.');
    nodes[0].remove();
    await mutations();
    nodes[0].click(); nodes[1].click();
    assert.equal(activations, 1, 'A removed control releases its listener while the mounted control still works.');
    root.remove();
    await mutations();
    nodes[1].click();
    assert.equal(activations, 1, 'Removing the root still disposes all remaining listeners.');
    assert.equal(lifetime.active, false);
  } finally { window.close(); }
});
