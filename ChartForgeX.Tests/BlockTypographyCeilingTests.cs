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
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TallSingleBranchGitKeepsMeasuredTagInsideItsBadge(bool dark) {
        var theme = VisualTheme.Report().WithFontFamily("Arial"); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: 256, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        var block = GitGraphBlock.Create().WithSize(600, 1200).WithTheme(theme, dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithCard(false).AddBranch("main").AddCommit("base", "main", tag: "i");
        var svg = block.ToSvg();
        Assert.Equal("i", Assert.Single(XDocument.Parse(svg).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "git-tag-label").Value);
        Assert.NotEmpty(block.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 600, 1200);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const text=document.querySelector('[data-cfx-role=git-tag-label]').getBoundingClientRect(), badge=document.querySelector('[data-cfx-role=git-tag]').getBoundingClientRect(); return text.width>0 && text.left>=badge.left && text.right<=badge.right; }"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await CaptureGit(session, block, "git-tall-tag-" + (dark ? "dark" : "light"));
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GitCaptionsWithLineBreaksFitAsSingleTextRuns(bool dark) {
        var theme = VisualTheme.Report().WithFontFamily("Courier New");
        var block = GitGraphBlock.Create().WithSize(320, 300).WithTheme(theme, dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithCard(false)
            .AddBranch("main").AddCommit("first", "main").AddCommit("last", "main", label: "MMMMMMMMMM\nMMMMMMMMMM");
        var svg = block.ToSvg();
        Assert.NotEmpty(block.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 320, 300);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(text => { const box=text.getBoundingClientRect(); return box.left>=-1 && box.right<=321; })"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await CaptureGit(session, block, "git-single-line-" + (dark ? "dark" : "light"));
    }

    private static async Task CaptureGit(HtmlTinkerX.HtmlBrowserSession session, GitGraphBlock block, string name) {
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), block.ToPng());
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, 320)]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    [InlineData(true, 960)]
    public async Task AcceptedMaximumGitFontKeepsCommitBranchAndTagCaptionsVisible(bool dark, int width) {
        var source = "%%{init: {'themeVariables':{'fontSize':256}}}%%\ngitGraph\ncommit id: \"base\" tag: \"v1\"\nbranch dev\ncommit id: \"work\"\ncheckout main\nmerge dev id: \"merge\"";
        var parsed = new MermaidParser().ParseGitGraph(source);
        Assert.Empty(parsed.Diagnostics);
        var block = parsed.Document!.ToGitGraphBlock(new MermaidGitGraphRenderOptions { Width = width, Height = 420 });
        block.Options.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
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
        var name = "git-font-ceiling-" + (dark ? "dark-" : "light-") + width;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), block.ToPng());
    }

    [Theory]
    [InlineData(0, "Arial")]
    [InlineData(1, "Arial")]
    [InlineData(2, "Arial")]
    [InlineData(0, "Segoe UI")]
    [InlineData(1, "Segoe UI")]
    [InlineData(2, "Segoe UI")]
    public void ClassicVennSetIntersectionAndTextShareTheirSvgBaselineAndWidth(int role, string family) {
        var theme = VisualTheme.Report().WithFontFamily(family);
        theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: 48, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        theme = theme.Configure(builder => { builder.Light.Foreground = builder.Dark.Foreground = builder.Light.Background = builder.Dark.Background = builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = builder.Light.Surface = builder.Dark.Surface = ChartColor.Transparent; builder.Light.Muted = builder.Dark.Muted = ChartColor.Transparent; });
        var red = ChartColor.FromHex("#FF0000");
        var block = VennDiagramBlock.Create().WithSize(600, 400).WithCard(false).WithTheme(theme).WithTransparentBackground()
            .AddSet("A", "MMMM", 60).AddSet("B", "NNNN", 50)
            .AddIntersection(new[] { "A", "B" }, "MMMM", 10).AddText(new[] { "A" }, "note", "Data");
        if (role == 0) block.Sets[0].TextColor = red;
        else if (role == 1) block.Intersections[0].TextColor = red;
        else block.TextNodes[0].TextColor = red;
        var raster = SvgRasterizer.Rasterize(block.ToSvg(), 600, 400);
        Assert.Empty(raster.Diagnostics);
        var expected = RedInkRows(raster.Image); var actual = RedInkRows(block.RenderRgba());
        Assert.InRange(actual.Min - expected.Min, -2, 2);
        Assert.InRange(actual.Max - expected.Max, -2, 2);
        Assert.InRange(actual.Left - expected.Left, -2, 2);
        Assert.InRange(actual.Right - expected.Right, -2, 2);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "venn-baseline-role-" + role + "-" + family.Replace(' ', '-');
        File.WriteAllBytes(Path.Combine(captures, name + "-svg-raster.png"), SvgRasterizer.ToPng(block.ToSvg(), 600, 400));
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), block.ToPng());
    }

    private static (int Min, int Max, int Left, int Right) RedInkRows(RgbaImage image) {
        var rows = new List<int>();
        var columns = new List<int>();
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset + 3] > 64 && image.Pixels[offset] > 200 && image.Pixels[offset + 1] < 20) { rows.Add(y); columns.Add(x); }
            }
        Assert.NotEmpty(rows);
        return (rows.Min(), rows.Max(), columns.Min(), columns.Max());
    }
}
