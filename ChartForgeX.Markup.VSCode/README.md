# ChartForgeX Markup for VS Code

Author ChartForgeX topology diagrams from Markdown-friendly markup, preview them while you write, and export SVG, PNG, HTML, or generated C# builder code.

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
- Markdown files that contain fenced `chartforgex topology` blocks

## Requirements

Packaged installs include a bundled `ChartForgeX.Markup.Cli` for common Windows, Linux, and macOS runtimes. Development installs fall back to the sibling `ChartForgeX.Markup.Cli` project when the bundled CLI is not present.

Set `chartforgexMarkup.cliPath` to use a custom executable, DLL, or `.csproj`.

## Development

```powershell
npm install
npm run compile
```

Package the VSIX and refresh bundled CLI assets with:

```powershell
npm run package
```
