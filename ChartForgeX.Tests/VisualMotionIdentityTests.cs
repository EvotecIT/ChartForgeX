using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Motion;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualMotionIdentityTests {
    [Fact]
    public void CompletedTerminalArtifactsKeepAccessibleReferencesIsolatedBetweenExports() {
        var terminal = TerminalStory.Create().WithTitle("Completed terminal").WithWidth(480)
            .WithPngOutputScale(1).WithFinalPrompt(false).Command("status", .1).Output("Ready");
        var artifact = terminal.ToVisualArtifact();
        Assert.Same(terminal, artifact.Model);
        var source = Assert.IsAssignableFrom<IStaticVisualSource>(artifact.RenderSource);
        var first = XDocument.Parse(source.RenderSvg("first"));
        var second = XDocument.Parse(source.RenderSvg("second"));
        Assert.NotEmpty(Ids(first));
        AssertReferencesResolve(first);
        AssertReferencesResolve(second);
        Assert.Empty(Ids(first).Intersect(Ids(second), StringComparer.Ordinal));
        Assert.Contains("Ready", first.Root!.Value, StringComparison.Ordinal);
        Assert.Equal(RasterImageDecoder.Decode(terminal.ToPng()).Pixels, source.RenderRgba().Pixels);
    }

    [Fact]
    public void GridMotionKeepsAccessibleReferencesWithinEachExport() {
        var grid = VisualGrid.Create().WithTitle("Capacity").WithSubtitle("Completed snapshot")
            .Add("metric", MetricCard.Create().WithMetric("Ready", 42));
        var motion = VisualMotionPresentation.Create(grid, VisualMotionTimeline.Create().Fade("metric"));
        var first = XDocument.Parse(motion.ToSvg("first"));
        var second = XDocument.Parse(motion.ToSvg("second"));
        AssertReferencesResolve(first);
        AssertReferencesResolve(second);
        AssertReferencesResolve(XDocument.Parse(((IStaticVisualSource)motion).RenderSvg("still")));
        Assert.Empty(Ids(first).Intersect(Ids(second), StringComparer.Ordinal));
        Assert.Contains("Capacity", first.Root!.Value, StringComparison.Ordinal);
        Assert.Equal(grid.ToPng(), motion.ToPng());
    }

    [Fact]
    public void NestedProducerLabelsPaintAndHrefRemainScopedWithoutChangingLabelText() {
        var motion = VisualMotionPresentation.Create(new NestedSource(), VisualMotionTimeline.Create().Fade("panel"));
        var document = XDocument.Parse(motion.ToSvg("nested"));
        AssertReferencesResolve(document);
        Assert.Equal("root", (string?)document.Root!.Attribute("aria-label"));
        Assert.Equal("root-title", (string?)document.Descendants().Single(e => e.Name.LocalName == "g").Attribute("aria-label"));
        AssertReferencesResolve(XDocument.Parse(((IStaticVisualSource)motion).RenderSvg("nested-still")));
    }

    private static HashSet<string> Ids(XDocument document) => new(document.Descendants()
        .Attributes("id").Select(a => a.Value), StringComparer.Ordinal);

    private static void AssertReferencesResolve(XDocument document) {
        var ids = Ids(document);
        Assert.Equal(ids.Count, document.Descendants().Attributes("id").Count());
        foreach (var attribute in document.Descendants().Attributes()) {
            if (attribute.Name.LocalName is "aria-labelledby" or "aria-describedby")
                foreach (var reference in attribute.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                    Assert.Contains(reference, ids);
            if (attribute.Name.LocalName == "href" && attribute.Value.StartsWith("#", StringComparison.Ordinal))
                Assert.Contains(attribute.Value.Substring(1), ids);
            foreach (Match match in Regex.Matches(attribute.Value, "url\\(#([^)]+)\\)"))
                Assert.Contains(match.Groups[1].Value, ids);
        }
    }

    private sealed class NestedSource : IStaticVisualSource {
        public string RenderSvg(string idScope) => """
            <svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'
                 id='root' aria-labelledby='root-title' aria-describedby='root-desc' aria-label='root'>
              <title id='root-title'>Outer</title><desc id='root-desc'>Completed source</desc>
              <defs><clipPath id='root-clip'><rect width='64' height='64'/></clipPath></defs>
              <g id='root-panel' data-cfx-target='panel' aria-labelledby='root-panel-title' aria-label='root-title' clip-path='url(#root-clip)'>
                <title id='root-panel-title'>Inner</title>
                <svg id='root-nested' aria-labelledby='root-nested-title'><title id='root-nested-title'>Nested</title>
                  <rect id='root-mark' width='64' height='64' fill='#ff0000'/><use href='#root-mark'/>
                </svg>
              </g>
            </svg>
            """;
        public RgbaImage RenderRgba() => new(64, 64, Enumerable.Range(0, 64 * 64)
            .SelectMany(_ => new byte[] { 255, 0, 0, 255 }).ToArray());
    }
}
