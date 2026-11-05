  // Mirrors ChartTimeScale.GenerateFitting for an automatic UTC calendar with invariant captions:
  // the same interval ladder, calendar phases, caption formats and comfortable caption gaps, so a
  // zoomed saved report names the dates a native preparation of that window would name.
  const standaloneTimeLadder = 's1 s5 s15 s30 m1 m2 m5 m10 m15 m30 h1 h2 h3 h6 h12 d1 d2 d3 d4 w1 w2 M1 M3 M6 y1'.split(' ').map(item => [item[0], +item.slice(1)]);
  // Unit lengths in milliseconds; months and years use the native nominal 30.44 and 365.25 days.
  const standaloneTimeUnitMs = { s: 1e3, m: 6e4, h: 36e5, d: 864e5, w: 6048e5, M: 2630016e3, y: 315576e5 };
  const standaloneTimeDays = ([unit, step]) => step * (standaloneTimeUnitMs[unit] / 864e5);
  const standaloneTimeRank = ([unit]) => 'smhdwMy'.indexOf(unit);
  const standaloneTimeChoose = target => standaloneTimeLadder.find(interval => standaloneTimeDays(interval) >= target)
    || ['y', target / 365.25 <= 1 ? 1 : Math.min(1e4, Math.ceil(standaloneNiceStep(target / 365.25, false)))];
  // OLE Automation days and UTC milliseconds, rounded to whole milliseconds as DateTime does.
  const standaloneTimeMs = value => Math.round(value * 864e5) - 2209161600000;
  const standaloneTimeOa = ms => (ms + 2209161600000) / 864e5;
  // Sub-day steps divide their parent unit, so they align with the epoch. Multi-day steps keep one
  // phase counted from 0001-01-01; multi-week steps count Mondays from 1970-01-05.
  const standaloneTimeFloor = (ms, [unit, step]) => {
    const date = new Date(ms), year = date.getUTCFullYear(), month = date.getUTCMonth(), day = Math.floor(ms / 864e5);
    const rest = value => (value % step + step) % step, monday = day - (date.getUTCDay() + 6) % 7;
    return unit === 'M' ? Date.UTC(year, month - month % step, 1) : unit === 'y' ? Date.UTC(Math.max(1, year - year % step), 0, 1)
      : unit === 'd' ? (day - rest(day + 719162)) * 864e5 : unit === 'w' ? (monday - rest((monday - 4) / 7) * 7) * 864e5
      : Math.floor(ms / (step * standaloneTimeUnitMs[unit])) * step * standaloneTimeUnitMs[unit];
  };
  const standaloneTimeNext = (ms, [unit, step]) => {
    const date = new Date(ms);
    return unit === 'M' || unit === 'y' ? Date.UTC(date.getUTCFullYear() + (unit === 'y' ? step : 0), date.getUTCMonth() + (unit === 'M' ? step : 0), 1)
      : ms + step * standaloneTimeUnitMs[unit];
  };
  const standaloneTimeInterval = (minimum, maximum, interval) => {
    const values = [], last = standaloneTimeMs(maximum);
    for (let ms = standaloneTimeFloor(standaloneTimeMs(minimum), interval), steps = 0; ms <= last && values.length < 2000 && steps < 1e5; ms = standaloneTimeNext(ms, interval), steps++) {
      const value = standaloneTimeOa(ms);
      if (value >= minimum && value <= maximum && new Date(ms).getUTCFullYear() >= 100 && !(value <= values[values.length - 1])) values.push(value);
    }
    return values;
  };
  const standaloneTimePad = (value, size = 2) => String(value).padStart(size, '0');
  // ChartTimeScale.Format rounds to whole seconds: midnight reads as a date, other instants as a clock.
  const standaloneTimeCaption = (value, ticks) => {
    const date = new Date(Math.round(standaloneTimeMs(value) / 1e3) * 1e3);
    const day = standaloneTimePad(date.getUTCMonth() + 1) + '-' + standaloneTimePad(date.getUTCDate()), year = standaloneTimePad(date.getUTCFullYear(), 4) + '-';
    if (ticks.format) return ticks.format === 'year' ? year + day : day;
    const seconds = date.getUTCSeconds();
    if (!date.getUTCHours() && !date.getUTCMinutes() && !seconds) return year + day;
    return standaloneTimePad(date.getUTCHours()) + ':' + standaloneTimePad(date.getUTCMinutes()) + (seconds ? ':' + standaloneTimePad(seconds) : '');
  };
  const standaloneTimeKeep = (source, values) => Object.assign(values, { format: source.format });
  // Regular bar observations keep strides of their own cadence, as native time bars do.
  const standaloneTimeCadence = (observations, minimum, maximum, limit, fits, compact) => {
    const visible = observations.filter(value => value >= minimum && value <= maximum);
    if (!visible.length) return null;
    let previous = 0, compactFit = null, single = null;
    for (let count = Math.min(limit, visible.length); count >= 1; count--) {
      const stride = count === 1 ? visible.length : Math.ceil((visible.length - 1) / (count - 1));
      if (stride === previous) continue;
      previous = stride;
      const ticks = visible.filter((_, index) => index % stride === 0);
      if (fits(ticks)) { if (ticks.length > 1) return ticks; single = ticks; }
      if (!compactFit && ticks.length > 1 && compact && (observations[1] - observations[0]) * stride >= 1 && fits(standaloneTimeKeep(compact, [...ticks]))) compactFit = standaloneTimeKeep(compact, [...ticks]);
    }
    return compactFit || single;
  };
  const standaloneTimeDayStep = days => days <= 5 || days % 7 === 0;
  // Mirrors ChartTimeScale.WithFinalDay: a daily or weekly window adds its final day when that caption fits after the regular ticks.
  const standaloneTimeFinalDay = (minimum, maximum, fits, ticks, limit = 8) => {
    if (!ticks || ticks.length < 2 || ticks.length >= limit) return ticks;
    const midnight = value => (Math.round(standaloneTimeMs(value) / 1e3) * 1e3) % 864e5 === 0;
    if (ticks.some((value, index) => !midnight(value) || (index && value - ticks[index - 1] > 14.5))) return ticks;
    const final = standaloneTimeOa(Math.floor(standaloneTimeMs(maximum) / 864e5) * 864e5);
    if (final < minimum || final > maximum || final <= ticks[ticks.length - 1]) return ticks;
    const extended = standaloneTimeKeep(ticks, [...ticks, final]);
    return fits(extended) ? extended : ticks;
  };
  const standaloneTimeFitting = (minimum, maximum, fits, observations, limit = 8) => {
    if (minimum < 0 || !(maximum <= 2958465) || maximum - minimum < 1 / 86400) return null;
    const ideal = standaloneTimeChoose((maximum - minimum) / (limit - 1));
    // An automatic count starts one real step finer, so its stride can keep clock precision.
    let interval = [...standaloneTimeLadder].reverse().find(item => standaloneTimeDays(item) < standaloneTimeDays(ideal)) || ideal;
    const compact = { format: new Date(standaloneTimeMs(minimum)).getUTCFullYear() !== new Date(standaloneTimeMs(maximum)).getUTCFullYear() ? 'year' : 'day' };
    let sparse = null, compactFit = null, cadenceFit = null;
    const cadence = observations && standaloneTimeCadence(observations, minimum, maximum, limit, fits, null);
    if (cadence?.length > 1) return cadence;
    for (let attempt = 0; attempt < 64; attempt++) {
      if (observations && standaloneTimeRank(interval) > 4) break;
      let ticks = standaloneTimeInterval(minimum, maximum, interval);
      if (observations) {
        const ratio = standaloneTimeDays(interval) / (observations[1] - observations[0]), stride = Math.round(ratio);
        ticks = stride < 1 || Math.abs(ratio - stride) > 1e-6 ? null : observations.filter((value, index) => index % stride === 0 && value >= minimum && value <= maximum);
        if (!ticks) { interval = standaloneTimeChoose(standaloneTimeDays(interval) * 1.01); continue; }
      }
      let five = false;
      if (ticks.length) {
        const ordinary = standaloneTimeDays(interval) + 1e-10 >= (maximum - minimum) / (limit - 1), counted = ordinary && ticks.length <= limit;
        const complete = counted && fits(ticks);
        five = !observations && interval[0] === 'd' && interval[1] === 4 && counted && ticks.length > 1 && !complete;
        if (complete) {
          if (ticks.length > 1) return ticks.length < 4 && cadenceFit?.length >= 4 ? cadenceFit : ticks;
          sparse = ticks;
        }
        if (!compactFit && counted && ticks.length > 1 && standaloneTimeRank(interval) >= 3 && fits(standaloneTimeKeep(compact, [...ticks]))) compactFit = standaloneTimeKeep(compact, [...ticks]);
        if (!cadenceFit && ticks.length > 2) for (let stride = 2; stride < ticks.length && !cadenceFit; stride++) {
          // Day strides stay calendar steps: up to five days or whole weeks, never 6, 8 or 10 days.
          if (interval[0] === 'd' && !standaloneTimeDayStep(interval[1] * stride)) continue;
          const retained = ticks.filter((_, index) => index % stride === 0);
          if (retained.length > 1 && retained.length <= limit && fits(retained)) cadenceFit = retained;
        }
        if (!sparse && fits([ticks[0]])) sparse = [ticks[0]];
      }
      if (!ticks.length) break;
      interval = five ? ['d', 5] : standaloneTimeChoose(standaloneTimeDays(interval) * 1.01);
    }
    if (observations) return standaloneTimeCadence(observations, minimum, maximum, limit, fits, compact) || standaloneTimeFitting(minimum, maximum, fits, null, limit);
    return cadenceFit || compactFit || sparse;
  };
  // Measures captions as VisualCartesianCompiler.AxisLabelSequenceFits does with ComfortableLabelGap.
  const standaloneTimeTicks = (axis, length, measure, fontSize, observations) => {
    const position = value => projectedAxisRatio(axis, value) * length;
    const fits = ticks => {
      const texts = ticks.map(value => standaloneTimeCaption(value, ticks));
      if (texts.some((text, index) => index && text === texts[index - 1])) return false;
      const span = ticks.length > 1 ? Math.abs(position(ticks[ticks.length - 1]) - position(ticks[0])) : 0;
      return texts.every((text, index) => {
        if (!index) return true;
        const extent = measure(text), previous = measure(texts[index - 1]), mean = (extent + previous) / 2;
        let ordinary = axis.labelGap, gap = Math.min(mean, Math.min(80, span / 5) - mean);
        if (Math.max(extent, previous) >= fontSize * 4) gap = Math.max(span < 400 && Math.min(extent, previous) >= fontSize * 4 ? Math.min(24, fontSize) : 24, gap);
        else if (span < 400) ordinary = Math.max(ordinary, fontSize);
        return Math.abs(position(ticks[index]) - position(ticks[index - 1])) + 1e-4 >= mean + Math.max(ordinary, gap);
      });
    };
    const fitted = standaloneTimeFitting(axis.minimum, axis.maximum, fits, observations);
    const ticks = observations ? fitted : standaloneTimeFinalDay(axis.minimum, axis.maximum, fits, fitted);
    return ticks && ticks.map(value => ({ value, text: standaloneTimeCaption(value, ticks) }));
  };
  // Native time bars use their source cadence when every series is a bar on one regular step.
  const standaloneTimeObservations = view => {
    if (view.timeObservations !== undefined) return view.timeObservations;
    const values = view.source.series.every(row => row.kind === 'bar') ? [...new Set(view.points.map(point => point.x))].sort((a, b) => a - b) : [];
    const step = values[1] - values[0];
    return view.timeObservations = values.length > 1 && step > 0 && values.every((value, index) => index < 2 || Math.abs((value - values[index - 1]) / step - 1) <= 1e-6) ? values : null;
  };
