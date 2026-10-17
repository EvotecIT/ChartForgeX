const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

// Exercise the real pointer-to-payload owner; the DOM boundary is an identity SVG transform.
const source = fs.readFileSync(path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/interactive.source/43-brush-projection.js'), 'utf8');
class Point {
  constructor(x, y) { this.x = x; this.y = y; }
  matrixTransform() { return this; }
}
const { brushDataSelection } = new Function('DOMPoint', 'dataValue', 'clamp', source + '\nreturn {brushDataSelection};')(
  Point, value => value, (value, min, max) => Math.max(min, Math.min(max, value)));

function brush(axis, from, to, horizontal = false) {
  const category = {minimum:0, maximum:1, transformedMinimum:0, transformedMaximum:1, scale:'linear', threshold:1, reversed:false};
  const projection = {horizontal, plotBounds:{x:0, y:0, width:100, height:100},
    x:horizontal ? category : axis, ...(horizontal ? {y:axis} : {})};
  const root = {_cfxDataProjection:projection, dataset:{cfxBrushAxis:horizontal ? 'xy' : 'x', cfxPreparedChart:'{"xLabels":[]}'},
    querySelector:() => ({getScreenCTM:() => ({a:1, d:1, is2D:true, inverse:() => ({})})})};
  return brushDataSelection(root, horizontal
    ? {left:0, right:100, top:100 * (1 - to), bottom:100 * (1 - from)}
    : {left:100 * from, right:100 * to, top:0, bottom:100});
}

for (const reversed of [false, true]) {
  test(`logarithmic brush captions follow the nearest painted tick across its geometric midpoint (reversed=${reversed})`, () => {
    const axis = {minimum:10, maximum:100, transformedMinimum:1, transformedMaximum:2,
      scale:'logarithmic', threshold:1, reversed, displayTicks:[{value:10, text:'10 units'}, {value:100, text:'100 units'}]};
    const result = brush(axis, .45, .55);
    assert.deepEqual(result.displayRange.x, [10, 100]);
    assert.equal(result.rangeLabel, '10 units – 100 units');
    assert.ok(Math.abs(result.range.x[0] - 28.183829312644534) < 1e-10);
    assert.ok(Math.abs(result.range.x[1] - 35.48133892335755) < 1e-10);
    assert.deepEqual(result.categoryRange, result.range.x);
    assert.notDeepEqual(result.range.x, result.displayRange.x);
  });
}

for (const horizontal of [false, true]) {
  test(`symmetric logarithmic brush captions use the painted negative and positive tick intervals (horizontal=${horizontal})`, () => {
    const axis = {minimum:-198, maximum:198, transformedMinimum:-2, transformedMaximum:2,
      scale:'symmetriclogarithmic', threshold:2, reversed:false,
      displayTicks:[{value:-18, text:'−18'}, {value:0, text:'zero'}, {value:18, text:'18'}]};
    const result = brush(axis, .36, .64, horizontal);
    const direction = horizontal ? 'y' : 'x';
    assert.deepEqual(result.displayRange[direction], [-18, 18]);
    assert.equal(result.rangeLabel, '−18 – 18');
    assert.ok(Math.abs(result.range[direction][0] + 5.261561095402026) < 1e-10);
    assert.ok(Math.abs(result.range[direction][1] - 5.261561095402026) < 1e-10);
    assert.deepEqual(result.categoryRange, result.range[direction]);
    const nearZero = brush(axis, .40, .60, horizontal);
    assert.deepEqual(nearZero.displayRange[direction], [0, 0]);
    assert.equal(nearZero.rangeLabel, 'zero – zero');
  });
}

test('linear brush captions retain arithmetic midpoint and stable first-tick ties', () => {
  const axis = {minimum:10, maximum:100, transformedMinimum:10, transformedMaximum:100,
    scale:'linear', threshold:1, reversed:false, displayTicks:[{value:10, text:'10'}, {value:100, text:'100'}]};
  const result = brush(axis, .45, .55);
  assert.deepEqual(result.displayRange.x, [10, 100]);
  assert.deepEqual(result.range.x, [50.5, 59.50000000000001]);
  assert.deepEqual(brush(axis, .5, .5).displayRange.x, [10, 10]);
});
