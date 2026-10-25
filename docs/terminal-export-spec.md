# Terminal export appearance and timing

Authored `TerminalStory` scenes and ConsoleMonster frame exports share the following
reference values. ChartForgeX has no ConsoleMonster dependency. The matching
ConsoleMonster definitions are `ConsoleMonster/Export/ExportOptions.cs`,
`WindowChrome.cs`, `ExportPalette.cs` and `ConsoleMonster/Styling/Theme.BuiltIn.cs`.

## Default appearance

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

| Geometry | Stories | ConsoleMonster definition |
| --- | --- | --- |
| macOS bar / radius | 28 / 10 px | `WindowChrome.BarHeight/Radius(MacOS)` |
| Windows Terminal bar / radius | 32 / 8 px | `WindowChrome.BarHeight/Radius(Windows)` |
| Linux bar / radius | 36 / 12 px | `WindowChrome.BarHeight/Radius(Linux)` |
| Content padding | 12 px inside an 8 px page inset | `ExportOptions.Padding = 12` |
| macOS traffic lights | radius 6 px, centres 18/38/58 px from window left and 14 px from top | `WindowChrome.AppendSvg` |
| Body font / line height | 14 / 16.8 px | `ExportOptions.FontSize = 14`, `LineHeight = 1.2` |
| Title font | 13 px | `WindowChrome.AppendHtmlCss` |
| Title fitting advance | 8 px | `WindowChrome.TitleAdvance` |

The font stack is `'Cascadia Mono', 'Cascadia Code', 'DejaVu Sans Mono', Menlo,
Consolas, 'Liberation Mono', 'Courier New', monospace`, matching
`ExportOptions.DefaultFontFamily`. Native PNG uses ChartForgeX's font resolver;
ConsoleMonster.Graphics embeds JetBrains Mono. For reproducible comparisons,
select the same installed font family in both renderers. Raster and SVG output retain
font metrics supplied by that font; the ConsoleMonster export grid uses a fixed
`fontSize * 0.6` cell advance. Stories retains its authored tab strip, shell icons,
page inset and deterministic raster shadow. ConsoleMonster exports a single frame
with a single title. These are presentation differences, not terminal data loss.

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
