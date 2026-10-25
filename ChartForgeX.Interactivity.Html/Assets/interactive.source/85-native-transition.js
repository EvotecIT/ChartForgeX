  // Captured native presentation only; acceptance, scene revision and scheduling remain with the incumbent owners.
  const rememberNativeDrawingSnapshot = (root, lifetime, source) => {
    const snapshot = lifetime.adaptive
      ? Object.freeze({ wide: lifetime.adaptive.wide, narrow: lifetime.adaptive.narrow })
      : Object.freeze({ wide: adaptiveSectionCopy(source), narrow: null });
    lifetime.nativeDrawingSnapshot = snapshot;
    ownChartWork(root, () => { if (lifetime.nativeDrawingSnapshot === snapshot) lifetime.nativeDrawingSnapshot = null; });
  };
  const parseCapturedNativeSvg = (markup, prefix) => {
    if (typeof markup !== 'string' || typeof prefix !== 'string' || !/^[A-Za-z][A-Za-z0-9_.-]*$/.test(prefix))
      throw new TypeError('Native transition: captured SVG and exact generated prefix required.');
    const parsed = new DOMParser().parseFromString(markup, 'image/svg+xml'), svg = parsed.documentElement;
    if (parsed.querySelector('parsererror') || svg.localName !== 'svg' || svg.namespaceURI !== 'http://www.w3.org/2000/svg'
        || svg.querySelector('script,foreignObject')
        || [svg, ...svg.querySelectorAll('*')].some(node => [...node.attributes].some(attribute => /^on/i.test(attribute.name))))
      throw new TypeError('Native transition: complete native SVG frame required.');
    // Validate every generated definition/reference before any comparison or presentation clone is installed.
    canonicalNativeExport(svg, prefix);
    return svg;
  };
  const canonicalNativeExport = (source, prefix, nodeIndexOffset = 0) => {
    if (nodeIndexOffset !== 0 && nodeIndexOffset !== 1) throw new TypeError('Native transition: unknown native scene-index comparison offset.');
    if (typeof prefix !== 'string' || !/^[A-Za-z][A-Za-z0-9_.-]*$/.test(prefix))
      throw new TypeError('Native transition: exact captured generated prefix required.');
    const svg = source.cloneNode(true), ids = new Map(), normalizedIds = new Set();
    const suffix = id => {
      if (!id.startsWith(prefix + '-')) throw new TypeError('Native transition: generated ID prefix mismatch.');
      const value = id.slice(prefix.length + 1);
      if (!/^(?:title|description|(?:node|clip|gradient|pattern-tile|png-glyph|surface-shadow|retained-mask|retained-outline|retained-bloom|retained-shadow|retained-inner-shadow)-(?:0|[1-9]\d*))$/.test(value))
        throw new TypeError('Native transition: unknown generated resource identity.');
      return value;
    };
    const kind = value => value === 'title' ? 'title' : value === 'description' ? 'desc'
      : value.startsWith('clip-') ? 'clipPath' : value.startsWith('gradient-') ? 'linearGradient'
      : value.startsWith('pattern-tile-') ? 'pattern' : value.startsWith('png-glyph-') ? 'path'
      : value.startsWith('surface-shadow-') || value.startsWith('retained-outline-') || value.startsWith('retained-bloom-') || value.startsWith('retained-shadow-') || value.startsWith('retained-inner-shadow-')
        ? 'filter' : value.startsWith('retained-mask-') ? 'mask' : null;
    for (const node of [svg, ...svg.querySelectorAll('*')]) if (node.hasAttribute('id')) {
      const id = node.getAttribute('id'), value = suffix(id), expected = kind(value);
      if (ids.has(id) || expected && node.localName !== expected)
        throw new TypeError('Native transition: duplicate or wrong-kind generated identity.');
      let normalized = value;
      const indexed = /^(node|clip|gradient|surface-shadow|retained-mask|retained-outline|retained-bloom|retained-shadow|retained-inner-shadow)-(0|[1-9]\d*)$/.exec(value);
      if (nodeIndexOffset && indexed) {
        const number = Number(indexed[2]);
        if (!Number.isSafeInteger(number) || number < 1 || indexed[1] === 'node' && !node.getAttribute('data-cfx-source-id'))
          throw new TypeError('Native transition: unproven or zero native scene-index resource.');
        normalized = indexed[1] + '-' + (number - 1);
      }
      if (normalizedIds.has(normalized)) throw new TypeError('Native transition: native resource comparison is not bijective.');
      normalizedIds.add(normalized); ids.set(id, { node, suffix: normalized });
    }
    const reference = (id, allowed) => {
      const value = suffix(id), found = ids.get(id);
      if (!found || !allowed.some(name => value === name || value.startsWith(name + '-')))
        throw new TypeError('Native transition: dangling or wrong-kind generated reference.');
      return 'cfx-native-export-check-' + found.suffix;
    };
    for (const node of [svg, ...svg.querySelectorAll('*')]) {
      const opacity = node.getAttribute('opacity');
      if (opacity !== null && (!opacity.trim() || !Number.isFinite(Number(opacity)) || Number(opacity) < 0 || Number(opacity) > 1))
        throw new TypeError('Native transition: invalid native opacity.');
      if (node.hasAttribute('id')) node.setAttribute('id', 'cfx-native-export-check-' + ids.get(node.getAttribute('id')).suffix);
      for (const [attribute, allowed] of [['clip-path', ['clip']], ['fill', ['gradient', 'pattern-tile']], ['stroke', ['gradient', 'pattern-tile']],
          ['filter', ['surface-shadow', 'retained-outline', 'retained-bloom', 'retained-shadow', 'retained-inner-shadow']], ['mask', ['retained-mask']]]) {
        const value = node.getAttribute(attribute); if (!value?.includes('url(')) continue;
        const match = /^url\(#([^()\s]+)\)$/.exec(value);
        if (!match) throw new TypeError('Native transition: native resource reference shape changed.');
        node.setAttribute(attribute, 'url(#' + reference(match[1], allowed) + ')');
      }
      if (node.localName === 'use') {
        const value = node.getAttribute('href');
        if (!value?.startsWith('#') || value.length < 2) throw new TypeError('Native transition: captured native glyph reference required.');
        node.setAttribute('href', '#' + reference(value.slice(1), ['png-glyph']));
      }
      for (const [attribute, expected] of [['aria-labelledby', 'title'], ['aria-describedby', 'description']]) if (node.hasAttribute(attribute))
        node.setAttribute(attribute, reference(node.getAttribute(attribute), [expected]));
      if (node.hasAttribute('data-cfx-png-glyphs')) {
        const value = node.getAttribute('data-cfx-png-glyphs');
        const rows = value ? value.split('|') : [];
        node.setAttribute('data-cfx-png-glyphs', rows.map(row => {
          const separator = row.indexOf(';');
          if (separator <= 0 || !/^matrix\([^;|]+\)$/.test(row.slice(separator + 1)))
            throw new TypeError('Native transition: native glyph capture slot changed.');
          return reference(row.slice(0, separator), ['png-glyph']) + row.slice(separator);
        }).join('|'));
      }
    }
    // Authored a[href], semantic IDs/data, roles, source values, text, paint, transforms and glyph matrices are untouched.
    return svg;
  };
  const nativeExportEquals = (left, leftPrefix, right, rightPrefix) =>
    canonicalNativeExport(left, leftPrefix).isEqualNode(canonicalNativeExport(right, rightPrefix));
  const validateReflowFrameFacts = frames => {
    const zero = frames.findIndex(frame => frame?.basis === 'destination');
    if (zero <= 0 || zero >= frames.length - 1 || Number(frames[0]?.nativeOpacity) !== 1
        || Number(frames[frames.length - 1]?.nativeOpacity) !== 1 || Number(frames[zero]?.nativeOpacity) !== 0)
      throw new TypeError('Native transition: captured source fade, zero switch and exact destination required.');
    for (let index = 0; index < frames.length; index++) {
      const frame = frames[index], opacity = frame?.nativeOpacity;
      if (typeof opacity !== 'string' || !opacity.trim() || !Number.isFinite(Number(opacity)) || Number(opacity) < 0 || Number(opacity) > 1
          || frame.basis !== (index < zero ? 'source' : 'destination')
          || index && index < zero && Number(opacity) >= Number(frames[index - 1].nativeOpacity)
          || index > zero && Number(opacity) <= Number(frames[index - 1].nativeOpacity))
        throw new TypeError('Native transition: ordered immutable Reflow frame basis/opacity facts required.');
    }
  };
  const validateReflowFrameInk = (svg, frame, basis, basisPrefix) => {
    const copy = svg.cloneNode(true), opacity = Number(frame.nativeOpacity);
    const wrappers = [...copy.children].filter(node => node.localName === 'g' && node.getAttribute('data-cfx-role') === 'native-transition-opacity');
    if (opacity < 1) {
      if (wrappers.length !== 1 || wrappers[0].getAttribute('opacity') !== frame.nativeOpacity
          || [...wrappers[0].attributes].some(attribute => !['data-cfx-role', 'opacity'].includes(attribute.name)))
        throw new TypeError('Native transition: exact whole-visual isolated Reflow opacity required.');
      const wrapper = wrappers[0];
      while (wrapper.firstChild) wrapper.parentNode.insertBefore(wrapper.firstChild, wrapper);
      wrapper.remove();
    } else if (wrappers.length) throw new TypeError('Native transition: unwrapped native endpoint required.');
    if (!canonicalNativeExport(copy, frame.prefix, opacity < 1 ? 1 : 0).isEqualNode(canonicalNativeExport(basis.querySelector('.cfx-stage svg'), basisPrefix)))
      throw new TypeError('Native transition: frame ink must equal its captured source or destination native scene.');
  };
  const captureNativeTransitionMotion = (root, next, payload, request, adaptive, initial = false) => {
    const lifetime = chartLifetimes.get(root), snapshot = lifetime?.nativeDrawingSnapshot;
    if (!payload || payload.kind !== 'native-transition' || !['values', 'initial-values', 'initial-fade', 'value-flash', 'legend-visibility', 'source-reflow', 'values-and-visibility'].includes(payload.effect)
        || initial !== ['initial-values', 'initial-fade'].includes(payload.effect) || typeof payload.destination !== 'string' || !payload.drawings || !snapshot)
      throw new TypeError('Native transition: supported captured intent and native source snapshot required.');
    const declared = readChartSection(payload.destination, true), declaredAdaptive = readAdaptiveLayout(declared);
    const actual = initial ? snapshot : adaptive || { wide: next, narrow: null };
    if (!!actual.narrow !== !!payload.drawings.narrow || !!declaredAdaptive !== !!actual.narrow || !!snapshot.narrow !== !!actual.narrow)
      throw new TypeError('Native transition: adaptive drawings changed.');
    const declaredDrawings = declaredAdaptive || { wide: declared, narrow: null };
    const joint = payload.effect === 'values-and-visibility';
    const sourceReflow = payload.effect === 'source-reflow';
    const fade = payload.effect === 'initial-fade', legend = joint || sourceReflow || payload.effect === 'legend-visibility', flash = payload.effect === 'value-flash';
    const reflow = !joint && (sourceReflow && payload.sourceKind === 'native-source-reflow' || !sourceReflow && legend && payload.sourceKind === 'native-visibility-reflow');
    const kinds = { 'progress-value': 'progress-row', 'layered-radial-value': 'layered-radial-point', 'circle-value': 'circle-chart',
      'progress-ring-value': 'progress-ring-point', 'gauge-value': 'gauge', 'bullet-value': 'bullet-row', 'cartesian-value': 'point', 'radial-value': 'radial-point', 'radial-raw-value': 'radial-point', 'hierarchy-value': 'hierarchy-node' };
    const role = kinds[payload.sourceKind], readings = payload.readings;
    if (fade ? payload.sourceKind !== 'native-visual-opacity' || !Array.isArray(readings) || readings.length !== 0 : joint ? payload.sourceKind !== 'cartesian-value' || !payload.visibility || !Array.isArray(readings) || !readings.length : legend ? sourceReflow && (!Array.isArray(readings) || !readings.length) || !['line-bar-visibility', 'native-visibility-reflow', ...(sourceReflow ? ['native-source-reflow'] : [])].includes(payload.sourceKind) || !payload.visibility : !role || !Array.isArray(readings) || !readings.length)
      throw new TypeError('Native transition: unsupported source capture.');
    if (joint && request?.sourceVisibility?.layout !== undefined) throw new TypeError('Native transition: joint values require a Preserve request.');
    if (sourceReflow && payload.sourceKind !== 'native-source-reflow') throw new TypeError('Native transition: captured source Reflow required.');
    if ((!legend || sourceReflow || joint) && !fade) {
      const ids = new Set();
      for (const row of readings) {
        const hierarchyValue = payload.sourceKind === 'hierarchy-value';
        const identity = row && JSON.stringify([row.seriesKey, hierarchyValue ? row.nodeId : row.observationId]);
        if (!row || typeof row.seriesKey !== 'string' || !row.seriesKey || (hierarchyValue ? typeof row.nodeId !== 'string' || !row.nodeId || row.observationId !== undefined : typeof row.observationId !== 'string' || !row.observationId || row.nodeId !== undefined)
            || ids.has(identity) || ![row.fromValue, row.toValue, row.minimum, row.maximum].every(Number.isFinite) || row.maximum <= row.minimum)
          throw new TypeError('Native transition: ordered authored reading facts required.');
        ids.add(identity);
      }
      if (!readings.some(row => row.fromValue !== row.toValue)) throw new TypeError('Native transition: at least one changed native reading required.');
      if (request && !sourceReflow && !joint) {
        const changed = readings.filter(row => row.fromValue !== row.toValue), row = changed[0];
        if (initial || request.channel !== 'progress' || request.kind !== 'replace-value' || payload.sourceKind !== 'progress-value'
            || changed.length !== 1 || row.seriesKey !== request.seriesKey || row.observationId !== request.observationId || row.toValue !== request.value)
          throw new TypeError('Native transition: request identity/value mismatch.');
      }
    }
    if (legend && request && (!request.sourceVisibility || !(['visibility', 'reset'].includes(request.kind) || reflow && request.channel === 'hierarchy' && request.kind === 'root') || request.kind === 'visibility' && request.channel !== 'legend'
        || (reflow ? request.sourceVisibility.layout !== 'reflow' || !sameNativeTargets(request.sourceVisibility.hiddenTargets, payload.visibility.toHiddenTargets)
          : !sameNativeHiddenSeries(request.sourceVisibility.hiddenSeries, payload.visibility.toHidden, payload.visibility.seriesKeys))
        || request.sourceVisibility.labelLayering !== payload.visibility.labelLayering
        || reflow && request.sourceVisibility.hierarchyRootId !== payload.visibility.toHierarchyRootId))
      throw new TypeError('Native transition: requested native visibility changed.');
    const raw = payload.sourceKind === 'radial-raw-value';
    const rowFor = (svg, fact) => [...svg.querySelectorAll(payload.sourceKind === 'hierarchy-value' ? '[data-cfx-role="hierarchy-series"] [data-cfx-node]' : '[data-cfx-role="' + role + '"]')]
      .find(node => node.dataset.cfxSeriesKey === fact.seriesKey && (payload.sourceKind === 'hierarchy-value' ? node.dataset.cfxNode === fact.nodeId : node.dataset.cfxObservationId === fact.observationId));
    const checkReadings = (svg, endpoint, acceptedValues = false, readingSection = null) => {
      if (joint) {
        const points = [...svg.querySelectorAll('[data-cfx-role="point"]')], tokens = points.map(node => JSON.stringify([node.dataset.cfxSeriesKey,node.dataset.cfxObservationId]));
        const expected = new Set(readings.map(row => JSON.stringify([row.seriesKey,row.observationId])));
        if (points.length !== readings.length || new Set(tokens).size !== tokens.length || tokens.some(token => !expected.has(token)))
          throw new TypeError('Native transition: exact joint authored point catalog required in every candidate.');
      }
      for (const fact of readings) {
        const row = rowFor(svg, fact), cartesian = payload.sourceKind === 'cartesian-value', value = Number(payload.sourceKind === 'hierarchy-value' ? row?.dataset.cfxAuthoredValue : cartesian ? row?.dataset.cfxY : row?.dataset.cfxValue);
        const domain = row && (['radial-value', 'hierarchy-value'].includes(payload.sourceKind) ? { minimum: 0, maximum: Number.MAX_VALUE } : cartesian ? chartAxisProjection(readingSection, row.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y')
          : payload.sourceKind === 'progress-value' ? progressDomain(svg, row)
          : { minimum: Number(row.dataset.cfxMin), maximum: Number(row.dataset.cfxMax) });
        if (!row || !domain || !Number.isFinite(value) || domain.minimum !== fact.minimum || domain.maximum !== fact.maximum
            || ['progress-value', 'progress-ring-value', 'cartesian-value', 'radial-value', 'hierarchy-value'].includes(payload.sourceKind) && (value < fact.minimum || value > fact.maximum)
            || endpoint && value !== (endpoint === 'source' ? fact.fromValue : fact.toValue) || acceptedValues && value !== fact.toValue)
          throw new TypeError('Native transition: captured reading identity/domain/value changed.');
      }
    };
    const read = name => {
      const drawing = payload.drawings[name], before = snapshot[name], after = actual[name], target = declaredDrawings[name];
      if (!drawing || typeof drawing.capturePngText !== 'boolean' || !Array.isArray(drawing.frames)
          || drawing.frames.length < (flash || reflow ? 3 : 2) || drawing.frames.length > 16
          || !adaptiveSectionCopy(target).isEqualNode(adaptiveSectionCopy(after)))
        throw new TypeError('Native transition: exact retained destination and finite frames required.');
      const capturedOpacity = fade || reflow ? drawing.frames.map(frame => frame?.nativeOpacity) : null;
      if (reflow) validateReflowFrameFacts(drawing.frames);
      else if (drawing.frames.some(frame => frame?.basis !== undefined)) throw new TypeError('Native transition: foreign frame basis.');
      if (fade && (capturedOpacity.some(value => typeof value !== 'string' || !value.trim() || !Number.isFinite(Number(value)) || Number(value) < 0 || Number(value) > 1)
          || Number(capturedOpacity[0]) !== 0 || Number(capturedOpacity[capturedOpacity.length - 1]) !== 1
          || capturedOpacity.some((value, index) => index && Number(value) <= Number(capturedOpacity[index - 1]))))
        throw new TypeError('Native transition: ordered captured native opacity facts required.');
      const prefixes = drawing.frames.map(frame => frame?.prefix);
      if (prefixes.some(prefix => typeof prefix !== 'string' || prefix === drawing.sourcePrefix || prefix === drawing.destinationPrefix)
          || new Set(prefixes).size !== prefixes.length)
        throw new TypeError('Native transition: distinct captured frame namespaces required.');
      const nativeBefore = before.querySelector('.cfx-stage svg'), destinationSvg = after.querySelector('.cfx-stage svg');
      const sourceSvg = initial ? parseCapturedNativeSvg(drawing.frames[0]?.svg, drawing.frames[0]?.prefix) : nativeBefore;
      const sourcePrefix = initial ? drawing.frames[0].prefix : drawing.sourcePrefix;
      if (legend) validateNativeVisibilityTransition(payload.visibility, before, after, sourceReflow);
      if (!legend && !fade) {
        const a = JSON.parse(before.dataset.cfxPreparedChart).visibility, b = JSON.parse(after.dataset.cfxPreparedChart).visibility, only = a || b;
        const changed = a && b ? JSON.stringify(a) !== JSON.stringify(b)
          : only && (only.layout === 'reflow' || only.hiddenSeries.length || only.labelLayering !== 'preserveOverlay');
        if (changed) throw new TypeError('Native transition: separate value intent retains the same native visibility view.');
      }
      if (joint) {
        const a = JSON.parse(before.dataset.cfxPreparedChart), b = JSON.parse(after.dataset.cfxPreparedChart);
        if (a.visibility?.layout === 'reflow' || b.visibility?.layout === 'reflow'
            || ['xProjection','yProjection','secondaryXProjection','secondaryYProjection','dataProjection'].some(key => JSON.stringify(a[key]) !== JSON.stringify(b[key])))
          throw new TypeError('Native transition: joint values retain exact full native Preserve domains/policies.');
        checkReadings(sourceSvg,'source',false,before); checkReadings(destinationSvg,'destination',false,after);
      }
      if (sourceReflow) {
        const a = nativeRawSource(before), b = nativeRawSource(after);
        if (!a || !b || a.structureKey !== b.structureKey || a.sourceKey === b.sourceKey)
          throw new TypeError('Native transition: same captured source structure and changed full raw source required.');
        validateRawTransitionReadings(before, readings, 'fromValue'); validateRawTransitionReadings(after, readings, 'toValue');
      }
      else if (!legend && !fade) {
        if (raw) {
          const a = initial ? drawing.frames[0]?.nativeValueSource : nativeRawSource(before), b = nativeRawSource(after);
          if (!a || !b || a.structureKey !== b.structureKey) throw new TypeError('Native transition: matching captured raw structure required.');
          validateRawTransitionReadings(a, readings, 'fromValue'); validateRawTransitionReadings(after, readings, 'toValue');
        }
        else { checkReadings(sourceSvg, 'source', false, before); checkReadings(destinationSvg, 'destination', false, after); }
      }
      return drawing.frames.map((frame, index) => {
        const svg = parseCapturedNativeSvg(frame?.svg, frame?.prefix);
        if (svg.getAttribute('viewBox') !== destinationSvg.getAttribute('viewBox')) throw new TypeError('Native transition: native viewport changed.');
        if ((fade || reflow) && Number(capturedOpacity[index]) < 1) {
          const wrappers = [...svg.children].filter(node => node.localName === 'g' && node.getAttribute('data-cfx-role') === 'native-transition-opacity');
          const wrapper = wrappers[0], expected = capturedOpacity[index];
          if (wrappers.length !== 1 || wrapper.parentElement !== svg || wrapper.localName !== 'g'
              || wrapper.getAttribute('opacity') !== expected
              || [...svg.children].some(node => node !== wrapper && !['title', 'desc', 'defs', 'style'].includes(node.localName)))
            throw new TypeError('Native transition: one complete captured whole-visual opacity frame required.');
        }
        if ((!legend || joint) && !fade && !raw) checkReadings(svg, index === 0 && !flash ? 'source' : index === drawing.frames.length - 1 ? 'destination' : null, flash, after);
        const frameBasis = reflow && frame.basis === 'source' ? before : after;
        if (reflow) {
          if (JSON.stringify(frame.nativeValueSource) !== JSON.stringify(nativeRawSource(frameBasis)))
            throw new TypeError('Native transition: foreign captured raw Reflow frame facts.');
          validateReflowFrameInk(svg, frame, frameBasis, frame.basis === 'source' ? drawing.sourcePrefix : drawing.destinationPrefix);
        }
        const candidate = frameBasis.cloneNode(true);
        candidate.querySelector('.cfx-stage svg').replaceWith(document.importNode(svg, true));
        if (raw) {
          if (!frame.nativeValueSource || frame.nativeValueSource.structureKey !== JSON.parse(after.dataset.cfxPreparedChart).nativeValueSource?.structureKey)
            throw new TypeError('Native transition: captured raw frame source/policy required.');
          const metadata = JSON.parse(candidate.dataset.cfxPreparedChart); metadata.nativeValueSource = frame.nativeValueSource;
          candidate.dataset.cfxPreparedChart = JSON.stringify(metadata);
          validateRawTransitionReadings(candidate, readings, index === 0 && !flash ? 'fromValue' : index === drawing.frames.length - 1 || flash ? 'toValue' : null);
        } else if (frame.nativeValueSource !== undefined && !reflow) throw new TypeError('Native transition: foreign raw frame facts.');
        readChartSection(candidate, true);
        if (index === 0 && !nativeExportEquals(svg, frame.prefix, flash ? destinationSvg : sourceSvg, flash ? drawing.destinationPrefix : sourcePrefix)
            || index === drawing.frames.length - 1 && !nativeExportEquals(svg, frame.prefix, destinationSvg, drawing.destinationPrefix))
          throw new TypeError('Native transition: exact native frame endpoint required.');
        return document.importNode(svg, true);
      });
    };
    const wide = read('wide'), narrow = actual.narrow ? read('narrow') : null;
    if (narrow && narrow.length !== wide.length) throw new TypeError('Native transition: adaptive frame counts differ.');
    return Object.freeze({ wide, narrow, ...(sourceReflow ? { sourceReflow: true } : {}) });
  };
  const captureInitialNativeTransition = (root, value) => {
    if (typeof value !== 'string' || value.length > 1048576) throw new TypeError('Native transition: bounded initial capture JSON required.');
    let payload; try { payload = JSON.parse(value); } catch { throw new TypeError('Native transition: invalid JSON.'); }
    return captureNativeTransitionMotion(root, null, payload, null, null, true);
  };
