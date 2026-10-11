using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BlockTypographyCeilingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TallSingleBranchGitKeepsMeasuredTagInsideItsBadge(bool graphite) {
        var theme = ChartTheme.Light(); theme.UseGraphiteLayout = graphite; theme.FontFamily = "Arial"; theme.DataLabelFontSize = 256;
        var block = GitGraphBlock.Create().WithSize(600, 1200).WithTheme(theme).WithCard(false).AddBranch("main").AddCommit("base", "main", tag: "i");
        var svg = block.ToSvg();
        Assert.Equal("i", Assert.Single(XDocument.Parse(svg).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "git-tag-label").Value);
        Assert.NotEmpty(block.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 600, 1200);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const text=document.querySelector('[data-cfx-role=git-tag-label]').getBoundingClientRect(), badge=document.querySelector('[data-cfx-role=git-tag]').getBoundingClientRect(); return text.width>0 && text.left>=badge.left && text.right<=badge.right; }"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await CaptureGit(session, block, "git-tall-tag-" + (graphite ? "graphite" : "classic"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GitCaptionsWithLineBreaksFitAsSingleTextRuns(bool graphite) {
        var theme = ChartTheme.Light(); theme.UseGraphiteLayout = graphite; theme.FontFamily = "Courier New";
        var block = GitGraphBlock.Create().WithSize(320, 300).WithTheme(theme).WithCard(false)
            .AddBranch("main").AddCommit("first", "main").AddCommit("last", "main", label: "MMMMMMMMMM\nMMMMMMMMMM");
        var svg = block.ToSvg();
        Assert.NotEmpty(block.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 320, 300);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(text => { const box=text.getBoundingClientRect(); return box.left>=-1 && box.right<=321; })"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await CaptureGit(session, block, "git-single-line-" + (graphite ? "graphite" : "classic"));
    }

    private static async Task CaptureGit(HtmlTinkerX.HtmlBrowserSession session, GitGraphBlock block, string name) {
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), block.ToPng());
    }

    [Theory]
    [InlineData(false, 320)]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    [InlineData(true, 960)]
    public async Task AcceptedMaximumGitFontKeepsCommitBranchAndTagCaptionsVisible(bool graphite, int width) {
        var source = "%%{init: {'themeVariables':{'fontSize':256}}}%%\ngitGraph\ncommit id: \"base\" tag: \"v1\"\nbranch dev\ncommit id: \"work\"\ncheckout main\nmerge dev id: \"merge\"";
        var parsed = new MermaidParser().ParseGitGraph(source);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToGitGraphBlock(new MermaidGitGraphRenderOptions { Width = width, Height = 420 });
        block.Options.Theme.UseGraphiteLayout = graphite;
        var svg = block.ToSvg();
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in new[] { "base", "work", "merge", "main", "dev", "v1" }) Assert.Contains(caption, labels);
        Assert.NotEmpty(block.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 420);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(text => { const box=text.getBoundingClientRect(); return box.left >= -1 && box.right <= innerWidth+1 && box.top >= -1 && box.bottom <= 421; })"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "git-font-ceiling-" + (graphite ? "graphite-" : "classic-") + width;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), block.ToPng());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ClassicVennSetIntersectionAndTextShareTheirSvgBaseline(int role) {
        var theme = ChartTheme.Light(); theme.UseGraphiteLayout = false; theme.FontFamily = "Arial";
        theme.DataLabelFontSize = 48;
        theme.Text = theme.MutedText = theme.Background = theme.CardBackground = theme.PlotBackground = ChartColor.Transparent;
        var red = ChartColor.FromHex("#FF0000");
        var block = VennDiagramBlock.Create().WithSize(600, 400).WithCard(false).WithTheme(theme).WithTransparentBackground()
            .AddSet("A", "MMMM", 60).AddSet("B", "NNNN", 50)
            .AddIntersection(new[] { "A", "B" }, "MMMM", 10).AddText(new[] { "A" }, "note", "MMMM");
        if (role == 0) block.Sets[0].TextColor = red;
        else if (role == 1) block.Intersections[0].TextColor = red;
        else block.TextNodes[0].TextColor = red;
        var raster = SvgRasterizer.Rasterize(block.ToSvg(), 600, 400);
        Assert.Empty(raster.Diagnostics);
        var expected = RedInkRows(raster.Image); var actual = RedInkRows(block.RenderRgba());
        Assert.InRange(actual.Min - expected.Min, -2, 2);
        Assert.InRange(actual.Max - expected.Max, -2, 2);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "venn-baseline-role-" + role;
        File.WriteAllBytes(Path.Combine(captures, name + "-svg-raster.png"), SvgRasterizer.ToPng(block.ToSvg(), 600, 400));
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), block.ToPng());
    }

    private static (int Min, int Max) RedInkRows(RgbaImage image) {
        var rows = new List<int>();
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset + 3] > 64 && image.Pixels[offset] > 200 && image.Pixels[offset + 1] < 20) { rows.Add(y); break; }
            }
        Assert.NotEmpty(rows);
        return (rows.Min(), rows.Max());
    }
}
