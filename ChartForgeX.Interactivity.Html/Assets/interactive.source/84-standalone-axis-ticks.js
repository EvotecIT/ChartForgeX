  // Mirrors ChartNiceNumbers.Step and ChartTicks.GenerateInside for a linear window. Whole-number
  // axes (hours, counts, indexes) step by 1, 2 or 5 × 10ⁿ only, never below one.
  const standaloneNiceStep = (value, whole) => {
    if (whole) value = Math.max(1, value);
    const power = Math.pow(10, Math.floor(Math.log10(value))), fraction = value / power, tolerance = fraction * 8 * 2.220446049250313e-16;
    return (fraction <= 1 + tolerance ? 1 : fraction <= 2 + tolerance ? 2 : !whole && fraction <= 2.5 + tolerance ? 2.5 : fraction <= 5 + tolerance ? 5 : 10) * power;
  };
  const standaloneInsideTicks = (minimum, maximum, count, whole) => {
    const step = standaloneNiceStep((maximum - minimum) / (count - 1), whole), start = Math.ceil(minimum / step) * step, values = [];
    for (let index = 0; index < count && Number.isFinite(step) && step > 0; index++) {
      let value = start + step * index;
      if (value > maximum) {
        if (value - maximum > Math.max(Math.abs(value), Math.abs(maximum)) * 8 * 2.220446049250313e-16) break;
        value = maximum;
      }
      values.push(Math.abs(value) < step / 1e6 ? 0 : value);
    }
    // A whole-number window narrower than two steps keeps its whole values rather than fractional endpoints.
    return { values: values.length >= 2 || whole && values.length ? values : [minimum, maximum], step };
  };
  // ChartTicks.GeneratePositiveLogarithmic places ticks on whole powers, thinned by an exponent stride.
  // A window holding fewer than two powers adds the 1-2-5 and 1…9 multiples of base ten, then the
  // linear policy for a window inside one sub-decade. Negative domains mirror their magnitudes.
  const standaloneLogarithmicTicks = axis => {
    const negative = axis.logarithmicDomain === 'negative', base = axis.logarithmBase ?? 10;
    const low = negative ? -axis.maximum : axis.minimum, high = negative ? -axis.minimum : axis.maximum;
    const log = value => Math.log(value) / Math.log(base), sets = [];
    const add = values => {
      values = values.filter(value => value >= low * (1 - 1e-12) && value <= high * (1 + 1e-12));
      if (values.length) sets.push(negative ? values.map(value => -value).reverse() : values);
    };
    const first = Math.ceil(log(low) - 1e-9), last = Math.floor(log(high) + 1e-9), powers = [];
    for (let exponent = first; exponent <= last && powers.length < 400; exponent++) powers.push(Math.pow(base, exponent));
    for (let stride = 1; stride <= Math.max(1, powers.length - 1); stride++) add(powers.filter((_, index) => index % stride === 0));
    if (base === 10 && last - first < 40) for (const mantissas of [[1, 2, 5], [1, 2, 3, 4, 5, 6, 7, 8, 9]]) {
      const values = [];
      for (let exponent = first - 1; exponent <= last; exponent++) for (const mantissa of mantissas) values.push(Number((mantissa * Math.pow(10, exponent)).toPrecision(12)));
      add(values);
    }
    for (let count = 2; count <= 10; count++) add(standaloneInsideTicks(low, high, count, false).values);
    return sets;
  };
  // ChartValueFormat's invariant compact captions ("0.##", or "0.#" with k/M/B), with the
  // native plain-significant fallback when a caption would not name its own tick.
  const standaloneCompact = value => {
    const fixed = (number, digits) => String(Number(number.toFixed(digits)) || 0), magnitude = Math.abs(value);
    return magnitude >= 1e9 ? fixed(value / 1e9, 1) + 'B' : magnitude >= 1e6 ? fixed(value / 1e6, 1) + 'M'
      : magnitude >= 1e3 ? fixed(value / 1e3, 1) + 'k' : fixed(value, 2);
  };
  const standaloneCompactValue = text => Number(text.replace(/[kMB]$/, '')) * ({ k: 1e3, M: 1e6, B: 1e9 }[text.slice(-1)] || 1);
  const standaloneCaptions = (axis, values, step) => {
    const captured = [...(axis.displayTicks || []), ...(axis.displayBounds || [])];
    const exact = value => captured.find(tick => Math.abs(tick.value - value) <= Math.abs(value) * 1e-12)?.text;
    if (axis.scale === 'time') return values.map(value => exact(value) ?? String(dataValue(value, value >= 0)));
    let texts = values.map(value => standaloneCompact(value));
    if (texts.some((text, index) => Math.abs(standaloneCompactValue(text) - values[index]) > step * .01))
      texts = values.map(value => value === 0 ? '0' : String(Number(value.toPrecision(6))));
    return values.map((value, index) => exact(value) ?? texts[index]);
  };
  // Picks the static tick policy's densest layout whose labels keep their own room,
  // preferring at least three ticks wherever three fit along the axis.
  const standaloneTicks = (axis, length, room, desired, categories, whole) => {
    const position = value => projectedAxisRatio(axis, value) * length;
    const fits = ticks => ticks.every((tick, index) => !index
      || Math.abs(position(tick.value) - position(ticks[index - 1].value)) >= (room(tick.text) + room(ticks[index - 1].text)) / 2);
    if (categories) {
      const values = categories.filter(tick => tick.value >= axis.minimum && tick.value <= axis.maximum);
      for (let stride = 1; stride < values.length; stride++) {
        const ticks = values.filter((_, index) => index % stride === 0);
        if (fits(ticks)) return ticks;
      }
      return values.slice(0, 1);
    }
    // Logarithmic captions are checked against their nearest neighbour, since steps vary along the axis.
    const label = values => standaloneCaptions(axis, values, values.length > 1 ? Math.min(...values.slice(1).map((value, index) => Math.abs(value - values[index])))
      : Math.abs(values[0]) || 1).map((text, index) => ({ value: values[index], text }));
    const target = Math.max(3, desired), candidates = [];
    const generated = axis.scale === 'logarithmic' ? standaloneLogarithmicTicks(axis)
      : Array.from({ length: Math.min(40, Math.max(target * 2, 6)) - 1 }, (_, index) => standaloneInsideTicks(axis.minimum, axis.maximum, index + 2, whole).values);
    for (const values of generated) {
      const ticks = label(values);
      if (fits(ticks) && !candidates.some(item => item.length === ticks.length)) candidates.push(ticks);
    }
    candidates.sort((first, second) => first.length - second.length);
    const readable = candidates.filter(ticks => ticks.length >= 3);
    return readable.filter(ticks => ticks.length <= target).pop() || readable[0] || candidates.pop()
      || label(standaloneInsideTicks(axis.minimum, axis.maximum, 2, whole).values);
  };
  // The native automatic search start (VisualCartesianCompiler.NumericFittingCount) for a linear window:
  // whole units on short domains across, then about one tick per 80 px across or 50 px down an axis of at
  // least 160 px. The densest fitting layout at or below it is what a native preparation of the window paints.
  const standaloneNativeTickCount = (axis, length, vertical, size) => {
    const unit = Math.max(6, Math.ceil(axis.maximum - axis.minimum) + 1);
    const count = vertical || !Number.isFinite(unit) ? 6 : unit <= 100 ? unit : Math.min(100, Math.max(7, Math.floor(length / (size * 5)) + 2));
    if (axis.integerTicks || length < 160) return count;
    return vertical ? Math.max(count, Math.min(9, Math.round(length / 50) + 1)) : count <= 13 ? count : Math.min(count, Math.max(4, Math.floor(length / 80) + 1));
  };
  // Values an observation places on one of its value axes, including its range and summary extents.
  const standaloneExtentKeys = ['cfxLow', 'cfxHigh', 'cfxOpen', 'cfxClose', 'cfxLower', 'cfxUpper', 'cfxYLower', 'cfxYUpper', 'cfxMinimum', 'cfxMaximum', 'cfxStart'];
  const standalonePointValues = (point, keys = standaloneExtentKeys) => [point.y,
    ...keys.filter(key => point.node.dataset[key] !== undefined).map(key => Number(point.node.dataset[key]))];
  // An axis is whole-numbered when the static policy said so, or when its full domain and every
  // observation on it are whole numbers. Logarithmic axes keep their own policy.
  const standaloneWholeAxis = (view, name, axis) => (view.whole ||= {})[name] ??= axis.scale !== 'logarithmic' && (!!axis.integerTicks
    || [axis.fullMinimum ?? axis.minimum, axis.fullMaximum ?? axis.maximum,
      ...view.points.filter(point => name === view.categoryAxis || point.axis === name).flatMap(point => name === view.categoryAxis ? [point.x] : standalonePointValues(point))]
      .every(value => Number.isFinite(value) && Number.isInteger(value)));
