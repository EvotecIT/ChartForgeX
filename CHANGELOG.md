# Changelog

## 3.3.0 — 1 November 2026

This release adds shared chart styling, optional linked interactions and architecture scenarios while keeping static rendering script-free by default.

### Added

- Server-rendered charts support immutable `ChartMarkOptions`, per-mark links and host hooks, shared-domain strips, fluid width, inherited page fonts and CSS colours, open-ended schedule fades, and optional adjacent HTML data tables. [#407](https://github.com/EvotecIT/ChartForgeX/pull/407)
- `WithSharedDesignRoles()` applies the page's shared colour and font roles. Charts share categorical colours, readable value axes, mark surfaces and missing-data hatching. [#408](https://github.com/EvotecIT/ChartForgeX/pull/408)
- `WithEnhanceHooks()` exposes record keys and fields for the optional `cfx-enhance` assets. Tooltips, keyboard navigation, legend toggles and family gestures work with the rendered SVG. `CfxLink.connect()` links charts and CanopyX grids; applied filters and highlights do not echo selection events. [#408](https://github.com/EvotecIT/ChartForgeX/pull/408)
- Architecture markup describes nodes, boundaries, connections, use cases and request/response payloads. `ResolveScenario`, `PrepareScenarioStep`, `ToSequenceArtifact` and `ToScenarioStory` reuse the topology model for synchronized diagram playback, sequence views, static frames and stories. The markup CLI also renders terminal text, SVG, PNG and interactive HTML. [#405](https://github.com/EvotecIT/ChartForgeX/pull/405)

### Improved

- Heatmaps, pies and donuts retain readable layouts at narrow widths. Value labels use consistent precision, axis captions describe their actual tick values, and long category captions shorten with their full text available as a tooltip. [#402](https://github.com/EvotecIT/ChartForgeX/pull/402)
- SVG accessibility modes distinguish static images from interactive graphics. Static generic groups preserve descriptions without invalid ARIA labels; interactive groups, links and selectable marks retain their appropriate roles and names, including transition exports. The website hero uses responsive image variants. [#409](https://github.com/EvotecIT/ChartForgeX/pull/409)
- Adaptive Cartesian windows retain their fitted tick steps, and fluid SVG preserves bar corners, stack envelopes and fixed column thickness. [#408](https://github.com/EvotecIT/ChartForgeX/pull/408)

### Build and packaging

- Updated VS Code packaging dependencies to Azure Identity 4.14.1 and MSAL Node 7.1.2 within the supported dependency range. These are development tools and are absent from the packaged extension runtime. [#403](https://github.com/EvotecIT/ChartForgeX/pull/403)
- Corrected isolated package probes and browser-test loopback socket handling. [#403](https://github.com/EvotecIT/ChartForgeX/pull/403), [#410](https://github.com/EvotecIT/ChartForgeX/pull/410)
- Restored complete macOS compatibility fixtures and corrected browser measurements for printed disclosure tables and fluid line spans. The existing navigation, geometry and visibility contracts run in Firefox and WebKit as well as Chromium. [#412](https://github.com/EvotecIT/ChartForgeX/pull/412)

All nine packages use version 3.3.0. Libraries retain `net472`, `netstandard2.0`, `net8.0` and `net10.0`; the command-line tool targets `net8.0`. The core rendering engine has no runtime package dependencies. Browser enhancement remains optional.

[Full comparison with 3.2.0](https://github.com/EvotecIT/ChartForgeX/compare/ChartForgeX-v3.2.0...ChartForgeX-v3.3.0)
