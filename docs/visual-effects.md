---
description: "Add shadows, glow, blur, masks and blend modes to charts, diagrams and canvases, with the same result in SVG and PNG."
---

# Visual effects

ChartForgeX can add drop shadows, inner shadows, glow, outlines, bloom, blur, masks and blend modes to a prepared chart, diagram or canvas, and it draws them the same way in SVG and in PNG. Effects are decoration: they never carry data on their own, and every default output stays exactly as it was until you ask for an effect.

## What you can use today

- **Capture a layer.** `PreparedVisual.CaptureLayer(effect)` captures one complete prepared view and applies a `VisualLayerBlur`, `VisualDropShadow`, `VisualInnerShadow`, `VisualMask`, outline or bloom to it. The result is a detached `CapturedVisualLayer`: its `Visual` is a finished image, and the original source facts stay on `Source`.
- **Keep the vectors.** `prepared.WithDropShadow`, `WithInnerShadow`, `WithOutline`, `WithBloom` and `WithMask` wrap the whole view without flattening it. Text, links and source IDs stay live, and the effects can be saved to portable JSON and replayed later.
- **Shadows on sequence diagrams.** Participants, notes, fragment tabs and backgrounds accept a `VisualDropShadow` directly.
- **Blend and composite.** In `ChartForgeX.Visuals`, `ImageComposition.DrawLayer` blends a captured layer with every standard blend mode plus `PlusLighter`, or with Porter–Duff operators such as `SourceIn`, `DestinationOut` and `Xor`, and supports a local backdrop blur.
- **Import simple SVG filters.** `SvgRasterFilterOptions` lets the PNG renderer draw a bounded set of SVG filters: Gaussian blur, drop and inner shadows built from morphology, offset, flood and composite, and flood/composite stencils. Anything outside that set reports `SFR002`.

Two SVG export profiles are available through `VisualSvgOptions.EffectProfile`:

- `Baked` (the default) embeds the effect as a PNG behind or in place of the vectors. It looks the same everywhere an SVG image is shown.
- `NativeFilters` writes a script-free SVG `<filter>` or `<mask>` on the original vectors, so the file stays small and text stays selectable. Browsers blur and round a little differently from the native PNG renderer, so expect small pixel differences.

## What's planned

I want one effects vocabulary for every family: a chart series, a Mermaid node, a topology path, a canvas tile, a story surface and a wallpaper background should all take the same effect definitions, resolve them through the theme, and render through the same SVG writer and PNG kernels. The next steps are:

- **Theme effect presets.** `VisualThemeEffects` slots such as card elevation, tile elevation, glow, text halo, glass and grain, so one theme restyles every family without per-chart code.
- **Backdrop effects.** Frosted glass, grain, vignette, light streaks, tapered and gradient strokes, and generators for perspective grids, particles and dot-matrix maps.
- **Family adoption.** Series glow and card elevation on charts, highlight and shadow mapping for Mermaid, elevation and path glow for topology, glass tiles on canvases and backgrounds for stories, followed by PowerBGInfo and ImagePlayground moving onto the shared effects.

Existing outputs won't change: the current card shadow, canvas glow, terminal shadow and line halo recipes keep their exact appearance, and new looks are opt-in. Effects will degrade sensibly per target: email and Office hosts get PNG, standalone SVG files bake what viewers can't draw, and reduced-motion and forced-colours settings drop glass and animated glow.

## Shadows on vector surfaces

`VisualDropShadow` also describes shadows behind native Sequence surfaces. Assign
`SequenceArtifactParticipant.SurfaceShadow` for a glyph/box, or
`SequenceArtifactPaint.NoteShadow`, `FragmentTabShadow` and `BackgroundShadow` for
those contours. Captions, safe links and source regions stay separate and appear
once. Null leaves the ordinary scene unchanged. Descriptors are immutable;
prepared and portable snapshots retain them independently of later model edits.

```csharp
var shadow = new VisualDropShadow(ChartColor.Black, 4, 4, 0,
    opacity: .06, relativeRegion: new ChartRect(-.1, -.1, 1.3, 1.3));
sequence.Participants[0].SurfaceShadow = shadow;
var prepared = sequence.Prepare(context);
var bakedSvg = prepared.ToSvg();
var filterSvg = prepared.ToSvg(new VisualSvgOptions(
    effectProfile: VisualSvgEffectProfile.NativeFilters));
var png = prepared.ToPng(new VisualRenderOptions(scale: 2), cancellationToken);
```

Opacity multiplies the tint alpha once, before the final RGBA8 conversion.
`relativeRegion` is in fractions of the surface geometry box, excluding its
centered stroke. It clips only shadow support. Public `CaptureLayer(shadow)`
continues to return the original source plus its shadow; an authored region
cannot crop that original source.

