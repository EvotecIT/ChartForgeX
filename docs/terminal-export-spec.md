# Terminal export appearance and timing

The reviewed Stories native PNG is the reference for window chrome. Palette and
body font metrics already agree with ConsoleMonster and retain their existing
values. The tables below distinguish the chosen appearance from ConsoleMonster's
current implementation; they are an adoption specification, not a claim that the
two exporters already draw identical windows. ChartForgeX has no ConsoleMonster
dependency.

## Element choices

| Element | Chosen reference | ConsoleMonster definition to update |
| --- | --- | --- |
| Title typography | Stories: 13 px, regular weight 400; muted centred macOS/Linux title, normal-text Windows tab title | `Export/WindowChrome.cs`: `AppendTokens`, `AppendHtmlCss`, `AppendSvg`; `Export/SvgFrameWriter.cs`: `AppendCss` |
| Windows tab style | Stories: shell tile, title, tab close ×, then + and ⌄ | `WindowChrome.HtmlBar`, `AppendPrompt`, `AppendRight`, `AppendSvg`, `AppendHtmlCss` |
| Corner radius | Existing 10/8/12 px values; Stories clips the body to retain all four corners | `WindowChrome.Radius`, `AppendSvg`, `AppendHtmlCss`; `SvgFrameWriter.Write` content layers |
| Shadow | Stories' deterministic two-layer PNG shadow; its blurred vector recipe for SVG/HTML | `WindowChrome.AppendSvg`, `AppendHtmlCss`; `ConsoleMonster.Graphics/GraphicsRenderer.cs`: `Svg` |
| Outer inset | Stories: 8 px on each side, filled with the page colour | `SvgFrameWriter.cs`: `SvgGeometry.For`, `Write`, `AppendChrome`; `WindowChrome.AppendTokens` |
| Body padding and origin | Keep 12 px padding; use Stories' top-origin glyph placement inside the inset window | `ExportOptions.Padding`; `SvgGeometry.For`; `SvgFrameWriter.AppendRow`, `AppendText`, `AppendCss` |

Stories owners are `TerminalWindowChrome` in `TerminalWindowStyle.cs`,
`PngTerminalStoryChromeRenderer`, `SvgTerminalStoryChromeRenderer`,
`TerminalStoryLayout`, and the PNG/SVG terminal story renderers under
`ChartForgeX.Stories/Terminal/`. ConsoleMonster paths in this document are relative
to its `ConsoleMonster/` project unless the Graphics project is named explicitly.

## Palette and body metrics

| Token | Dark | Light | Matching definition |
| --- | --- | --- | --- |
| Terminal background | `#0c0c0c` | `#ffffff` | `ExportPalette.Dark/Light.Background` |
| Output | `#cccccc` | `#24292f` | `ExportPalette.Dark/Light.Foreground` |
| Title bar | `#1f2a3c` | `#e5e7eb` | `ThemeToken.Surface` background |
| Muted | `#8b8f9a` | `#6b7280` | `ThemeToken.Muted` |
| Accent / prompt | `#60a5fa` | `#2563eb` | `ThemeToken.Accent` |
| Success | `#4ade80` | `#15803d` | `ThemeToken.Pass` |
| Warning | `#fbbf24` | `#b45309` | `ThemeToken.Warn` |
| Error | `#f87171` | `#b91c1c` | `ThemeToken.Fail` |
| Border | `#4b5060` | `#8a8a94` | `ThemeToken.Border` |
| Page | `#3a3a3f` | `#c9c9cf` | `WindowChrome.AppendTokens`, `--cm-desk` |
| macOS close / minimise / zoom | `#ce534c / #c59a2a / #26a443` | `#ff5f57 / #febc2e / #28c840` | `WindowChrome.AppendTokens` |

`TerminalThemes.Default()` uses these paired palettes. Named PowerShell, Ubuntu,
Campbell, Classic and Graphite recipes retain their specific identity; callers
can continue supplying a `VisualTheme` and `TerminalStyleOptions`.

| Metric | Retained value | Matching ConsoleMonster definition |
| --- | --- | --- |
| Body size / row advance | 14 / 16.8 px | `ExportOptions.FontSize = 14`, `LineHeight = 1.2`, `CellHeight` |
| Monospace cell advance | 8.4 px at 14 px (`fontSize * 0.6`) | `ExportOptions.CellWidth` |
| Regular body paint | Weight 400, opacity 1; Output colour above | `SvgFrameWriter.AppendCss`, `AppendText`; `ExportPalette.ForegroundOpacity` |
| Padding inside the window | 12 px | `ExportOptions.Padding` |

