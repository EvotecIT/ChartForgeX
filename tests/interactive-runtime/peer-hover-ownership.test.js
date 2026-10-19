const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

// Exercise the actual receiver against a small DOM boundary. Browser contracts cover native input.
function fixture() {
  const classes = new Set(['cfx-hovered']);
  const node = { dataset: { cfxSeriesKey: 'shared-X', cfxSeriesScope: 'shared', cfxTargetKind: 'point', cfxTargetId: 'present' },
    classList: { contains: value => classes.has(value), toggle: (value, enabled) => enabled ? classes.add(value) : classes.delete(value),
      remove: (...values) => values.forEach(value => classes.delete(value)) }, getAttribute: () => null };
  const root = { dataset: { cfxChartId: 'receiver', cfxHovering: 'true' }, reveals: ['present'],
    removeAttribute(name) { delete this.dataset[name.replace(/^data-/, '').replace(/-([a-z])/g, (_, c) => c.toUpperCase())]; },
    querySelectorAll(selector) {
      if (selector.includes('legend-item')) return [];
      return selector.split(',').some(value => classes.has(value.replace(/^\./, ''))) ? [node] : [];
    } };
  const source = fs.readFileSync(path.resolve(__dirname,
    '../../ChartForgeX.Interactivity.Html/Assets/interactive.source/40-selection-hover-sync.js'), 'utf8');
  const identity = item => ({ seriesKey: item.dataset.cfxSeriesKey,
    targetKind: item.dataset.cfxTargetKind, targetId: item.dataset.cfxTargetId });
  const apply = new Function('interactiveTargets', 'seriesKey', 'renderedTargetKind', 'renderedTargetId',
    'targetIdentity', 'clearRootBranchHighlight', 'clearReveals', 'revealTargets',
    source + '\nreturn applySync;')(
      () => [node], item => item.dataset.cfxSeriesKey, item => item.dataset.cfxTargetKind,
      item => item.dataset.cfxTargetId, identity, () => {},
      owner => { owner.reveals = []; }, (owner, targets) => {
        const represented = targets.filter(value => value.seriesKey === node.dataset.cfxSeriesKey
          && value.targetKind === node.dataset.cfxTargetKind && value.targetId === node.dataset.cfxTargetId);
        if (represented.length) owner.reveals = represented.map(value => value.targetId);
      });
  const state = () => ({ hovered: classes.has('cfx-hovered'), related: classes.has('cfx-hover-related'),
    series: classes.has('cfx-hover-series'), reveals: root.reveals.slice(), hovering: root.dataset.cfxHovering });
  const present = identity(node);
  return { root, apply: detail => apply(root, detail), state, present };
}

for (const action of ['hover', 'crosshair', 'navigate']) {
  test(`${action} ignores absent peer targets without clearing local or another sender's readout`, () => {
    for (const owner of [undefined, 'another-sender']) {
      for (const target of [
        { seriesKey: 'shared-X', targetKind: 'point', targetId: 'missing' },
        { seriesKey: 'shared-Y', targetKind: 'point', targetId: 'present' }
      ]) {
        const f = fixture();
        if (owner !== undefined) f.root._cfxPeerHoverSource = owner;
        const before = f.state();
        f.apply({ chartId: 'sender', action, target });
        assert.deepEqual(f.state(), before);
        f.apply({ chartId: 'sender', action: 'hover-clear' });
        assert.deepEqual(f.state(), before);
      }
    }
  });

  test(`${action} retires this sender's accepted readout when its next identity is absent`, () => {
    for (const target of [
      { seriesKey: 'shared-X', targetKind: 'point', targetId: 'missing' },
      { seriesKey: 'shared-Y', targetKind: 'point', targetId: 'present' }
    ]) {
      const f = fixture();
      f.apply({ chartId: 'sender', action, target: f.present });
      assert.equal(f.state().hovered, true);
      assert.deepEqual(f.state().reveals, ['present']);
      f.apply({ chartId: 'sender', action, target });
      assert.equal(f.state().hovered, false);
      assert.deepEqual(f.state().reveals, []);
      assert.equal(f.state().hovering, undefined);
    }
  });

  test(`${action} transfers accepted readout ownership and only its sender can clear it`, () => {
    const f = fixture();
    f.root._cfxPeerHoverSource = 'old-sender';
    f.apply({ chartId: 'sender', action, target: f.present });
    const accepted = f.state();
    f.apply({ chartId: 'old-sender', action: 'hover-clear' });
    assert.deepEqual(f.state(), accepted);
    f.apply({ chartId: 'sender', action: 'hover-clear' });
    assert.equal(f.state().hovered, false);
    assert.deepEqual(f.state().reveals, []);
  });
}
