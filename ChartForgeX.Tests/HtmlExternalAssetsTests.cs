using System.Text.RegularExpressions;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlExternalAssetsTests {
    private static readonly HtmlAssetReferences Assets = new("assets/");

    [Fact]
    public void SplitMovesInlineScriptsAndStylesIntoFilesInPlace() {
        var page = HtmlExternalAssets.Split("<!doctype html><html><head><style>.a{color:red}</style></head><body>"
            + "<div style=\"width:4px\">x</div><script>var tag = '<script>' + '</div>';</script>"
            + "<script type=\"application/json\" id=\"d\">{\"a\":\"<b style=1>\"}</script><script src=\"host.js\"></script>"
            + "<script data-k=\"1\">var tag = '<script>' + '</div>';</script><!-- <script>x</script> --></body></html>", Assets);
        var css = page.Files[0];
        var script = page.Files[1];
        Assert.Equal(2, page.Files.Count);
        Assert.Matches("^cfx-page\\.[0-9a-f]{12}\\.css$", css.FileName);
        Assert.Matches("^cfx-script\\.[0-9a-f]{12}\\.js$", script.FileName);
        Assert.Equal("var tag = '<script>' + '</div>';", script.Content);
        var name = Regex.Match(page.Html, "cfx-s-[0-9a-f]{12}").Value;
        Assert.Equal("<!doctype html><html><head><link rel=\"stylesheet\" href=\"assets/" + css.FileName + "\" data-cfx-external-styles=\"true\"></head><body>"
            + "<div class=\"" + name + "\">x</div><script src=\"assets/" + script.FileName + "\"></script>"
            + "<script type=\"application/json\" id=\"d\">{\"a\":\"<b style=1>\"}</script><script src=\"host.js\"></script>"
            + "<script data-k=\"1\" src=\"assets/" + script.FileName + "\"></script><!-- <script>x</script> --></body></html>", page.Html);
        Assert.Equal(".a{color:red}\n." + name + "." + name + "." + name + "{width:4px}", css.Content);
    }

    [Fact]
    public void SplitUsesTheSharedRuntimeFilesAndTheGraphPhysicsWorker() {
        var scene = GraphScene.Create("map", "Map").AddNode("a", "A").AddNode("b", "B").AddEdge("ab", "a", "b");
        var page = HtmlExternalAssets.Split(scene.ToGraphExplorerHtmlPage(), new HtmlAssetReferences("assets/") { IncludeIntegrity = true });
        var runtime = HtmlInteractiveAssetFiles.GraphExplorerScript;
        var worker = HtmlInteractiveAssetFiles.GraphExplorerPhysicsWorker;
        Assert.Contains(runtime, page.Files);
        Assert.Contains(worker, page.Files);
        Assert.Contains("src=\"assets/" + runtime.FileName + "\" integrity=\"" + runtime.Integrity + "\" crossorigin=\"anonymous\" data-cfx-graph-physics-worker-src=\"assets/" + worker.FileName + "\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("integrity=\"" + page.Files[0].Integrity + "\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("installGraphPhysicsWorker(self);", worker.Content, StringComparison.Ordinal);    }

    [Fact]
    public void GraphRuntimeReferencesNameThePhysicsWorker() {
        var scene = GraphScene.Create("map", "Map").AddNode("a", "A");
        var html = scene.ToGraphExplorerHtmlPage(options => options.ExternalAssets = Assets);
        Assert.Contains("data-cfx-graph-physics-worker-src=\"assets/" + HtmlInteractiveAssetFiles.GraphExplorerPhysicsWorker.FileName + "\"", html, StringComparison.Ordinal);
        Assert.Contains(HtmlInteractiveAssetFiles.GraphExplorerPhysicsWorker, HtmlInteractiveAssetFiles.GraphExplorer());
    }
}
