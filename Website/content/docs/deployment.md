---
title: Deployment
description: Deployment and local validation shape for the ChartForgeX dedicated project site.
slug: deployment
collection: docs
layout: docs
canonical: https://chartforgex.evotec.xyz/docs/deployment/
---

# Deployment

ChartForgeX uses the dedicated project-site model: the Evotec hub keeps the
catalog page, while this folder publishes the richer demo surface for
`https://chartforgex.evotec.xyz/`.

## Local Commands

Run the library build from the repository root when the generated examples need
to be refreshed:

```powershell
.\Build.ps1
```

Then build the website from `Website/`:

```powershell
.\build.ps1 -Dev
.\build.ps1 -Ci
```

For local review, serve the generated `_site` folder:

```powershell
.\build.ps1 -Dev -Serve -Port 8021
```

The wrapper prefers the sibling `PSPublishModule` checkout when it exists, then
falls back to `powerforge-web` from `PATH`.

## Publish Target

The preferred target is the same hosting family as the main Evotec site:
PowerForge.Web static output on Linux Apache behind the Evotec domain setup. The
site is not designed around GitHub Pages as the primary publish target because
Evotec already depends on shared deployment behavior for headers, redirects,
cache policy, quality gates, and future cross-site operations.

The pipeline emits deployable files into `_site`. Generated reports and
temporary files stay in `_reports` and `_temp`; those folders are local build
artifacts and should not be committed.

## Release Checklist

1. Refresh ChartForgeX examples from the repository root.
2. Run `.\build.ps1 -Ci` from `Website/`.
3. Review `/`, `/examples/`, `/docs/`, and `/docs/deployment/` locally.
4. Publish `_site` to the `chartforgex.evotec.xyz` virtual host.
5. Keep the hub page at `https://evotec.xyz/projects/chartforgex/` as the
   central registry entry.

## Route Ownership

The website shell owns curated pages such as `/examples/` and `/docs/`. Copied
generated outputs under `/examples/generated/` and `/examples/topology/` remain
ChartForgeX product artifacts; they are linked from the shell but are not
rewritten into website-native documentation pages.

Project-specific discovery files are also owned here:

- `/llms.txt`
- `/llms.json`
- `/agents.json`
- `/.well-known/agents.json`
- `/.well-known/api-catalog`
- `/.well-known/agent-skills/index.json`
