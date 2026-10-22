# Native Treemap and Sunburst views

`Chart.PrepareHierarchy(context, new ChartHierarchyView("services"))` prepares an existing source item as the visible root. `PrepareHierarchy(context, null)` restores the full hierarchy. The source `HierarchyItems` remain unchanged: IDs, authored parents, original ordinals, values, node states and point paint stay attached to the same items. Numeric color scales retain the full source domain. The selected Treemap subtree uses the current plot with its configured padding and gaps; Sunburst rings rebase their visible depth and preserve inclusive authored parent totals and unused capacity.

```csharp
var renderer = new HtmlInteractiveChartRenderer();
var section = renderer.PrepareHierarchySection(chart,
    new ChartHierarchyView("services"), options => {
        options.Interaction.Enable(ChartInteractionFeatures.HierarchyNavigation
            | ChartInteractionFeatures.StateBookmarks | ChartInteractionFeatures.Export);
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
    });
// section.PreparedVisual and NarrowPreparedVisual match the two native drawings.
// Keep the unchanged chart and the accepted section/basis in the embedding host.
```

Connect the existing native callback with the hierarchy capability:

```javascript
const controller = ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
controller.connectNative(async (request, signal) => {
  // The host validates request.basis and prepares the requested root against its
  // original chart. Return the generated section and a new opaque basis token.
  return host.prepareHierarchy(request, signal); // { html, basis }
}, { basis: initialBasis, axes: null, hierarchy: true });
controller.setHierarchyView({ rootId: 'services' });
// controller.setHierarchyView(null) requests the full source hierarchy.
```

Hierarchy requests use `channel: "hierarchy"`, `kind: "root"` or `"reset"`, and `rootId` (null for reset), with the ordinary request ID, scene revision, accepted basis, adaptive drawing and cancellation signal. The host maps the root to `ChartHierarchyView` and calls `PrepareHierarchySection`. Missing IDs, unsupported families, changed source-series identity and mismatched replies are rejected; there is no silent data-copy or full-view fallback. Retain the current root when handling a different native channel, such as an annotation edit.

With `HierarchyNavigation` enabled and connected, activating a group requests its subtree; leaf activation retains ordinary selection. **Up one level** and **Full hierarchy** use the same native request. Their path and status text are localizable through `HtmlChartText`. `getHierarchyView()` returns a detached observation of the committed root and its source path. `cfxhierarchyrequest`, `cfxhierarchychange` and `cfxhierarchyerror` describe requests, accepted scenes and failures. The basis advances only after the matching section is accepted. Superseded, rejected, disconnected or disposed requests cannot change the view or export controls.

Source items outside the selected subtree remain passive semantic facts with `outside-view` geometry status; they have no paint, hit area, focus stop or selection. Source IDs inside the view keep their interaction identity. Reset requests the complete hierarchy and clears transient reader state through the existing native reset owner. SVG/PNG downloads and matching `HtmlChartSection` native exports use the committed drawing; browser selection cues remain a browser export concern. Navigation chrome is omitted from print.

With `StateBookmarks`, interaction snapshots include an optional `nativeHierarchy` observation. Replay prepares that root before restoring reader state or announcing completion; rejection preserves the committed state. Existing snapshots without that field retain their behavior. Linked bookmark state uses the receiving host's own preparation basis. Native hierarchy views currently cover one Treemap or Sunburst series, without a maximum-depth truncation policy or a new graph/explorer engine.

## Sunburst caption size and sector spacing

Both options are opt-in and use the same native drawing for Core, HTML, SVG and PNG:

```csharp
chart.ConfigureSunburst(options => {
    options.MinimumLabelFontSize = 10;
    options.SectorGap = 6;
    options.CornerRadius = 4;
});
```

`MinimumLabelFontSize` is a nullable, finite positive primary-caption floor in logical pixels. Its null default keeps fixed-size typography. The existing complete horizontal/tangential placement and balanced wrapping run at the resolved authored size first. Only a complete primary caption that still cannot fit may shrink, down to the requested floor. A larger floor never enlarges an authored style, and an impossible caption stays omitted with its complete text in source semantics. A fitting primary does not shrink to accommodate secondary text. Inherited secondary typography follows the final primary size before `SecondaryLabelStyle` overrides; an explicit secondary font size remains explicit. The secondary formatter still runs once per original item during preparation, including off-view and zero items, and never again during prepared exports.

`SectorGap` is finite and nonnegative, with a zero default that preserves the existing geometry. It separates sector fill boundaries in logical chart pixels, before the existing separator strokes. Each radial boundary reserves half the gap. Angular boundaries reserve half the gap at their inner painted arc, so the gap widens toward the outer arc. Full disks and full rings have no artificial angular seam; they retain only the radial inset and an outer margin of half the gap. Rounding is applied inward after the inset through the existing native outline.

Spacing never changes source values, inclusive parent totals, percentages, unused capacity, parents, source depth, color domains or allocated angles. Existing `data-cfx-start-angle`, `data-cfx-sweep`, `data-cfx-inner-radius` and `data-cfx-outer-radius` describe that original allocation. A positive gap adds `data-cfx-paint-*` geometry fields and `data-cfx-sector-gap` for the actual inset mark. Browser pointer acquisition and highlights use the native path, so empty gaps are not click targets. The same inset bounds constrain captions, hatch clips and native exports.

An explicit gap can exhaust a thin sector or even a small root disk. Such a positive item retains its original facts with `padding-collapse` status and has no painted mark or interactive target. Zero and precision-collapsed items remain distinguishable; with positive spacing, unpainted items are passive. The zero-gap default retains the existing zero-item keyboard readouts. A selected subtree applies the same policy after its visible root and rings rebase; Up, full reset, bookmarks and committed exports retain those configured native options. Both active options require a Sunburst chart.
