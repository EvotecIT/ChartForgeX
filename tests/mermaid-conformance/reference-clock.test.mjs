import assert from 'node:assert/strict';
import test from 'node:test';
import { useReferenceClock } from './reference-clock.mjs';

test('A fixture clock fixes default dates and restores both oracle environments', () => {
  const NativeDate = globalThis.Date;
  class BrowserDate extends NativeDate {}
  const browser = { Date: BrowserDate };
  const restore = useReferenceClock('2028-02-20T00:00:00.000Z', browser);
  try {
    assert.equal(new Date().toISOString(), '2028-02-20T00:00:00.000Z');
    assert.equal(Date.now(), NativeDate.UTC(2028, 1, 20));
    assert.equal(new browser.Date().getTime(), Date.now());
    assert.equal(new Date(2026, 0, 2).getFullYear(), 2026);
    assert.equal(new Date('2026-01-02T00:00:00.000Z').getTime(), NativeDate.parse('2026-01-02T00:00:00.000Z'));
  } finally { restore(); }
  assert.equal(globalThis.Date, NativeDate);
  assert.equal(browser.Date, BrowserDate);
});

test('An invalid reference date cannot change the runtime clock', () => {
  const NativeDate = globalThis.Date;
  const browser = { Date: NativeDate };
  assert.throws(() => useReferenceClock('2028-02-30T00:00:00.000Z', browser), /valid day/);
  assert.equal(globalThis.Date, NativeDate);
  assert.equal(browser.Date, NativeDate);
  useReferenceClock(undefined, browser)();
  assert.equal(globalThis.Date, NativeDate);
});
