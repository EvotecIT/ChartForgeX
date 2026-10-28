'use strict';
const { fixture } = require('./native-value-fixture');
// Synthetic header/identity routing only. Real native membership, domains, layout and ink are covered by C# and browser contracts.
function reflowFixture() {
  const f = fixture('cartesian-value');
  const catalog = [{ kind: 'series', seriesKey: 'current' }, { kind: 'observation', seriesKey: 'current', observationId: 'value' }, { kind: 'observation', seriesKey: 'current', observationId: 'other' }];
  const make = (hidden = false) => {
    const root = f.make(20), prefix = hidden ? 'synthetic-destination' : 'synthetic-source', metadata = JSON.parse(root.dataset.cfxPreparedChart), svg = root.querySelector('svg');
    root.insertAdjacentHTML('afterbegin', '<button class="cfx-reset" type="button" data-cfx-reset="true" hidden>Reset</button>');
    root.dataset.cfxInteractionFeatures += ',LegendToggles,Export'; metadata.series[0].kind = 'bar'; metadata.series[0].indices = hidden ? [0] : [0, 1];
    metadata.series[0].observationIds = [{ index: 0, id: 'value' }, { index: 1, id: 'other' }];
    metadata.visibility = { layout: 'reflow', sourceKey: 'a'.repeat(64), labelLayering: 'preserveOverlay', hiddenSeries: [], targetCatalog: catalog,
      hiddenTargets: hidden ? [catalog[2]] : [] };
    const maximum = hidden ? 40 : 100; metadata.yProjection.maximum = maximum; metadata.yProjection.fullMaximum = maximum;
    metadata.yProjection.transformedMaximum = maximum; metadata.yProjection.transformedFullMaximum = maximum;
    const point = svg.querySelector('[data-cfx-role=point]'); point.dataset.cfxKind = 'bar';
    point.setAttribute('id', prefix + '-node-1'); point.setAttribute('data-cfx-source-id', 'series-0-point-0');
    if (!hidden) { const other = point.cloneNode(true); Object.assign(other.dataset, { cfxPoint: '1', cfxSourcePoint: '1', cfxObservationId: 'other', cfxX: '2', cfxY: '80' }); other.setAttribute('id', prefix + '-node-3'); other.setAttribute('data-cfx-source-id', 'series-0-point-1'); svg.appendChild(other); }
    for (let index = 0; index < 2; index++) { const legend = svg.ownerDocument.createElementNS(svg.namespaceURI, 'g');
      Object.assign(legend.dataset, { cfxRole: 'legend-entry', cfxSourceId: 'legend-series-0-point-' + index, cfxSeries: '0', cfxSeriesKey: 'current', cfxPoint: String(index), cfxSourcePoint: String(index), cfxObservationId: index ? 'other' : 'value' });
      const rect = svg.ownerDocument.createElementNS(svg.namespaceURI, 'rect'); for (const [name, value] of Object.entries({ x: 40 + index * 40, y: 10, width: 12, height: 12, fill: '#2468ac' })) rect.setAttribute(name, value);
      legend.appendChild(rect); svg.appendChild(legend);
    }
    root.dataset.cfxPreparedChart = JSON.stringify(metadata); return root;
  };
  const capture = (hidden = true) => {
    const source = make(!hidden), destination = make(hidden);
    const frames = [1, .5, 0, .5, 1].map((opacity, index) => {
      const basis = index < 2 ? 'source' : 'destination', svg = (basis === 'source' ? source : destination).querySelector('svg').cloneNode(true);
      if (opacity !== 1) { const wrapper = svg.ownerDocument.createElementNS(svg.namespaceURI, 'g'); wrapper.setAttribute('data-cfx-role', 'native-transition-opacity'); wrapper.setAttribute('opacity', String(opacity));
        while (svg.firstChild) wrapper.appendChild(svg.firstChild); svg.appendChild(wrapper); }
      const prefix = 'synthetic-reflow-' + index;
      for (const node of svg.querySelectorAll('[id]')) { const match = /-node-(\d+)$/.exec(node.id); if (match) node.id = prefix + '-node-' + (Number(match[1]) + (opacity < 1 ? 1 : 0)); }
      return { basis, nativeOpacity: String(opacity), prefix, svg: svg.outerHTML };
    });
    return { kind: 'native-transition', effect: 'legend-visibility', sourceKind: 'native-visibility-reflow', destination: destination.outerHTML,
      visibility: { layout: 'reflow', labelLayering: 'preserveOverlay', targetCatalog: catalog, fromHiddenTargets: hidden ? [] : [catalog[2]], toHiddenTargets: hidden ? [catalog[2]] : [] },
      drawings: { wide: { capturePngText: false, sourcePrefix: hidden ? 'synthetic-source' : 'synthetic-destination', destinationPrefix: hidden ? 'synthetic-destination' : 'synthetic-source', frames } } };
  };
  f.controller.update(make(false)); f.changes.length = 0;
  return { ...f, makeReflow: make, captureReflow: capture };
}
const flush = async () => { for (let turn = 0; turn < 24; turn++) await Promise.resolve(); };
module.exports = { reflowFixture, flush };
