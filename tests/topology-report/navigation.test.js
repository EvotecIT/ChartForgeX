const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/TopologyReportHtmlExtensions.Assets.cs'), 'utf8');
const script = source.match(/private const string ReportScript = """([\s\S]*?)""";/)[1];

// A DOM boundary double counts allocations, including detached buttons. This
// catches implementations that create every relationship and later hide most.
function report(count) {
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
  const records = Array.from({ length: count }, (_, index) => ['1', 'Źródło </script> ' + index, ' · page 1', 'pl']);
  ids.objects = { textContent: JSON.stringify(records) };
  for (const page of [0, 1]) {
    ids['links-' + page] = { textContent: JSON.stringify(records) };
    ids['page-' + page] = { content: { cloneNode() {
      const children = { '.links': element(), '.relationship-pages': element(), '.diagram': element() };
      return { querySelector: selector => children[selector] };
    } } };
  }
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
