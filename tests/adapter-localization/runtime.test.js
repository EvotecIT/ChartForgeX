const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/adapter-localization.js'), 'utf8');
const key = Symbol.for('ChartForgeX.uiText');
function runtime() {
  const context = vm.createContext({ window: {} });
  vm.runInContext(source, context);
  return { context, text: context.window[key] };
}

test('Resolved root messages preserve literal authored placeholders and independent bundles', () => {
  const { text } = runtime();
  const first = { dataset: { cfxUi: JSON.stringify({ Selected: 'Wybrano {label} / {count} / {unknown}' }) } };
  const second = { dataset: { cfxUi: JSON.stringify({ Selected: 'اختيار {label}: {count}' }) } };
  const values = { label: 'Native {count}', count: 2 };
  assert.equal(text(first, 'Selected', 'fallback', values), 'Wybrano Native {count} / 2 / {unknown}');
  assert.equal(text(second, 'Selected', 'fallback', values), 'اختيار Native {count}: 2');
  assert.deepEqual(values, { label: 'Native {count}', count: 2 });
  assert.equal(text(first, 'Missing', 'default {count}', values), 'default 2');
});

test('Repeated assets keep one owner and a changed or malformed root bundle resolves safely', () => {
  const { context, text } = runtime();
  const root = { dataset: { cfxUi: '{"Find":"Find A"}' } };
  assert.equal(text(root, 'Find', 'fallback'), 'Find A');
  vm.runInContext(source, context);
  assert.equal(context.window[key], text);
  root.dataset.cfxUi = '{"Find":"Find B"}';
  assert.equal(text(root, 'Find', 'fallback'), 'Find B');
  for (const raw of ['{malformed', 'null', 'true', '[]', '{"Find":4}']) {
    root.dataset.cfxUi = raw;
    assert.equal(text(root, 'Find', 'fallback'), 'fallback');
  }
  assert.equal(text(null, 'Find', 'fallback'), 'fallback');
});
