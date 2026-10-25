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


## Curved primary captions and difficult Treemap names

Sunburst can opt into circular primary captions without changing its source allocation:

```csharp
chart.ConfigureSunburst(options => {
    options.CurvedLabels = true;
    options.MinimumLabelFontSize = 11;
});
```

`CurvedLabels` defaults to false. With it enabled, a noncentral ring first tries one complete primary name on an upright circular lane. Preparation shapes the complete name once, retains its captured font faces, glyph clusters, ligatures, offsets and advances, then places those unchanged clusters through the ordinary scene text and rotation owners. Each cluster's native ink must fit inside the actual inset and rounded sector. SVG and native PNG use the same captured placement; no external renderer, JavaScript or SVG `textPath` layout is required.

The curved path currently supports bounded single-line, nonjoining text with monotonic source clusters. Joining and bidirectional scripts, combining sequences, supplementary characters, decorated text, absent outline fonts and long or unfittable runs use the ordinary complete horizontal/tangential/wrapped policy. Central captions and captions with secondary text also retain that policy. `hierarchy.curved-label-fallback` records the reason. An explicit primary font floor can recover a complete curved or straight caption, but does not enlarge text. Unsupported text is neither split and reshaped nor silently removed; the existing overflow diagnostic applies if ordinary placement cannot fit it. Complete names and numeric facts remain in descriptive regions and source metadata. This is a supported curved-caption policy, not a guarantee for every script, font or dense hierarchy.

Treemap provides a separate opt-in policy for difficult names:

```csharp
chart.ConfigureTreemap(options => {
    options.MinimumLabelFontSize = 11;
    options.LabelTrimming = TextTrimming.Ellipsis;
});
```

The null font floor and `TextTrimming.None` defaults retain complete-or-omitted captions. Complete wrapping, ordinary type and tall-strip rotation take priority. Only a primary name that still cannot fit may shrink to the supplied finite positive floor; a higher floor never enlarges it. A fitting primary name does not shrink merely to include its value row. When complete placement remains impossible, `Ellipsis` permits measured word/character wrapping and a whole-text-element ellipsis at the configured floor or ordinary size. Group headers stay on one row. Numeric captions remain complete or are omitted, never abbreviated. Shortening or omission reports `hierarchy.label-overflow`, while full authored names, raw values, IDs and parents remain in source facts. Unsupported family configuration fails preparation.

The complete [curved Sunburst recipe](../ChartForgeX.Examples/Recipes/SunburstCurvedLabelsRecipe.cs) and [Treemap caption-policy recipe](../ChartForgeX.Examples/Recipes/TreemapCaptionPoliciesRecipe.cs) are registered with the native gallery and downloadable standalone source. Both use ordinary package imports and the same chart factories for compact and wide poses.
