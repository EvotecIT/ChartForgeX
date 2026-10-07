# V2 public API conventions

These rules govern the shared prepared-rendering contracts and their model entry points. Phase 1 implements `Chart.Prepare` for the selected Cartesian and pie/donut proof, plus bounded topology and sequence subsets through `TopologyChart.Prepare` and `SequenceArtifact.Prepare`. Existing exporters cover the remaining chart, diagram, composition and animation capabilities until their migration phases. Their remaining naming changes are tracked in the [API ledger](api-ledger.md); this document does not claim that every existing public member conforms already.

## Operation names

| Operation | Meaning | Example / rule |
| --- | --- | --- |
| `With*` | Set configuration on a mutable model builder and return that builder. Do not add data. | `chart.WithTitle("Capacity")` |
| `Add*` | Add model data or a semantic child and return the builder. Do not silently replace configuration. | `chart.AddLine("Used", points)` |
| `Configure*` | Pass the existing sub-object to a typed callback, then return the builder. | `chart.ConfigureXAxis(axis => axis.Minimum = 0)` |
| `Prepare` | Validate and compile a model with an explicit render context; return a detached prepared result. | `chart.Prepare(context)` |
| `To*` | Return an in-memory representation without writing a file or changing the model. | `prepared.ToSvg()`, `prepared.ToPng()`, `prepared.ToRgba()` |
| `Save*` | Write to an explicitly supplied path or stream. Keep file ownership and format options visible. | File-saving helpers are outside the Phase 1 prepared contract. |
| Constructors and named factories | Create immutable requests, snapshots or presets. They are deliberate exceptions to builder prefixes. | `new VisualFrame(...)`, `VisualTheme.Graphite()`, `VisualTheme.FromJson(json)` |
| `Resolve` | Select a validated immutable view of an existing value. | `theme.Resolve(VisualThemeMode.Dark)` |

Use domain names after prefixes. Avoid interchangeable `Set`, `Use`, `Build`, `Render`, `Options`, `Configure` and `With` methods for the same operation. A meaningful operation such as preparing, importing or exporting is not forced into a configuration prefix.

Immutable render requests use constructors and read-only properties. They do not expose `Add*` or `Configure*` mutation. A future immutable `With*` API would need an explicit copy-return contract and review; Phase 1 uses constructors instead. Existing mutable `FontSpec`, `VisualDesignTokens`, status-token and accessibility objects enter this boundary through snapshots or leave it through independent copies.

## Shared requests and ownership

| Contract | Ownership and behavior |
| --- | --- |
| `IVisualRenderable` | A core static compilation boundary returning `PreparedVisual`. Optional producers implement the same boundary without making core reference their concrete types. |
| `VisualSize` | Finite positive logical width and height. A default zero-valued struct is rejected when used in a layout. |
| `VisualLayoutOptions` | A resolved fixed size and finite non-negative outer padding. Padding must leave a positive interior. |
| `VisualFrame` | Title, subtitle, legend visibility/position, optional content surface and optional transparent canvas. |
| `VisualTheme` | Immutable light/dark colors plus typography and geometry defaults. Graphite is a preset. |
| `VisualRenderContext` | A complete layout/theme/mode/frame/font request. `Font` returns a copy. |
| `PreparedVisual` | Detached compiled scene, size, diagnostics, descriptive regions and accessible metadata. Export does not repeat layout. |
| `VisualRenderOptions` | Raster scale, supersampling and working pixel budget. These do not change logical layout. |
| `VisualDiagnostic` | A stable code, human explanation and the existing shared `Diagnostics.VisualDiagnosticSeverity`. |
| `VisualSemanticRegion` | Stable ID, role, descriptive rectangular extent and optional text. It is not an exact hit-test shape. |

Keep the numeric scene and painter implementation internal. Public signatures use core-owned types and do not reference Visuals, Stories, browser hosts, their encoders or their policy objects. Static rendering is script-free. Optional animation, interaction and composition have the ownership described in the architecture and [consumer migration guide](migration.md).

`ChartColor` is the color value type. Palette collections contain that type; do not introduce another RGB wrapper for the prepared pipeline. Diagnostic severity reuses the existing enum. Finding severity, test outcome and operational state remain distinct domain vocabularies even where their presentation uses the same colors.

## Configuration precedence

The prepared context owns size, theme, font and frame behavior. `Chart.WithSize`, `Chart.WithTheme` and other legacy presentation options retain their legacy-export meaning; they do not silently override an explicit prepared request. Callers migrating to the new path set these values in `VisualRenderContext`.

