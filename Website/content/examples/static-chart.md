---
title: "Your first static chart"
order: 1
description: "Render the same typed ChartForgeX chart as SVG, HTML, and PNG from one C# definition."
layout: example
---

One typed definition renders to every surface. The output above is this program's SVG; choose `--dark` when running the complete program to use the dark theme.

## Describe the chart

Create the chart once: a title, axis titles, a size and the category labels. Series draw in the order you add them, so the failed checks sit on top of the passed area.

{{< cfx-example-code id="static-chart" part="chart" >}}

## Save each surface

`To*` methods return content, `Save*` methods write files, and `Write*` methods stream bytes. SVG is the highest-fidelity static target; HTML wraps inline SVG; PNG is useful for email and document pipelines.

{{< cfx-example-code id="static-chart" part="save" >}}

## Supply the points

A small helper turns plain values into points at x = 1, 2, 3 and so on, one for each category label.

{{< cfx-example-code id="static-chart" part="points" >}}
