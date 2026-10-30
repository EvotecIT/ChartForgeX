---
description: "Upgrade static ChartForgeX SVG in place with states, tooltips, keyboard focus, legend toggles, family behaviours and linked views, using one small script."
---

# Enhancing static charts

A static ChartForgeX SVG is complete without script: links, titles, accessible names and an optional data table. The enhance runtime upgrades that SVG in place on any page. It reads the [DOM contract](dom-contract.md), adds states, tooltips, keyboard focus, legend toggles and linked views, and never re-renders the chart.

Use it for web pages and reports. Dashboards that need zoom, scenarios, export or annotation keep the [full interactive runtime](interactivity.md).

## Add it to a page

Export the chart with hooks, then include the runtime once per document:

```csharp
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;

var svg = chart.Prepare(context).ToSvg(new VisualSvgOptions("activity")
    .WithFluidWidth().WithInheritedFonts().WithEnhanceHooks(includeData: true));

var document = new HtmlEnhanceDocument(nonce: cspNonce);   // or new HtmlEnhanceDocument(assets: new HtmlAssetReferences("assets/"))
var html = svg + document.Assets();                         // later charts on the same page: document.Assets() returns ""
```

`HtmlEnhanceAssets.InlineMarkup(nonce)` returns the `<style>` and `<script>` pair, and `HtmlEnhanceAssets.ReferenceMarkup(assets)` links the shared files that `HtmlInteractiveAssetFiles.WriteTo(folder, HtmlEnhanceAssets.Files())` writes. On a hand-written page, one `<script src="cfx-enhance.….js" defer>` and its stylesheet are enough: every `svg[data-cfx-enhance]` is enhanced when the document loads. Add `data-cfx-manual` to the script element to call `CfxEnhance.scan(root)` or `CfxEnhance.enhance(svg, options)` yourself.

The runtime has no dependencies, uses no `eval` or HTML parsing, fills every text with `textContent`, and sets styles only through the CSS object model, so it runs under a strict CSP with a script nonce. Budgets: the core (`cfx-shared.js` and `cfx-enhance.js`) stays at or below 12 KB gzip, the built-in behaviours at or below 4 KB, the link helper at or below 2 KB, and the stylesheet at or below 2 KB. `EnhanceAssetTests` enforces them.

## What every chart gets

The core works from `data-cfx-mark`, which the hooks write on the interaction target of every family: points, bars, slices, cells, tiles, regions, words, flow and hierarchy nodes and links, schedule items, gauges and rows. So every family gets the same behaviour with no per-family code:

- **States** (design language section 3): hover lifts a mark; hovering a legend entry fades the other series to 25%; a selection is a 2px accent outline and dims the rest; marks highlighted from another view get an info-tone start bar and glow while the rest dim to 35%; focus rings appear only for the keyboard. Forced-colours mode uses system colours, and transitions run only without a reduced-motion preference.
- **Tooltip**: one per page, `role="tooltip"`, tied to the focused mark with `aria-describedby`. The value comes first, the series second, with a short line key in the series colour. It reads the chart's own formatted readouts (`data-cfx-formatted-value`, `data-cfx-range-label` and the rest); an authored mark tooltip becomes its hint. It flips and clamps to the viewport. On touch, the first tap shows it and a second tap follows the link or selects.
- **Hit targets** are at least 24px, and a pointer inside a painted mark always wins.
- **Keyboard**: one tab stop per chart. Arrows move along a series and between series, Home and End jump, Enter follows a mark's link, Space selects (Shift adds), and Escape clears. A polite live region announces each mark.
- **Legend**: entries become toggle buttons (`aria-pressed`). Shift isolates. A pie or donut legend toggles its slice. Colours never change.
- **Selection and events**: clicking or pressing Space selects a mark. The chart dispatches `cfx:hover`, `cfx:select`, `cfx:linkchange` and `cfx:legend`. Links keep native navigation unless the chart is linked (`selectOnClick`), and Ctrl or Cmd-click always navigates.

## Behaviours

Family gestures and keyboard models are behaviours. The built-in ones register through the same public call a host or another package uses:

| Behaviour | Attaches when | Adds |
|---|---|---|
| `crosshair` | The root has `data-cfx-crosshair` (vertical lines and areas) | A 1px hairline snapping to the nearest X, dots on each series, a shared-X tooltip (all series at that X, from the data island when present), column selection, and a brush across a linear or time axis that emits a range |
| `grid` | Cells, tiles, regions, words, pictorial items, map points, flow and hierarchy nodes | Arrows move to the nearest mark on screen in that direction |
| `rows` | Schedule items, bullets, progress rows, funnel and pyramid stages | Up and Down follow the rows as drawn |
| `radial` | Slices, rings, radar and polar points, gauges, and Cartesian points without a plot | Arrows walk around the ring and wrap |

A behaviour is `{ match(kit), attach(kit) }`. `match` decides from the chart's model; `attach` returns hooks. Charts already on the page receive a behaviour registered later. Pass `behaviours: ["grid"]` to `CfxEnhance.enhance` to limit a chart to named behaviours.