The default `Baked` SVG profile inserts a decorative shadow-only native PNG
behind retained vector surfaces. `NativeFilters` emits a finite, script-free
SourceAlpha/morphology/Gaussian/offset/flood/in/merge filter on those same vectors.
It has no decorative clone or `use` instance. Both profiles retain live vector
text and single source identities. An explicit `CaptureLayer` image remains an
image: changing the export profile cannot reconstruct a discarded operation.

`effectRasterOptions` on `VisualSvgOptions` uses the existing density, coverage
and pixel cap for baked effects. Native filters do not allocate native effect
pixels and ignore that raster selection; their finite parameters and support
remain bounded. Native PNG uses the existing three-box Gaussian approximation.
A browser's Gaussian implementation need not have identical pixels. Baked
shadows capture resolved literal alpha; use native filters if later host color
variables must also change the shadow's source alpha.

One existing raster preflight reserves source, downsample, effect, isolation and
result pixel arrays cumulatively before allocating them, including group
buffers and borrowed images. Encoder, font and geometry allocations retain
their established separate boundaries; these are not process-memory or browser
resource guarantees. Operation cancellation is not retained by a scene and is
checked at effect allocation, scope/kernel rows and encoding boundaries.

Surface shadows apply to numeric native geometry;
its text, images and nested surface effects are deliberately rejected. Ordinary
whole-scene capture still supports a scene containing retained surface effects.
The SVG importer retains its documented finite, device-aligned subset; the
native filter export profile does not imply it can import every emitted
fractional region or arbitrary SVG filter graph.

Mermaid sequence sources keep the neo look's glyph filter: 4/4 offset,
zero sigma, exact 6% opacity and object-relative region. `dropShadow:none`
disables stylesheet roles and does not disable that inserted glyph filter.
Known preset function descriptors and one finite pixel-unit `drop-shadow`
function use the same native shadow. The known preset bare `#drop-shadow`
style target does not resolve in standalone Mermaid Sequence output and paints
nothing; native lowering preserves that no-paint result. It is not an alias for
the namespaced inserted glyph filter. Explicit unsupported URL targets,
fractional logical offsets, chained functions and other unsupported inputs retain
located diagnostics. Genuine omitted-default examples keep their source effects
enabled and no longer report an unapplied loss for that faithfully absent paint.

Complete-root masks, outer and inner shadows, outlines and bloom share the ordered portable replay described below.
Backdrop blur and layer blends are available through ImageComposition in ChartForgeX.Visuals.


## Masks on a prepared view

`prepared.WithMask(mask)` returns another detached `PreparedVisual`. It retains
source regions, projections, viewport and source semantic facts independently.
The original prepared source remains unchanged. Use the same immutable
`VisualMask` as `CaptureLayer(mask)`; its device density must be supported by the
existing `VisualRenderOptions`. Ordered retained masks share one density,
and native export options must select it. Conflicting grids reject clearly;
there is no implicit resampling.

```csharp
var masked = prepared.WithMask(mask);
var nativeSvg = masked.ToSvg(new VisualSvgOptions(
    effectProfile: VisualSvgEffectProfile.NativeFilters));
var bakedSvg = masked.ToSvg(new VisualSvgOptions(
    effectRasterOptions: new VisualRenderOptions(scale: mask.PixelsPerUnit)));
var png = masked.ToPng(new VisualRenderOptions(scale: mask.PixelsPerUnit), token);
```

Native PNG captures the completed local object through the existing raster
compositor and mask plan, then applies outer clip/opacity once. A complete-view
mask returns that result directly, preserving nonzero unassociated source RGB
without an additional image-sampling pass. The shared preflight counts capture,
returned source, mask result, isolation and borrowed bitmap arrays once, including
nested complete-view results reused as inputs, and keeps the existing capture,
kernel work and pixel limits. Operation
cancellation does not enter the scene or mask snapshot.

The default Baked SVG profile is explicitly a static image of the complete masked
object. The NativeFilters profile keeps its original vector scene under a typed
alpha mask with an embedded bitmap input. It keeps the current text/font
backend, source IDs and safe links; it does not create cloned source glyphs.
Masking is a presentation operation: descriptive source regions do not become a
per-pixel hit-test map. A mask may intentionally hide source paint while those
facts remain available.

Native `VisualMaskMode.Luminance` uses its documented sRGB coefficients
0.2126/0.7152/0.0722. Native SVG materializes that same coverage into an alpha-only
input with the native coverage/alpha conversion, rather than
substituting the SVG standards luminance coefficients. This explicitly quantizes
the mask before browser source-alpha multiplication, so browser pixels can differ
from native PNG's single final output conversion. Alpha mode can reuse its
original alpha bitmap. Native SVG uses the mask's captured grid and does not
allocate a native source canvas; its coverage input/output budgets still apply.
No browser/font/encoder process-memory or universal pixel identity is promised.

