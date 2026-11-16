# One authoring convention for the Evotec engines

Design, 4 November 2026. This document sets one authoring convention for ChartForgeX, ConsoleMonster, CanopyX and CodeGlyphX. Their main consumers, ImagePlayground, PowerBGInfo, HtmlForgeX, OfficeIMO and TestimoX, use several of them in the same code and the same scripts, so they should read alike. The [ChartForgeX next-major design](next-major-api.md) applies this convention to charts and diagrams; where the two differ, this document wins.

It replaces the separate-builder design in `next-major-api.md` and changes ConsoleMonster, CanopyX and CodeGlyphX in their next major or minor versions.

Status, 16 November 2026: the shared generator and vocabulary ship in `PowerForge.Generators` (PSPublishModule 3.0.190 and later), and ConsoleMonster 3.0 is the first engine on them. [What the ConsoleMonster pilot settled](#what-the-consolemonster-pilot-settled) records the rules the pilot had to add or correct; they apply to every engine that follows. ChartForgeX, CanopyX and CodeGlyphX are not converted yet.

## Where the four libraries stand

Inspected on 4 November 2026 from the default branches.

| | ChartForgeX 3.3.1 | ConsoleMonster | CanopyX 0.3 | CodeGlyphX 3.1 |
| --- | --- | --- | --- | --- |
| Entry point | `Chart.Create()` | `Ui.Panel()`, `Ui.Table()` | `new CanopyTreeView(id, title)` | `QR.Create(...)`, `Barcode.Create(...)` |
| Settings | ~490 `With*`, 59 `Configure*` | generated `Title(...)` on the model | properties only | `WithModuleSize(...)` on builders |
| Children | ~250 `Add*` | generated `AddX` / `AddXs` | `Columns.Add(...)` | none |
| Callbacks | `WithTooltipFormatter(...)` | one generated `Configure(...)` per type | properties | `WithEncoding(o => ...)` |
| Output | `ToSvg`, `ToPng`, `SavePng`, `SaveSvg`, `SaveHtml`, `Save`, `RenderSvg`, `WriteGif` | `Show()`, `Export(path, format)` | `ToHtmlPage`, `SaveHtmlPage`, `SaveHtmlBundle` | `Save(path)`, `Save(stream, format)`, `Render(format)` |
| Theme mode | `VisualThemeMode` (Light, Dark) | `ThemePreference` (Auto, Light, Dark) | `CanopyTheme` (Auto, Light, Dark) | none |
| Tone / status | `VisualStatus`, `TopologySeverity` | `StatusKind`, `Severity`, `ThemeToken` | `CanopyTone` | none |
| Colour | `ChartColor` | `Color` | `string` | `Rgba32` |
| Generator | none | `ConsoleMonster.Generators` (internal rules) | none | none |

ConsoleMonster's generator already proves the shape I want: methods named after properties, emitted on the model itself, with no parallel builder. The other three should converge on it, and ConsoleMonster changes where its current rules conflict with the decisions below.

## Decisions

1. **The model is the API.** Every public authoring type is a mutable class whose properties are the source of truth. Object initializers always work. There is no separate builder type, options mirror or `Edit` transaction.
2. **Fluent methods are generated on the model.** For every exported property `Title`, the generator emits `Title(value)` as an extension method that sets the property and returns the same instance. Chaining, variables and initializers all edit the same object.
3. **No prefixes on mutable models.** No `With*`, `Add*`, `Set*`, `Use*` or `Configure*` methods on mutable models. `With*` stays only on immutable values, where it returns a copy (`color.WithAlpha(0.5)`, `style.WithWeight(600)`), as records and `DateTime` do. Three things keep their verbs because they are not authoring: a bare `Add(item)` on a container, which collection initializers need; `void` runtime operations on live widgets, handles and canvases (`liveTable.AddRow(key, values)`, `region.Set(widget)`, `canvas.Set(x, y)`); and framework registration conventions such as `services.AddConsoleMonster()`.
4. **Children are named by their singular noun.** A collection property `Series` gets `Series(item)`, `Series(configure)` and `Series(IEnumerable<T>)`. `Nodes` gets `Node(...)`, `Rows` gets `Row(...)`. Handwritten overloads add required constructor arguments, for example `Series(name, values, configure)`.
5. **One factory shape.** A static facade per library creates a model from its required identity and an optional configuration lambda: `Chart.Line("Title", c => ...)`, `Ui.Panel("Build", p => ...)`, `Canopy.TreeView("files", "Project files", t => ...)`, `QR.Create("https://evotec.xyz", q => ...)`. The lambda receives the model, not a builder.
6. **One output vocabulary.** `Save(path)` infers the format from the extension. `Save(path, format)`, `Save(stream, format)`, `ToSvg()`, `ToPng()`, `ToHtml()` and `Render(format)` cover the rest. `Show()` remains ConsoleMonster's terminal verb.
7. **One semantic vocabulary.** `ThemeMode` (Auto, Light, Dark), `Tone`, `Icon` and the colour contract have the same names, members and wire spellings in every engine, generated from one definition into each engine's own namespace. No engine gains a runtime package dependency.
8. **One generator.** The shared generator lives in PowerForge/PSPublishModule, starts from ConsoleMonster's implementation, and also emits the metadata that PowerShell cmdlets and documentation consume.
9. **One PowerShell shape.** `New-<Prefix><Noun>` commands, flat parameters for settings, hashtables for nested settings, and `-Content {}` for children in every module.
10. **One look.** Every chart, diagram, card and image comes from the shared token theme in light or dark. Theme presets that change typography, card geometry or surfaces are removed; only palette variants remain. See [one visual standard](#one-visual-standard).

## Why the model carries the fluent methods

`next-major-api.md` proposed a generated `LineChartBuilder` that edits a transactional copy of `LineChart` and installs it after the lambda succeeds. ConsoleMonster generates the same methods directly on the model. I am choosing ConsoleMonster's shape.

- **One type to learn.** `chart.Title("x")`, `c.Title("x")` inside the factory lambda and `new LineChart { Title = "x" }` all touch the same type. A builder doubles the public surface and its documentation.
- **Retained handles stay simple.** `var s = chart.Series[0]; s.Name = "Renamed";` edits the real child. The builder design needed `Edit`, ID reconciliation, builder expiry and candidate copies to keep handles valid. All of that disappears.
- **The atomicity it bought is cheap to keep where it matters.** A child method creates the child, runs its callback, validates it, and only then appends it, so a throwing callback never leaves a half-built child. Whole-model rules (category counts, graph endpoints, duplicate IDs) are already checked at preparation, and preparation already produces a detached snapshot. A caller who wants all-or-nothing edits to a live model builds a new model and swaps the reference.
- **The C# collision is already solved.** An extension method can share a property's name because method invocation only considers invocable members. ConsoleMonster ships this today. Only delegate-typed properties collide, and decision 10 below covers them.

Models remain mutable and need caller synchronization, as today. Prepared and rendered results remain immutable and thread-safe.

## Generated method rules

The generator emits one `public static partial class <Type>Fluent` per opted-in type.

| Property shape | Generated methods | Notes |
| --- | --- | --- |
| Settable scalar `T Name` | `Name(T value)` | Setter validation runs as usual. |
| `bool Name` / `bool IsName` | `Name(bool value = true)` | `Visible()` reads better than `Visible(true)`; the `Is` prefix is dropped. |
| Nullable `T? Name` | `Name(T? value)` | `null` clears it. |
| Nested settings object `S Name` | `Name(Action<S> configure)` | Creates `S` if null and settable, then configures it in place. |
| Collection `ICollection<T> Names` | `Name(T item)`, `Name(Action<T> configure)`, `Names(IEnumerable<T> items)` | `T` needs a public parameterless constructor for the lambda overload. Singular names come from a fixed rule (`Series`, `-ies`, `Children`) or `[Child("Name")]`. A collection gets these methods and no setter, even when the property is settable; assign the property to replace the list. The plural takes `IEnumerable<T>` only, so several literal items need an array or repeated singular calls. |
| Delegate `Func<...> Name` | `<Verb>(Func<...> value)` with the verb declared by `[Fluent("Verb")]` | See decision 10 and the verb names below. |
| Compound conveniences | handwritten (`Size(w, h)`, `Bounds(min, max)`, `Series(name, values, ...)`) | Recorded in metadata with their backing properties. |

10. **Delegate properties declare their verb.** A property such as `Func<LabelContext, string?> LabelFormatter` cannot have a same-named method, because `x.LabelFormatter(...)` would invoke the delegate. The property declares `[Fluent("FormatLabels")]`, and the generator emits `FormatLabels(Func<...>)`. A delegate property without that attribute is a build error. This replaces ConsoleMonster's generic `Configure(...)`.

Inheritance follows ConsoleMonster: methods for a non-sealed type are generic over `TSelf : Type`, so chains keep the derived type. Properties inherited from a generated base are not emitted again.

Attributes are shared: `[Fluent]` opts a type in, `[Fluent("Name")]` renames a member, `[NoFluent]` skips one, `[Child("Name")]` names a child method and `[FluentForward(...)]` routes a method through a helper (ConsoleMonster's `Content(...)` goes through `ContentBuilder.Replace`). The generator adds them to each compilation as internal, embedded types, so libraries that see each other's internals never get conflicting copies. ConsoleMonster's interface- and assembly-name discovery became explicit `[Fluent]` opt-in.

Delegate verbs follow one naming rule, so callbacks read alike in every engine:

| Callback | Verb | Examples |
| --- | --- | --- |
| Computes a value | `<Value>By` | `TitleBy`, `ChoicesBy`, `PreviewBy`, `KeyBy`, `LabelBy` |
| Answers yes or no | `<State>When` | `ShowWhen`, `HealthyWhen`, `AvailableWhen` |
| Reacts to an event | `When<Event>ed` | `WhenChanged`, `WhenClicked`, `WhenSubmitted` |
| Does something | an imperative verb | `Validate`, `Run`, `RunAsync`, `WriteTo` |

The verb can never be the property's own name (`OnChange` cannot set `OnChange`, because the call would invoke the delegate), and a `Source` or `Load` affix is dropped (`TitleSource` is set with `TitleBy`, `LoadChoices` with `ChoicesBy`). Test seams such as clocks and delays get `[NoFluent]` rather than a verb; tests assign them.

Collisions are build errors with a located diagnostic, not silent skips. This covers a generated method that would shadow a handwritten method with the same signature, two members mapping to one name, an ambiguous singular, or a delegate property without a verb.

## Factories and entry points

| Library | Facade | Examples |
| --- | --- | --- |
| ChartForgeX | `Chart`, `Diagram` | `Chart.Line("Orders", c => ...)`, `Chart.Mixed(...)`, `Diagram.Topology("Services", d => ...)` |
| ConsoleMonster | `Ui` | `Ui.Panel("Build", p => ...)`, `Ui.Table(rows, t => ...)`; `Ui.Panel()` stays for an untitled panel |
| CanopyX | `Canopy` | `Canopy.TreeView("files", "Project files", t => ...)`, `Canopy.Board(...)` |
| CodeGlyphX | `QR`, `Barcode`, `DataMatrixCode`, `Pdf417Code`, `AztecCode` | `QR.Create("payload", q => q.ModuleSize(8).Foreground("#111827"))` |

A factory takes only what the model cannot sensibly exist without (title, ID, payload, data) and an optional `Action<T>`. Every factory returns the concrete model type, which supports ordinary property access afterwards.

CodeGlyphX's `QrBuilder` and `BarcodeBuilder` become `QrCodeImage` and `BarcodeImage` models with generated methods; `WithModuleSize` becomes `ModuleSize`, and `WithEncoding(o => ...)` becomes `Encoding(o => ...)`. Its one-shot static helpers (`QR.Save(payload, path)`, `QR.Render(...)`) stay as conveniences over the same model.

## Output

| Verb | Meaning | Replaces |
| --- | --- | --- |
| `Save(path)` | Writes the format implied by the extension; rejects an unknown extension | CFX `SavePng`/`SaveSvg`/`SaveHtml`/`SaveRasterImage`/`SaveGif`; CM `Export(path)`; CanopyX `SaveHtmlPage` |
| `Save(path, format)`, `Save(stream, format)` | Explicit format | CM `Export(path, format)`; CFX `WriteGif` |
| `Save(path, o => ...)` | Output options such as `Scale(2)`, `Theme`, `Mode`, `Bundle()` | CFX `VisualRenderOptions` overloads; CanopyX `SaveHtmlBundle` |
| `ToSvg()`, `ToHtml()`, `ToText()` | String output | CFX `RenderSvg`; CM `ConsoleExporter.ToSvg` |
| `ToPng()`, `ToJpeg()` | Byte output | unchanged |
| `ToHtmlFragment()` | Embeddable HTML without page chrome | unchanged in CFX and CanopyX |
| `Render(format)` | `RenderedOutput` with `Format`, `MediaType`, `ToBytes()` and `ToText()` | CodeGlyphX's existing contract, adopted everywhere |
| `Prepare(context)` | Detached, reusable layout | CFX only |
| `Show()` | Paints to the terminal | ConsoleMonster only |

Formats are one engine-local `OutputFormat` enum per library containing only what that engine family supports, add-on formats included: ConsoleMonster's lists `Png` (ConsoleMonster.Graphics) and `Markdown` (ConsoleMonster.Markdown) beside the built-in ones, so a provider registers by enum value and `Save(path)` maps `.png` without strings. A format whose add-on is not registered fails with the package name. Format providers keep ConsoleMonster's registration model (`IFrameExportProvider`, now with `Render`), so ConsoleMonster.Graphics adds PNG without reflection. `ToSvg()`, `ToHtml()` and `ToText()` are extension methods on the model, and an add-on's typed options go through an overload with a required options argument (`Save(path, GraphicsExportOptions)`), so the plain `Save(path)` stays unambiguous.

## Shared semantic vocabulary

| Type | Members | Notes |
| --- | --- | --- |
| `ThemeMode` | `Auto`, `Light`, `Dark` | `Auto` resolves at the host: the browser `prefers-color-scheme` for HTML output, the terminal profile for ConsoleMonster, and `Light` for static images. Replaces `VisualThemeMode`, `ThemePreference` and `CanopyTheme`. Anything that is not light or dark is a palette, chosen beside the mode: ConsoleMonster has `ThemePalette` (`Default`, `HighContrast`, `ColorBlind`, `Terminal`), and its old `ColorBlindLight` is `ColorBlind` in `Light` mode. |
| `Tone` | `None`, `Neutral`, `Info`, `Success`, `Warning`, `Danger`, `Muted`, `Named(string)` | CanopyX's `CanopyTone` contract. A named tone resolves to the engine's own theme role of that name (`Tone.Named("accent")` is ConsoleMonster's `ThemeToken.Accent`); an unknown one renders neutral. `Tone.None` means unset, so tone properties are not nullable. Domain enums (`StatusKind`, `Severity`, `TopologyHealthStatus`) stay as facts and map to a `Tone` for paint with `ToTone()`. |
| `Icon` | `Icon.Named("server")`, captured artwork | A catalog key. Each renderer binds it to its own artwork (glyph, SVG path, emoji fallback). Never a URL or raw markup. Each engine lists the keys it draws in an `Icons` class (`Icons.Run`, `Icons.All`); a key it does not know, including one from another pack (`tabler:server`), draws its default mark. Free text that is not a key belongs in a separate property (ConsoleMonster's `MenuItem.Mark`). |
| Colour | `FromHex`, `FromRgb`, `Parse`, `TryParse`, `ToHex()`, implicit conversion from a hex string | Engines keep their existing colour type names (`ChartColor`, `Color`, `Rgba32`) because they collide with framework types in consumer code. CanopyX gains a real colour type instead of `string`. All of them accept the same strings, so PowerShell can pass `'#2a78d6'` everywhere. |
| `Theme` | `Theme.Default`, `Theme.FromTokens(json)`, `Theme(...)`, `Mode(...)` on every root model of an engine with themed output (CodeGlyphX has none yet, so it has neither) | Built from the shared visual-token export (`evotec.tokens.json`). ChartForgeX's `VisualTheme.Graphite()` becomes `Theme.Default`. |

The generator emits `ThemeMode`, `Tone` and `Icon` into each engine from one definition file in PowerForge, so the four copies cannot drift. Bridges such as `HtmlForgeX.CanopyX` and ImagePlayground convert them by name.

## One visual standard

Today ChartForgeX ships 16 complete theme presets besides Graphite: Report (the classic pre-Graphite look), Executive, Editorial, Dashboard, Product, Minimal, Aurora, Candy, PeopleInfographic, SaasDashboard, RestaurantDashboard, Accessible, Colorblind, Terminal and TransparentOverlay. Each carries its own typography scale, title weight, card radius, shadows and surfaces. In the current gallery about 170 example charts use one of them (118 use Report), and about 144 places set fonts or sizes by hand. ImagePlayground exposes 14 preset names on top. The result is charts that look like they come from different products, and a user can still produce them.

The next major removes that choice:

| Today | Next major |
| --- | --- |
| Graphite | `Theme.Default`, built from the shared token export, light and dark |
| Accessible, Colorblind | Palette variants of `Theme.Default`: `Theme.Default.Palette(Palette.Accessible)`; typography and geometry unchanged |
| TransparentOverlay | A frame setting, `Surface(SurfaceStyle.None)`, for wallpapers and overlays such as PowerBGInfo |
| Terminal | Not a chart theme. Terminal output belongs to ConsoleMonster and Stories, which consume the same tokens |
| Report, Executive, Editorial, Dashboard, Product, Minimal, Aurora, Candy, PeopleInfographic, SaasDashboard, RestaurantDashboard | Removed. Gallery examples use `Theme.Default` |
| ImagePlayground `ChartTheme` (14 values) | `-Mode Light/Dark/Auto` and `-Palette Default/Accessible/Colorblind` |

Typography, spacing, radius and elevation come only from tokens. A product with its own brand supplies its own token export through `Theme.FromTokens(json)`; it does not pick a built-in look. Per-chart font overrides stay for explicit host needs such as wallpapers with a required face, but examples and recipes do not use them. A repository test fails when an example uses a non-default theme or hand-set typography outside the theme and font-feature demonstrations.

### Size and fit

Typography is in logical pixels, so the canvas size decides how large text looks wherever charts sit side by side. The current gallery uses more than 40 different sizes, from 390 to 1,600 pixels wide, which is why identical 17 px titles look tiny on one card and loud on the next. Fixed-geometry content (calendars, tile maps, progress rows, gauges) also leaves bands of empty frame when its canvas has the wrong proportions.

| Rule | Contract |
| --- | --- |
| Width is the authored dimension | `Size(width)` is the normal call. Height follows from the content: the family's preferred aspect for plots (Cartesian 16:9 at wide widths, 4:3 below the compact breakpoint; radial and gauge square), and the measured content height for fixed-geometry families such as calendars, tile maps, progress rows and topology. `Size(width, height)` remains for a host that must fill an exact box; content is then centred as one block, never pinned to opposite edges. |
| Standard widths | Recipes, gallery and documentation use 800 (wide), 600 (standard) and 390 (compact). Reports and dashboards pick from the same set, so cards placed together share a text scale. |
| Typography by size class | The token theme's wide and compact roles switch at the compact breakpoint. There is no third ad hoc scale and no per-chart font size in recipes. |
| Captions never cross marks | Data captions are placed outside the mark ink they describe, beside stems and wicks rather than on them, and are omitted with their fact retained when no clear position exists. |

The same rule applies to ConsoleMonster's built-in themes (`Theme.Dark`, `Theme.Light`, `Theme.HighContrast`), CanopyX and CodeGlyphX output. They resolve one token theme in a mode, and high contrast is an accessibility palette rather than a separate look.

CSS custom properties keep the HtmlForgeX `--hfx-*` names. HtmlForgeX owns the token export, so a neutral `--evx-*` prefix would rename every host stylesheet for no visible gain. Engine-specific roles stay `--cfx-*` and `--cx-*` and resolve through `--hfx-*` first.

## PowerShell

| Module | Prefix | Owner |
| --- | --- | --- |
| ConsoleMonster | `Console` | ConsoleMonster |
| ChartForgeX | `Cfx` | ImagePlayground |
| CanopyX | `Canopy` | HtmlForgeX (with its CanopyX bridge) |
| CodeGlyphX | `Image` (`New-ImageQRCode`, `New-ImageBarCode`) | ImagePlayground, unchanged |
| PowerBGInfo | `BGInfo` | PowerBGInfo, unchanged |

Rules for every module:

- Settings are parameters named after the model property. Nested settings take a hashtable whose keys are property names (`-X @{ Title = 'Day'; Labels = 'Mon','Tue' }`), validated against the same metadata and rejected when a key is unknown.
- Children are commands that write typed objects to the pipeline, collected from `-Content {}` or the pipeline. Today's `-ChartsDefinition`, `-TopologyDefinition` and `-BGInfoContent` become `-Content`, and the old names stay as aliases for one major version. `New-ImageQRCode -Content` keeps its payload meaning; it has no children.
- Output uses `-FilePath`, inferring the format like `Save(path)`, or returns the object when absent.
- Cmdlet parameter blocks are generated from the shared metadata at build time with `[FluentCmdlet(typeof(Model))]` on a partial cmdlet. Hand-written behaviour (data projection such as `-XProperty`, pipeline collection) lives in the same partial class. An existing module adopts it with `Include` listing the parameters it already has: those declarations keep their help, aliases and validation, and the generated `ApplyParameters(model)` replaces the hand-written copying. Generating every property as a new parameter is a separate, reviewed change per command.

## What changes in each library

| Library | Change | Size |
| --- | --- | --- |
| ConsoleMonster | Done in 3.0: PowerForge generator with explicit `[Fluent]`; `AddX`/`AddXs` became `X`/`Xs`; `Configure(...)` became declared verbs; `Export` became `Save`/`Render`/`ToX`; `ThemePreference` became `ThemeMode` and `ThemePalette`; `Tone` and `Icon` adopted | The pilot. See below. |
| ChartForgeX | Everything in [next-major-api.md](next-major-api.md), with the generated methods on the models instead of on builders; remove the theme presets except palette variants | Largest |
| CanopyX | Add `[Fluent]` to its models and a `Canopy` facade; `CanopyTone` becomes `Tone`, `CanopyTheme` becomes `ThemeMode`, `string` colours become a colour type; `SaveHtmlPage`/`SaveHtmlBundle` become `Save` with options | Small. It is still 0.x. |
| CodeGlyphX | Builders become models; `With*` become property methods; adopt `ThemeMode` only if it gains themed output | Medium |
| ImagePlayground | `New-Cfx*` commands; migrate chart, QR and console commands to the new libraries; keep its own image commands | Large |
| PowerBGInfo | Migrate chart, topology and canvas calls; `-BGInfoContent` alias | Medium |
| HtmlForgeX, OfficeIMO, TestimoX | Thin bridge migrations, including `ChartForgeX.Interactivity` | Medium |

## What the ConsoleMonster pilot settled

ConsoleMonster went first: 211 generated model types across six assemblies, 64 delegate properties that needed a verb (and four test seams that got none), changes in about 480 files and a website whose fluent examples are generated from initializer examples. The switch went through `PowerForgeFluentNaming=Prefixed` first, which reproduced the old generated files exactly, and then to the convention. These are the rules it added or corrected; an engine that follows applies them from the start.

- **Opt-in is per root, reachability is fragile.** `[Fluent]` on `IWidget`, `Prompt<T>` and `Field` covers most models, but option types are found only through the properties of opted-in types. Marking one property `[NoFluent]` silently removed every method of a type reached only through it (`Menu`, through `MenuItem.Submenu`). Mark every type that callers create on their own with `[Fluent]`, and compare the generated type list before and after any `[NoFluent]`.
- **One `<Type>Fluent` class per model.** Handwritten helpers move into the type's class (`TableFluent.Totals`, `SplitterFluent.First`) rather than a shared namespace class. Tools that look methods up (documentation generators, parity tests) must search the type and its base types.
- **A child accessor that returns the child stays possible, as an extension.** A class cannot have a method with the same name as its property, so `LineChart.Series(key)`, which returns the series it finds or creates, lives in `LineChartFluent`. Private nested types named like a new method must be renamed too.
- **Rename all overloads together.** An overload left with the old name keeps compiling and can capture calls meant for the renamed one. In ConsoleMonster a generic `AddField<T>` began calling itself once its `object` overload became `Field`, and only a test run found it. Search for every overload of a renamed member, generic ones included.
- **Collisions are real build errors.** Three handwritten methods duplicated a generated one exactly (PFF002); two were removed and one (`MenuItem.Submenu(Action<Menu>)`, which builds the menu with the item's label) kept with `[NoFluent]` on the property.
- **`params` is gone from plurals.** `Crumbs("Review", "Run")` becomes `Crumbs(new[] { "Review", "Run" })`. This is the one place the convention reads worse than the prefixed API; a `params T[]` plural overload in the generator would restore it without changing anything else.
- **Bool setters take an optional value.** `Visible(bool value = true)` replaces the separate `Visible()` and `Visible(bool)`; callers do not change.
- **Titled factories only where no other string is the identity.** `Ui.Panel("Build", p => ...)` works; live widgets whose factory already takes a string widget ID keep that signature, because a titled overload would be ambiguous.

## Order of work

1. **Clean bases.** Finish the open HtmlForgeX, CanopyX and ConsoleMonster merges, and land the ChartForgeX visual and token work, so that each library starts from a settled default branch and visual baselines that are not moving.
2. **Generator and vocabulary in PowerForge.** Done: `PowerForge.Generators` (PSPublishModule #1244) ports ConsoleMonster's generator with the attributes, the PFF001-PFF009 diagnostics, the delegate-verb rule, `[FluentCmdlet]` and the shared `ThemeMode`/`Tone`/`Icon` definition.
3. **ConsoleMonster pilot.** Done for ConsoleMonster 3.0. It proved the generator on a library that already worked this way, before the large ChartForgeX change.
4. **ChartForgeX 4.0.** Follow the implementation sequence in `next-major-api.md`: Line/Bar vertical slice, then every family, diagrams, Visuals and Stories, then removal of the old surface. The gallery must produce the same images before and after, which is only a useful check because step 1 froze the visuals.
5. **CanopyX and CodeGlyphX.** Apply the generator and vocabulary.
6. **Consumers, once each.** ConsoleMonster.Graphics, then OfficeIMO, the HtmlForgeX bridges, PowerBGInfo, ImagePlayground with `New-Cfx*`, and TestimoX last through HtmlForgeX. They move straight from the ChartForgeX package each one references today (between 1.8.1 and 3.1.0, depending on the consumer) to the new major of every engine, without stopping at ChartForgeX 3.x.

Each library ships one breaking release with a short migration note. There are no compatibility shims or obsolete forwarding methods. PowerShell keeps parameter aliases for one major version because scripts cannot be recompiled.

## Choices still open

| Choice | Recommendation |
| --- | --- |
| ChartForgeX model granularity | Typed models per family group rather than per series kind: `CartesianChart` (Line, Area, Bar, Scatter, Bubble, financial, range and statistical series), `RadialChart`, `HierarchyChart`, `RelationshipChart` (Sankey, Chord, Tree), `ScheduleChart` (Gantt, Timeline, StateTimeline), `MapChart`, `GaugeChart`, `ProportionChart` (Pie, Donut, Funnel, Pyramid). `Chart.Line(...)` returns a `CartesianChart` whose first series is a `LineSeries`, so `Mixed` is just a `CartesianChart` with several series types. That is about eight root models instead of 54, and no irrelevant option bags. |
| ConsoleMonster factory titles | Add titled overloads (`Ui.Panel("Build", ...)`) and keep the parameterless ones, because many ConsoleMonster widgets have no natural title. |
| CodeGlyphX theme | Leave `Theme` and `ThemeMode` out until it renders anything mode-dependent; this is the one exception to the root-model rule above. |
