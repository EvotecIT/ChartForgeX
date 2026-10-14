using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Series fill availability follows the native surface at an authored observation.</summary>
public sealed partial class CartesianFillObservationBrowserTests {
    [BrowserTheory]
    [InlineData(ChartStepPosition.Start, false, false)]
    [InlineData(ChartStepPosition.End, false, false)]
    [InlineData(ChartStepPosition.Start, true, true)]
    [InlineData(ChartStepPosition.End, true, true)]
    public async Task DifferenceTerminalCannotBorrowRemoteSameDirectionFill(ChartStepPosition position, bool negative, bool reversedSecondary) {
        if (!Enabled) return;
        var chart = Frame();
        chart.AddSeries(ChartSeries.CreateDifferenceArea(new ChartSeriesId("observed"), "Observed", new[] {
            new ChartDifferencePoint(0, negative ? 2 : 8, negative ? 8 : 2),
            new ChartDifferencePoint(1, negative ? 8 : 2, negative ? 2 : 8),
            new ChartDifferencePoint(2, negative ? 2 : 8, negative ? 8 : 2)
        }));
        chart.Series[0].WithColor(ChartColor.Transparent).ConfigureDifferenceArea(options => {
            options.PositiveFill = ChartColor.FromHex("#DF3040"); options.NegativeFill = ChartColor.FromHex("#305CDF");
            options.PositiveFillOpacity = negative ? 0 : .8; options.NegativeFillOpacity = negative ? .8 : 0;
        });
        Configure(chart, position, reversedSecondary);
        await AssertAvailabilityAsync(chart, position == ChartStepPosition.Start ? 0 : 2, false,
            $"difference-{position}-{negative}-{reversedSecondary}");
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.RangeArea, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.RangeArea, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.RangeBand, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.RangeBand, ChartStepPosition.End)]
    public async Task RangeTerminalNeedsFillInsideItsOwnExtent(ChartSeriesKind kind, ChartStepPosition position) {
        if (!Enabled) return;
        var ranges = position == ChartStepPosition.Start
            ? new[] { new ChartRangeBand(0, 1, 2), new ChartRangeBand(1, 6, 8), new ChartRangeBand(2, 6, 8) }
            : new[] { new ChartRangeBand(0, 6, 8), new ChartRangeBand(1, 6, 8), new ChartRangeBand(2, 1, 2) };
        var chart = Range(kind, ranges); Configure(chart, position);
        await AssertAvailabilityAsync(chart, position == ChartStepPosition.Start ? 0 : 2, false, $"{kind}-{position}-disjoint");
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Area, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.Area, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.StepArea, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.StepArea, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.StackedArea, ChartStepPosition.Start)]
    public async Task ScalarAreaTerminalStemIsNotAPaintedSurface(ChartSeriesKind kind, ChartStepPosition position) {
        if (!Enabled) return;
        var points = position == ChartStepPosition.Start
            ? new[] { new ChartPoint(0, 8), new ChartPoint(1, 2), new ChartPoint(2, 2) }
            : new[] { new ChartPoint(0, 2), new ChartPoint(1, 2), new ChartPoint(2, 8) };
        var chart = Frame();
        if (kind == ChartSeriesKind.StepArea) chart.AddSeries(new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.StepArea, points));
        else if (kind == ChartSeriesKind.StackedArea) chart.AddSeries(new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.StackedArea, points));
        else chart.AddSeries(ChartSeries.CreateArea(new ChartSeriesId("observed"), "Observed", points));
        Configure(chart, position);
        await AssertAvailabilityAsync(chart, position == ChartStepPosition.Start ? 0 : 2, false, $"{kind}-{position}-stem");
    }

    [BrowserFact]
    public async Task ScalarLinearPeakKeepsItsPaintedObservation() {
        if (!Enabled) return;
        var chart = Frame().AddSeries(ChartSeries.CreateArea(new ChartSeriesId("observed"), "Observed", new[] {
            new ChartPoint(0, 2), new ChartPoint(.025, 8), new ChartPoint(.05, 2), new ChartPoint(2, 2)
        }));
        Configure(chart, ChartStepPosition.End);
        chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        await AssertAvailabilityAsync(chart, 1, true, "scalar-linear-peak", expectedReadout: "8");
    }

    [BrowserTheory]
    [InlineData(ChartInterpolation.Linear, true, false)]
    [InlineData(ChartInterpolation.Linear, false, true)]
    [InlineData(ChartInterpolation.Smooth, false, false)]
    public async Task ScalarPeakKeepsItsOwnValueAcrossNativeSurfaces(ChartInterpolation interpolation, bool negative, bool reversedSecondary) {
        if (!Enabled) return;
        var direction = negative ? -1 : 1;
        var chart = Frame().AddSeries(ChartSeries.CreateArea(new ChartSeriesId("observed"), "Observed", new[] {
            new ChartPoint(0, 2 * direction), new ChartPoint(.025, 8 * direction),
            new ChartPoint(.05, 2 * direction), new ChartPoint(2, 2 * direction)
        }));
        Configure(chart, ChartStepPosition.End, reversedSecondary);
        if (negative) chart.Options.YAxis.WithBounds(-10, 0);
        chart.Series[0].WithInterpolation(interpolation);
        await AssertAvailabilityAsync(chart, 1, true, $"scalar-peak-{interpolation}-{negative}-{reversedSecondary}",
            expectedReadout: negative ? "-8" : "8", captureScalarInk: interpolation == ChartInterpolation.Smooth);
    }

    [BrowserFact]
    public async Task StackedPeakKeepsItsRawValueAboveAnotherSeries() {
        if (!Enabled) return;
        var chart = Frame().WithYAxisBounds(0, 12)
            .AddSeries(new ChartSeries(new ChartSeriesId("base"), "Base", ChartSeriesKind.StackedArea, new[] { new ChartPoint(0, 2), new ChartPoint(.025, 2), new ChartPoint(.05, 2), new ChartPoint(2, 2) }))
            .AddSeries(new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.StackedArea, new[] { new ChartPoint(0, 2), new ChartPoint(.025, 8), new ChartPoint(.05, 2), new ChartPoint(2, 2) }));
        chart.Options.ClipMarksToPlot = true;
        chart.Series[0].ConfigureMarkers(markers => markers.Enabled = false);
        chart.Series[1].WithInterpolation(ChartInterpolation.Linear)
            .ConfigureMarkers(markers => markers.Enabled = false);
        await AssertAvailabilityAsync(chart, 1, true, "scalar-stacked-peak", seriesIndex: 1, expectedReadout: "8");
    }

    [BrowserFact]
    public async Task ScalarPeakOnTheNativeClipBoundaryRemainsAvailable() {
        if (!Enabled) return;
        var chart = Frame().WithYAxisBounds(0, 8).AddSeries(ChartSeries.CreateArea(new ChartSeriesId("observed"), "Observed", new[] {
            new ChartPoint(0, 2), new ChartPoint(.025, 8), new ChartPoint(.05, 2), new ChartPoint(2, 2)
        }));
        Configure(chart, ChartStepPosition.End);
        chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        await AssertAvailabilityAsync(chart, 1, true, "scalar-clipped-peak", expectedReadout: "8");
    }

    [BrowserFact]
    public async Task ScalarPeakOutsideTheNativeClipCannotUseItsPartialFootprint() {
        if (!Enabled) return;
        var chart = Frame().WithYAxisBounds(0, 8).AddSeries(ChartSeries.CreateArea(new ChartSeriesId("observed"), "Observed", new[] {
            new ChartPoint(0, 2), new ChartPoint(.025, 8.02), new ChartPoint(.05, 2), new ChartPoint(2, 2)
        }));
        Configure(chart, ChartStepPosition.End);
        chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        await AssertAvailabilityAsync(chart, 1, false, "scalar-outside-clip-peak");
    }

    [BrowserTheory]
    [InlineData(7.95, true)]
    [InlineData(7.8, false)]
    public async Task ScalarStepFillUsesOnlyTheOriginalLocalFootprint(double heldValue, bool available) {
        if (!Enabled) return;
        var chart = Frame().AddSeries(new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.StepArea, new[] {
            new ChartPoint(0, 8), new ChartPoint(1, heldValue), new ChartPoint(2, heldValue)
        }));
        Configure(chart, ChartStepPosition.Start);
        await AssertAvailabilityAsync(chart, 0, available, $"scalar-local-step-{heldValue}", expectedReadout: available ? "8" : null);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.RangeArea, false, false)]
    [InlineData(ChartSeriesKind.RangeBand, false, false)]
    [InlineData(ChartSeriesKind.RangeArea, true, false)]
    [InlineData(ChartSeriesKind.RangeBand, true, true)]
    public async Task PartialPairedFillKeepsTheWholeAuthoredObservation(ChartSeriesKind kind, bool clipped, bool reversedSecondary) {
        if (!Enabled) return;
        var chart = Range(kind, new[] { new ChartRangeBand(0, 0, 10), new ChartRangeBand(1, 7, 8), new ChartRangeBand(2, 7, 8) });
        Configure(chart, ChartStepPosition.Start, reversedSecondary);
        if (clipped) (reversedSecondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).WithBounds(7.2, 7.8);
        await AssertAvailabilityAsync(chart, 0, true, $"{kind}-partial-{clipped}-{reversedSecondary}");
    }

    [BrowserTheory]
    [InlineData(ChartInterpolation.Linear)]
    [InlineData(ChartInterpolation.Step)]
    public async Task NativeThreeDecimalThinRangeRemainsAvailable(ChartInterpolation interpolation) {
        if (!Enabled) return;
        var chart = Range(ChartSeriesKind.RangeArea, new[] {
            new ChartRangeBand(0, 7.001, 7.0011), new ChartRangeBand(1, 7.001, 7.0011), new ChartRangeBand(2, 7.001, 7.0011)
        });
        Configure(chart, ChartStepPosition.Middle);
        if (interpolation == ChartInterpolation.Linear) chart.Series[0].WithInterpolation(interpolation);
        await AssertAvailabilityAsync(chart, 0, true, $"thin-{interpolation}");
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.RangeArea, false)]
    [InlineData(ChartSeriesKind.RangeArea, true)]
    [InlineData(ChartSeriesKind.StepArea, false)]
    [InlineData(ChartSeriesKind.StepArea, true)]
    public async Task HatchSurfaceUsesTheSameLocalEnvelope(ChartSeriesKind kind, bool overlap) {
        if (!Enabled) return;
        Chart chart;
        if (kind == ChartSeriesKind.RangeArea) chart = Range(kind, new[] {
            new ChartRangeBand(0, overlap ? 0 : 1, overlap ? 10 : 2), new ChartRangeBand(1, 7, 8), new ChartRangeBand(2, 7, 8)
        });
        else chart = Frame().AddSeries(new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.StepArea, new[] { new ChartPoint(0, overlap ? 2 : 8), new ChartPoint(1, 2), new ChartPoint(2, 2) }));
        Configure(chart, ChartStepPosition.Start);
        chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        await AssertAvailabilityAsync(chart, 0, overlap, $"{kind}-hatch-{overlap}", patternOnly: true);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TiedRangeHoldCannotPaintThroughItsHorizontalRetrace(bool patternOnly) {
        if (!Enabled) return;
        var chart = Range(ChartSeriesKind.RangeArea, new[] {
            new ChartRangeBand(0, 1, 3), new ChartRangeBand(1, 2, 2), new ChartRangeBand(2, 6, 8)
        });
        Configure(chart, ChartStepPosition.Start);
        if (patternOnly) chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        await AssertAvailabilityAsync(chart, 0, false, $"range-tied-hold-{patternOnly}", patternOnly);
    }

    [BrowserFact]
    public async Task VisibleHatchKeepsItsExtentWhenTheBaseFillIsNotDisplayed() {
        if (!Enabled) return;
        var chart = Range(ChartSeriesKind.RangeArea, new[] {
            new ChartRangeBand(0, 0, 10), new ChartRangeBand(1, 7, 8), new ChartRangeBand(2, 7, 8)
        });
        Configure(chart, ChartStepPosition.Start);
        chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        await AssertAvailabilityAsync(chart, 0, true, "hatch-hidden-base", patternOnly: true, hideBase: true);
    }

    [BrowserFact]
    public async Task ExplicitEnvelopeStrokeRetainsTheScalarEndpointStem() {
        if (!Enabled) return;
        var chart = Frame().AddSeries(new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.StepArea, new[] { new ChartPoint(0, 8), new ChartPoint(1, 2), new ChartPoint(2, 2) }));
        Configure(chart, ChartStepPosition.Start);
        await AssertAvailabilityAsync(chart, 0, true, "scalar-envelope-stroke", envelopeStroke: true);
    }

    private static Chart Frame() => Chart.Create().WithSize(600, 380).WithTitle("Native fill availability")
        .WithTheme(VisualTheme.Graphite()).WithLegend(false).WithDataLabels(false)
        .WithXAxisBounds(-.2, 2.2).WithYAxisBounds(0, 10).WithLineVisualStyle(ChartLineVisualStyle.Plain());

    private static Chart Range(ChartSeriesKind kind, ChartRangeBand[] ranges) {
        var chart = Frame();
        return kind == ChartSeriesKind.RangeArea ? chart.AddSeries(ChartSeries.CreateRangeArea(new ChartSeriesId("observed"), "Observed", ranges)) : chart.AddSeries(ChartSeries.CreateRangeBand(new ChartSeriesId("observed"), "Observed", ranges));
    }

    private static void Configure(Chart chart, ChartStepPosition position, bool reversedSecondary = false) {
        chart.Options.ClipMarksToPlot = true;
        chart.Series[0].WithInterpolation(ChartInterpolation.Step, position)
            .ConfigureMarkers(markers => markers.Enabled = false);
        if (reversedSecondary) {
            chart.Series[0].UseSecondaryYAxis();
            chart.Options.XAxis.WithReversal(); chart.Options.SecondaryYAxis.WithBounds(0, 10).WithReversal();
            chart.Options.YAxis.WithBounds(0, 100);
        }
    }

    private static async Task AssertAvailabilityAsync(Chart chart, int index, bool available, string name, bool patternOnly = false, bool hideBase = false, bool envelopeStroke = false,
        int seriesIndex = 0, string? expectedReadout = null, bool captureScalarInk = false) {
        // Hide only native boundaries. Transparent browser targets must not establish mark ink.
        var css = "[data-cfx-role='line'],[data-cfx-role^='range-upper'],[data-cfx-role^='range-lower'],[data-cfx-role='range-midline']{stroke:none!important}";
        if (patternOnly) css += "[data-cfx-role='area'],[data-cfx-role='range-area'],[data-cfx-role='range-band'],[data-cfx-role='difference-positive'],[data-cfx-role='difference-negative']{fill:none!important}";
        if (hideBase) css += "[data-cfx-role='range-area']{display:none!important}";
        if (envelopeStroke) css += "[data-cfx-role='area']{fill:none!important;stroke:#DF3040!important;stroke-width:2!important}";
        var html = chart.ToInteractiveHtmlPage().Replace("</head>", "<style>" + css + "</style></head>", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, 800, 560);
        var page = session.Page; var point = page.Locator(Point(seriesIndex, index));
        if (hideBase) {
            Assert.Equal("none", await page.Locator("[data-cfx-role='range-area']").EvaluateAsync<string>("node=>getComputedStyle(node).display"));
            Assert.True(await page.Locator("[data-cfx-role='range-area-pattern']").First.EvaluateAsync<bool>(
                "node=>getComputedStyle(node).display!=='none'&&getComputedStyle(node).stroke!=='none'"));
        }
        var position = await point.EvaluateAsync<double[]>("""
            node=>{const b=node.getBoundingClientRect();let top=b.top,bottom=b.bottom;
              const fill=node.closest('[data-cfx-role="series"]').querySelector('[data-cfx-role="range-area"],[data-cfx-role="range-band"]');
              for(let parent=fill;parent&&parent.ownerSVGElement;parent=parent.parentElement){
                const ref=/^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path')||'');if(!ref)continue;
                const clip=parent.ownerSVGElement.getElementById(ref[1]).firstElementChild;if(!clip.matches('rect'))continue;
                const r=clip.getBBox(),m=parent.getScreenCTM(),a=new DOMPoint(r.x,r.y).matrixTransform(m),z=new DOMPoint(r.x+r.width,r.y+r.height).matrixTransform(m);
                top=Math.max(top,Math.min(a.y,z.y));bottom=Math.min(bottom,Math.max(a.y,z.y));
              }return [b.x+b.width/2,top/2+bottom/2]}
            """);
        Assert.Equal(index.ToString(), await page.EvaluateAsync<string>("p=>document.elementFromPoint(p[0],p[1])?.closest('[data-cfx-point]')?.dataset.cfxPoint||''", position));
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        var hover = await TooltipTextAsync(page);
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        var pointerSelected = await point.GetAttributeAsync("aria-selected");
        var reset = page.Locator("[data-cfx-reset]"); if (await reset.IsVisibleAsync()) await reset.ClickAsync();
        await MoveAwayAsync(page); await point.FocusAsync();
        var active = await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId||''");
        var keyboard = await TooltipTextAsync(page);
        await page.Keyboard.PressAsync("Space");
        var keyboardSelected = await point.GetAttributeAsync("aria-selected");
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            JsonElement? nativeFill = null;
            if (captureScalarInk) nativeFill = await point.EvaluateAsync<JsonElement>("""
                node=>{const region=node._cfxNativeRegion,path=node.closest('[data-cfx-role="series"]').querySelector('[data-cfx-role="area"]');
                  const sourceMatrix=node.getScreenCTM(),inverse=path.getScreenCTM().inverse(),x=region.x+region.width/2,y=region.y+region.height/2;
                  const witnesses=(node._cfxFillInteriors||[]).map(([cx,cy,radius])=>({x:cx,y:cy,radius,
                    corners:[-1,1].flatMap(dx=>[-1,1].map(dy=>{
                      const point=new DOMPoint(cx+dx*radius,cy+dy*radius).matrixTransform(sourceMatrix).matrixTransform(inverse);
                      return {x:point.x,y:point.y,inside:path.isPointInFill(point)};
                    }))}));
                  return {region,x,y,path:path.getAttribute('d'),witnesses};}
                """);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "fill-" + name + ".png") });
            await File.WriteAllTextAsync(Path.Combine(directory, "fill-" + name + ".json"), JsonSerializer.Serialize(new {
                available, position, hover, pointerSelected, active, keyboard, keyboardSelected,
                tabindex = await point.GetAttributeAsync("tabindex"), nativeFill, console = session.ConsoleLog
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        if (available) {
            Assert.Contains("Observed", hover); Assert.Equal("true", pointerSelected);
            Assert.Equal("observed:" + index, active); Assert.Contains("Observed", keyboard); Assert.Equal("true", keyboardSelected);
            if (expectedReadout is not null) { Assert.Contains(expectedReadout, hover); Assert.Contains(expectedReadout, keyboard); }
        } else {
            Assert.Empty(hover); Assert.NotEqual("true", pointerSelected);
            Assert.NotEqual("observed:" + index, active); Assert.NotEqual("true", keyboardSelected);
            Assert.Equal("-1", await point.GetAttributeAsync("tabindex"));
        }
        AssertNoConsoleErrors(session);
    }
}
