---
title: ChartForgeX documentation
description: Documentation entry point for the ChartForgeX dedicated project site pilot.
slug: index
collection: docs
layout: docs
canonical: https://chartforgex.evotec.xyz/docs/
---

# ChartForgeX Documentation

This dedicated site starts as a visual/demo pilot. The goal is to prove a
repeatable model for Evotec projects that need more than a catalog card, without
turning the main hub into a giant per-project documentation app.

## Site Ownership

- `https://evotec.xyz/projects/chartforgex/` stays the central Evotec project
  registry page.
- `https://chartforgex.evotec.xyz/` owns the richer product experience:
  examples, visual cases, docs, and project-specific browsing.
- `Website/site.json` and `Website/pipeline.json` define the publishable site.
- Generated examples stay produced by the repository build and are copied under
  `/examples/generated/` when present.
- The generated example HTML is treated as product output. The website shell
  links to it, but does not pretend to own demo-internal application routes.

## Hosting Shape

Use the same deployment family as the main Evotec website: PowerForge.Web output
hosted on Linux Apache behind the Evotec domain/subdomain setup. GitHub Pages is
not the preferred target for this pilot because Evotec sites already rely on the
main deployment model for redirects, headers, cache policy, and future shared
ops.

See the [deployment notes](/docs/deployment/) for local commands, release
checks, and route ownership.

## Example Case Contract

Promoted examples should be easy to continue from. The public pages are
data-driven so new ChartForgeX work can add visuals without rewriting the
website shell:

- `data/showcase.json` feeds the selected examples on `/examples/`;
- `data/gallery.json` feeds the generated catalog on `/gallery/`;
- `static/examples/generated/` stores seed SVG/PNG/HTML artifacts for the site;
- `build/Sync-GeneratedExamples.ps1` can refresh the gallery data from the
  example output folder.

Each curated showcase case should include:

- rendered preview;
- links to the generated HTML, SVG, and PNG artifacts;
- the C# snippet or source pattern that creates the same shape;
- copyable metadata that can be pasted into an issue, docs page, or follow-up
  implementation note.

That keeps the user journey practical: see the visual, open the output, jump to
the code, and adapt it.

The same contract is also published as structured data:

- `/examples/promoted-cases.json` lists promoted cases, source links, output
  paths, and artifact URLs.
- `/schemas/promoted-cases.schema.json` describes the reusable manifest shape
  for the next dedicated project site.

## Discovery Metadata

The dedicated site owns its own agent-facing discovery files instead of relying
on the central Evotec hub to advertise project-specific resources:

- `/llms.txt` and `/llms.json` summarize the project and important links.
- `/agents.json` and `/.well-known/agents.json` expose project resources for
  agent-aware clients.
- `/.well-known/api-catalog` points at the promoted-case manifest, schema,
  docs, examples, and source repository.
- `/.well-known/agent-skills/index.json` lists the lightweight reproduction
  skill for source-linked examples.

## Install

```powershell
dotnet add package ChartForgeX
```

## Repository

The source repository is
[EvotecIT/ChartForgeX](https://github.com/EvotecIT/ChartForgeX).
