  // Mirrors ChartTimeScale.GenerateFitting for an automatic calendar: the same interval ladder, calendar
  // phases, caption formats and comfortable caption gaps, so a zoomed saved report names the dates a native
  // preparation of that window would name. Calendar steps walk the display zone's wall clock; captions use
  // the invariant ISO patterns or the captured culture's patterns and names.
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
  const standaloneTimeCaptured = axis => axis?.captions === 'calendar' && (axis.timeZone === undefined || Array.isArray(axis.timeZone) && axis.timeZone.length > 0
    && axis.timeZone.every(entry => Array.isArray(entry) && entry.length === 2 && entry.every(Number.isFinite)));
  // The display zone from its captured offsets (ChartTimeZoneFacts): wall-clock milliseconds for an instant, and the
  // instants a wall-clock boundary names, as TimeZoneInfo resolves them for ChartTimeScale.ResolveInstants.
  const standaloneCalendar = axis => {
    const offsets = axis.timeZone || [[-Infinity, 0]], minutes = [...new Set(offsets.map(entry => entry[1]))];
    const offset = ms => { let low = 0, high = offsets.length - 1; while (low < high) { const middle = (low + high + 1) >> 1; if (offsets[middle][0] <= ms) low = middle; else high = middle - 1; } return offsets[low][1]; };
    const valid = wall => minutes.filter(value => offset(wall - value * 6e4) === value).map(value => wall - value * 6e4).sort((a, b) => a - b);
    return {
      wall: ms => ms + offset(ms) * 6e4,
      // A repeated wall-clock hour hides instants beyond a bound, so walks extend past it.
      repeated: wall => { const instants = valid(wall); return instants.length > 1 ? instants[instants.length - 1] - instants[0] : 0; },
      resolve: (wall, subDay) => {
        const instants = valid(wall);
        // A repeated time keeps both instants for clock steps and its first instant for calendar steps.
        if (instants.length) return subDay ? instants : instants.slice(0, 1);
        if (subDay) return [];
        // A skipped calendar boundary moves to the first valid instant: the change that skipped it.
        const change = offsets.find((entry, index) => index && entry[0] + offsets[index - 1][1] * 6e4 <= wall && entry[0] + entry[1] * 6e4 > wall);
        return change ? [change[0]] : [];
      },
      culture: axis.timeCulture || null
    };
  };
  // Floors and steps work on wall-clock milliseconds. Sub-day steps divide their parent unit; multi-day steps
  // keep one phase counted from 0001-01-01; multi-week steps count Mondays from 1970-01-05.
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
  const standaloneTimeInterval = (minimum, maximum, interval, calendar) => {
    const subDay = standaloneTimeRank(interval) < 3, first = calendar.wall(standaloneTimeMs(minimum)), last = calendar.wall(standaloneTimeMs(maximum));
    const end = last + calendar.repeated(last), margin = standaloneTimeDays(interval) * 1.5, candidates = [];
    for (let wall = standaloneTimeFloor(first - calendar.repeated(first), interval), steps = 0; wall <= end && candidates.length < 2000 && steps < 1e5; wall = standaloneTimeNext(wall, interval), steps++) {
      if (new Date(wall).getUTCFullYear() < 100) continue;
      for (const instant of calendar.resolve(wall, subDay)) {
        const value = standaloneTimeOa(instant);
        if (value >= minimum - margin && value <= maximum + margin) candidates.push(value);
      }
    }
    return candidates.sort((a, b) => a - b).filter((value, index, values) => (!index || value > values[index - 1]) && value >= minimum && value <= maximum);
  };
  const standaloneTimePad = (value, size = 2) => String(value).padStart(size, '0');
  // .NET custom date patterns: quoted literals, escapes, separators and the day, month, year, hour, minute,
  // second and designator fields. Month names take their genitive form beside a day number, as DateTimeFormat does.
  const standaloneTimeFormat = (date, pattern, culture) => {
    const run = (at, character) => { let count = 1; while (pattern[at + count] === character) count++; return count; };
    const dayRun = (from, step) => {
      let at = from; while (at >= 0 && at < pattern.length && pattern[at] !== 'd') at += step;
      if (at < 0 || at >= pattern.length) return false;
      let count = 0; while ((at += step) >= 0 && at < pattern.length && pattern[at] === 'd') count++;
      return count <= 1;
    };
    let text = '';
    for (let at = 0; at < pattern.length;) {
      const character = pattern[at];
      if (character === "'" || character === '"') {
        const close = pattern.indexOf(character, at + 1);
        text += pattern.slice(at + 1, close < 0 ? pattern.length : close); at = close < 0 ? pattern.length : close + 1; continue;
      }
      if (character === '\\') { text += pattern[at + 1] ?? ''; at += 2; continue; }
      const count = run(at, character), hours = date.getUTCHours(), month = date.getUTCMonth(), year = date.getUTCFullYear();
      const two = value => count === 1 ? String(value) : standaloneTimePad(value);
      const genitive = () => dayRun(at - 1, -1) || dayRun(at + count, 1);
      text += character === 'd' ? count <= 2 ? two(date.getUTCDate()) : culture[count === 3 ? 'days' : 'dayNames'][date.getUTCDay()]
        : character === 'M' ? count <= 2 ? two(month + 1) : culture[count === 3 ? genitive() ? 'genitiveMonths' : 'months' : genitive() ? 'genitiveMonthNames' : 'monthNames'][month]
        : character === 'y' ? count === 1 ? String(year % 100) : standaloneTimePad(count === 2 ? year % 100 : year, count)
        : character === 'h' ? two(hours % 12 || 12) : character === 'H' ? two(hours)
        : character === 'm' ? two(date.getUTCMinutes()) : character === 's' ? two(date.getUTCSeconds())
        : character === 't' ? (hours < 12 ? culture.am : culture.pm).slice(0, count === 1 ? 1 : undefined)
        : character === ':' ? culture.timeSeparator.repeat(count) : character === '/' ? culture.dateSeparator.repeat(count) : character.repeat(count);
      at += count;
    }
    return text;
  };
  // ChartTimeScale.Format rounds the display time to whole seconds: midnight reads as a date, other instants as a clock.
  const standaloneTimeDisplay = (value, calendar) => new Date(Math.round(calendar.wall(standaloneTimeMs(value)) / 1e3) * 1e3);
  const standaloneTimeCaption = (value, ticks, calendar) => {
    const date = standaloneTimeDisplay(value, calendar), culture = calendar.culture, seconds = date.getUTCSeconds();
    const midnight = !date.getUTCHours() && !date.getUTCMinutes() && !seconds;
    if (culture) return standaloneTimeFormat(date, ticks.format ? culture[ticks.format === 'year' ? 'dayYear' : 'day']
      : midnight ? culture[!date.getUTCMonth() && date.getUTCDate() === 1 ? 'dayYear' : 'day'] : culture[seconds ? 'seconds' : 'time'], culture);
    const day = standaloneTimePad(date.getUTCMonth() + 1) + '-' + standaloneTimePad(date.getUTCDate()), year = standaloneTimePad(date.getUTCFullYear(), 4) + '-';
    if (ticks.format) return ticks.format === 'year' ? year + day : day;
    if (midnight) return year + day;
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
  const standaloneTimeFinalDay = (minimum, maximum, fits, ticks, calendar, limit = 8) => {
    if (!ticks || ticks.length < 2 || ticks.length >= limit) return ticks;
    const midnight = value => standaloneTimeDisplay(value, calendar).getTime() % 864e5 === 0;
    if (ticks.some((value, index) => !midnight(value) || (index && value - ticks[index - 1] > 14.5))) return ticks;
    const instant = calendar.resolve(Math.floor(calendar.wall(standaloneTimeMs(maximum)) / 864e5) * 864e5, false)[0];
    const final = instant === undefined ? NaN : standaloneTimeOa(instant);
    if (!(final >= minimum && final <= maximum) || final <= ticks[ticks.length - 1]) return ticks;
    const extended = standaloneTimeKeep(ticks, [...ticks, final]);
    return fits(extended) ? extended : ticks;
  };
  const standaloneTimeFitting = (minimum, maximum, fits, observations, calendar, limit = 8) => {
    if (minimum < 0 || !(maximum <= 2958465) || maximum - minimum < 1 / 86400) return null;
    const ideal = standaloneTimeChoose((maximum - minimum) / (limit - 1));
    // An automatic count starts one real step finer, so its stride can keep clock precision.
    let interval = [...standaloneTimeLadder].reverse().find(item => standaloneTimeDays(item) < standaloneTimeDays(ideal)) || ideal;
    const compact = { format: standaloneTimeDisplay(minimum, calendar).getUTCFullYear() !== standaloneTimeDisplay(maximum, calendar).getUTCFullYear() ? 'year' : 'day' };
    let sparse = null, compactFit = null, cadenceFit = null;
    const cadence = observations && standaloneTimeCadence(observations, minimum, maximum, limit, fits, null);
    if (cadence?.length > 1) return cadence;
    for (let attempt = 0; attempt < 64; attempt++) {
      if (observations && standaloneTimeRank(interval) > 4) break;
      let ticks = standaloneTimeInterval(minimum, maximum, interval, calendar);
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
    if (observations) return standaloneTimeCadence(observations, minimum, maximum, limit, fits, compact) || standaloneTimeFitting(minimum, maximum, fits, null, calendar, limit);
    return cadenceFit || compactFit || sparse;
  };
  // Measures captions as VisualCartesianCompiler.AxisLabelSequenceFits does with ComfortableLabelGap.
  const standaloneTimeTicks = (axis, length, measure, fontSize, observations, calendar) => {
    const position = value => projectedAxisRatio(axis, value) * length;
    const fits = ticks => {
      const texts = ticks.map(value => standaloneTimeCaption(value, ticks, calendar));
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
    const fitted = standaloneTimeFitting(axis.minimum, axis.maximum, fits, observations, calendar);
    const ticks = observations ? fitted : standaloneTimeFinalDay(axis.minimum, axis.maximum, fits, fitted, calendar);
    return ticks && ticks.map(value => ({ value, text: standaloneTimeCaption(value, ticks, calendar) }));
  };
  // Native time bars use their source cadence when every series is a bar on one regular step.
  const standaloneTimeObservations = view => {
    if (view.timeObservations !== undefined) return view.timeObservations;
    const values = view.source.series.every(row => row.kind === 'bar') ? [...new Set(view.points.map(point => point.x))].sort((a, b) => a - b) : [];
    const step = values[1] - values[0];
    return view.timeObservations = values.length > 1 && step > 0 && values.every((value, index) => index < 2 || Math.abs((value - values[index - 1]) / step - 1) <= 1e-6) ? values : null;
  };
