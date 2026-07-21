# ChartForgeX Markup for VS Code

Author ChartForgeX visual blocks from Markdown-friendly markup, preview them while you write, export SVG, PNG, or HTML, and generate C# builder code for topology diagrams.

## What You Get

- Syntax highlighting for `chartforgex <kind> v1` fenced blocks.
- Markdown diagnostics that point back to the original source line.
- Live preview backed by `ChartForgeX.Markup.Cli`.
- Export commands for SVG, PNG, and standalone HTML.
- C# builder generation for moving a diagram into application code.

## Quick Start

Create a native visual block in Markdown:

````markdown
```chartforgex chart v1 {#trend title="Trend" type="line"}
labels Jan Feb Mar
values 12 18 16
```
````

Run `ChartForgeX Markup: Open Preview` from the command palette.

Native v1 snippets are available for topology, flow, sequence diagrams, table-backed topology, charts, multi-series charts, timelines, and Gantt plans. Mermaid fences are scanned by the language, while semantic Mermaid parsing is provided by the optional ChartForgeX Mermaid adapter used by the packaged CLI.

The extension package includes `schemas/chartforgex-markup-v1.schema.json`, the same machine-readable v1 grammar schema packed by `ChartForgeX.Markup`, so editor tooling can discover supported fence families and table sections without scraping docs.

## Commands

- `ChartForgeX Markup: Open Preview`
- `ChartForgeX Markup: Validate`
- `ChartForgeX Markup: Export SVG`
- `ChartForgeX Markup: Export PNG`
- `ChartForgeX Markup: Export HTML`
- `ChartForgeX Markup: Generate C#`
- `ChartForgeX Markup: Generate C# File`
- `ChartForgeX Markup: Open Output Folder`

## Supported Files

The extension activates for:

- `.cfx.md`
- `.chartforgex.md`
- Markdown files that contain fenced `chartforgex <kind> v1` blocks

## Requirements

Packaged installs include a bundled `ChartForgeX.Markup.Cli` for:

- `win-x64`
- `win-arm64`
- `linux-x64`
- `linux-arm64`
- `osx-x64`
- `osx-arm64`

The package also includes a portable .NET fallback. Development installs fall back to the sibling `ChartForgeX.Markup.Cli` project when the bundled CLI is not present.

Set `chartforgexMarkup.cliPath` to use a custom executable, DLL, or `.csproj`.

## Settings

- `chartforgexMarkup.cliPath`: custom CLI executable, DLL, or project path.
- `chartforgexMarkup.validateDebounceMs`: validation delay after edits.
- `chartforgexMarkup.previewAutoRefresh`: refresh previews while editing.
- `chartforgexMarkup.previewDebounceMs`: preview refresh delay.
- `chartforgexMarkup.outputDirectoryMode`: write exports beside the Markdown file or into the workspace.
- `chartforgexMarkup.outputSubfolderName`: output folder name.
- `chartforgexMarkup.defaultExportFormat`: default export format.

## Development

```powershell
npm install
npm run compile
```

Install the extension as a linked development build:

```powershell
.\scripts\dev-install.ps1 -Insiders -Force
```

Reload VS Code after compiling changes.

## Package

Package the VSIX and refresh bundled CLI assets with:

```powershell
npm run package
```

This runs `scripts/package-vsix.cjs`, publishes `ChartForgeX.Markup.Cli`, copies portable and self-contained runtime assets into `tools/ChartForgeX.Markup.Cli`, compiles the extension, and creates a VSIX under `dist/`.

PowerShell and CI callers can use the equivalent script directly:

```powershell
.\scripts\package-vsix.ps1
```

Install a packaged build into VS Code Insiders:

```powershell
.\scripts\install-insiders.ps1 -Force
```

## Marketplace Publishing

Set `VSCE_PAT` and publish:

```powershell
$env:VSCE_PAT = '<token>'
npm run publish:marketplace
```

The GitHub workflow packages the extension on pull requests and pushes that touch ChartForgeX markup, runtime dependencies, or the workflow itself. It uploads the VSIX as the `chartforgex-markup-vsix` artifact.

Release publishing follows the OfficeIMO-style flow:

- Normal package release tags such as `ChartForgeX-v1.0.3` stamp the extension version from UTC publication time as three SemVer-safe numeric components: `year.(month*100+day).(hour*10000+minute*100+second)`. For example, 2026-07-21 10:58:09 UTC becomes `2026.721.105809`.
- Legacy `ChartForgeX-vYYYYMMDDHHMMSS` release tags remain supported and map to the same unpadded numeric components.
- Release publishing is allowed only from commits contained in `origin/main`.
- Existing Marketplace versions are skipped, while the VSIX is still attached to the GitHub Release.
- Pre-release GitHub releases and workflow-dispatch `pre_release` runs package as VS Code pre-release extensions.
- Marketplace publishing requires the repository secret `VSCE_PAT`.

## Support

Use the ChartForgeX issue tracker for bugs, diagram syntax ideas, and extension packaging problems.
