---
title: "Interactive HTML"
description: "Add self-contained browser interaction without changing ChartForgeX's static rendering contract."
layout: docs
---

Install `ChartForgeX.Interactivity.Html` when a chart or topology needs browser behavior. It provides self-contained pages for tooltips, selection, zoom, pan, brush ranges, scenarios, hierarchy navigation, SVG, Canvas, or WebGL graph rendering, and exports.

The adapter is opt in. The same underlying models can still produce script-free SVG, PNG, and static HTML for email, documentation, reports, and release artifacts.

For graph explorers, use stable node ids and explicit host callbacks for validated changes. Search, navigation, persisted interaction state, and graph manipulation remain adapter or host concerns; application data and authorization remain with the application.

See the maintained [interactivity reference](https://github.com/EvotecIT/ChartForgeX/blob/main/docs/interactivity.md) and [graph explorer reference](https://github.com/EvotecIT/ChartForgeX/blob/main/docs/graph-explorer.md) for the complete option and event contracts.