The model's title and subtitle supply fallbacks when the corresponding `VisualFrame` values are null. An empty string explicitly suppresses that line. Frame legend visibility, placement, content surface and transparent canvas come from the context, including its defaults. Legacy `WithLegend` and card settings do not override that request. This is an intentional Phase 1 bridge; later migration converges model configuration through the reviewed ledger. Data semantics, explicit series/point colors, supported label styles, axes and formatters remain on the model during the proof.

Frame headings and legends fit within the resolved size. Overflow is reported through diagnostics; a fixed render request never grows silently. Full textual values remain available through accessible metadata and descriptive regions where provided. A PNG requires concrete dimensions. Responsive hosts measure their viewport and prepare a new result; proportional SVG scaling does not claim compact-layout reflow.

`VisualTheme.Graphite()` reads the checked-in generated HtmlForgeX chart palette. `VisualTheme.FromJson` accepts that generated chart-token shape, not the full HtmlForgeX owner schema. HtmlForgeX governs color values; the chart look specification governs layout and typography defaults. Typography, spacing and geometry settings absent from the color export have explicit ChartForgeX defaults. Register or supply suitable fonts when portable appearance matters; equal geometry does not guarantee identical browser/native glyph painting.

`VisualTheme.ToThemeJson()` exports the complete paired theme, including typography and geometry, using `schemaVersion: 1`. `VisualTheme.FromThemeJson(json)` imports that versioned format. Keep these round-trip methods distinct from the generated color-token importer; unsupported versions, duplicate keys and incomplete themes fail explicitly.

## Prepare once, export several ways

```csharp
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

var chart = Chart.Create()
    .WithTitle("Storage capacity")
    .WithXLabels("Used", "Available")
    .AddDonut("Capacity", new[] {
        new ChartPoint(1, 60), new ChartPoint(2, 40)
    });
chart.Options.ValueFormatter = value => value.ToString("0") + " GB";
chart.Accessibility.Name = "Storage capacity: 60 GB used and 40 GB available";

var context = new VisualRenderContext(
    layout: new VisualLayoutOptions(new VisualSize(640, 400)),
    theme: VisualTheme.Graphite(),
    themeMode: VisualThemeMode.Light,
    frame: new VisualFrame(showLegend: true));

PreparedVisual prepared = chart.Prepare(context);
string svg = prepared.ToSvg();
byte[] png = prepared.ToPng(new VisualRenderOptions(scale: 2));
// Both exports consume the same prepared layout; the PNG has 1280 x 800 pixels.
```

Changing the chart, its input tokens, its accessible metadata or a font request after preparation does not alter an existing prepared result. Returned raster images and byte arrays belong to the caller; changing those buffers does not edit the scene. `PreparedVisual.Accessibility` also returns an independent copy.

SVG exports retain accessible names, descriptions, language and decorative intent. The default export scopes DOM IDs using a deterministic digest of the prepared scene and accessible metadata. When embedding identical copies in one document, give each a distinct host prefix, such as `prepared.ToSvg("report-capacity-left")`. Prefixes start with an ASCII letter and contain only ASCII letters, digits, hyphens, underscores or periods. DOM IDs and fragment references receive the prefix; `data-cfx-*` source identities and `Regions` retain their model IDs.

Unsupported Phase 1 families and options fail explicitly with the remaining legacy export route identified. Empty/all-zero pie and donut input produces a no-data diagnostic rather than invalid angles. `MaximumPieSlices` applies consistently across prepared themes; an aggregate `Other` retains contributing source indices and passes point index `-1` to the slice formatter. The model and semantic interchange remain the source for editable document data; a display list is not a substitute for topology or sequence semantics.

## Enforcement boundary

`V2ApiConventionTests` checks the reviewed immutable contracts, their operation roles, canonical color/severity types, core-only public signatures, in-memory export signatures and detached request/output lifetime. It also checks the selected mutable chart bridge's `With*`, `Add*` and `Configure*` behavior. Focused chart, topology and sequence fixtures protect the implemented preparation subsets and their explicit limits. These are compiled API and observable-output checks; they do not read this document or enforce editorial wording.

Each migrated family expands the reviewed API selection and adds its own data/layout/output fixtures. The old surface remains inventoried rather than being subjected to a blanket prefix rule during a partial migration. Do not weaken a current contract merely to pass the naming test, and do not claim that a naming check proves visual quality, package publication or consumer compatibility.
