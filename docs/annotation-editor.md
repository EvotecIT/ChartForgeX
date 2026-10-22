# Editing source annotations

The HTML adapter can edit native notes, finite line segments and parallel channels. The editor keeps a transient source-coordinate draft. Applying an edit asks the application's existing native preparation callback for a replacement section; the committed chart changes only after that section passes validation.

Connect the [native preparation bridge](interactivity.md#connected-native-x-windows) before enabling the editor:

```javascript
const controller = ChartForgeXCharts.get('#measurements');
controller.connectNative(renderNativeSection, {
  basis: initialNativeToken,
  axes: null
});

const editor = controller.editAnnotations({
  historyLimit: 32,
  createId: kind => crypto.randomUUID()
});
```

`renderNativeSection(request, signal)` is the application's bridge to its native chart model. It returns `{ html, basis }` through the same accepted-section owner used by viewport changes. The adapter does not implement transport, storage or a second chart model. `axes: null` selects annotation-only preparation; a connection can also serve physical viewport axes.

The toolbar adds drawings, edits captions, removes authored drawings and replays accepted edits. Supplying `createId` enables the Add controls; the application owns durable identities. Without it, existing authored source drawings remain editable. Notes have one anchor, lines have two, and channels add a source-Y offset handle. Drag a handle or focus it and use the arrow keys; Shift moves ten logical pixels. Apply commits a draft, Escape cancels it, and Enter on a handle applies it.

The same draft can be controlled programmatically:

```javascript
editor.begin({
  operation: 'add',
  id: 'review.threshold',
  drawing: {
    kind: 'LineSegment',
    start: { x: 2, y: 35 },
    end: { x: 8, y: 45 },
    label: 'Review threshold',
    yAxis: 'Primary'
  }
});
editor.preview({
  kind: 'LineSegment',
  start: { x: 2, y: 35 },
  end: { x: 8, y: 50 },
  label: 'Review threshold',
  yAxis: 'Primary'
});
editor.commit();
```

`begin` accepts `add`, `replace` or `remove`. Replace can omit its drawing to start from the current authored geometry; remove omits the drawing. `preview` replaces the current drawable draft without changing the committed scene. Draft handles and guides are excluded from SVG and PNG downloads. Source drawings retain the native vertical Cartesian contract and an explicit primary or secondary Y axis; the editor requires its captured, unambiguous physical projections.

`commit`, `undo` and `redo` start native preparation and return immediately. `getState()` reports detached draft data, pending/error state and history availability. A rejected reply leaves the committed chart and history intact. Cancel aborts pending work, and stale completions cannot enter history. Undo and redo verify the current authored source before requesting a change, so another owner's drawing is not overwritten by an obsolete command.

History contains only native-accepted commands, with a configurable limit from zero through 256. Zero disables history. An ordinary external update or native reconnection clears the draft and history. `disable()` hides the editor and cancels its draft; disposing the chart releases editor controls, pointer capture and listeners. UI text uses the existing `HtmlChartLocalization` bundle and `HtmlChartText.Annotation…` keys.

Applications can persist accepted changes through the chart root's event:

```javascript
controller.root.addEventListener('cfxannotationcommit', event => {
  const { id, before, after, history } = event.detail;
  persistAcceptedDrawing({ id, before, after, history });
});
```

`before` and `after` are source drawings or null; `history` is `edit`, `undo` or `redo`. Existing `cfxannotationrequest`, `cfxannotationchange` and `cfxannotationerror` notifications retain their native request identities. The application decides whether and where to persist them.