The font stack is `'Cascadia Mono', 'Cascadia Code', 'DejaVu Sans Mono', Menlo,
Consolas, 'Liberation Mono', 'Courier New', monospace`, matching
`ExportOptions.DefaultFontFamily`. Native PNG uses ChartForgeX's font resolver;
ConsoleMonster.Graphics embeds JetBrains Mono. Use the same font file and regular
face for visual comparisons. Keep these sizes, colours and row/cell advances when
correcting baseline placement; do not compensate by enlarging or lightening text.
Prompt emphasis remains separate from the regular command/output text.

## Frame, inset and body coordinates

The shared chrome recipe covers windowed macOS, Windows Terminal and Linux
exports. All numbers are logical pixels before output scaling. `W` and `H` include the
page inset; `B` is the bar height and `r` the radius. Recipes use the Stories
viewport range, 480–1800 px wide. Coordinates below are **canvas coordinates**.
An implementation retaining window-local coordinates subtracts `(8, 8)` from
positions after evaluating the expressions with `W` and `H`.

| Style | B | r | Matching ConsoleMonster definition |
| --- | --- | --- | --- |
| macOS | 28 | 10 | `WindowChrome.BarHeight/Radius(MacOS)` |
| Windows Terminal | 32 | 8 | `WindowChrome.BarHeight/Radius(Windows)` |
| Linux | 36 | 12 | `WindowChrome.BarHeight/Radius(Linux)` |

| Part | Chosen rectangle or position | ConsoleMonster adoption point |
| --- | --- | --- |
| Page | `(0, 0, W, H)`, opaque Page colour | `SvgFrameWriter.Write`; `WindowChrome.AppendTokens` already supplies `--cm-desk` |
| Window | `(8, 8, W-16, H-16)`, radius `r`, Terminal background fill | `WindowChrome.AppendSvg`, `.cm-window` in `AppendHtmlCss` |
| Frame stroke | Border colour, 1.2 px | `WindowChrome.AppendSvg`, `AppendHtmlCss`; replace the generic grey outline |
| Bar | Top `y=8`, height `B`, Title bar fill; only its top corners rounded | `WindowChrome.AppendSvg`, `AppendHtmlCss` |
| Bar divider | From `(8, B+8)` to `(W-8, B+8)`, Border colour, 1 px | `WindowChrome.AppendSvg`, `AppendHtmlCss` |
| Body paint | `(9, B+9, W-18, H-B-18)` | `SvgFrameWriter.Write` background/content layers |
| Inner clip | `(9, 9, W-18, H-18)`, radius `r-1`; clip **all body backgrounds and cells** | `SvgFrameWriter.Write`; retain `overflow:hidden` in `AppendHtmlCss` |
| Text origin, row i | `x=20`, `y=B+20+i*16.8` in the top-origin native glyph painter | `SvgGeometry.ScreenX/ScreenY`, `SvgFrameWriter.AppendRow`/`AppendText` |

The text origin is 8 px page inset plus 12 px window padding. A natural grid
therefore adds 40 px horizontally and `B+40` px vertically to its grid extent;
round the final canvas dimensions upward. An explicitly sized viewport retains
its requested size and has `W-40` px of usable body width.
`ConsoleExporter.MeasureFrame` must report that same outer size, so the
`ConsoleMonster.Graphics/GraphicsLayout.cs` constructor and its `Fits` calculation
include the inset exactly once. Slide safe areas, backgrounds and font fitting
remain host layout choices. `GraphicsComposition.Compose` must account for the
new window extent and avoid duplicating the terminal frame border/shadow with
its existing surrounding decoration.

For ConsoleMonster.Graphics, reproduce `RgbaCanvas.DrawText`'s top-origin
placement: the alphabetic baseline is the row origin plus that selected font's
ascent at 14 px, subject to the same glyph grid fitting. This names the internal
reference painter, not a callable consumer API. For the embedded
`ConsoleMonster.Graphics/Fonts/JetBrainsMono.ttf` used in these comparisons,
`hhea.ascender = 1020` and `head.unitsPerEm = 1000`, so the offset is exactly
`14 * 1020 / 1000 = 14.28` px. Emit SVG text with
`dominant-baseline="alphabetic"` at `y=B+34.28+i*16.8`; keep the registered regular
face. The reviewed font's SHA-256 is
`e6fd0d7e91550b3ed2b735d4312474362c4716edc4fc0577a0f61ed782d5aed1`.
The 14.28 px offset belongs to this face, not to every family in the font stack.
Its current
`SvgFrameWriter.AppendRow` places text at `rowTop + CellHeight/2` with
`dominant-baseline:central`. Convert that text anchor rather than changing the
cell pitch, font size or palette. Preserve the original row coordinates for
box/block glyph shapes. Stories' ordinary SVG text uses an alphabetic baseline
at the row origin plus 14 px; its native glyph painter is the reference for the
reviewed PNG placement.

