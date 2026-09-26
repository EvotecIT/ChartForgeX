using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ExternalAssetTests {
    [Fact]
    public void RenderPage_Default_KeepsRuntimeInline() {
        var html = CreateChart().ToInteractiveHtmlPage();
        Assert.Contains(RuntimeSnippet(HtmlInteractiveAssetFiles.ChartScript), html, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<link rel=\"stylesheet\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderPage_ExternalAssets_ReferencesSharedFilesInsteadOfInlining() {
        var assets = new HtmlAssetReferences("assets");
        var page = CreateChart().ToInteractiveHtmlPage(options => { options.ExternalAssets = assets; options.ScriptNonce = "n0nce"; });
        var dashboard = new[] { CreateChart(), CreateChart() }.ToInteractiveHtmlDashboardPage(options => { options.ExternalAssets = assets; options.ScriptNonce = "d4sh"; });
        var script = HtmlInteractiveAssetFiles.ChartScript;
        var style = HtmlInteractiveAssetFiles.ChartStyle;

        Assert.Matches(@"^cfx-interactive\.[0-9a-f]{12}\.js$", script.FileName);
        foreach (var html in new[] { page, dashboard }) {
            Assert.Contains("<link rel=\"stylesheet\" href=\"assets/" + style.FileName + "\">", html, StringComparison.Ordinal);
            Assert.Contains("src=\"assets/" + script.FileName + "\"", html, StringComparison.Ordinal);
            Assert.DoesNotContain(RuntimeSnippet(script), html, StringComparison.Ordinal);
            Assert.DoesNotContain(RuntimeSnippet(style), html, StringComparison.Ordinal);
            Assert.DoesNotContain("integrity=", html, StringComparison.Ordinal);
        }

        Assert.Contains("<script src=\"assets/" + script.FileName + "\" nonce=\"n0nce\"></script>", page, StringComparison.Ordinal);
        Assert.Contains("<script src=\"assets/" + script.FileName + "\" nonce=\"d4sh\"></script>", dashboard, StringComparison.Ordinal);
        Assert.Equal(HtmlInteractiveChartRenderer.BuildInteractionScript(), script.Content);
        Assert.True(page.Length < CreateChart().ToInteractiveHtmlPage().Length - script.Content.Length / 2);
    }

    [Fact]
    public void RenderPage_WithIntegrity_EmitsSha384OfUtf8Content() {
        var assets = new HtmlAssetReferences("https://reports.example.test/cfx/") { IncludeIntegrity = true };
        var html = CreateChart().ToInteractiveHtmlPage(options => options.ExternalAssets = assets);
        var script = HtmlInteractiveAssetFiles.ChartScript;
        using var sha384 = SHA384.Create();
        var expected = "sha384-" + Convert.ToBase64String(sha384.ComputeHash(new UTF8Encoding(false).GetBytes(script.Content)));
        Assert.Equal(expected, script.Integrity);
        Assert.Contains("src=\"https://reports.example.test/cfx/" + script.FileName + "\" integrity=\"" + expected + "\" crossorigin=\"anonymous\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphExplorerPage_ExternalAssets_MatchesInlineRuntime() {
        var scene = GraphScene.Create("g", "Graph").AddNode("a", "A").AddNode("b", "B").AddEdge("ab", "a", "b");
        var inline = new HtmlGraphExplorerRenderer().RenderPage(scene);
        var external = new HtmlGraphExplorerRenderer().RenderPage(scene, options => { options.ExternalAssets = new HtmlAssetReferences("../shared"); options.ScriptNonce = "abc"; });
        var script = HtmlInteractiveAssetFiles.GraphExplorerScript;
        Assert.Contains(script.Content, inline, StringComparison.Ordinal);
        Assert.Contains("<script src=\"../shared/" + script.FileName + "\" nonce=\"abc\"></script>", external, StringComparison.Ordinal);
        Assert.Contains("href=\"../shared/" + HtmlInteractiveAssetFiles.GraphExplorerStyle.FileName + "\"", external, StringComparison.Ordinal);
        Assert.DoesNotContain(RuntimeSnippet(script), external, StringComparison.Ordinal);
    }

    [Fact]
    public void TopologyPage_ExternalAssets_UsesPrefixSpecificRuntimeFile() {
        var chart = new TopologyChart { Id = "sites", Title = "Sites" };
        chart.Nodes.Add(new TopologyNode { Id = "a", Label = "A", X = 80, Y = 80 });
        chart.Nodes.Add(new TopologyNode { Id = "b", Label = "B", X = 320, Y = 80 });
        var renderer = new HtmlInteractiveTopologyRenderer();
        var html = renderer.RenderPage(chart, null, new HtmlAssetReferences("assets/"));
        var file = HtmlInteractiveAssetFiles.TopologyScript();
        Assert.Contains("src=\"assets/" + file.FileName + "\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(RuntimeSnippet(file), html, StringComparison.Ordinal);
        Assert.Contains(file.Content, renderer.RenderPage(chart), StringComparison.Ordinal);
        var custom = new TopologyRenderOptions { CssClassPrefix = "acme-topology" };
        var customFile = HtmlInteractiveAssetFiles.TopologyScript(custom);
        Assert.NotEqual(file.FileName, customFile.FileName);
        Assert.Contains("src=\"assets/" + customFile.FileName + "\"", renderer.RenderPage(chart, custom, new HtmlAssetReferences("assets/")), StringComparison.Ordinal);
        Assert.Same(customFile, HtmlInteractiveAssetFiles.TopologyScript(custom));
    }

    [Fact]
    public void WriteTo_WritesOnceAndKeepsIdenticalFiles() {
        var directory = Path.Combine(Path.GetTempPath(), "cfx-assets-" + Guid.NewGuid().ToString("N"));
        try {
            var paths = HtmlInteractiveAssetFiles.WriteTo(directory, HtmlInteractiveAssetFiles.Charts().Concat(HtmlInteractiveAssetFiles.GraphExplorer()));
            Assert.Equal(4, paths.Count);
            Assert.All(paths, path => Assert.True(File.Exists(path)));
            var bytes = File.ReadAllBytes(paths[1]);
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "Asset files are written without a byte order mark.");
            Assert.Equal(HtmlInteractiveAssetFiles.ChartScript.Content, File.ReadAllText(paths[1]));
            var stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(paths[1], stamp);
            HtmlInteractiveAssetFiles.WriteTo(directory, HtmlInteractiveAssetFiles.Charts());
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(paths[1]));

            File.WriteAllText(paths[1], "truncated");
            HtmlInteractiveAssetFiles.WriteTo(directory, HtmlInteractiveAssetFiles.Charts());
            Assert.Equal(HtmlInteractiveAssetFiles.ChartScript.Content, File.ReadAllText(paths[1]));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("assets", "assets/")]
    [InlineData(@"..\shared\cfx", "../shared/cfx/")]
    [InlineData("https://cdn.example.test/cfx", "https://cdn.example.test/cfx/")]
    [InlineData("/reports/assets/", "/reports/assets/")]
    public void AssetReferences_NormalizeBasePath(string input, string expected) => Assert.Equal(expected, new HtmlAssetReferences(input).BasePath);

    [Theory]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/javascript,x")]
    [InlineData("assets\" onload=\"x")]
    [InlineData("as sets")]
    [InlineData("//evil.host/cfx")]
    [InlineData(@"\\server\share")]
    [InlineData("assets?v=1")]
    [InlineData("assets#top")]
    public void AssetReferences_RejectUnsafeBasePath(string input) => Assert.Throws<ArgumentException>(() => new HtmlAssetReferences(input));

    private static Chart CreateChart() => Chart.Create().WithSize(480, 240).AddLine("Series", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3) });

    private static string RuntimeSnippet(HtmlAssetFile file) {
        var content = file.Content.Trim();
        return content.Substring(content.Length / 2, Math.Min(160, content.Length / 2));
    }
}
