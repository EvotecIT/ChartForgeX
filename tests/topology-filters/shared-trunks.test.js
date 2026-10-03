const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');

test('one shared tail follows the active visible relationship through focus and filtering', async () => {
  const dom = new JSDOM(`<div id="chart"><svg>
    <g data-cfx-role="topology-edge" data-edge-id="a" data-trunk-owner-id="a">
      <g data-cfx-role="topology-shared-trunk-tail" data-trunk-owner-id="a"><path marker-end="url(#arrow)"/></g>
    </g>
    <g data-cfx-role="topology-edge" data-edge-id="b" data-trunk-owner-id="a"></g>
  </svg></div>`, { pretendToBeVisual: true, runScripts: 'outside-only' });
  try {
    const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/topology-interaction.source/65-shared-trunks.js'), 'utf8');
    dom.window.eval(`const wrapper=document.querySelector('#chart');const attr=(element,name)=>element.getAttribute(name)||'';${source}`);
    const [a, b] = dom.window.document.querySelectorAll('[data-cfx-role="topology-edge"]');
    const tail = dom.window.document.querySelector('[data-cfx-role="topology-shared-trunk-tail"]');
    const frame = () => new Promise(resolve => dom.window.requestAnimationFrame(() => dom.window.requestAnimationFrame(resolve)));
    assert.equal(tail.parentElement, a);
    b.classList.add('cfx-topology-html-related');
    await frame();
    assert.equal(tail.parentElement, b);
    b.style.display = 'none';
    await frame();
    assert.equal(tail.parentElement, a);
    a.style.display = 'none';
    await frame();
    assert.equal(tail.style.display, 'none');
    b.style.display = '';
    await frame();
    assert.equal(tail.parentElement, b);
    assert.equal(tail.style.display, '');
    assert.equal(dom.window.document.querySelectorAll('[marker-end]').length, 1);
  } finally { dom.window.close(); }
});
