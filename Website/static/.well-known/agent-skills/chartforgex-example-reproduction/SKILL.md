# ChartForgeX Example Reproduction

Use this skill when a user wants to reproduce, adapt, or compare a ChartForgeX visual example.

## Primary Resources

- Promoted case manifest: `/examples/promoted-cases.json`
- Manifest schema: `/schemas/promoted-cases.schema.json`
- Examples page: `/examples/`
- Full generated catalog: `/gallery/`
- Source repository: `https://github.com/EvotecIT/ChartForgeX`

## Workflow

1. Read `/examples/promoted-cases.json` first.
2. Match the requested visual by `id`, `title`, `category`, or `tags`.
3. Use `source.path` and `source.entry` as the source-of-truth implementation pointer.
4. Use `build.command` to regenerate the example output from the repository root.
5. Use the available entries under `artifacts` to compare rendered output.

## Boundaries

- The dedicated website owns browsing, documentation, and metadata.
- ChartForgeX.Examples owns generated visual output.
- Generated demo HTML may contain demo-internal links that are not website routes.