Complete-root ordered masks carry typed portable inputs through the common envelope.
Other captured scopes reject interchange even if a host manually adds the Json flag.
Native SVG/PNG, accessibility, regions and source metadata remain usable independently.
There is no per-family output-policy bag or generic scene rehydrator.

### Saving a retained mask

A complete-root ordered `PreparedVisual.WithMask` chain exports its original
RGBA inputs and policy as `Mask` steps in `presentation.capturedEffects`. The common envelope emits
version2 only when required. Empty lists add no fields or default facts. RGBA bytes
use canonical base64 under the existing8MiB envelope limit; large native-valid
masks can still render while portable export rejects the limit. The payload keeps
native `VisualMask` luminance/dither/density and exact original RGB, including RGB
at zero alpha. It does not store sampled browser coverage or a scene-command graph.

Editable family imports reject an envelope carrying captured effects. To edit source,
read a detached second envelope, explicitly clear its `Presentation.CapturedEffects`,
and use the existing typed importer. Prepare that model in the captured viewport,
then call `original.ApplyCapturedEffects(preparedSource)`. The caller owns layout and
theme selection; replay neither reflows source nor resamples its mask. A different
viewport or already effect-wrapped source rejects. Source IDs, links, metadata, regions
and projections remain separate from mask pixels.

Complete-root mask wrappers can be ordered with the other supported retained effects.
Embedded or transformed mask scopes still reject common interchange and omit Json support;
SVG/PNG remain valid. Baked SVG and NativeFilters retain their explicit existing
representation/quantization differences. Portable replay does not make fresh family
layout or browser/native pixels universally identical. The ordinary retained-mask
program saves `masked.json`, restores it, explicitly edits `Messages[0].Text`, and
replays the same captured mask.

### Explicit source and destination alpha operators

`VisualCompositeOperator` selects Porter–Duff source/destination coverage, separately
from the existing `VisualBlendMode` channel blends. In optional Visuals, call the
mandatory-operator `ImageComposition.DrawLayer(source, x, y, operation, ...)` over
the composition's current explicit backdrop. The same native plan, fixed device
grid, preallocation/work budgets, row cancellation and atomic commit apply.
Source outside the placed capture is transparent across the whole destination
frame. `Copy` preserves exact source RGBA inside its extent and clears outside;
`Clear` clears the entire frame. No ambient page backdrop is inferred.

The old three-argument affine sampler and explicit Normal/Multiply/Screen/
PlusLighter paths keep their behavior. The same required-mode overload also accepts
Overlay, Darken, Lighten, ColorDodge, ColorBurn, HardLight, SoftLight, Difference,
Exclusion, Hue, Saturation, Color and Luminosity. Unassociated normalized helpers
are shared with COLR without moving its linear-light conversion or float caller
arithmetic into gamma-sRGB effects. Standard mode channels enter the same existing
16-bit overlap and final quantizer. Finite feBlend imports require computed sRGB,
SourceGraphic against the known flood and normal source-over alpha. PlusLighter still adds/clamps
premultiplied channels AND alpha; it is not duplicated in the coverage enum.
Gamma sRGB16 and one final optional dither quantizer remain the effect contract;
COLR's linear-light float composition remains separate and unchanged.

