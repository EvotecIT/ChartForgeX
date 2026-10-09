# ChartForgeX - Dependency-Free Chart Rendering for .NET

ChartForgeX renders polished charts, animated visual stories, visual blocks, topology diagrams, and static report visuals from .NET without adding runtime chart dependencies to generated output.

The core package renders charts and diagrams to SVG, script-free HTML, PNG, JPEG, BMP, PPM and TIFF. `ChartForgeX.Visuals` adds static canvases, image composition, factual blocks and watermarks. `ChartForgeX.Stories` adds scene playback, terminal stories, motion and GIF/APNG encoding; both optional packages depend on core and remain independent of each other. `TerminalStory` creates PowerShell, Bash, command prompt, Python, C#, or custom console presentations from structured commands and output, with independent palettes and macOS, Windows Terminal, minimal, or chrome-free window styles. `VisualStory` generalizes that model to source, terminal, text, image, SVG, and result panels, with declared outcomes that must remain visible in the completed scene. The core consumes resolved artifacts and renderer-neutral syntax spans; it never executes showcased code or depends on a language parser. PNG, print, and reduced-motion output keep the exact completed state. Optional browser behavior lives in adapter packages, so generated reports can stay static while dashboard hosts can opt into tooltips, selection, zoom, pan, synchronized charts, and export controls.

## Install

```powershell
dotnet add package ChartForgeX
```

For canvas and factual-block examples, also install `ChartForgeX.Visuals`. For stories, terminal playback and GIF/APNG output, install `ChartForgeX.Stories`.

ChartForgeX targets `net472`, `netstandard2.0`, `net8.0`, and `net10.0`. The core package has no runtime package dependencies.

| Package | Purpose |
| --- | --- |
| `ChartForgeX` | Charts, genuine diagrams, prepared scenes, semantic artifacts and still-image codecs. |
| `ChartForgeX.Visuals` | Static canvas, image composition, metric tiles, tables, lists and ordered watermark decoration. |
| `ChartForgeX.Stories` | Visual and terminal stories, transcripts, motion, animated topology and GIF/APNG output. |
| `ChartForgeX.Interactivity` | Host-neutral interaction contracts. |
| `ChartForgeX.Interactivity.Html` | Self-contained chart and topology interaction adapter, including interactive topology pages, the stencil browser, and the graph explorer with SVG, Canvas, WebGL, hierarchy navigation, compact large-scene documents, and runtime patches. |

## Visual Tour

Charts and diagrams share the same theme, typography and chart area. These 400 × 280 previews are generated from the current examples. Open an example to switch between light and dark, compare SVG with native PNG, or copy its C# source.

<table>
  <tr>
    <td width="50%">
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-bar-wide-light.html"><img src="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-bar-wide-light.thumbnail.png" alt="Completed orders by region" width="100%" /></a>
      <br />
      <strong>Completed orders by region</strong><br />Bar
      <br />
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-bar-wide-light.html">Light HTML</a> / <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-bar-wide-dark.html">Dark HTML</a> /
      <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-bar-wide-light.svg">SVG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-bar-wide-light.png">PNG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-bar-wide-light.csharp.txt">C#</a>
    </td>
    <td width="50%">
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-line-wide-light.html"><img src="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-line-wide-light.thumbnail.png" alt="Weekly request volume" width="100%" /></a>
      <br />
      <strong>Weekly request volume</strong><br />Line
      <br />
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-line-wide-light.html">Light HTML</a> / <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-line-wide-dark.html">Dark HTML</a> /
      <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-line-wide-light.svg">SVG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-line-wide-light.png">PNG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-line-wide-light.csharp.txt">C#</a>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-donut-wide-light.html"><img src="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-donut-wide-light.thumbnail.png" alt="Services contributing to revenue" width="100%" /></a>
      <br />
      <strong>Services contributing to revenue</strong><br />Donut
      <br />
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-donut-wide-light.html">Light HTML</a> / <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-donut-wide-dark.html">Dark HTML</a> /
      <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-donut-wide-light.svg">SVG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-donut-wide-light.png">PNG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-donut-wide-light.csharp.txt">C#</a>
    </td>
    <td width="50%">
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-heatmap-wide-light.html"><img src="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-heatmap-wide-light.thumbnail.png" alt="Coverage across service areas" width="100%" /></a>
      <br />
      <strong>Coverage across service areas</strong><br />Heatmap
      <br />
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-heatmap-wide-light.html">Light HTML</a> / <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ffamily-heatmap-wide-dark.html">Dark HTML</a> /
      <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-heatmap-wide-light.svg">SVG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-heatmap-wide-light.png">PNG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/family-heatmap-wide-light.csharp.txt">C#</a>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ftopology-expanded-light.html"><img src="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/topology-expanded-light.thumbnail.png" alt="Service request topology" width="100%" /></a>
      <br />
      <strong>Service request topology</strong><br />Topology
      <br />
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ftopology-expanded-light.html">Light HTML</a> / <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Ftopology-expanded-dark.html">Dark HTML</a> /
      <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/topology-expanded-light.svg">SVG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/topology-expanded-light.png">PNG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/topology-expanded-light.csharp.txt">C#</a>
    </td>
    <td width="50%">
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Fsequence-expanded-light.html"><img src="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/sequence-expanded-light.thumbnail.png" alt="Request processing sequence" width="100%" /></a>
      <br />
      <strong>Request processing sequence</strong><br />Sequence
      <br />
      <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Fsequence-expanded-light.html">Light HTML</a> / <a href="https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Fsequence-expanded-dark.html">Dark HTML</a> /
      <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/sequence-expanded-light.svg">SVG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/sequence-expanded-light.png">PNG</a> / <a href="https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/sequence-expanded-light.csharp.txt">C#</a>
    </td>
  </tr>
