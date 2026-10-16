# Visual effects

Status: **design, not implemented.** This document defines one shared visual-effects system for every ChartForgeX family. Implementation follows the phases in [Delivery phases](#delivery-phases); the active checklist lives in [TODO.md](../TODO.md#visual-effects).

## Purpose

Wallpapers, social cards, report covers, dashboards, story frames and diagram highlights increasingly need effects that ChartForgeX cannot draw today. These include soft shadows and glow, frosted-glass panels, light streaks, grain, blend modes, masks, dot-matrix maps, perspective grids and vignettes. Consumers currently build them by hand:

- PowerBGInfo composes glows, grid and dot patterns, and glass tiles on `ImageComposition`/`VisualCanvas`, and turns card shadows off.
- ImagePlayground blurs, vignettes and fades through OfficeIMO, copying pixels between `RgbaImage` and `OfficeRasterImage` in both directions.
- Inside ChartForgeX itself, five unrelated approximations exist (see [Existing effects to migrate](#existing-effects-to-migrate)), and their SVG and PNG output already disagree.

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
4. **Off by default.** Every existing default output stays byte-identical. Effects appear only when authored, or when a theme opts in. The unchanged-output benchmark rule therefore keeps holding.
5. **Bounded cost.** Every effect declares its extent. Raster allocates only the region it needs, and the pixel budget (`RasterAllocationGuard`) covers effect buffers. Producers reserve room inside their padding, as the card shadow already does, and report a diagnostic instead of growing the viewport.
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
| `VisualStrokeProfile` | A gradient along the path length, and a width profile (taper). Lowered to outline geometry. |
| `VisualColorRef` | A literal colour, or a theme role with optional alpha, resolved at prepare time. |

**Generators** are not pixel effects. They produce ordinary scene geometry, so they are exact in SVG and need no lowering:

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

Every preset compiles into a small directed graph of primitives named after SVG filter primitives, so SVG lowering is one-to-one. The primitives are:

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

The order is fixed and matches CSS and SVG: content → filter (effect stack) → clip → mask → opacity and blend.

- A layer with a backdrop effect or a non-normal blend mode is a backdrop root.
- `Isolation` stops blending from reaching past the layer.
- A backdrop effect samples everything painted earlier inside the nearest enclosing backdrop root, limited to the layer shape plus the effect extent.

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
| `CardElevation` | Chart, grid and block frame cards. Replaces `CardShadowOpacity`/`CardShadowColor`. | Today's card shadow values, so output is unchanged. |
| `TileElevation` | Canvas tiles, factual cards, topology node cards | None |
| `RaisedGlow` | Canvas hero badges, emphasised marks that opt in | None |
| `TextHalo` | Text drawn over imagery or backdrops (canvas, wallpaper, story titles) | None. Data-label halos keep their existing readability strokes. |
| `Glass` | Surfaces that request glass | None. When unset, surfaces that request glass fall back to an opaque tint. |
| `Grain` | Backdrops and glass that request grain | None |

Theme JSON:
- New members are additive and optional under the existing `effects` object in schema version 1, following the backward-default rule.
- Values that differ by mode go under optional `light.effects`/`dark.effects`, or use role references.
- `cardShadowOpacity`/`cardShadowColor` stay readable and map onto `CardElevation`.
- `FromJson`/`ToJson` round-trip every slot.

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
   - Allow a gradient as stroke paint. Tapered strokes become filled outlines from `RasterStroker`; no new node is needed.
4. **Resolution at prepare time.** Effects resolve to concrete values (resolved colours, logical-unit radii, seeds) on the scene. Export never consults the theme, which is the existing rule.
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
  - the opacity layer in `RgbaCanvas.MarkLayers.cs` and `SvgRaster`'s full-canvas group buffers move onto this stack.
- **Working format.**
  - Premultiplied RGBA in 16-bit integers.
  - Pixels are unpremultiplied only for blend-mode formulas and colour matrices.
  - The result is quantised to 8-bit once, at the end, with deterministic ordered dither (Bayer 8×8 or interleaved gradient noise) so glows and gradients do not band.
- **Colour space.**
  - Filters and blending run in gamma-encoded sRGB by default, because that is the only space in which browser SVG, CSS blending and native PNG agree. SVG output always sets `color-interpolation-filters="sRGB"`.
  - A linear-light option may follow later for raster-only bloom.
  - `LinearRgba.Composite`, which implements all 28 COLR/W3C modes in linear light for colour fonts, generalises into a buffer-level blend engine with an enum and an sRGB variant. Colour fonts keep the linear variant.
- **Kernels.**

| Effect | Algorithm | Notes |
| --- | --- | --- |
| Blur, σ < 2 | Direct integer convolution, radius ⌈3σ⌉ | Exact enough; cheap at small radius |
| Blur, σ ≥ 2 | Triple box blur with integer running sums and the W3C box-size formula | Matches browsers to about 3% of a true Gaussian; O(1) per pixel; bit-exact on every target framework. `RasterTextStroke`'s separable pass is the template. |
| Blur, σ ≥ 8 | Downsample 2× (4× at σ ≥ 16), triple box, bilinear upsample | Visually identical; 4–16× fewer pixels |
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

- **`SvgEffectWriter` (`ChartForgeX/Svg/`)** writes `<filter>`, `<mask>` and radial/conic gradient definitions from the IR. It:
  - deduplicates them by content key;
  - scopes ids with the caller's prefix;
  - returns `url(#...)` references.
- `VisualSceneSvgRenderer.WriteClips` becomes a general `WriteDefinitions` pre-pass that calls it. Layers write `filter`, `mask`, `opacity` and `style="mix-blend-mode:...;isolation:isolate"`.
- The `VisualCanvas`, terminal and block writers call the same helper with their provisional `SvgRenderedIdentity` ids. Their local `soft-glow`, `raised-depth` and shadow filters disappear.
- **Backdrop emulation.** SVG has no portable backdrop: `BackgroundImage` is unsupported in browsers. A backdrop node therefore emits:
  1. a clipped `<use>` of the sampled sibling range;
  2. the blur and tint filter on that copy;
  3. the layer content.
- In HTML output, real CSS `backdrop-filter` may be used on HTML overlay panels.
- Decorative layers get `aria-hidden="true"`. Titles, descriptions and data text stay as real text above effect layers.

### SVG rasteriser

`SvgRaster` applies no filters today and reports `SFR002`. Block PNGs are produced by rasterising their own SVG, and parity tests rasterise chart SVG and expect no diagnostics. So `SvgRaster` **must** learn every primitive `SvgEffectWriter` emits, through `RasterEffects`, before any family emits filters:
- `feGaussianBlur`, `feOffset`, `feFlood`, `feComposite`, `feMerge`, `feMorphology`, `feColorMatrix`, `feComponentTransfer`, `feDropShadow`, `feTurbulence`;
- `mix-blend-mode`.

`SFR002` then shrinks to the primitives that remain genuinely unsupported.

### Producers outside the scene

These families do not convert to native scenes: `VisualCanvas`, `VisualGrid`, factual blocks, terminal stories and `ImageComposition`. They use the same `SvgEffectWriter` and `RasterEffects` directly until they adopt scene layers. They never get private copies.

### HTML and interactivity

`ChartForgeX.Interactivity.Html` inlines SVG through `RawTrusted`, with scoped prefixes. Effects work natively there. Generated CSS includes:
- `@media (prefers-reduced-motion: reduce)` rules for any CSS-driven effect animation;
- `@media (forced-colors: active)` and `prefers-reduced-transparency` rules that swap glass for solid fills and drop glows.

## Output profiles and degradation

Support differs sharply by target:
- Microsoft removed inline SVG from Outlook on the web and new Outlook in 2025.
- Gmail rasterises SVG images.
- GitHub strips inline SVG but renders SVG referenced as an image.
- Office has no documented filter support.
- No browser supports `BackgroundImage`.

Each export therefore takes an **effect profile** (`VisualSvgOptions.EffectProfile`, and a matching option on HTML exports). For every primitive the profile decides one of four outcomes:
- **Native:** emit it.
- **Emulate:** rewrite it into portable primitives.
- **Bake:** pre-rasterise **only the effect layer** to an embedded PNG `<image>`, keeping marks and text as vectors.
- **Fallback:** replace it, for example glass becomes an opaque tint and glow becomes a plain stroke.

Every outcome other than native emits a located diagnostic.

| Profile | Use for | Policy |
| --- | --- | --- |
| `Browser` | Self-contained HTML, inline SVG on web pages | Native filters, masks and blending. Backdrop emulated in SVG; real `backdrop-filter` on HTML overlays. |
| `SvgImage` (default for `ToSvg`) | Standalone SVG files, GitHub README images, documentation `<img>` | Native core filters and masks, emulated backdrop, baked bloom and dithered grain, no external references |
| `SvgPortable` | resvg, librsvg, Inkscape, design tools | As `SvgImage`. Blend modes and `feTurbulence` are native only where each renderer is qualified. |
| `Email` | HTML email | PNG only, at 2× density with explicit size and an effect-free text alternative. No inline SVG, CSS filters or blending. |
| `Office` | Word, PowerPoint, PDF through Office | PNG at 2×, or SVG with every effect baked and a PNG fallback. Never live filters. |
| `Raster` | PNG, wallpapers, PowerShell image generation | Full managed pipeline with dither: the reference output |

## Accessibility

- **Effects never encode data** (WCAG 1.4.1). Blur must not reduce a mark's contrast against its neighbours below 3:1 (WCAG 1.4.11).
- **Text over glass, imagery or blurred backdrops.**
  - In raster, measure the minimum contrast between each text box and the composited pixels behind it (the worst pixel, per WCAG failure F83).
  - Enforce a minimum glass tint, or add a text scrim automatically.
  - Report the measured ratio in diagnostics.
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

Animated stories precompute static effect layers once and animate only transforms and opacity.

An effects lane joins the PowerForge benchmark suite (`Benchmarks/Rendering`) before any default theme or example enables effects.

## Testing and qualification

Every phase follows [AGENTS.md](../AGENTS.md): smoke tests, generated examples for visible output, SVG/PNG parity, four target frameworks, no `NoWarn`, the 800-line source budget and shared helpers.

- **Kernel tests.**
  - Blur, shadow, glow and blend against analytic or golden numeric results.
  - Edge handling and extent.
  - Premultiplication round-trips.
  - Dither determinism.
- **Determinism.** Output hashes are identical across net472, net8.0 and net10.0, and between scalar and vector paths.
- **Parity.**
  - Native PNG against browser rendering of the emitted SVG, in the required Chromium lane (`Build/Test-BrowserQuality.ps1`), with a stated per-effect tolerance. Pixel identity is not the contract.
  - `SvgRaster` against native PNG, with no unexpected diagnostics.
- **Profiles.** Each profile's lowering is asserted on emitted markup:
  - no inline SVG for `Email`;
  - no live filters for `Office`;
  - no external references for `SvgImage`.
- **Visual baselines.** Effected examples get new entries. Existing entries change only through an intentional `-UpdateVisualBaseline` after gallery inspection.
- **Existing assertions that will change deliberately:**
  - `TopologyTests` asserts no `filter=` in topology SVG; it gains an opt-in exception.
  - Card-shadow rectangle-count tests are replaced by shadow-extent and parity tests.
  - `SvgRasterizer`-based parity tests require the new primitives.
- **Gallery.** Every preset has a compact/wide × light/dark example. One integrated acceptance example recreates a full "HUD wallpaper" (glass panels, glow, light streaks, dot-matrix map, perspective floor, grain, vignette) as both SVG and PNG.

## Existing effects to migrate

| Existing | Becomes | Notes |
| --- | --- | --- |
| Card shadow: 12 stacked rectangles in `VisualFrameSurface`, plus unused `Svg/PngCardShadow*` constants | `VisualDropShadow` on the card layer; theme `CardElevation` | Keep the padding-room rule and `frame.card-shadow-no-room`. A true blur in both backends. Delete the unused constants. |
| `VisualCanvas` `soft-glow`/`raised-depth` SVG filters, their PNG offset-rectangle approximations, `HeroBadgeGlowColor` | `VisualGlow`/`VisualDropShadow` stacks in `VisualCanvasStyleOptions`; glass tiles gain `VisualBackdropBlur` | Fixes today's SVG ≠ PNG mismatch |
| Terminal story `feDropShadow` and its two-rectangle PNG version | `VisualDropShadow` | Same fix |
| Line halo layers (`ChartLineVisualLayers`, `WithLuminousLineStyle`) | Optional `VisualGlow` behind the series line | The stroke stack stays as the low-cost mode |
| Bar cap shadow strokes (`ChartBarVisualStyle.CapShadow*`) | Optional `VisualDropShadow` | The current version is cheap and parity-exact; migrate only if it improves the look |
| Text halos (`ChartTextHalo` offset redraws) | Unchanged for data labels; `VisualDropShadow`/`VisualGlow` for decorative text | Do not blur data-label halos by default |
| the opacity layer in `RgbaCanvas.MarkLayers.cs`, `SvgRaster` full-canvas group buffers, `ApplyOpacity` | The bounded raster layer stack | Shared and pooled |
| `LinearRgba.Composite` (colour fonts only) | The general blend engine | Adds an enum and an sRGB variant |
| `DrawImageMasked`, `ApplyCenteredCircleAlphaMask` | `VisualMask` | Anti-aliased |
| `VisualCanvas` `TechHorizon` backdrop; PowerBGInfo patterns and glows | `VisualBackdrop` with generators, glow and blend | Seeded and deterministic |
| ImagePlayground OfficeIMO blur, vignette and opacity; offset-only text shadow | `ImageComposition` effect operations | Removes the pixel copies between libraries |

## Delivery phases

Each phase is one coherent, reviewable outcome. The first five are prerequisites for public effects; later phases can move in parallel once phase 2 lands.

| Phase | Outcome | Exit criteria |
| --- | --- | --- |
| 0. Spike and decisions | Integer triple-box blur prototype, benchmarked at 2560×1440 on net472 and net10. Browser parity measured for blur, shadow and two blend modes. Office, Safari and resvg behaviour checked with one blur each. The decisions below confirmed. | Benchmark numbers recorded; tolerance per effect agreed; open decisions closed |
| 1. Raster foundation | Bounded premultiplied layer stack and pool; blur, morphology, flood, offset, composite and merge kernels; sRGB buffer-level blend engine; ordered dither; allocation guard coverage | Kernel and determinism tests; benchmark lane; no change to any existing output |
| 2. Scene and SVG foundation | `VisualSceneLayer` and builder scopes; `SvgEffectWriter` and the `WriteDefinitions` pre-pass; identity and append support; `SvgRaster` primitives | Parity tests across scene SVG, browser and native PNG for internal fixtures; existing output byte-identical |
| 3. Public effects and themes | `ChartForgeX.Effects` presets (drop and inner shadow, glow, layer blur, opacity, blend, mask); `VisualThemeEffects` and JSON; output profiles; migrations of the card shadow, canvas glow and terminal shadow | Gallery examples per preset; API conventions and migration notes; intentional baseline refresh |
| 4. Glass, bloom and strokes | `VisualBackdropBlur` with SVG emulation and the HTML `backdrop-filter` path; `VisualBloom` (baked for SVG); gradient and tapered strokes; light streaks | Contrast check and scrim; parity per profile |
| 5. Procedural and generators | Grain; spec-exact turbulence; particles; dot matrix; perspective grid; vignette; `VisualBackdrop` | Determinism across frameworks; gallery acceptance example |
| 6. Family adoption | Chart series glow and card elevation; Mermaid highlight and shadow mapping; topology elevation and path glow; canvas glass tiles; story backgrounds; `ImageComposition` operations | Family examples, both themes, both outputs |
| 7. Consumers and release | PowerBGInfo backgrounds and panels on `VisualBackdrop`, glass and glow; ImagePlayground effect commands on `ImageComposition`, removing its OfficeIMO pixel copies; coordinated package release | Consumer qualification through candidate packages, then public packages |

## Decisions to confirm

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
- Compositing and Blending Level 1: <https://www.w3.org/TR/compositing-1/>
- CSS `backdrop-filter` and `mix-blend-mode`: <https://developer.mozilla.org/en-US/docs/Web/CSS/backdrop-filter>, <https://developer.mozilla.org/en-US/docs/Web/CSS/mix-blend-mode>
- librsvg feature list: <https://librsvg-b807a6.pages.gitlab.gnome.org/devel-docs/features.html>; resvg unsupported features: <https://github.com/linebender/resvg/blob/main/docs/unsupported.md>
- SVG in email: <https://www.caniemail.com/features/image-svg>, <https://www.caniemail.com/features/html-svg/>; inline SVG removed from Outlook on the web: <https://blog-en.topedia.com/2025/08/microsoft-is-retiring-support-for-inline-svg-images-in-outlook/>
- GitHub Markdown sanitisation: <https://github.com/github/markup>
- Gaussian blur approximations: P. Getreuer, "A Survey of Gaussian Convolution Algorithms", IPOL 2013, <https://www.ipol.im/pub/art/2013/87/>
- WCAG failure F83 (text over backgrounds): <https://www.w3.org/WAI/WCAG22/Techniques/failures/F83>; animation from interactions: <https://w3c.github.io/wcag/understanding/animation-from-interactions.html>
