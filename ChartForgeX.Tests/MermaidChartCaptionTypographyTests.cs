using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidChartCaptionTypographyTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("sankey", 320, false)]
    [InlineData("sankey", 900, false)]
    [InlineData("sankey", 320, true)]
    [InlineData("sankey", 900, true)]
    [InlineData("treemap", 320, false)]
    [InlineData("treemap", 900, false)]
    [InlineData("treemap", 320, true)]
    [InlineData("treemap", 900, true)]
    public Task MaximumSourceFontKeepsCaptionsInTheirNativeLayout(string family, int width, bool graphite) =>
        VerifyCaptions(family, width, 460, graphite, false);

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("sankey", false)]
    [InlineData("sankey", true)]
    [InlineData("treemap", false)]
    [InlineData("treemap", true)]
    public Task TallNarrowMarksFitOversizedCaptionsInBothDimensions(string family, bool graphite) =>
        VerifyCaptions(family, 320, 1200, graphite, true);

    private static async Task VerifyCaptions(string family, int width, int height, bool graphite, bool flat) {
        var body = family == "sankey" ? "sankey-beta\nSource,Target,10\n" : flat ? "treemap-beta\n\"Documents\": 5\n\"Media\": 5\n"
            : "treemap-beta\n\"Storage\"\n  \"Documents\": 5\n  \"Media\": 5\n";
        var rendered = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\n" + body);
        Assert.False(rendered.HasErrors);
        var chart = Assert.IsType<Chart>(rendered.Artifact!.Model);
        chart.WithSize(width, height); chart.Options.Theme.UseGraphiteLayout = graphite;
        var request = VisualExportRequest.ForChart(chart);
        var prepared = chart.Prepare(request.Context);
        var labels = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        var expected = family == "sankey" ? new[] { "Source", "Target" } : flat ? new[] { "Documents", "Media" } : new[] { "Storage", "Documents", "Media" };
        foreach (var label in expected) Assert.Contains(labels, text => text.Replace("\n", "").Contains(label, StringComparison.Ordinal));
        string WithoutSpaces(string value) => new(value.Where(character => !char.IsWhiteSpace(character)).ToArray());
        foreach (var heading in new[] { request.Context.Frame.Title, request.Context.Frame.Subtitle })
            if (!string.IsNullOrEmpty(heading)) Assert.Contains(WithoutSpaces(heading), WithoutSpaces(string.Concat(labels)));
        Assert.True(prepared.ToPng().Length > 64);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", width, height);
        Assert.True(await session.Page.EvaluateAsync<bool>("labels => labels.every(label => [...document.querySelectorAll('svg text')].some(text => {const b=text.getBoundingClientRect();return text.textContent.includes(label) && b.width>0 && b.height>0 && b.left>=-.01 && b.right<=innerWidth+.01 && b.top>=-.01 && b.bottom<=innerHeight+.01;}))", expected));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); var name = family + "-font-ceiling-" + width + "-" + graphite + "-" + height;
            File.WriteAllBytes(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
        }
    }

    [Theory]
    [InlineData("calc(1em +\n 2px)", false)]
    [InlineData("calc(1em +\n 2px)", true)]
    [InlineData("clamp(1rem,\r\n 2rem, 3rem)", false)]
    [InlineData("clamp(1rem,\r\n 2rem, 3rem)", true)]
    public void MultilineCssSizingFunctionsAreRetainedWithoutPreventingRendering(string value, bool yaml) {
        var quoted = JsonSerializer.Serialize(value);
        var configuration = yaml ? "---\nconfig:\n  themeVariables:\n    fontSize: " + quoted + "\n---\n"
            : "%%{init: {\"themeVariables\":{\"fontSize\":" + quoted + "}}}%%\n";
        var rendered = MermaidRenderer.Render(configuration + "flowchart LR\nA --> B");
        Assert.False(rendered.HasErrors); Assert.NotNull(rendered.Artifact);
        Assert.Null(rendered.Document!.Configuration.FontSize);
        Assert.Equal(value, Assert.Single(rendered.Document.Configuration.Settings, setting => setting.Path == "themeVariables.fontSize").Value);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }
}
