import assert from 'node:assert/strict';

// Upstream partial dates read the clock. Scope the oracle's clock to one fixture;
// native parsing receives the same date explicitly and never changes a runtime clock.
export function useReferenceClock(utc, browserWindow) {
  if (utc === undefined) return () => {};
  assert.match(utc, /^\d{4}-\d{2}-\d{2}T00:00:00\.000Z$/, 'Reference clocks use a UTC calendar day.');
  const NativeDate = globalThis.Date;
  const BrowserDate = browserWindow.Date;
  const instant = NativeDate.parse(utc);
  assert.ok(Number.isFinite(instant) && new NativeDate(instant).toISOString() === utc, 'Reference clock must be a valid day.');
  class FixtureDate extends NativeDate {
    constructor(...args) { super(...(args.length ? args : [instant])); }
    static now() { return instant; }
  }
  globalThis.Date = browserWindow.Date = FixtureDate;
  return () => { globalThis.Date = NativeDate; browserWindow.Date = BrowserDate; };
}