### Shadows

For PNG, paint these black rounded rectangles behind the window, in order,
without a blur. These are the exact `PngTerminalStoryChromeRenderer.Draw` layers.
Do not clip them to the window's inner body clip.

| Layer | Rectangle | Radius | Black alpha |
| --- | --- | --- | --- |
| Far | `(12, 18, W-24, H-24)` | `r+2` | 0.18, stored as 46/255 (`#0000002e`) |
| Near | `(10, 14, W-20, H-20)` | `r+1` | 0.12, stored as 31/255 (`#0000001f`) |

ConsoleMonster.Graphics renders SVG through ChartForgeX. Emit these two painted
shapes in `WindowChrome.AppendSvg` for its PNG path, before the frame and clipped
body, so the shadow survives rasterization.

For vector SVG/HTML, Stories' blurred recipe is black, offset `(0,12)`, SVG
`stdDeviation=18`, opacity `0.28`; the filter bounds are `x=-15%`, `y=-15%`,
`width=130%`, `height=140%`. HTML can use an inline SVG window underlay with this
same filter and the separate 1.2 px Border stroke above. Replace ConsoleMonster's current
`0 10px 30px rgba(0,0,0,.35)` box shadow and grey outline in
`WindowChrome.AppendHtmlCss` rather than stacking both recipes. The PNG's
deterministic painted layers and the vector blur are explicit format-specific
recipes.

## Title and controls

Titles use the terminal's monospace family, **explicit 13 px and weight 400**.
Set these properties explicitly in `SvgFrameWriter.AppendCss` and
`WindowChrome.AppendHtmlCss`/`AppendSvg`, including the Graphics title override in
`GraphicsRenderer.Svg`. `WindowChrome.AppendTokens` maps macOS/Linux title colour
to Muted rather than the Surface foreground; Windows tab titles use Output.
Keep title opacity 1 when the tab is settled. Stories' SVG title weight is also
400, matching its native PNG painter.

For macOS/Linux, centre the natural measured title at `x=W/2`. Its native draw
origin is `y=8+B/2-6.5` (13 px text box centred in the bar). For Windows, draw at
`(tabX+40,18)`. `WindowChrome.TitleAdvance` stays 8 px as a **fitting budget**;
remove forced `textLength`/`lengthAdjust` stretching from `AppendSvg` and measure
the actual title run for centring. Stories reserves 168 px for macOS controls,
112 px for Linux before dividing the remaining width by 8.
Windows title capacity is `floor(max(8,tabWidth-72)/8)`. Truncate on text-element
boundaries with `…` (U+2026), not three full stops.

### macOS

The close/minimise/zoom centres are `(26,22)`, `(46,22)`, `(66,22)`, radius 6 px.
Relative to the window these are `(18,14)`, `(38,14)`, `(58,14)`.
`WindowChrome.AppendSvg` already has these local positions and
`AppendHtmlCss` uses 12 px circles with an 8 px gap; retain them and the existing
light/dark colours above. Only the canvas translation, title paint and frame
composition change.

### Windows Terminal

For `n` tabs, use `tabWidth = min(300, max(72, max(96,W-230)/n))`,
`tabX = 16+i*tabWidth` for zero-based tab `i`, and `R = 16+n*tabWidth` for the
strip's right edge. ConsoleMonster's single exported tab uses `n=1`; at the
reviewed `W=656`, its tab width is 300 px.