The finite source fast path admits explicit user-space `feFlood` followed by
`feComposite` with SourceGraphic against that known flood result and
`over`/`in`/`out`/`atop`/`xor`, in computed sRGB. It uses the same math and existing
source style, filter and context handling. The [ordered native filter path](../api-conventions/#ordered-native-svg-filter-inputs)
also supports earlier named results, arithmetic composition, computed sRGB or
linearRGB and aligned primitive subregions. Missing or forward result names use
the omitted-input semantics; uncaptured backdrop/paint inputs and unrepresented
primitives retain located strict diagnostics. A null filter option keeps the
earlier unsupported behavior. Browsers can round and blur slightly differently from
the native renderer, so pixels are close rather than identical.

The complete Core-only source program emits all five finite SVG/PNG examples.
The separate Core+Visuals composition program creates actual source-in stencils,
destination-out cutouts and xor overlaps from explicit captured native data.
It stays outside the Core-only recipe glob; no dependency expansion is needed.
Outline and bloom captures, local backdrop blur and root replay use the same native plan described below.


## Glow

Use an existing `VisualDropShadow` with offset0/0, positive sigma and an optional positive spread. The source's alpha selects the glow, including dark paint that contributes no Bloom bright pass. The shared kernel applies square alpha spread and the existing three-box Gaussian approximation, then gamma-sRGB16 tint/normal under-source composition with one final conversion. The source appears once. This is a soft halo rather than Outline's hard difference band or Bloom's threshold/Screen operation.

`NativeAppearanceExample` selects sigma3/spread1/raw opacity.6 and a resolved Light/Dark tint; its ordinary outline/bloom defaults are unchanged. It shares one request between the displayed native capture and retained vector profile, and the same finite source-import program uses the existing explicit sRGB alpha-shadow chain. Capture Bounds include propagated support; retained fixed-root support clips at the original viewport. Mask order and semitransparent coverage remain explicit. The complete Core program saves source, effect JSON and replay without a new request type, renderer, dependency, theme preset or alias. Browsers may blur and round slightly differently.

## Saving and replaying effects

`PreparedVisual.WithOutline`, `WithBloom` and `WithDropShadow` retain the complete child in its original
viewport. `WithMask` joins the same ordered scope. SVG `NativeFilters` retains vector text, links and source IDs
once; `Baked` paints one source-plus-effect image. PNG uses the canonical native effect plan. These profiles do
not promise browser-identical filter sampling or quantization. Imported general outline/bloom filter graphs are
outside the SVG filter subset the importer reads; exporting a native filter does not imply arbitrary graph import.

Outline uses the nonnegative dilated-alpha difference, Bloom uses the explicit gamma-sRGB luminance ramp and
Screen merge, and DropShadow paints under the complete source. Transparent shadow tint/opacity is identity.
Zero offset and sigma alone are not: a nonzero tint still changes semitransparent source. Fixed-root support clips
at the source viewport; ordinary `CaptureLayer` retains expanded output support.

The single ordered `Presentation.CapturedEffects` list holds the saved effects. Each step has one typed effect.
For root presentation use `ApplyCapturedEffects(saved, freshPreparedSource)`. A terminal capture records actual
output Bounds/density/options in its `CaptureFrame`, while envelope Width/Height stay the source viewport.
Use `CapturedVisualLayer.ToArtifact()` to save it, and `ApplyCapturedLayer` to replay root effects, capture once
and apply subsequent captured-layer masks. A source edit that changes that output footprint rejects and needs
a fresh capture. Baked terminal visuals intentionally carry no stale source-coordinate animation plans.

`NativeRetainedAppearanceExample` is a complete Core-only save/load/edit consumer. Its generated program uses
the existing typed Sequence importer after explicitly clearing effects on a detached source copy. It preserves
raw source facts and the presentation separately; it does not reconstruct native scene commands from JSON.


### Inner shadow

`PreparedVisual.CaptureLayer(new VisualInnerShadow(...))` captures an inner shadow in the original viewport. `WithInnerShadow` retains the same operation on the complete root, keeping source geometry, text, links and semantic facts independent of its presentation. Positive inset spread erodes source alpha once; the existing three-box Gaussian and offset resolve that field, and its complement is tinted SourceAtop the original. The complementary exterior is one. Output alpha remains the original source alpha, including semitransparent marks. This uses gamma-encoded sRGB premultiplied16 channels with one final quantization; it is distinct from an outer alpha glow, an outer band and luminance-thresholded bloom.

Transparent tint or opacity zero preserves ordinary source bytes. Zero spread, blur and offset alone is not a general identity: a half-transparent source still has a nonzero complementary field. At zero coverage the original raw sample is copied. Inner output is clipped to the original frame; offset/blur support still counts toward the existing preallocation and work ceilings, and operation cancellation is observed by the shared capture, kernel and output steps.

Ordered `CapturedEffects` has one `InnerShadow` payload per selected step, in the version 2 envelope. Root replay uses `ApplyCapturedEffects`; a terminal `CaptureLayer` records its actual original-sized output and uses `ApplyCapturedLayer`. A model-only importer rejects a retained appearance instead of silently dropping it. `NativeFilters` emits the finite SourceAlpha morphology/Gaussian/offset/flood/out/atop chain on the original vectors; Baked uses the native kernel. Browser filter precision, blur implementation and channel quantization may differ, so profiles do not promise pixel identity. SvgRaster preserves the exact explicit user-space sRGB chain through its finite fast path. Other represented compositions, sRGB/linearRGB stages and aligned primitive subregions use the [ordered native filter path](../api-conventions/#ordered-native-svg-filter-inputs); implicit backdrop inputs remain unsupported.

The `inner` selection of the complete NativeAppearance example and generated Core program shows compact/wide Light/Dark capture, vector NativeFilters output and typed replay. It adds no composition package to the Core-only program.
