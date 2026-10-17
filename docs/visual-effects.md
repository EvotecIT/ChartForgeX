# Visual effects

Status: **design, not implemented.** This document defines one shared visual-effects system for every ChartForgeX family. Implementation follows the phases in [Delivery phases](#delivery-phases); the active checklist lives in [TODO.md](../TODO.md#visual-effects).

## Purpose

Wallpapers, social cards, report covers, dashboards, story frames and diagram highlights increasingly need effects that ChartForgeX cannot draw today. These include soft shadows and glow, frosted-glass panels, light streaks, grain, blend modes, masks, dot-matrix maps, perspective grids and vignettes. Consumers currently build them by hand:

- PowerBGInfo composes glows, grid and dot patterns, and glass tiles on `ImageComposition`/`VisualCanvas`, and turns card shadows off.
- ImagePlayground blurs, vignettes and fades through OfficeIMO, copying pixels between `RgbaImage` and `OfficeRasterImage` in both directions.
- Inside ChartForgeX itself, several separate approximations exist (see [Existing effects to migrate](#existing-effects-to-migrate)). Canvas glow and terminal shadow have known SVG/PNG differences; other inexpensive effects already have parity and should keep it.

The goal is one vocabulary and one implementation:

- A **chart series**, a **Mermaid node**, a **topology path**, a **canvas tile**, a **story surface** and a **wallpaper backdrop** all use the same effect definitions.
- They resolve them through the same theme.
- They render through the same SVG writer and the same raster kernels.

### Scope

- Layer effects: drop shadow, inner shadow, outer glow, layer blur, bloom.
- Backdrop effects: frosted glass (backdrop blur with tint, grain and highlight).
- Compositing: layer opacity, blend modes, isolation, alpha and luminance masks.
- Paint: linear, radial and conic gradients; gradients along a stroke; tapered stroke width; seeded noise and grain.
- Generators that emit ordinary geometry: perspective grids and horizons, Poisson-disk particles, dot-matrix sampling of shapes (for example a dotted world map from the existing map catalog), light streaks.
- Output profiles that decide, per target, whether each effect is emitted natively, emulated, baked or replaced by a fallback.

### Non-goals

- No 3D engine. "Depth" comes from 2D effects and projected geometry.
- No product looks in core. ChartForgeX ships primitives, presets and a few neutral theme effect tokens. A "neon admin HUD" look belongs to its consumer.
- No scripts, runtime package dependencies or platform codecs. The existing rules in [AGENTS.md](../AGENTS.md) apply unchanged.
- No public, editable scene DSL. Scene commands stay internal, as [architecture.md](v2/architecture.md) requires.
- Effects never carry data. A glow may emphasise a series that is already distinguished by shape, label or position. It is never the only signal.

## Principles

1. **One owner per concern.**
   - Effect definitions live in `ChartForgeX.Effects`.
   - SVG emission lives in one `SvgEffectWriter`.
   - Raster application lives in one `RasterEffects` module with a bounded layer stack.
   - Families attach effects; they never implement them.
2. **The native PNG path is the reference.** Every effect has a managed raster implementation. SVG reaches parity natively where targets allow, and otherwise through emulation, baking or a declared fallback, always reported in diagnostics.
3. **Deterministic everywhere.** The same input produces the same pixels on net472, netstandard2.0, net8.0 and net10.0:
   - integer kernels;
   - the library's own seeded generator (never `System.Random`);
   - no transcendental functions per pixel;
   - SIMD only where scalar and vector output hashes are proven equal.
4. **Preserve defaults.** Every existing default output stays byte-identical in its own backend, including presets with a nonzero card shadow and legacy recipes whose SVG and PNG already differ. New shared effects require explicit authorship or a new opt-in theme. Migrating allocation or pooling preserves the legacy compositing and mask arithmetic; it does not opt a caller into new precision, dither or a different appearance. Retiring a legacy appearance requires a separately identified visual migration and inspected baseline changes.
5. **Bounded cost.** Every effect declares its extent. Preflight bounds graph structure, total pixel-pass work, geometry and concurrently live working memory, as well as individual allocations. Producers reserve room inside their padding and report a diagnostic instead of growing the viewport. See [Performance](#performance) for the shared budget boundary.
6. **Theme-driven.** Colours can be literal or reference theme roles, so light and dark resolve correctly. Named theme effect tokens let one theme give every family the same elevation, glow and grain.
7. **Accessible by construction.**
   - Decorative layers are marked decorative.
   - Text over glass is checked against the worst pixel behind it.
   - Reduced-motion and forced-colours output degrade gracefully.

## Model

### Public vocabulary

New namespace `ChartForgeX.Effects` in the core package. Every type is immutable, so every package (Visuals, Stories, Mermaid, Interactivity.Html) can share instances safely.

| Type | Purpose |
| --- | --- |
| `VisualEffect` | Abstract base for one effect. Each effect carries an `EffectExtent`, which reports how far it paints beyond its source bounds. |
| `VisualDropShadow` | Offset, blur, spread, colour. |
| `VisualInnerShadow` | Offset, blur, spread, colour, painted inside the source alpha. |
| `VisualGlow` | Outer glow: dilate, blur, flood with colour. An optional blend (screen or plus-lighter) makes light accumulate. |
| `VisualLayerBlur` | Blurs the layer itself. |
| `VisualBloom` | Soft threshold, then a blur pyramid, then an additive composite. |
| `VisualBackdropBlur` | Frosted glass: blur of what is underneath, then tint, saturation, grain and highlight edge, clipped to the layer shape. |
| `VisualEffectStack` | An ordered list of effects applied to one layer. |
| `VisualBlendMode` | One public enum: `Normal`, the 15 W3C separable and non-separable modes, plus `PlusLighter`. sRGB semantics by default. |
| `VisualMask` | Alpha or luminance mask from a gradient, a shape or a nested layer. |
| `VisualPaint` | Solid, linear, radial or conic gradient, noise (spec-exact turbulence) or grain. Usable as a fill and as a stroke paint. |
| `VisualStrokeProfile` | Colour and width along cumulative path length. Uses shared arc-length sampling and bounded outline subdivision, with bake/fallback when that lowering cannot meet its error budget. |
| `VisualColorRef` | A literal colour, or a theme role with optional alpha. Preparation captures both the resolved raster colour and its `SvgPaint` binding for SVG colour-variable export. |

**Generators** produce ordinary scene geometry. That geometry needs no effect lowering; an attached glow, blur or blend still follows the effect profile:

| Generator | Output |
| --- | --- |
| `VisualPerspectiveGrid` | Horizon and floor-grid lines projected with a homography, with spacing-based depth fade (lines closer than about 2 px fade out). |
| `VisualParticleField` | Bridson Poisson-disk points from the library PRNG, drawn as small marks with optional glow. |
| `VisualDotMatrix` | Samples a shape on a grid and emits a dot wherever the shape covers the grid point. It is used with existing geometry, for example map regions from `ChartMapCatalog`. |
| `VisualLightStreak` | A curve with a stroke profile and an anisotropic blur (σx ≫ σy), blended additively. |

**`VisualBackdrop`** is a composable background description, defined once and reused by `VisualCanvas`, `ImageComposition`, chart and grid card surfaces, and story scenes. It has these parts:
- base paint;
- glows;
- pattern;
- generators;
- grain;
- vignette.

PowerBGInfo's `New-BGInfoBackground` becomes a thin mapping onto it.

### Internal primitive IR

Every preset compiles into a small directed graph of primitives named after SVG filter primitives. The core filter operations have direct SVG equivalents; non-native paint and engine operations require the lowering described below. The primitives are:

- `Flood`, `Offset`, `GaussianBlur(σx, σy, edge)`, `Morphology`, `ColorMatrix`, `ComponentTransfer`;
- `Composite(operator | arithmetic k1..k4)`, `Blend(mode)`, `Merge`, `Turbulence(seed, frequency, octaves, type, stitch)`;
- `Image(baked)`.

Inputs are `Source`, `SourceAlpha` and named results.

Three primitives exist only in the engine and have no portable SVG form: `Threshold`, `BloomPyramid` and `Dither`.

Each primitive declares its support for each [output profile](#output-profiles-and-degradation).

The IR is internal. The public API is the preset vocabulary above. A later public "custom filter graph" API would need a concrete consumer first, which is the same rule that applies to scene commands.

### Layer semantics

The effect unit is a **layer**, following Skia's `saveLayer`:
- a group of scene content;
- an optional effect stack, mask, opacity and blend mode;
- an optional backdrop effect.

The engine order is fixed: content → filter (effect stack) → clip → mask → opacity and blend.

- Every explicit effect layer is an engine capture boundary for its descendants. Its own backdrop samples preceding content within the nearest enclosing boundary, limited to the required support region. Ancestor opacity, clip and mask apply once when that enclosing layer is composited.
- `Isolation` stops blending from reaching past the layer. It does not let a backdrop capture bypass the enclosing boundary.
- This is a deliberate engine boundary. A live CSS overlay is eligible only after its browser backdrop and ancestor compositing match the captured result; otherwise it uses the captured image or a declared fallback. CSS backdrop-root equivalence is not assumed.

### Bounds and coordinates

Source bounds, effect lengths and support regions use layer-local logical coordinates. Compile the whole effect graph before allocating: sequential offsets and blurs accumulate support rather than taking the largest individual extent. Raster transforms the required region to device coordinates, rounds outward and reserves kernel support before the final viewport clip. SVG uses the same regions and transforms for filters, primitive subregions, masks and captures, including zero-width or zero-height path bounds.

Definition and cache keys include resolved regions, units, transforms and every target-dependent value affecting paint, or use a qualified equivalent local-coordinate normalization. A recipe reused on differently sized targets must not inherit another target's clipping rectangle.

### Where callers attach effects

Effects reach output through three routes:

1. **Theme effect tokens.** These are named slots that families read for their own chrome, so one theme changes every family consistently. See [Themes](#themes).
2. **Model options.** These are typed options on the owner that already styles the thing being decorated. Examples:
   - chart frame card: `WithCardEffects(stack)`;
   - series emphasis: `WithGlow(...)`, replacing the stacked-stroke "luminous" approximation;
   - topology node and edge styles: elevation and highlighted-path glow;
   - Mermaid presentation: shadow and highlight mapping;
   - `VisualCanvasStyleOptions` for tiles and hero text;
   - `TerminalStyleOptions` for window shadow;
   - story surfaces.

   Each family adds only the options its users need, and every option holds a `VisualEffectStack` or a preset rather than hard-coded filters.
3. **Composition operations.** `ImageComposition` has no SVG path, so it gets direct raster operations on the shared kernels:
   - `ApplyEffect(rect, effect)`;
   - `DrawImage(..., blend, mask)`;
   - `DrawText(..., shadow or glow)`;
   - `FillBackdrop(shape, effect)`;
   - procedural fills and `VisualBackdrop`.

Product-specific chrome stays in local style options. Only the reusable primitive lives in core.

## Themes

`VisualTheme` gains one immutable `VisualThemeEffects` object, edited through `VisualThemeBuilder.Effects` and returned in the usual immutable paired snapshot.

| Slot | Used by | Default |
| --- | --- | --- |
| `CardElevation` | Chart, grid and block frame cards. Replaces `CardShadowOpacity`/`CardShadowColor`. | The existing stacked-rectangle recipe with today's values and serialization. True blurred elevation is opt-in. |
| `TileElevation` | Canvas tiles, factual cards, topology node cards | None |
| `RaisedGlow` | Canvas hero badges, emphasised marks that opt in | None |
| `TextHalo` | Text drawn over imagery or backdrops (canvas, wallpaper, story titles) | None. Data-label halos keep their existing readability strokes. |
| `Glass` | Surfaces that request glass | None. When unset, surfaces that request glass fall back to an opaque tint. |
| `Grain` | Backdrops and glass that request grain | None |

Theme JSON:
- New members are additive and optional under the existing `effects` object in schema version 1, following the backward-default rule.
- Values that differ by mode go under optional `light.effects`/`dark.effects`, or use role references.
- When `CardElevation` is absent, `cardShadowOpacity`/`cardShadowColor` map to the legacy rectangle recipe, including its existing no-room behavior. When both representations are authored, the explicit new slot wins, including an explicit `None`; property order never changes precedence.
- `FromJson`/`ToJson` round-trip every slot. The canonical writer emits the new effect slots; it emits legacy shadow scalars only when they exactly represent the selected legacy recipe. A blurred or multi-effect stack is never flattened into those scalars. Existing JSON without effects retains its defaults and appearance.

The built-in presets (`Graphite`, `Report` and the rest) keep their current effects. New opt-in looks ship as separate presets or as consumer themes, never as a change to an existing default.

## Scene integration

The prepared scene (`ChartForgeX/Rendering/VisualScene.cs`) is a flat display list. Groups are `VisualSceneGroup`/`VisualSceneEndGroup` pairs with a clip and a rigid transform. Today no group carries opacity, a filter, a mask or a blend mode.

1. **Layer node.**
   - Add an internal `VisualSceneLayer` begin node with `Opacity`, `BlendMode`, `Isolation`, `Mask`, `Effects` (the resolved IR) and an explicit `Bounds`. It closes with the existing end-group node.
   - Ordinary clip and transform groups stay on today's fast path.
   - Builder scopes: `PushLayer(...)` and `PushBackdrop(shape, effect)`.
2. **Backdrop node.**
   - `VisualSceneBackdrop` records its shape, its effect and the preceding node range it samples.
   - Raster reads the current canvas region in order.
   - SVG emulates it (see [SVG backend](#svg)).
3. **Paint.**
   - Extend `VisualSceneGradient` from linear to linear, radial and conic. Raster already draws radial gradients.
   - Add a procedural paint for turbulence and grain.
   - Tapered strokes become filled outlines from `RasterStroker`. A gradient along a path additionally captures cumulative centreline distance and stop interpolation, including subpath starts, joins, overlap order and a closed-path seam. Both backends use that parameterization. SVG lowers it to bounded subdivisions with shared colours, or bakes the stroke paint; an ordinary linear/radial fill on the outline does not implement arc-length colour. Phase 0 fixes the flattening/colour error and segment budgets before this feature is implemented.
4. **Resolution at prepare time.** Effects capture resolved raster colours, logical-unit radii and seeds on the scene, alongside the original `SvgPaint` binding or theme-role identity. `SvgColorVariables` maps effect paints, including `feFlood`, just as it maps ordinary marks, with resolved fallbacks. Export never consults a mutable theme.
5. **Identity and splicing.**
   - Every new field joins `VisualSceneSvgRenderer.Identity()`, because ids are content hashes.
   - Every new field is carried through scene `Append()`, which grids and stories use.
   - Topology motion insertion respects layer bracketing.

## Backends

### Raster

`RgbaCanvas` stores straight-alpha RGBA8 and blends in sRGB through `BlendPixel`, at supersampled output resolution. It has no layer stack.

- **Layer stack.**
  - `PushLayer(deviceBounds)` takes a bounds-limited working buffer from a per-render pool.
  - `PopLayer(opacity, blend, mask, effects)` applies the effect IR and composites back.
  - `VisualSceneRasterRenderer` maps layer begin and end exactly where it pushes and pops clip scopes today.
  - Existing opacity layers in `RgbaCanvas.MarkLayers.cs` and `SvgRaster` may share bounds allocation and pooling only with their current RGBA8 arithmetic and per-composite quantization intact. Keep their compatibility path until byte-identity proof permits replacing its plumbing.
- **Working format.**
  - Explicit new-effect units use premultiplied RGBA in 16-bit integers.
  - Pixels are unpremultiplied only for blend-mode formulas and colour matrices.
  - Their result is quantised to 8-bit once, at the end, with deterministic ordered dither (Bayer 8×8 or interleaved gradient noise) so glows and gradients do not band. Legacy units retain RGBA8 and their existing rounding without dither; merely disabling dither after an RGBA16 conversion is not sufficient compatibility proof.
- **Colour space.**
  - Filters and blending use gamma-encoded sRGB as the proposed working policy. SVG output explicitly sets `color-interpolation-filters="sRGB"`; browser/native agreement is measured per primitive and blend mode rather than assumed from that setting.
  - A linear-light option may follow later for raster-only bloom.
  - `LinearRgba.Composite`, which implements all 28 COLR/W3C modes in linear light for colour fonts, generalises into a buffer-level blend engine with an enum and an sRGB variant. Colour fonts keep the linear variant.
- **Masks.** New alpha masks use mask alpha as coverage. New luminance masks use non-premultiplied, gamma-encoded sRGB channels with weights `2126/7152/722` over `10000`, then multiply by mask alpha exactly once. Clamp to the working range and round to nearest, with half values rounded up, at each specified integer conversion; zero mask alpha gives zero coverage. Apply the resulting coverage to source alpha once. This shared policy is separate from the existing `DrawImageMasked` compatibility arithmetic. SVG declares the mask mode and `color-interpolation="sRGB"`; where native luminance conversion is not qualified, emit the shared computed coverage as an alpha mask. `color-interpolation-filters` alone does not define mask conversion.
- **Kernels.**

| Effect | Algorithm | Notes |
| --- | --- | --- |
| Blur, σ < 2 | Direct integer convolution, radius ⌈3σ⌉ | Exact enough; cheap at small radius |
| Blur, σ ≥ 2 | Triple box blur with integer running sums and the W3C box-size formula | Approximation with constant work per pixel; phase 0 measures error and sets a tolerance. `RasterTextStroke`'s separable pass is the template. |
| Blur, σ ≥ 8 | Downsample 2× (4× at σ ≥ 16), triple box, bilinear upsample | Candidate optimization; qualify edges, small sources and parity before enabling it |
| Drop shadow, inner shadow, glow | Alpha → dilate (existing min/max) → blur → flood → composite | Lowers one-to-one to SVG |
| Bloom | Soft threshold → half-resolution pyramid → tent upsample → additive | About 1.33× the cost of one large-radius pass |
| Glass | Backdrop snapshot (bounds + 3σ) → downsampled blur → clip → tint → grain → highlight | One downsampled blur per panel |
| Grain | Integer hash per pixel | Stateless and parallel-safe |
| Turbulence | Port of the reference code in the Filter Effects specification | Matches SVG `feTurbulence` |
| Particles, dot matrix, grid, streaks | Geometry from the generators | Ordinary marks |

- **Performance.**
  - Kernels are scalar and integer over arrays. The core references no `System.Memory` or `System.Numerics.Vectors` on net472/netstandard2.0, and adding them would break the zero-dependency rule.
  - On net8.0/net10.0, `Vector<T>` and `Span` paths may be added behind `#if NET8_0_OR_GREATER`, with a test that scalar and vector output hashes match.
  - Row-band parallelism is allowed only if output stays identical.

### SVG

There is no central defs owner today. Scene SVG writes clips and gradients in a pre-pass (`WriteClips`), and legacy writers rebind ids by text.

- **`SvgEffectWriter` (`ChartForgeX/Svg/`)** writes `<filter>`, `<mask>`, linear/radial gradient definitions and portable paint geometry from the IR. It:
  - deduplicates them by content key;
  - scopes ids with the caller's prefix;
  - returns `url(#...)` references.
- Filters explicitly use `filterUnits="userSpaceOnUse"` and `primitiveUnits="userSpaceOnUse"`, with filter and primitive rectangles from the propagated support regions, including offset, spread and blur. Masks explicitly set `maskUnits`, `maskContentUnits` and their region in the same coordinate system. No default percentage region may clip an effect or disappear on a thin path; region-dependent definitions follow the complete-key rule above.
- `VisualSceneSvgRenderer.WriteClips` becomes a general `WriteDefinitions` pre-pass that calls it. Layers write `filter`, `mask`, `opacity` and `style="mix-blend-mode:...;isolation:isolate"`.
- The `VisualCanvas`, terminal and block writers call the same helper with their provisional `SvgRenderedIdentity` ids. Shared compatibility definitions preserve existing SVG output; new recipes replace the local filter implementations only for callers that opt in.
- **Conic paint.** SVG has no native conic-gradient paint server. Lower it to bounded wedge geometry, sampled with the same stops, interpolation and seam rules as the native raster sampler. Qualification includes the wrap seam and translucent stops. If its geometry budget is exceeded, bake the affected paint or use the declared fallback and report that choice. `SvgRaster` consumes the resulting ordinary paths or embedded image; no invented `<conicGradient>` element is emitted.
- **Backdrop emulation.** Do not rely on SVG `BackgroundImage`. The definitions pre-pass materializes the captured preceding content as one dedicated group (or a baked snapshot) before a backdrop node references it. Nested transforms, clips and layer order are preserved; copied IDs and references are rebound within the caller's scope. The capture is acyclic, contains no external references, and is decorative so it does not duplicate accessible names. A backdrop node then emits:
  1. a clipped `<use>` of that single captured group, or the snapshot image;
  2. the blur and tint filter on that copy;
  3. the layer content.
  An unsupported capture boundary requires an explicit bake or fallback with a diagnostic; `<use>` never refers to an arbitrary sibling range.
- In HTML output, real CSS `backdrop-filter` may be used on HTML overlay panels.
- Decorative layers get `aria-hidden="true"`. Unaffected visible text stays real vector text. Text inside a source-replacing blur, mask or composite participates in that unit's bake; a sharp duplicate above it would bypass the effect. Semantic names and data remain available separately.

### SVG rasteriser

`SvgRaster` reports `SFR002` for unsupported filters today. Block PNG rendering uses a captured label/mark scene whose mark painter invokes the SVG rasterizer; it is not a blanket `ToSvg`-then-rasterize route. Other producers also have direct native raster paths. Inventory those actual entry points before adoption, and route both SVG-import effects and native layer effects through `RasterEffects`.

Before any family emits effects, `SvgRaster` must support every primitive emitted by `SvgEffectWriter`:
- `feGaussianBlur`, `feOffset`, `feFlood`, `feComposite`, `feBlend`, `feMerge`, `feMorphology`, `feColorMatrix`, `feComponentTransfer`, `feDropShadow`, `feTurbulence`;
- `mix-blend-mode` and `isolation:isolate`, including nested isolated group buffers so descendants cannot blend with content outside their layer.

`SFR002` then shrinks to the primitives that remain genuinely unsupported.

### Producers outside the scene

Some canvas, grid, block, story and composition producers use family-specific or captured-mark paths rather than the native chart display list. Each SVG writer uses `SvgEffectWriter`; each native raster owner uses `RasterEffects`, directly or through its captured mark painter. Adoption must qualify the actual PNG producer as well as standalone SVG import. Families never get private kernel copies.

An authored group effect spans every mark, label decoration and text paint belonging to that group, in paint order. Captured producers currently paint marks and positioned text in separate passes; adoption must expose one atomic capture/composite scope around both passes. Filtering them independently is not equivalent to filtering the group. If a producer cannot expose that boundary, hold its adoption or diagnose a whole-unit bake/fallback. Unaffected producers need no migration.

### HTML and interactivity

`ChartForgeX.Interactivity.Html` inlines SVG through `RawTrusted`, with scoped prefixes. Effects work natively there. Generated CSS includes:
- `@media (prefers-reduced-motion: reduce)` rules for any CSS-driven effect animation;
- `@media (forced-colors: active)` and `prefers-reduced-transparency` rules that swap glass for solid fills and drop glows.

## Output profiles and degradation

Target capabilities need qualification for the actual viewer and embedding mode. Email and Office hosts use conservative format policies; a successful browser render does not establish their support.

An SVG export takes an **effect profile** through proposed `VisualSvgOptions.EffectProfile`. It controls SVG lowering only and always returns SVG. Format selection belongs to the email, document or HTML host exporter; `Email` and `Raster` are not SVG effect-profile values. For every primitive an SVG profile decides one of four outcomes:
- **Native:** emit it.
- **Emulate:** rewrite it into portable primitives.
- **Bake:** embed a PNG of the required compositing unit. An additive shadow or glow can be baked separately while retaining its vector source. Destructive blur, masks and composites bake the complete affected source, including its text and decorations, together with the effect and replace that unit's vector paint. Unaffected marks and text remain vectors.
- **Fallback:** replace it, for example glass becomes an opaque tint and glow becomes a plain stroke.

Every outcome other than native emits an export diagnostic. Proposed `ToSvgWithDiagnostics(options)` returns an immutable SVG result containing markup and export diagnostics; existing `ToSvg(options)` remains a string convenience over the same lowering. Neither mutates `PreparedVisual.Diagnostics`, which remains the preparation snapshot. Export diagnostics include a stable scene/layer locator when one exists (not a generated SVG ID); a document-wide fallback is explicitly unlocated. Semantic data and accessible names remain available separately when their paint is baked.

| Profile | Use for | Policy |
| --- | --- | --- |
| `Browser` | Self-contained HTML, inline SVG on web pages | Native filters, masks and blending. Backdrop emulated in SVG; real `backdrop-filter` on HTML overlays. |
| `SvgImage` (default for `ToSvg`) | Standalone SVG files, GitHub README images, documentation `<img>` | Native core filters and masks, emulated backdrop, baked bloom and dithered grain, no external references |
| `SvgPortable` | resvg, librsvg, Inkscape, design tools | As `SvgImage`. Blend modes and `feTurbulence` are native only where each renderer is qualified. |
| `OfficeSvg` | SVG chosen by an Office host exporter | Bake every effect using the compositing-unit rule above; no live filters. The host supplies a PNG fallback. |

Host format policies sit outside `VisualSvgOptions`:

| Host target | Policy |
| --- | --- |
| Email | PNG at 2× density with explicit size and an effect-free text alternative; no inline SVG, CSS filters or blending. |
| Office | PNG at 2×, or `OfficeSvg` with a PNG fallback, after qualification in the receiving application. |
| Raster | PNG through the managed pipeline: new-effect units use their declared dither; legacy units preserve their compatibility output. |

## Accessibility

- **Effects never encode data** (WCAG 1.4.1). Blur must not reduce a mark's contrast against its neighbours below 3:1 (WCAG 1.4.11).
- **Text over glass, imagery or blurred backdrops.**
  - Shared preparation preflight samples the composed background immediately before text paint and freezes a tint/scrim decision for both SVG and PNG, including an SVG-only export. Use resolved foreground colour and the worst relevant background under the text: at least 4.5:1 for normal text and 3:1 for qualifying large text.
  - Sampling has the same work budget as effects. If the background cannot be bounded or qualified, use a conservative opaque scrim/tint rather than certify an unknown contrast ratio. Record the measured ratio or the reason for fallback.
  - Output-profile lowering rechecks any changed background in shared export preflight without mutating the preparation snapshot. Animated backdrop changes invalidate the decision. Arbitrary host colour variables, CSS or external backdrops require host requalification, a frozen capture, or the conservative fallback; a resolved-theme measurement does not certify later overrides.
- **Motion.** Animated effects in stories respect reduced motion. CSS media queries cannot stop SMIL animation, so effect animation in SVG uses CSS keyframes inside the SVG's style, or renders a static frame. Sparkle stays under three flashes per second.
- **Semantics.** Decorative layers are hidden from assistive technology, and baked images carry empty alternative text. Existing accessible names, descriptions and semantic regions are unchanged.

## Performance

Budgets, all at 2560×1440 on one core with managed scalar kernels, to be confirmed by the phase-0 benchmark:

| Measure | Budget |
| --- | --- |
| Full-frame blur, σ = 20 | 50 ms or less |
| Full wallpaper effect stack (bloom, two glass panels, grain, vignette, dot map) | 250 ms or less |
| Peak effect working memory | Tracked in the raster allocation guard. A 2560×1440 RGBA16 buffer is 29.5 MB, so layers are bounds-limited and pooled. |
| Effects disabled | No measurable cost. Existing benchmark cases stay byte-identical, within the 10% regression gate. |

Before allocation or execution, one per-render budget bounds authored effects, expanded IR nodes/depth, bounds-weighted pixel passes at actual density, generated segments and capture/bake expansion, and concurrently live bytes including pooled buffers. Nested layers and SVG-import effects debit that same budget rather than resetting it per child. Preflight rejects an over-budget input or chooses its declared fallback with a diagnostic; it never executes an arbitrarily long stack merely because buffers can be reused. Phase 0 fixes the cost model and limits before public/JSON authoring is enabled.

Animated stories cache an effect only while its source and sampled backdrop are invariant. Keys bind content, effects/colours/seeds, transforms, clips/masks, density and sampled preceding content. Moving content beneath stationary glass invalidates its capture and readability decision. A source-only bitmap may survive a later transform/opacity change only when sampling remains equivalent. Native frames and baked SVG snapshots use the same dependency and invalidation rules.

An effects lane joins the PowerForge benchmark suite (`Benchmarks/Rendering`) before any default theme or example enables effects.

## Testing and qualification

Every phase follows [AGENTS.md](../AGENTS.md): smoke tests, generated examples for visible output, SVG/PNG parity, four target frameworks, no `NoWarn`, the 800-line source budget and shared helpers.

- **Kernel tests.**
  - Blur, shadow, glow and blend against analytic or golden numeric results.
  - Edge handling and extent.
  - Premultiplication round-trips.
  - Dither determinism.
  - Coloured/translucent/zero-alpha masks with the pinned conversion, and retained legacy mask/composite bytes.
  - Allocation, nesting and total-work limits, including repeated full-frame passes that reuse buffers.
- **Determinism.** Fixed-input kernel hashes are identical across net472, netstandard2.0, net8.0 and net10.0, and between scalar and vector paths. A compatible-runtime harness directly loads the built netstandard2.0 asset; compiling it alone is insufficient. Whole-image comparisons also bind the selected font bytes, captured scene, scale and working format so host-font differences are not misclassified as kernel drift.
- **Parity.**
  - Native PNG against browser rendering of the emitted SVG, in the required Chromium lane (`Build/Test-BrowserQuality.ps1`), with a stated per-effect tolerance. Pixel identity is not the contract.
  - `SvgRaster` against native PNG, with no unexpected diagnostics.
  - Large effects on small sources, offset shadows, thin paths, transformed/reused definitions, curved/reversing/closed gradient strokes, atomic mark/text groups, and moving content beneath glass.
  - Shared contrast decisions in SVG-only export and PNG, including an unqualified-background fallback.
- **Profiles and hosts.** Assert each SVG profile's lowering, and test format selection separately in its owning host:
  - email exports select PNG and contain no inline SVG;
  - `OfficeSvg` has no live filters, with source-replacing effects baked together with their source;
  - `SvgImage` has no external references;
  - degradation is returned through export diagnostics without changing preparation diagnostics;
  - nested isolation, captured backdrop IDs and colour-variable mappings retain their semantics.
- **Visual baselines.** Effected examples get new entries. Existing entries change only through an intentional `-UpdateVisualBaseline` after gallery inspection.
- **Existing assertions:**
  - `TopologyTests` asserts no `filter=` in topology SVG; it gains an opt-in exception.
  - Existing card-shadow rectangle and byte-identity tests remain for the compatibility recipe. Opt-in blurred shadows gain extent and parity tests.
  - `SvgRasterizer`-based parity tests require the new primitives.
- **Gallery.** Every preset has a compact/wide × light/dark example. One integrated acceptance example recreates a full "HUD wallpaper" (glass panels, glow, light streaks, dot-matrix map, perspective floor, grain, vignette) as both SVG and PNG.

## Existing effects to migrate

| Existing | Becomes | Notes |
| --- | --- | --- |
| Card shadow: 12 stacked rectangles in `VisualFrameSurface` | Shared compatibility recipe on the card layer; theme `CardElevation` | Preserve existing bytes, padding-room rule and `frame.card-shadow-no-room`. `VisualDropShadow` supplies a separate opt-in true blur in both backends. Remove obsolete constants only after checking actual callers. |
| `VisualCanvas` `soft-glow`/`raised-depth` SVG filters, their PNG offset-rectangle approximations, `HeroBadgeGlowColor` | Shared legacy recipes plus opt-in `VisualGlow`/`VisualDropShadow` stacks; glass tiles gain `VisualBackdropBlur` | Preserve each legacy output. New recipes provide qualified parity; callers migrate explicitly. |
| Terminal story `feDropShadow` and its two-rectangle PNG version | Shared legacy recipes plus opt-in `VisualDropShadow` | Preserve each legacy output; qualify the new recipe in both backends. |
| Line halo layers (`ChartLineVisualLayers`, `WithLuminousLineStyle`) | Optional `VisualGlow` behind the series line | The stroke stack stays as the low-cost mode |
| Bar cap shadow strokes (`ChartBarVisualStyle.CapShadow*`) | Optional `VisualDropShadow` | The current version is cheap and parity-exact; migrate only if it improves the look |
| Text halos (`ChartTextHalo` offset redraws) | Unchanged for data labels; `VisualDropShadow`/`VisualGlow` for decorative text | Do not blur data-label halos by default |
| the opacity layer in `RgbaCanvas.MarkLayers.cs`, `SvgRaster` full-canvas group buffers, `ApplyOpacity` | Shared bounded allocation/pooling, with legacy RGBA8 composites retained | No precision, rounding or dither change without opt-in |
| `LinearRgba.Composite` (colour fonts only) | The general blend engine | Adds an enum and an sRGB variant |
| `DrawImageMasked`, `ApplyCenteredCircleAlphaMask` | Shared compatibility masks plus opt-in `VisualMask` | Preserve legacy arithmetic; new masks use the pinned coverage rule. |
| `VisualCanvas` `TechHorizon` backdrop; PowerBGInfo patterns and glows | `VisualBackdrop` with generators, glow and blend | Seeded and deterministic |
| ImagePlayground OfficeIMO blur, vignette and opacity; offset-only text shadow | `ImageComposition` effect operations | Removes the pixel copies between libraries |

## Delivery phases

Each phase is one coherent outcome. Phases 0–2 precede public effects in phase 3. Phases 4–5 extend that foundation and can proceed independently where their required primitives exist. Family and consumer adoption in phases 6–7 depend on the specific effects they use, not on every generator being implemented. These are implementation stages, not a requirement to open a PR for each stage.

| Phase | Outcome | Exit criteria |
| --- | --- | --- |
| 0. Spike and decisions | Integer triple-box blur prototype, benchmarked at 2560×1440 on net472 and net10. Browser parity measured for blur, shadow and two blend modes. Office, Safari and resvg behaviour checked with one blur each. The decisions below confirmed. | Benchmark numbers, per-effect tolerances, stroke subdivision/error budgets and per-render work limits recorded; open decisions closed |
| 1. Raster foundation | Bounded premultiplied layer stack and pool; blur, morphology, flood, offset, composite and merge kernels; sRGB buffer-level blend engine; ordered dither for opt-in units; legacy composite/mask paths retained | Kernel, total-work and determinism tests; benchmark lane; existing bytes unchanged |
| 2. Scene and SVG foundation | `VisualSceneLayer` and atomic producer scopes; `SvgEffectWriter` and the `WriteDefinitions` pre-pass; complete bounds/identity/append support; `SvgRaster` primitives | Parity tests across scene SVG, browser and native PNG for internal fixtures, including mark/text capture and masks; existing bytes unchanged |
| 3. Public effects and themes | `ChartForgeX.Effects` presets (drop and inner shadow, glow, layer blur, opacity, blend, mask); `VisualThemeEffects` and JSON; SVG profiles and host policies; shared compatibility card shadow and opt-in blurred elevation | Gallery examples per preset; API conventions and migration notes; existing default baselines unchanged, new entries for opt-in effects |
| 4. Glass, bloom and strokes | `VisualBackdropBlur` with SVG emulation and a qualified HTML `backdrop-filter` path; `VisualBloom` (baked for SVG); arc-length gradient and tapered strokes; light streaks | Shared contrast/scrim and cache invalidation; bounded stroke lowering; parity per profile |
| 5. Procedural and generators | Grain; spec-exact turbulence; particles; dot matrix; perspective grid; vignette; `VisualBackdrop` | Determinism across frameworks; gallery acceptance example |
| 6. Family adoption | Chart series glow and card elevation; Mermaid highlight and shadow mapping; topology elevation and path glow; canvas glass tiles; story backgrounds; `ImageComposition` operations | Family examples, both themes, both outputs |
| 7. Consumers and release | PowerBGInfo backgrounds and panels on `VisualBackdrop`, glass and glow; ImagePlayground effect commands on `ImageComposition`, removing its OfficeIMO pixel copies; coordinated package release | Consumer qualification through candidate packages, then public packages |

## Decisions to confirm

Merging this proposal records its direction and acceptance criteria. It does not implement the effects or approve a new runtime dependency, maintained toolchain, or final public API. Resolve the following during phase 0 before committing the corresponding implementation boundary.

1. **Layer node.** A separate `VisualSceneLayer` (recommended, keeping clip groups fast) or new fields on `VisualSceneGroup`.
2. **Working precision.** 16-bit integer premultiplied buffers (recommended, deterministic on every framework), or float on net8.0/net10.0 if profiling favours it.
3. **Parallelism.** Whether row-band `Parallel.For` is acceptable in core raster when output stays identical.
4. **Default SVG profile.** `SvgImage` (recommended) or `Browser`, and the size limit for baked layers inside SVG.
5. **Mermaid scope.** Which Mermaid styling inputs (`classDef`, `style`, `look`) map to effects, and how far CSS `filter` values in Mermaid styles are honoured.
6. **Presets in core.** Whether neutral looks such as an "elevated" or "glass" theme ship as core presets, or only as consumer themes.
7. **Public names.** `ChartForgeX.Effects` with `Visual*` type names, matching existing conventions.

## Sources

- Skia layers and image filters: <https://skia.org/docs/user/api/skcanvas_overview/>, <https://api.skia.org/classSkImageFilters.html>
- Filter Effects Module Level 1, including the `feTurbulence` reference code: <https://www.w3.org/TR/filter-effects-1/>
- Mask processing and explicit mask coordinates: <https://www.w3.org/TR/css-masking-1/#mask-processing>
- SVG 2 paint servers and reusable subtrees: <https://www.w3.org/TR/SVG2/pservers.html#Gradients>, <https://www.w3.org/TR/SVG2/struct.html#UseElement>
- Compositing and Blending Level 1: <https://www.w3.org/TR/compositing-1/>
- CSS `backdrop-filter` and `mix-blend-mode`: <https://developer.mozilla.org/en-US/docs/Web/CSS/backdrop-filter>, <https://developer.mozilla.org/en-US/docs/Web/CSS/mix-blend-mode>
- librsvg feature list: <https://librsvg-b807a6.pages.gitlab.gnome.org/devel-docs/features.html>; resvg unsupported features: <https://github.com/linebender/resvg/blob/main/docs/unsupported.md>
- Gaussian blur approximations: P. Getreuer, "A Survey of Gaussian Convolution Algorithms", IPOL 2013, <https://www.ipol.im/pub/art/2013/87/>
- WCAG contrast minimum: <https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html>; failure F83 (text over backgrounds): <https://www.w3.org/WAI/WCAG22/Techniques/failures/F83>; animation from interactions: <https://w3c.github.io/wcag/understanding/animation-from-interactions.html>