</table>

Explore all chart families in the [gallery](https://preview.evotec.xyz/?url=https%3A%2F%2Fgithub.com%2FEvotecIT%2FChartForgeX%2Fblob%2Fmain%2FWebsite%2Fstatic%2Fexamples%2Fgenerated%2Fcatalog.html). Each example includes light and dark themes, SVG and PNG previews, and C# source.

## Quick Start

```csharp
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var chart = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML, and PNG chart rendering")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(1180, 640)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    .AddSmoothArea("Passed", ChartPoints.FromValues(820, 940, 980, 1040, 1120, 1180, 1230))
    .AddSmoothLine("Warnings", ChartPoints.FromValues(120, 138, 132, 110, 98, 86, 72), ChartColor.FromRgb(251, 191, 36));

chart.SaveSvg("domain-security.svg");
chart.SavePng("domain-security.png");
chart.SaveHtml("domain-security.html");
```

`WithXLabels` places its labels at x = 1 through N, and `ChartPoints.FromValues` numbers its points the same way, so the first label sits under the first point.

## Typed Data and Scales

Hosts keep their own records and map them directly into native charts. `ChartDataset<T>` provides immutable filter, sort, projection, grouping, summary, and numeric-bin operations without introducing a second chart-definition API.

```csharp
using ChartForgeX.Core;
using ChartForgeX.Data;

var samples = ChartDataset<Sample>.From(new[] {
    new Sample(1, 32),
    new Sample(2, 48),
    new Sample(3, 61)
});

var chart = Chart.Create()
    .WithTitle("CPU")
    .WithYAxis("Percent")
    .ConfigureYAxis(axis => axis.WithBounds(0, 100))
    .AddLine("CPU", samples, sample => sample.Index, sample => sample.Value);

chart.SavePng("cpu.png");
chart.SaveSvg("cpu.svg");

record Sample(double Index, double Value);
```

`ChartAxis` centralizes bounds, ticks, labels, and `Linear`, `Logarithmic`, `SymmetricLogarithmic`, or `Time` scaling. Direct helpers such as `ChartPoints.FromValues(...)` remain available for small inline datasets.

## Output API

| Need | Use |
| --- | --- |
| SVG markup | `chart.ToSvg()` or `chart.SaveSvg("chart.svg")` |
| Static HTML | `chart.ToHtmlFragment()`, `chart.ToHtmlPage()`, or `chart.SaveHtml("chart.html")` |
| Source-to-result story | `story.ToSvg()`, `story.ToPng()`, `story.ToGif()`, `story.ToApng()`, or `story.ToTranscript()` |
| Interactive topology HTML | `topology.ToInteractiveHtmlFragment()`, `topology.ToInteractiveHtmlPage()`, or `topology.SaveInteractiveHtml("topology.html")` from `ChartForgeX.Interactivity.Html` |
| Interactive relationship graph | `graph.ToGraphExplorerHtmlFragment()`, `graph.ToGraphExplorerHtmlPage()`, premium system/light/dark themes, accessible keyboard navigation, SVG/Canvas/WebGL backends, worker physics, direct hierarchy navigation, static stage images, and `GraphScenePatch` |
| PNG bytes/file | `chart.ToPng()` or `chart.SavePng("chart.png")` |
| Still raster file output | `chart.Save("chart.jpg", rasterOptions)` or `chart.SaveRasterImage("chart.tiff")`; add Stories for `chart.ToRgbaImage().ToGif()` |
| Reusable image composition | Add Visuals for `ImageComposition.TryFromBytes(bytes, out var composition)`, `composition.StrokeRectangle(...)`, `composition.DrawCallout(...)` and `composition.Write(stream, RasterImageFormat.Png)`; add Stories for `composition.ToImage().ToGif()` |
| RGBA frame animation | Add Stories for `RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif, options)` or `.WriteTo(stream, frames, RasterAnimationFormat.Apng, options)` with individual frame durations and finite or infinite playback |
| Extension-inferred file output | `chart.Save("chart.svg")`, `chart.Save("chart.html")`, `chart.Save("chart.png")`, `chart.Save("chart.jpg")`, `chart.Save("chart.tiff")` |

`RasterImageOptions` controls JPEG quality, PNG compression level, and the flattening background for opaque formats.

## Links

- Repository: https://github.com/EvotecIT/ChartForgeX
- Project page and documentation: https://evotec.xyz/projects/chartforgex/
- Curated demos: https://evotec.xyz/demos/chartforgex/
- Complete gallery: https://evotec.xyz/demos/chartforgex/gallery/
- Issues: https://github.com/EvotecIT/ChartForgeX/issues