The kit holds `api`, `svg`, `marks` (each with `el`, `s`, `p`, `role`, `x`, `head`, `value`, `keys`, `fields`), `series`, `order`, `bySeries`, `data` (the island), `roles`, `layer` (an SVG group for overlays) and the helpers `rects`, `user`, `box`, `plot`, `inside`, `visible`, `color`, `hit`, `markOf`, `hover`, `tip`, `choose`, `clear`, `paint`, `go` and `iso`.

| Hook | Called with | Return |
|---|---|---|
| `pointer(e)` | Pointer moves | `true` when the behaviour showed its own hover |
| `down(e)`, `drag(e)`, `up(e, cancelled)` | Pointer gestures | `drag` returns `true` while it owns the gesture |
| `click(e)` | Clicks, before selection | `true` to consume the click |
| `targets(e)` | A click | The marks to select |
| `key(e, mark)` | Key presses on a mark | The next mark, `true` when handled, or nothing for the default |
| `focus(mark, x, y)` | Keyboard focus | `true` when the behaviour showed its own hover |
| `group(mark)` | Space or Enter, and announcements | The marks that act together |
| `describe(marks)` | A selection | `{ label, range }` for the link payload |
| `tooltip(marks, content)` | Before showing | The tooltip content |
| `select(selection)`, `paint()`, `leave()` | State changes | Nothing |

### Example: jump a week in calendar heatmaps

```js
CfxEnhance.behaviour("week-jump", {
  match: kit => !!kit.roles["calendar-cell"],
  attach: kit => ({
    key(e, mark) {
      if (e.key !== "PageDown" && e.key !== "PageUp") return;      // other keys keep their default
      const cells = kit.bySeries[mark.s].filter(kit.visible), i = cells.indexOf(mark);
      return cells[Math.max(0, Math.min(cells.length - 1, i + (e.key === "PageDown" ? 7 : -7)))];
    }
  })
});
```

The runtime moves focus, shows the tooltip and announces the new cell. A behaviour that throws is reported to the console and skipped; the chart keeps its generic behaviour.

### Tooltip content

Supply a `<template data-cfx-tooltip="view-id">` (or `data-cfx-tooltip=""` for every chart) with elements marked `data-cfx-slot="head"`, `"row"`, `"key"`, `"value"`, `"name"` and `"hint"`. The runtime clones it and fills it with text only. To change the content instead, pass `tooltip(marks, content, kit)` to `CfxEnhance.enhance`, or register a formatter for a family:

```js
CfxEnhance.tooltip("treemap-tile", (marks, content) => ({ ...content, hint: "Share of the parent group" }));
```

## Linked views

Every enhanced chart implements the LinkedView methods with CanopyX's names and payloads, so a CanopyX grid links with no adapter, and so does any other view with the same shape:

| Method | Behaviour |
|---|---|
| `setExternalFilter(source, { field, values } \| { field, range } \| [..] \| null)` | Dims marks whose field values fall outside (or hides them with `filterMode: "hide"`). Marks without that field are unaffected. Resolves `true`. |
| `clearExternalFilters(source?)`, `getExternalFilters()` | As in CanopyX. |
| `setHighlight(keys, { source, scroll })` | Marks holding any key get the linked state; the rest dim. Resolves with the number of keys. Scrolls only when `scroll: true`. |
| `clearHighlight()`, `getHighlight()`, `startLinkEvents()` | As in CanopyX. |
| `getSelection()`, `clearSelection({ silent })` | The chart's own selection. |

A mark's values come from `data-cfx-fields`, and its keys from `data-cfx-keys` (or the data island). Set them with `ChartMarkOptions(keys: …, fields: …)`; series fields are inherited by their marks.

Selecting dispatches `cfx:linkchange` with the `canopyx:linkchange` shape: `{ view, action: "select" | "clear", scope: "select", keys, values, label, range? }`. A brush or a time column adds `range: { column, from, to }` with `to` exclusive, as a CanopyX calendar reports it. Applied filters and highlights never dispatch `cfx:linkchange`, so two views that follow each other cannot echo.

`CfxLink.connect(source, target, options)` wires two views both ways:

```js
const chart = CfxEnhance.get(document.getElementById("activity"));
CfxLink.connect(chart, document.querySelector("#records"), { field: "day", mode: "filter", sourceLabel: "Activity chart" });
CfxLink.connect(trendChart, versionsChart, { field: "day" });            // chart to chart: brush one, filter the other
CfxLink.connect(chart, tableView, { mode: "highlight" });                   // any object with the LinkedView methods
```

- The source's selection filters the target by `field` (a time range becomes an inclusive `range`), or highlights its keys with `mode: "highlight"`.
- The target's selection highlights the source; its own conditions (`action: "filter"`) filter the source.
- When the reader removes the source's chip in the target (`externalfilter` with `reason: "user"`), the source clears its selection silently.
- The target may be a CanopyX host element (its `.canopyx` API is used), a ChartForgeX chart (its `cfx:` events are used), or any LinkedView object with a `host` or `element`. `options.events` names other event types. `connect` returns a `disconnect` function.

## Shared parts

`cfx-shared.js` (the tooltip anatomy, the live region and DOM helpers), `cfx-states.css` (the state classes and tokens) and `cfx-link.js` (the link protocol) are separate modules so the full interactive runtime and other packages can adopt the same look and protocol. Read one with `HtmlEnhanceAssets.Module(name)`.