| Element | Exact chosen geometry / paint | Matching ConsoleMonster definition |
| --- | --- | --- |
| Tab | `(tabX,12,tabWidth,28)`, radius 6; inactive Title bar, active Terminal background | `AppendSvg` tab rectangle; `AppendHtmlCss` `.cm-tab`; `HtmlBar` |
| Shell tile | `(tabX+12,17,18,18)`, radius 3, Accent fill | Replace bare `AppendPrompt` glyph |
| Shell mark inside tile | Path `M4 5l4 4-4 4M9.5 13h5`, relative to tile; Terminal background stroke 1.7 px, round caps/joins | `AppendPrompt`, `HtmlControl` |
| Title | `(tabX+40,18)` top-origin, 13 px/400, Output colour | `Left`, `Right`, title-area construction in `AppendSvg`, `AppendHtmlCss` |
| Tab close × | Centre `(tabX+tabWidth-19,26)`, diagonals from offsets `(-4,-4)` to `(4,4)` and `(-4,4)` to `(4,-4)`; Muted, 1.4 px | Add tab close in `HtmlBar`/`AppendSvg`; separate from window close |
| New tab + | Centre `(R+25,26)`, horizontal/vertical arms ±6 px, Muted, 1.4 px | `AppendRight`, `AppendHtmlCss` |
| Tab menu ⌄ | Polyline `(R+52,23) (R+56,27) (R+60,23)`, Muted, 1.5 px | `AppendRight`, `AppendHtmlCss` |
| Minimise | Line `(W-106,26)` to `(W-94,26)`, Output, 1.3 px | `AppendRight` |
| Maximise | Rectangle `(W-66,21,12,12)`, Output stroke 1.3 px, no fill | `AppendRight` |
| Window close × | Centre `(W-25,26)`, diagonal offsets ±6 px, Output, 1.4 px | `AppendRight` |

Render ×, + and ⌄ with these vectors, avoiding font-dependent Unicode glyph
shapes. The shell tile is the PowerShell/Windows PowerShell recipe. Stories'
Ubuntu icon uses an Accent disc of radius 9 with a Terminal-background centre of
radius 3; `None` omits the shell icon. Real terminal hosts select the appropriate
profile icon while retaining the same 18 px slot. `AppendPrompt` currently emits
only a bare `>_`-shaped path; it does not supply this tile or tab-close control.

### Linux

Retain a centred regular 13 px Muted title. The close ring centre is
`(W-30,26)`, radius 11, Muted stroke 1.2 px. Its × uses diagonal offsets ±4 px,
Muted stroke 1.4 px. These correspond to `WindowChrome.AppendRight`'s Linux ring
and cross after adding the page inset; remove the bold Linux title override in
`AppendSvg` and `AppendHtmlCss`.

## ConsoleMonster adoption checklist

1. Make title size, weight, colour and natural-width alignment explicit across
   HTML, SVG and the Graphics title override.
2. Replace the Windows bare prompt with the tab, shell tile, tab close, + and ⌄
   geometry above; retain separate window controls.
3. Add the 8 px page inset, page fill and frame stroke; clip every body layer to
   the rounded inner window, including the bottom corners.
4. Add the specified shadow recipe for each output path.
5. Keep the existing palette, font file, 14 px size and 16.8/8.4 px grid metrics;
   align native text with the top-origin/ascent baseline instead of cell-centred
   placement. Recheck at 1× and 2× without changing regular text opacity.

## Playback presets

| Preset | Initial delay | Characters / second | Line delay | Tab reading hold |
| --- | --- | --- | --- | --- |
| Slow | 0.60 s | 28 | 0.14 s | 2.00 s |
| Normal | 0.35 s | 42 | 0.08 s | 1.20 s |
| Fast | 0.15 s | 72 | 0.04 s | 0.55 s |

`story.GetCommandDurationSeconds(command)` returns
`clamp(visibleTextElements(command) / story.CharactersPerSecond, 0.35, 4.5)`.
The count uses grapheme clusters, so a combining mark does not add a typing step.
The prompt is excluded from the automatic duration calculation. An explicit
positive `Command` duration overrides it. Wrapping distributes that duration
across visible elements in the prompt and command; each physical line adds the
preset line delay.

Output and table rows start at successive line-delay intervals. Each row reveals
for 0.22 s with a cubic ease-out and a 3 px upward settling motion. Before a tab
switch, playback waits until the current reveal has ended and at least the tab
hold has elapsed after the latest content completion. Explicit `Pause` steps and
tab transition durations remain additive. `WithTiming` and `WithTabHold` override
individual values. Stories remain bounded to a 60 s logical timeline.

The final prompt starts 0.08 s after the authored steps. GIF/APNG sampling uses
whole-centisecond intervals rounded up from the requested frame rate. The final
hold starts at the first completed sample and defaults to 1.2 s; configure it with
`TerminalStoryAnimationOptions.WithEndHold`. Core encoders instead take each
`RasterAnimationFrame.Duration` directly, including the final hold, and use
`RasterAnimationOptions.PlayCount` (0 = infinite, 1 = once, N = total plays).
