
  const pointLabelTarget = (root, target) => {
    const label = target instanceof Element ? target.closest('[data-cfx-label-for]') : null;
    const point = label && label._cfxLabelTarget;
    return point && root.contains(label) && root.contains(point) ? point : null;
  };
  const pointLabelSurfaces = (root, point) => (point._cfxPointLabels || []).filter(label => root.contains(label));

  const appendNativeBrowserHitArea = (node, region, minimumSize) => {
    if (![region.x, region.y, region.width, region.height].every(Number.isFinite)) return;
    const width = Math.max(minimumSize, region.width), height = Math.max(minimumSize, region.height);
    const x = region.x + (region.width - width) / 2, y = region.y + (region.height - height) / 2;
    if (![x, y, width, height, x + width, y + height].every(Number.isFinite) || !(width > 0 && height > 0)
      || Array.from(node.children).some(child => child.hasAttribute('data-cfx-browser-hit-area'))) return;
    const hit = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
    hit.setAttribute('x', x); hit.setAttribute('y', y);
    hit.setAttribute('width', width); hit.setAttribute('height', height);
    hit.setAttribute('fill', 'transparent'); hit.setAttribute('pointer-events', 'all');
    hit.setAttribute('data-cfx-browser-hit-area', 'true');
    node.appendChild(hit);
  };

  const prepareChartTargets = (root) => {
    const svg = root.querySelector('.cfx-stage svg');
    if (!svg || !root.dataset.cfxPreparedChart) return;
    let metadata;
    try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch (_) { return; }
    const series = metadata.series || [];
    const reflow = metadata.visibility?.layout === 'reflow';
    const annotations = annotationCatalog(svg, metadata);
    svg.querySelectorAll('[data-cfx-role="annotation"]').forEach(node => {
      node._cfxAnnotationId = annotations.get(node);
    });
    root._cfxDataProjection = metadata.dataProjection || null;
    root._cfxObservationIds = series.map(item => new Map((item.observationIds || []).map(value => [value.index, value.id])));
    const xLabels = new Map((metadata.categoryLabels || metadata.xLabels || []).map((item) => [item.value, item.text]));
    const grouped = (metadata.categoryLabels || []).filter(item => Array.isArray(item.groups) && item.groups.length);
    if (grouped.length) root._cfxCategoryPaths = new Map(grouped.map(item => [item.value, Object.freeze([...item.groups, item.text])]));
    else delete root._cfxCategoryPaths;
    const regions = new Map((metadata.regions || []).map((region) => [region.id, region]));

    if (metadata.hierarchy?.kind === 'topology') {
      const facts = new Map(metadata.hierarchy.nodes.map(node => [node.id, node]));
      const nativeRegions = new Map((metadata.regions || []).filter(region => region.role === 'topology-node').map(region => [region.id, region]));
      const parents = new Set(metadata.hierarchy.nodes.map(node => node.parentId));
      const collapsed = new Set(metadata.hierarchy.collapsedNodeIds);
      svg.querySelectorAll('[data-cfx-role="topology-node"][data-node-id]').forEach(node => {
        const id = node.dataset.nodeId, fact = facts.get(id), region = nativeRegions.get(id); if (!fact || !region) return;
        node.dataset.cfxNode = id; node.dataset.cfxTargetKind = 'node'; node.dataset.cfxTargetId = id;
        node.dataset.cfxKind = 'topology'; node.dataset.cfxLabel = fact.label;
        node.dataset.cfxLeaf = parents.has(id) ? 'false' : 'true';
        node._cfxNativeRegion = Object.freeze({ x: region.x, y: region.y, width: region.width, height: region.height });
        if (parents.has(id)) node.setAttribute('aria-expanded', collapsed.has(id) ? 'false' : 'true');
      });
    }

    const plots = (metadata.regions || []).filter(region => region.role === 'cartesian-plot');
    const plot = plots.length === 1 ? plots[0] : null;
    root._cfxCartesianPlot = plot && [plot.x, plot.y, plot.width, plot.height, plot.x + plot.width, plot.y + plot.height].every(Number.isFinite)
      && plot.width > 0 && plot.height > 0 ? plot : null;
    series.forEach((item, index) => {
      svg.setAttribute('data-cfx-series-name-' + index, item.name);
      svg.setAttribute('data-cfx-series-key-' + index, item.key);
      svg.setAttribute('data-cfx-series-scope-' + index, item.local ? 'local' : 'shared');
      svg.setAttribute('data-cfx-series-state-' + index, item.state);
      svg.setAttribute('data-cfx-series-source-indices-' + index, item.indices.join(','));
    });
    const sourceFacts = new Map(Array.from(svg.querySelectorAll('[data-cfx-point][data-cfx-source-points],[data-cfx-point][data-cfx-derived]'))
      .map((node) => [node.dataset.cfxSeries + ':' + node.dataset.cfxPoint,
        { points: node.dataset.cfxSourcePoints, derived: node.dataset.cfxDerived, identity: derivedPointIdentity(node),
          groupIndex: node.dataset.cfxGroupIndex, groupId: node.dataset.cfxGroupId }]));
    const observationIds = new Map(Array.from(svg.querySelectorAll('[data-cfx-point][data-cfx-observation-id]'))
      .map(node => [node.dataset.cfxSeries + ':' + node.dataset.cfxPoint, node.dataset.cfxObservationId]));
    svg.querySelectorAll('[data-cfx-role="legend-entry"],[data-cfx-role="legend-item"],[data-cfx-role="legend-entry-omitted"]').forEach((node) => {
      const data = node.dataset;
      const source = data.cfxSourceId || '';
      const match = source.match(/^legend-series-(\d+)(?:-point-(\d+|other))?$/);
      const index = match ? Number(match[1]) : series.findIndex((item) => item.key === data.cfxSeriesKey);
      if (index < 0) return;
      if (data.cfxRole !== 'legend-entry-omitted') data.cfxRole = 'legend-item';
      data.cfxSeries = String(index);
      const region = regions.get(source);
      if (series[index] && node.localName === 'g' && data.cfxRole === 'legend-item'
        && region?.role === 'legend' && isInteractiveTarget(node)) appendNativeBrowserHitArea(node, region, 0);
      data.cfxLabel = data.cfxLabel || (region ? region.label : node.getAttribute('aria-label') || '');
      const reference = legendTarget(node);
      if (reference && !reflow) {
        const mark = referencedTargetNode(svg, { ...reference, seriesKey: data.cfxSeriesKey });
        if (mark) {
          data.cfxLabel = mark.dataset.cfxLabel || data.cfxLabel;
          if (mark.dataset.cfxValue !== undefined) data.cfxValue = mark.dataset.cfxValue;
          if (mark.dataset.cfxFormattedValue !== undefined) data.cfxFormattedValue = mark.dataset.cfxFormattedValue;
          if (mark.dataset.cfxLabelValue !== undefined) data.cfxLabelValue = mark.dataset.cfxLabelValue;
        }
      }
      if (match && match[2] !== undefined) {
        data.cfxPoint = match[2] === 'other' ? '-1' : match[2];
        const collectionKey = index + ':' + data.cfxPoint;
        const observationId = observationIds.get(collectionKey);
        const facts = sourceFacts.get(collectionKey);
        if (!reflow && observationId !== undefined && !facts?.derived) data.cfxObservationId = observationId;
        if (!reflow && facts) {
          if (data.cfxSourcePoints === undefined && facts.points !== undefined) data.cfxSourcePoints = facts.points;
          if (data.cfxDerived === undefined && facts.derived !== undefined) data.cfxDerived = facts.derived;
          if (data.cfxDerivedIdentity === undefined) data.cfxDerivedIdentity = facts.identity;
          if (data.cfxGroupIndex === undefined && facts.groupIndex !== undefined) data.cfxGroupIndex = facts.groupIndex;
          if (data.cfxGroupId === undefined && facts.groupId !== undefined) data.cfxGroupId = facts.groupId;
        }
      }
      if (reflow && data.cfxPoint !== undefined) {
        data.cfxLegendTargetKind = 'point'; data.cfxLegendTargetId = pointTargetId(node);
      }
    });
    svg.querySelectorAll('[data-cfx-point],[data-cfx-series],[data-cfx-role="gauge"],[data-cfx-role="circle-chart"]').forEach((node) => {
      const data = node.dataset;
      if (data.cfxSeries === undefined) {
        const owner = node.parentElement && node.parentElement.closest('[data-cfx-series]');
        data.cfxSeries = owner ? owner.dataset.cfxSeries : '0';
      }
      const item = series[Number(data.cfxSeries)];
      if (!item) return;
      data.cfxSeriesName = item.name; data.cfxSeriesKey = item.key;
      data.cfxSeriesScope = item.local ? 'local' : 'shared';
      if (data.cfxKind === undefined) data.cfxKind = item.kind;
      if (data.cfxState === undefined) data.cfxState = item.state;
      if (data.cfxRole === 'gauge') data.cfxPoint = '0';


      if (data.cfxDerived === 'missing-category-zero') {
        data.cfxRegion = item.key + ':category:' + Number(data.cfxCategory);
        delete data.cfxPoint;
        delete data.cfxSourcePoint;
        delete data.cfxValue;
        delete data.cfxY;
        data.cfxLabel = item.name + ' / Category ' + data.cfxCategory + ' / No observation';
        data.cfxStatus = 'No observation';
      }
      if (data.cfxPoint !== undefined && data.cfxSourcePoint === undefined && !hasDerivedPointIdentity(node)) {
        const point = Number(data.cfxPoint);
        data.cfxSourcePoint = String(item.indices[point] === undefined ? point : item.indices[point]);
      }
      if (data.cfxPoint !== undefined && data.cfxXLabel === undefined) {
        const label = data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : xLabels.get(Number(data.cfxX));
        if (label !== undefined) data.cfxXLabel = label;
      }
      if (data.cfxRole === 'legend-item' || data.cfxRole === 'legend-entry-omitted') return;
      const region = regions.get(data.cfxSourceId || '');

      if (region && data.cfxPoint === undefined && data.cfxValue !== undefined && data.cfxPercent !== undefined
        && !node.hasAttribute('aria-label') && !node.hasAttribute('data-cfx-label'))
        node.setAttribute('aria-label', region.label || item.name);
      if (!region || (data.cfxPoint === undefined && data.cfxRegion === undefined)) return;
      node._cfxNativeRegion = Object.freeze({ x: region.x, y: region.y, width: region.width, height: region.height });
      node._cfxFillInteriors = Object.freeze((region.fillInteriors || []).map(candidate => Object.freeze(candidate.slice())));
      if (pairedExtentObservation(node)) node._cfxPairedRegion = region;
      if (isPassiveTarget(node) || data.cfxVisible === 'false') return;
      const box = node.getBBox();
      if (['line', 'stepline', 'area', 'steparea', 'stackedarea', 'trendline', 'slope'].includes(item.kind)) {
        const marker = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        marker.setAttribute('cx', region.x + region.width / 2); marker.setAttribute('cy', region.y + region.height / 2);
        marker.setAttribute('r', '4'); marker.setAttribute('class', 'cfx-prepared-point-marker');
        marker.setAttribute('pointer-events', 'none');
        const owner = node.closest('[data-cfx-role="series"]');
        const line = owner && owner.querySelector('[data-cfx-role="line"]');
        marker.setAttribute('fill', line ? getComputedStyle(line).stroke : 'currentColor');
        node.appendChild(marker);
      }



      if (retainedGeometryFact(node)) return;

      const hollowCandle = data.cfxRole === 'point' && (data.cfxKind || '').toLowerCase() === 'candlestick'
        && node.querySelector('[data-cfx-role="candlestick-body"][fill="none"]');
      if (box.width > 0 && box.height > 0 && !hollowCandle) return;
      appendNativeBrowserHitArea(node, region, 8);
    });
    const marks = new Map(Array.from(svg.querySelectorAll('[data-cfx-source-id]')).map(node => [node.dataset.cfxSourceId, node]));
    svg.querySelectorAll('[data-cfx-label-for]').forEach(label => {
      const mark = marks.get(label.dataset.cfxLabelFor);
      const point = mark && mark.closest('[data-cfx-point],[data-cfx-target-kind]');
      if (!point || point.closest('[data-cfx-role="legend-item"]') || !label.querySelector('text,[data-cfx-native-text-ink]')) return;
      label._cfxLabelTarget = point;
      (point._cfxPointLabels ||= []).push(label);
    });
  };
