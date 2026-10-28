using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing-API byte controls can run before and after the measured-layout source batch.</summary>
public sealed class FishboneDefaultOutputControls {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryDirectAndSourceDefaultScenesRemainDeterministic(bool fromSource) {
        FishboneDiagramBlock block;
        if (fromSource) block = new MermaidParser().ParseIshikawa("ishikawa-beta\nAPI\n  People\n    Data\n  Process\n    Review").Document!
            .ToFishboneDiagramBlock(new MermaidIshikawaRenderOptions { Title = "", Width = 900, Height = 520 });
        else {
            block = FishboneDiagramBlock.Create("API").WithSize(900, 520).WithPadding(30);
            block.AddCause("People").AddChild("Data"); block.AddCause("Process").AddChild("Review");
        }
        var svg = block.RenderSvg("fishbone-default-control"); var png = block.ToPng(); var html = block.ToHtmlPage();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var name = "fishbone-default-" + (fromSource ? "source" : "native");
            File.WriteAllText(Path.Combine(output, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllText(Path.Combine(output, name + ".html"), html);
        }
        Assert.Equal(svg, block.RenderSvg("fishbone-default-control")); Assert.Equal(png, block.ToPng()); Assert.Equal(html, block.ToHtmlPage());
        Assert.Equal(2, block.Causes.Count); foreach (var caption in new[] { "API", "People", "Data", "Process", "Review" }) Assert.Contains(caption, svg, StringComparison.Ordinal);
    }
}
