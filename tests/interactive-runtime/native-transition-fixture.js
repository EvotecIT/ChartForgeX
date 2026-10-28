'use strict';
const { fixture } = require('./native-value-fixture');
function capture(f, before, after, effect = 'values') {
  const source = f.make(before), destination = f.make(after), sourceSvg = source.querySelector('svg').outerHTML, destinationSvg = destination.querySelector('svg').outerHTML;
  return { kind: 'native-transition', effect, sourceKind: 'progress-value', destination: destination.outerHTML,
    readings: [{ seriesKey: 'current', observationId: 'value', fromValue: before, toValue: after, minimum: 0, maximum: 1 }],
    drawings: { wide: { capturePngText: false, sourcePrefix: 'synthetic-source', destinationPrefix: 'synthetic-destination', sourceSvg, destinationSvg,
      frames: (effect === 'value-flash' ? [after, after, after] : [before, (before + after) / 2, after]).map((value, index) =>
        ({ prefix: 'synthetic-frame-' + index, svg: f.make(value).querySelector('svg').outerHTML })) } } };
}
const flush = async () => { for (let turn = 0; turn < 24; turn++) await Promise.resolve(); };
module.exports = { fixture, capture, flush };
