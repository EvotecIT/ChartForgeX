  // Raw rows are detached native facts, not synthetic interactive paint. No geometry, sums or formatting is reconstructed here.
  const nativeRawSource = section => JSON.parse(section.dataset.cfxPreparedChart).nativeValueSource;
  // Intrinsic sign/zero policy is independent of the finite display window. No transform is evaluated here.
  const nativeComponentMemberValid = (axis, value) => !!axis && Number.isFinite(value)
    && (axis.scale !== 'logarithmic' || (axis.logarithmicDomain === 'negative' ? value < 0 : value > 0));
  const validateNativeComponentAxes = section => {
    const x = chartAxisProjection(section, 'x');
    if (!x) throw new TypeError('Section: captured native component X axis required.');
    for (const point of section.querySelectorAll('.cfx-stage [data-cfx-role="point"]')) {
      const y = chartAxisProjection(section, point.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y');
      const rawX = point.getAttribute('data-cfx-x');
      if (!rawX?.trim() || !nativeComponentMemberValid(x, Number(rawX))) throw new TypeError('Section: finite intrinsically valid native component X required.');
      if (!y || y.scale === 'time' || x.scale === 'time' && (point.hasAttribute('data-cfx-x-lower') || point.hasAttribute('data-cfx-x-upper')))
        throw new TypeError('Section: component values require numeric member axes and only fixed temporal X.');
      if (x.scale === 'time' ? point.dataset.cfxXScale !== 'time' : point.dataset.cfxXScale === 'time')
        throw new TypeError('Section: captured native component X scale metadata mismatch.');
    }
  };
  const validateNativeComponentCoordinateFacts = (section, basis) => {
    for (const name of ['x', 'y', 'secondaryX', 'secondaryY'])
      if (JSON.stringify(chartAxisProjection(section, name)) !== JSON.stringify(chartAxisProjection(basis, name)))
        throw new TypeError('Native transition: exact native component axis/session/calendar facts required.');
    const points = [...section.querySelectorAll('.cfx-stage [data-cfx-role="point"]')], originals = [...basis.querySelectorAll('.cfx-stage [data-cfx-role="point"]')];
    if (points.length !== originals.length) throw new TypeError('Native transition: exact native component coordinate catalog required.');
    const matched = new Set();
    for (const point of points) {
      const expected = originals.filter(row => row.dataset.cfxSeriesKey === point.dataset.cfxSeriesKey && row.dataset.cfxObservationId === point.dataset.cfxObservationId);
      if (expected.length !== 1 || matched.has(expected[0]) || ['data-cfx-x', 'data-cfx-x-scale', 'data-cfx-x-instant'].some(name => point.getAttribute(name) !== expected[0].getAttribute(name)))
        throw new TypeError('Native transition: fixed native component X/instant facts required.');
      matched.add(expected[0]);
    }
  };
  // Component identity is native (series, observation, member); encoded tuple slots never become observations.
  const validateComponentValueSourceSection = (section, metadata, source) => {
    const keys = (value, names) => value && typeof value === 'object' && !Array.isArray(value) && Object.keys(value).every(key => names.includes(key));
    if (!keys(source, ['kind', 'sourceKey', 'structureKey', 'mapping', 'reversed', 'readings'])
        || !/^[a-f0-9]{64}$/.test(source.sourceKey) || !/^[a-f0-9]{64}$/.test(source.structureKey)
        || !['linear-area', 'sqrt-radius-range'].includes(source.mapping) || typeof source.reversed !== 'boolean'
        || metadata.series.some(row => row.local || row.kind !== 'bubble')
        || !Array.isArray(source.readings) || !source.readings.length || source.readings.length > 600000)
      throw new TypeError('Section: finite captured native Bubble component source required.');
    const expected = metadata.series.flatMap(series => (series.observationIds || []).flatMap((row, index) =>
      ['y', 'size'].map(component => ({ ...row, ordinal: index, seriesKey: series.key, component }))));
    const painted = [...section.querySelectorAll('.cfx-stage [data-cfx-role="point"]')], matched = new Set(), identities = new Set();
    let sizeDomain = null;
    if (expected.length !== source.readings.length || painted.length * 2 !== expected.length)
      throw new TypeError('Section: exact native Bubble component catalog required.');
    for (let index = 0; index < expected.length; index++) {
      const row = expected[index], fact = source.readings[index], token = JSON.stringify([row.seriesKey, row.id, row.component]);
      const matches = painted.filter(node => node.dataset.cfxSeriesKey === row.seriesKey && node.dataset.cfxObservationId === row.id);
      if (typeof row.id !== 'string' || !row.id || row.index !== row.ordinal || identities.has(token)
          || !keys(fact, ['seriesKey', 'observationId', 'component', 'value', 'minimum', 'maximum'])
          || fact.seriesKey !== row.seriesKey || fact.observationId !== row.id || fact.component !== row.component
          || ![fact.value, fact.minimum, fact.maximum].every(Number.isFinite) || fact.maximum <= fact.minimum || matches.length !== 1)
        throw new TypeError('Section: exact ordered authored component readings required.');
      identities.add(token); const mark = matches[0];
      const attribute = mark.getAttribute('data-cfx-' + row.component);
      if (!attribute?.trim() || Number(attribute) !== fact.value || mark.dataset.cfxSizeMapping !== source.mapping)
        throw new TypeError('Section: native component value/size mapping mismatch.');
      if (row.component === 'y') {
        const axis = chartAxisProjection(section, mark.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y');
        if (!nativeComponentMemberValid(axis, fact.value) || axis.minimum !== fact.minimum || axis.maximum !== fact.maximum)
          throw new TypeError('Section: native Bubble Y domain mismatch.');
      } else {
        if (fact.value <= 0 || fact.minimum < 0 || sizeDomain && (sizeDomain.minimum !== fact.minimum || sizeDomain.maximum !== fact.maximum))
          throw new TypeError('Section: positive raw Size and one captured size domain required.');
        sizeDomain = fact;
      }
      matched.add(mark);
    }
    if (matched.size !== painted.length) throw new TypeError('Section: foreign native Bubble point.');
  };
  const validateRangeValueSourceSection = (section, metadata, source) => {
    const keys = (value, names) => value && typeof value === 'object' && !Array.isArray(value) && Object.keys(value).every(key => names.includes(key));
    if (!keys(source, ['kind', 'sourceKey', 'structureKey', 'readings']) || !/^[a-f0-9]{64}$/.test(source.sourceKey) || !/^[a-f0-9]{64}$/.test(source.structureKey)
        || metadata.series.some(row => row.local || !['rangeband', 'rangearea'].includes(row.kind))
        || !Array.isArray(source.readings) || !source.readings.length || source.readings.length > 600000)
      throw new TypeError('Section: finite captured native range component source required.');
    const expected = metadata.series.flatMap(series => (series.observationIds || []).flatMap((row, index) =>
      ['lower', 'upper'].map(component => ({ ...row, ordinal: index, seriesKey: series.key, component }))));
    const points = [...section.querySelectorAll('.cfx-stage [data-cfx-role="point"]')], matched = new Set(), identities = new Set();
    if (expected.length !== source.readings.length || points.length * 2 !== expected.length)
      throw new TypeError('Section: exact native range component catalog required.');
    for (let index = 0; index < expected.length; index++) {
      const row = expected[index], fact = source.readings[index], token = JSON.stringify([row.seriesKey, row.id, row.component]);
      const matches = points.filter(node => node.dataset.cfxSeriesKey === row.seriesKey && node.dataset.cfxObservationId === row.id);
      if (typeof row.id !== 'string' || !row.id || row.index !== row.ordinal || identities.has(token)
          || !keys(fact, ['seriesKey', 'observationId', 'component', 'value', 'minimum', 'maximum'])
          || fact.seriesKey !== row.seriesKey || fact.observationId !== row.id || fact.component !== row.component
          || ![fact.value, fact.minimum, fact.maximum].every(Number.isFinite) || fact.maximum <= fact.minimum || matches.length !== 1)
        throw new TypeError('Section: exact ordered native lower/upper readings required.');
      identities.add(token); const mark = matches[0], attribute = mark.getAttribute('data-cfx-' + row.component);
      const axis = chartAxisProjection(section, mark.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y');
      if (!attribute?.trim() || Number(attribute) !== fact.value || !nativeComponentMemberValid(axis, fact.value) || axis.minimum !== fact.minimum || axis.maximum !== fact.maximum
          || row.component === 'upper' && source.readings[index - 1].value > fact.value)
        throw new TypeError('Section: native range tuple/domain/value mismatch.');
      matched.add(mark);
    }
    if (matched.size !== points.length) throw new TypeError('Section: foreign native range point.');
  };
  const validateErrorValueSourceSection = (section, metadata, source) => {
    const keys = (value, names) => value && typeof value === 'object' && !Array.isArray(value) && Object.keys(value).every(key => names.includes(key));
    if (!keys(source, ['kind', 'sourceKey', 'structureKey', 'readings']) || !/^[a-f0-9]{64}$/.test(source.sourceKey) || !/^[a-f0-9]{64}$/.test(source.structureKey)
        || metadata.series.some(row => row.local || row.kind !== 'errorbar') || !Array.isArray(source.readings) || !source.readings.length || source.readings.length > 600000)
      throw new TypeError('Section: finite captured native uncertainty source required.');
    const observations = metadata.series.flatMap(series => (series.observationIds || []).map((row, index) => ({ ...row, ordinal: index, seriesKey: series.key })));
    const points = [...section.querySelectorAll('.cfx-stage [data-cfx-role="point"]')], matched = new Set(), identities = new Set(); let offset = 0;
    if (points.length !== observations.length) throw new TypeError('Section: exact native uncertainty point catalog required.');
    for (const observation of observations) {
      const matches = points.filter(node => node.dataset.cfxSeriesKey === observation.seriesKey && node.dataset.cfxObservationId === observation.id);
      if (typeof observation.id !== 'string' || !observation.id || observation.index !== observation.ordinal || matches.length !== 1 || matched.has(matches[0]))
        throw new TypeError('Section: exact authored uncertainty observation required.');
      const point = matches[0], hasX = point.hasAttribute('data-cfx-x-lower'), hasY = point.hasAttribute('data-cfx-y-lower');
      if (hasX !== point.hasAttribute('data-cfx-x-upper') || hasY !== point.hasAttribute('data-cfx-y-upper') || !hasX && !hasY)
        throw new TypeError('Section: actual native nullable uncertainty intervals required.');
      const members = ['value', ...(hasX ? ['x-lower', 'x-upper'] : []), ...(hasY ? ['y-lower', 'y-upper'] : [])], values = new Map();
      for (const component of members) {
        const fact = source.readings[offset++], token = JSON.stringify([observation.seriesKey, observation.id, component]);
        const axis = chartAxisProjection(section, component.startsWith('x-') ? 'x' : point.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y');
        const attribute = point.getAttribute('data-cfx-' + component);
        if (!keys(fact, ['seriesKey', 'observationId', 'component', 'value', 'minimum', 'maximum']) || fact.seriesKey !== observation.seriesKey || fact.observationId !== observation.id
            || fact.component !== component || identities.has(token) || ![fact.value, fact.minimum, fact.maximum].every(Number.isFinite) || fact.maximum <= fact.minimum
            || !nativeComponentMemberValid(axis, fact.value) || axis.minimum !== fact.minimum || axis.maximum !== fact.maximum
            || !attribute?.trim() || Number(attribute) !== fact.value)
          throw new TypeError('Section: native uncertainty member/domain/value mismatch.');
        identities.add(token); values.set(component, fact.value);
      }
      const x = point.getAttribute('data-cfx-x'), y = point.getAttribute('data-cfx-y');
      if (!x?.trim() || !Number.isFinite(Number(x)) || !y?.trim() || Number(y) !== values.get('value')
          || hasX && (values.get('x-lower') > Number(x) || values.get('x-upper') < Number(x))
          || hasY && (values.get('y-lower') > values.get('value') || values.get('y-upper') < values.get('value')))
        throw new TypeError('Section: native uncertainty intervals must contain their actual estimate.');
      matched.add(point);
    }
    if (offset !== source.readings.length || matched.size !== points.length) throw new TypeError('Section: foreign uncertainty member or point.');
  };
  const validateFinancialValueSourceSection = (section, metadata, source) => {
    const keys = (value, names) => value && typeof value === 'object' && !Array.isArray(value) && Object.keys(value).every(key => names.includes(key));
    if (!keys(source, ['kind', 'sourceKey', 'structureKey', 'readings']) || !/^[a-f0-9]{64}$/.test(source.sourceKey) || !/^[a-f0-9]{64}$/.test(source.structureKey)
        || metadata.series.some(row => row.local || !['candlestick', 'ohlc'].includes(row.kind)) || !Array.isArray(source.readings) || !source.readings.length || source.readings.length > 600000)
      throw new TypeError('Section: finite captured native financial source required.');
    const observations = metadata.series.flatMap(series => (series.observationIds || []).map((row, index) => ({ ...row, ordinal: index, seriesKey: series.key })));
    const points = [...section.querySelectorAll('.cfx-stage [data-cfx-role="point"]')], matched = new Set(), identities = new Set(); let offset = 0;
    if (points.length !== observations.length) throw new TypeError('Section: exact native financial point catalog required.');
    for (const observation of observations) {
      const matches = points.filter(node => node.dataset.cfxSeriesKey === observation.seriesKey && node.dataset.cfxObservationId === observation.id);
      if (typeof observation.id !== 'string' || !observation.id || observation.index !== observation.ordinal || matches.length !== 1 || matched.has(matches[0]))
        throw new TypeError('Section: exact authored financial observation required.');
      const point = matches[0], values = new Map();
      const axis = chartAxisProjection(section, point.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y');
      for (const component of ['open', 'high', 'low', 'close']) {
        const fact = source.readings[offset++], token = JSON.stringify([observation.seriesKey, observation.id, component]), attribute = point.getAttribute('data-cfx-' + component);
        if (!keys(fact, ['seriesKey', 'observationId', 'component', 'value', 'minimum', 'maximum']) || fact.seriesKey !== observation.seriesKey || fact.observationId !== observation.id
            || fact.component !== component || identities.has(token) || ![fact.value, fact.minimum, fact.maximum].every(Number.isFinite) || fact.maximum <= fact.minimum
            || !nativeComponentMemberValid(axis, fact.value) || axis.minimum !== fact.minimum || axis.maximum !== fact.maximum
            || !attribute?.trim() || Number(attribute) !== fact.value)
          throw new TypeError('Section: native financial member/domain/value mismatch.');
        identities.add(token); values.set(component, fact.value);
      }
      const x = point.getAttribute('data-cfx-x');
      if (!x?.trim() || !Number.isFinite(Number(x)) || values.get('high') < Math.max(values.get('open'), values.get('low'), values.get('close'))
          || values.get('low') > Math.min(values.get('open'), values.get('high'), values.get('close')))
        throw new TypeError('Section: native financial high/low must contain their actual open and close.');
      matched.add(point);
    }
    if (offset !== source.readings.length || matched.size !== points.length) throw new TypeError('Section: foreign financial member or point.');
  };
  const validateNativeValueSourceSection = (section, metadata) => {
    const source = metadata.nativeValueSource;
    if (['bubble-components', 'range-components', 'error-components', 'financial-components'].includes(source?.kind)) validateNativeComponentAxes(section);
    if (source === undefined) return;
    if (source?.kind === 'bubble-components') { validateComponentValueSourceSection(section, metadata, source); return; }
    if (source?.kind === 'range-components') { validateRangeValueSourceSection(section, metadata, source); return; }
    if (source?.kind === 'error-components') { validateErrorValueSourceSection(section, metadata, source); return; }
    if (source?.kind === 'financial-components') { validateFinancialValueSourceSection(section, metadata, source); return; }
    const keys = (value, names) => value && typeof value === 'object' && !Array.isArray(value) && Object.keys(value).every(key => names.includes(key));
    if (!keys(source, ['kind', 'sourceKey', 'structureKey', 'readings', 'slices']) || source.kind !== 'radial'
        || !/^[a-f0-9]{64}$/.test(source.sourceKey) || !/^[a-f0-9]{64}$/.test(source.structureKey)
        || metadata.series.some(row => row.local || !['pie', 'donut'].includes(row.kind))
        || !Array.isArray(source.readings) || source.readings.length > 600000 || !Array.isArray(source.slices) || source.slices.length > 600000)
      throw new TypeError('Section: finite captured native raw source required.');
    const expected = metadata.series.flatMap(series => (series.observationIds || []).map((row, index) => ({ ...row, ordinal: index, seriesKey: series.key })));
    if (expected.length !== source.readings.length || expected.some((row, index) => typeof row.id !== 'string' || !row.id || row.index !== row.ordinal
        || !keys(source.readings[index], ['seriesKey', 'observationId', 'value']) || row.seriesKey !== source.readings[index].seriesKey || row.id !== source.readings[index].observationId
        || !Number.isFinite(source.readings[index].value) || source.readings[index].value < 0)
        || new Set(expected.map(row => JSON.stringify([row.seriesKey, row.id]))).size !== expected.length)
      throw new TypeError('Section: exact ordered authored raw rows required.');
    const counts = new Map(metadata.series.map(series => [series.key, expected.filter(row => row.seriesKey === series.key).length]));
    const tokens = new Set(), contributors = new Set(), matched = new Set();
    const painted = [...section.querySelectorAll('.cfx-stage [data-cfx-role="radial-point"]')];
    for (const slice of source.slices) {
      const token = slice && JSON.stringify([slice.seriesKey, slice.pointIndex]);
      if (!keys(slice, ['seriesKey', 'pointIndex', 'sourceIndices', 'paintedMetadata']) || !counts.has(slice.seriesKey) || !Number.isSafeInteger(slice.pointIndex) || slice.pointIndex < -1 || slice.pointIndex >= counts.get(slice.seriesKey)
          || tokens.has(token) || !Array.isArray(slice.sourceIndices) || !slice.sourceIndices.length
          || slice.sourceIndices.some(index => !Number.isSafeInteger(index) || index < 0 || index >= counts.get(slice.seriesKey))
          || new Set(slice.sourceIndices).size !== slice.sourceIndices.length || slice.pointIndex >= 0 && slice.sourceIndices.length !== 1)
        throw new TypeError('Section: exact native slice contributor identity required.');
      tokens.add(token);
      for (const index of slice.sourceIndices) {
        const key = JSON.stringify([slice.seriesKey, index]);
        if (contributors.has(key)) throw new TypeError('Section: duplicate native slice contributor.');
        contributors.add(key);
      }
      if (slice.paintedMetadata === null) continue;
      if (!slice.paintedMetadata || typeof slice.paintedMetadata !== 'object' || Array.isArray(slice.paintedMetadata) || Object.entries(slice.paintedMetadata).some(([key, value]) => !key.startsWith('data-cfx-') || typeof value !== 'string'))
        throw new TypeError('Section: native completed slice paint metadata required.');
      const matches = painted.filter(node => node.dataset.cfxSeriesKey === slice.seriesKey && node.dataset.cfxPoint === String(slice.pointIndex)
        && node.dataset.cfxSourcePoints === slice.sourceIndices.join(','));
      if (matches.length !== 1 || matched.has(matches[0]) || Object.entries(slice.paintedMetadata).some(([key, value]) => matches[0].getAttribute(key) !== value))
        throw new TypeError('Section: native raw slice/paint owner mismatch.');
      const mark = matches[0], original = slice.sourceIndices[0], reading = source.readings.find(row => row.seriesKey === slice.seriesKey && row.observationId === expected.find(row => row.seriesKey === slice.seriesKey && row.index === original)?.id);
      if (slice.pointIndex === -1 ? mark.dataset.cfxObservationId !== undefined : !reading || mark.dataset.cfxObservationId !== reading.observationId || Number(mark.dataset.cfxValue) !== reading.value)
        throw new TypeError('Section: native raw slice observation/value mismatch.');
      matched.add(mark);
    }
    if (matched.size !== painted.length) throw new TypeError('Section: foreign raw native slice paint.');
  };
  const validateRawTransitionReadings = (section, readings, endpoint) => {
    const source = section instanceof Element ? nativeRawSource(section) : section;
    if (['bubble-components', 'range-components', 'error-components', 'financial-components'].includes(source?.kind)) {
      if (source.readings.length !== readings.length || source.readings.some((row, index) => {
        const fact = readings[index]; return row.seriesKey !== fact.seriesKey || row.observationId !== fact.observationId || row.component !== fact.component
          || row.minimum !== fact.minimum || row.maximum !== fact.maximum || endpoint && row.value !== fact[endpoint]
          || !Number.isFinite(row.value) || row.component === 'size' && row.value <= 0;
      })) throw new TypeError('Native transition: exact native component source/domain/value required.');
      return;
    }
    if (!source || source.readings.length !== readings.length || source.readings.some((row, index) => {
      const fact = readings[index]; return row.seriesKey !== fact.seriesKey || row.observationId !== fact.observationId
        || fact.minimum !== 0 || fact.maximum !== Number.MAX_VALUE || endpoint && row.value !== fact[endpoint]
        || row.value < fact.minimum || row.value > fact.maximum;
    })) throw new TypeError('Native transition: exact raw reading source/domain/value required.');
  };
