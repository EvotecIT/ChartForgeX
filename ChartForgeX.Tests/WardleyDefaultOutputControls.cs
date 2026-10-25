using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing public API controls captured before the Wardley overlay batch.</summary>
public sealed class WardleyDefaultOutputControls {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryDirectAndSourceDefaultScenesRemainDeterministic(bool fromSource) {
        WardleyMapBlock block;
        if (fromSource) block = new MermaidParser().ParseWardley("wardley-beta\nanchor User [0.9,0.3]\ncomponent API [0.6,0.4]\ncomponent Compute [0.3,0.7]\nUser -> API\nAPI -> Compute").Document!
            .ToWardleyMapBlock(new MermaidWardleyRenderOptions { Title = "", Width = 900, Height = 600 });
        else {
            block = WardleyMapBlock.Create().WithSize(900, 600).WithPadding(30);
            block.AddNode("User", "User", .9, .3, WardleyMapNodeKind.Anchor);
            block.AddNode("API", "API", .6, .4); block.AddNode("Compute", "Compute", .3, .7);
            block.AddLink("User", "API"); block.AddLink("API", "Compute");
        }
        var svg = block.RenderSvg("wardley-default-control"); var png = block.ToPng(); var html = block.ToHtmlPage();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var name = "wardley-default-" + (fromSource ? "source" : "native");
            File.WriteAllText(Path.Combine(output, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllText(Path.Combine(output, name + ".html"), html);
        }
        Assert.Equal(svg, block.RenderSvg("wardley-default-control")); Assert.Equal(png, block.ToPng()); Assert.Equal(html, block.ToHtmlPage());
        Assert.Equal(3, block.Nodes.Count); Assert.Equal(2, block.Links.Count);
        foreach (var text in new[] { "User", "API", "Compute", "Visibility", "Evolution" }) Assert.Contains(text, svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
    }
}
