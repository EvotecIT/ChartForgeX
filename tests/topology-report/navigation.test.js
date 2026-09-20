const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/TopologyReportHtmlExtensions.Assets.cs'), 'utf8');
const script = source.match(/private const string ReportScript = """([\s\S]*?)""";/)[1];

// A DOM boundary double counts allocations, including detached buttons. This
// catches implementations that create every relationship and later hide most.
function report(count, customRecords) {
  let buttonsCreated = 0;
  function element(tag = 'div') {
    if (tag === 'button') buttonsCreated++;
    return {
      children: [], dataset: {}, events: {}, textContent: '',
      classList: { toggle() {} },
      append(...children) { this.children.push(...children); },
      replaceChildren(...children) { this.children = children; },
      addEventListener(name, callback) { this.events[name] = callback; },
      setAttribute() {},
      click() { this.events.click(); }
    };
  }
  const ids = Object.fromEntries(['page', 'content', 'fit', 'search', 'results', 'search-status', 'previous', 'next', 'page-status'].map(id => [id, element()]));
  ids.page.options = [0, 1];
  const records = customRecords || Array.from({ length: count }, (_, index) => ['1', 'Źródło </script> ' + index, ' · page 1', 'pl']);
  ids.objects = { textContent: JSON.stringify(records) };
  for (const page of [0, 1]) {
    ids['links-' + page] = { textContent: JSON.stringify(records) };
    ids['page-' + page] = { textContent: JSON.stringify('<section>page ' + page + '</section>') };
  }
  Object.defineProperty(ids.content, 'innerHTML', { set(value) {
    const children = { '.links': element(), '.relationship-pages': element(), '.diagram': element() };
    this.children = [{ querySelector: selector => children[selector] }];
  } });
  ids.content.querySelector = selector => ids.content.children[0].querySelector(selector);
  new Function('document', script)({
    getElementById: id => ids[id], createElement: element, addEventListener() {}
  });
  return { ids, allocated: () => buttonsCreated, links: () => ids.content.querySelector('.links').children,
    controls: () => ids.content.querySelector('.relationship-pages').children };
}

test('relationship paging allocates only one bounded batch, including detached DOM', () => {
  const view = report(1203);
  assert.equal(view.allocated(), 22);
  assert.equal(view.links().length, 20);
  assert.equal(view.links()[0].lang, 'pl');
  assert.equal(view.links()[0].children[0].lang, 'en');
  const [previous, status, next] = view.controls();
  assert.equal(previous.disabled, true);
  assert.equal(previous.textContent, 'Previous detail pages');
  assert.equal(next.textContent, 'Next detail pages');
  next.click();
  assert.equal(view.allocated(), 42);
  assert.equal(view.links().length, 20);
  assert.equal(view.links()[0].textContent, 'Źródło </script> 20');
  assert.equal(status.textContent, '21–40 of 1203');
  while (!next.disabled) next.click();
  assert.equal(view.links().length, 3);
  assert.equal(status.textContent, '1201–1203 of 1203');
  previous.click();
  assert.equal(view.links().length, 20);
  assert.equal(view.links()[0].textContent, 'Źródło </script> 1180');
});

test('search creates only twenty matching buttons and clears the previous results', () => {
  const view = report(1203);
  const before = view.allocated();
  view.ids.search.value = 'Źródło';
  view.ids.search.events.input();
  assert.equal(view.allocated() - before, 20);
  assert.equal(view.ids.results.children.length, 20);
  assert.match(view.ids['search-status'].textContent, /^1203 matches/);
  view.ids.search.value = '1202';
  view.ids.search.events.input();
  assert.equal(view.ids.results.children.length, 1);
  assert.equal(view.ids.results.children[0].textContent, 'Źródło </script> 1202');
  view.ids.search.value = '';
  view.ids.search.events.input();
  assert.equal(view.ids.results.children.length, 0);
});

test('detail pages describe relationship pagination', () => {
  const view = report(25);
  view.ids.page.value = '1';
  view.ids.page.events.change();
  assert.equal(view.controls()[0].textContent, 'Previous relationships');
  assert.equal(view.controls()[2].textContent, 'Next relationships');
});

test('search uses each record language and falls back for invalid language tags', () => {
  const view = report(0, [['1','İSTANBUL','','tr'],['1','Other','','invalid_language_tag']]);
  view.ids.search.value = 'istanbul'; view.ids.search.events.input();
  assert.equal(view.ids.results.children.length,1);
  assert.equal(view.ids.results.children[0].textContent,'İSTANBUL');
  view.ids.search.value = 'OTHER'; view.ids.search.events.input();
  assert.equal(view.ids.results.children[0].textContent,'Other');
});

test('inactive page SVGs remain text and page changes retain one parsed SVG', () => {
  const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
  const data = (id, value) => `<script type="application/json" id="${id}">${JSON.stringify(value).replace(/</g, '\\u003c')}</script>`;
  const pages = Array.from({length:200}, (_, index) => data('page-' + index,
    `<section class="diagram"><svg><circle id="node-${index}"/></svg></section><section><div class="links"></div><div class="relationship-pages"></div></section>`) + data('links-' + index, []));
  const dom = new JSDOM(`<select id="page">${Array.from({length:200},(_,i)=>`<option value="${i}">${i}</option>`).join('')}</select>
    <input id="fit" type="checkbox"><input id="search"><div id="results"></div><output id="search-status"></output>
    <button id="previous"></button><button id="next"></button><output id="page-status"></output><div id="content"></div>
    ${data('objects',[])}${pages.join('')}`, {runScripts:'outside-only'});
  try {
    const document = dom.window.document;
    assert.equal(document.querySelectorAll('svg').length,0);
    dom.window.eval(script);
    assert.equal(document.querySelectorAll('svg').length,1);
    document.getElementById('page').value='199';
    document.getElementById('page').dispatchEvent(new dom.window.Event('change'));
    assert.equal(document.querySelectorAll('svg').length,1);
    assert.equal(document.getElementById('node-0'),null);
    assert.ok(document.getElementById('node-199'));
  } finally { dom.window.close(); }
});
