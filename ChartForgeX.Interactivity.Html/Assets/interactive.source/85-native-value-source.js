  // Raw rows are detached native facts, not synthetic interactive paint. No geometry, sums or formatting is reconstructed here.
  const nativeRawSource = section => JSON.parse(section.dataset.cfxPreparedChart).nativeValueSource;
  const validateNativeValueSourceSection = (section, metadata) => {
    const source = metadata.nativeValueSource;
    if (source === undefined) return;
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
    if (!source || source.readings.length !== readings.length || source.readings.some((row, index) => {
      const fact = readings[index]; return row.seriesKey !== fact.seriesKey || row.observationId !== fact.observationId
        || fact.minimum !== 0 || fact.maximum !== Number.MAX_VALUE || endpoint && row.value !== fact[endpoint]
        || row.value < fact.minimum || row.value > fact.maximum;
    })) throw new TypeError('Native transition: exact raw reading source/domain/value required.');
  };
