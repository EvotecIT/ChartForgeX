  // Core exports describe immutable source identity and layout. Browser-only focus and hit areas belong here.
  const prepareChartTargets = (root) => {
    const svg = root.querySelector('.cfx-stage svg');
    if (!svg || !root.dataset.cfxPreparedChart) return;
    let metadata;
    try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch (_) { return; }
    const series = metadata.series || [];
    const xLabels = new Map((metadata.xLabels || []).map((item) => [item.value, item.text]));
    const regions = new Map((metadata.regions || []).map((region) => [region.id, region]));
    series.forEach((item, index) => {
      svg.setAttribute('data-cfx-series-name-' + index, item.name);
      svg.setAttribute('data-cfx-series-key-' + index, item.key);
      svg.setAttribute('data-cfx-series-state-' + index, item.state);
      svg.setAttribute('data-cfx-series-source-indices-' + index, item.indices.join(','));
    });
    const sourceCollections = new Map(Array.from(svg.querySelectorAll('[data-cfx-point][data-cfx-source-points]'))
      .map((node) => [node.dataset.cfxSeries + ':' + node.dataset.cfxPoint, node.dataset.cfxSourcePoints]));
    svg.querySelectorAll('[data-cfx-role="legend-entry"]').forEach((node) => {
      const data = node.dataset;
      const source = data.cfxSourceId || '';
      const match = source.match(/^legend-series-(\d+)(?:-point-(\d+|other))?$/);
      const index = match ? Number(match[1]) : series.findIndex((item) => item.key === data.cfxSeriesKey);
      if (index < 0) return;
      data.cfxRole = 'legend-item'; data.cfxSeries = String(index);
      const region = regions.get(source);
      data.cfxLabel = region ? region.label : node.getAttribute('aria-label') || '';
      if (match && match[2] !== undefined) {
        data.cfxPoint = match[2] === 'other' ? '-1' : match[2];
        const collectionKey = index + ':' + data.cfxPoint;
        if (data.cfxSourcePoints === undefined && sourceCollections.has(collectionKey))
          data.cfxSourcePoints = sourceCollections.get(collectionKey);
      }
    });
    svg.querySelectorAll('[data-cfx-point],[data-cfx-series],[data-cfx-role="gauge"]').forEach((node) => {
      const data = node.dataset;
      if (data.cfxSeries === undefined) {
        const owner = node.parentElement && node.parentElement.closest('[data-cfx-series]');
        data.cfxSeries = owner ? owner.dataset.cfxSeries : '0';
      }
      const item = series[Number(data.cfxSeries)];
      if (!item) return;
      data.cfxSeriesName = item.name; data.cfxSeriesKey = item.key;
      data.cfxKind = item.kind;
      if (data.cfxState === undefined) data.cfxState = item.state;
      if (data.cfxRole === 'gauge') data.cfxPoint = '0';
      // A zero-imputed category has no authored observation. Category identity remains stable
      // when a peer has another category or orders its authored observations differently.
      if (data.cfxDerived === 'missing-category-zero') {
        data.cfxRegion = item.key + ':category:' + Number(data.cfxCategory);
        delete data.cfxPoint;
        delete data.cfxSourcePoint;
        delete data.cfxValue;
        delete data.cfxY;
        data.cfxLabel = item.name + ' / Category ' + data.cfxCategory + ' / No observation';
        data.cfxStatus = 'No observation';
      }
      if (data.cfxPoint !== undefined && data.cfxSourcePoint === undefined && data.cfxSourcePoints === undefined) {
        const point = Number(data.cfxPoint);
        data.cfxSourcePoint = String(item.indices[point] === undefined ? point : item.indices[point]);
      }
      if (data.cfxPoint !== undefined && !data.cfxXLabel) data.cfxXLabel = xLabels.get(Number(data.cfxX)) || '';
      if (data.cfxRole === 'legend-item') return;
      const region = regions.get(data.cfxSourceId || '');
      if (!region || (data.cfxPoint === undefined && data.cfxRegion === undefined)) return;
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
      // Marker-free lines still expose their observations to pointer, keyboard, lasso and crosshair tools.
      // Empty or zero-sized native marks get a minimum eight-unit transparent browser target.
      if (box.width > 0 && box.height > 0) return;
      const hit = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
      const width = Math.max(8, region.width); const height = Math.max(8, region.height);
      hit.setAttribute('x', region.x + (region.width - width) / 2);
      hit.setAttribute('y', region.y + (region.height - height) / 2);
      hit.setAttribute('width', width); hit.setAttribute('height', height);
      hit.setAttribute('fill', 'transparent'); hit.setAttribute('pointer-events', 'all');
      hit.setAttribute('data-cfx-browser-hit-area', 'true');
      node.appendChild(hit);
    });
  };
