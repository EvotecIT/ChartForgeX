using System.Text.RegularExpressions;
using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using HtmlTinkerX;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveChartSpecificationGuardTests {
    private static readonly Regex SpecificationScript = new("<script[^>]*data-cfx-spec(?:-gzip)?[^>]*>.*?</script>", RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static string Fragment(bool compressed) {
        var chart = Chart.Create().WithSize(320, 160).WithHeader(false).WithLegend(false)
            .AddLine("Count", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) });
        return new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 480, compressLayouts: compressed);
    }

    private static string Page(string fragment) =>
        "<html><body style='width:320px'><script>" + HtmlResponsiveChartRenderer.InitializationScript + "</script>" + fragment + "</body></html>";

    [Fact]
    public void Fragment_ContainsOneSpecificationScript() {
        Assert.Single(SpecificationScript.Matches(Fragment(false)));
        Assert.Single(SpecificationScript.Matches(Fragment(true)));
    }

    [BrowserTheory]
    [InlineData("missing")]
    [InlineData("undefined")]
    [InlineData("malformed-gzip")]
    [InlineData("invalid-base64")]
    public async Task Initialize_UnusableSpecification_KeepsStaticDrawingAndWarnsOnce(string defect) {
        if (!InteractiveChartBrowser.Enabled) return;
        string source = Fragment(defect.EndsWith("gzip", StringComparison.Ordinal) || defect == "invalid-base64");
        string fragment = defect switch {
            "missing" => SpecificationScript.Replace(source, string.Empty),
            "undefined" => SpecificationScript.Replace(source, "<script type=\"application/json\" data-cfx-spec>undefined</script>"),
            "malformed-gzip" => SpecificationScript.Replace(source, "<script type=\"application/octet-stream\" data-cfx-spec-gzip>bm90IGd6aXA=</script>"),
            _ => SpecificationScript.Replace(source, "<script type=\"application/octet-stream\" data-cfx-spec-gzip>%%%</script>")
        };
        var pageErrors = new List<string>();
        await using var session = await InteractiveChartBrowser.OpenAsync(Page(fragment),
            configurePage: page => page.PageError += (_, error) => pageErrors.Add(error));
        await session.Page.EvaluateAsync("()=>Promise.all([...document.querySelectorAll('[data-cfx-responsive-chart]')].map(host=>host.cfxResponsiveReady))");
        // A second mount must not reuse a failed initialization or repeat the warning.
        await session.Page.EvaluateAsync("()=>window.ChartForgeXResponsive.mount(document)");

        Assert.Empty(pageErrors);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Single(session.ConsoleLog, entry => entry.Type == HtmlConsoleMessageType.Warning && entry.Text.Contains("kept its static drawing", StringComparison.Ordinal));
        Assert.False(await session.Page.EvaluateAsync<bool>("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive"));
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>!!document.querySelector('[data-cfx-responsive-chart] svg')"));
    }

    [BrowserFact]
    public async Task Initialize_SpecificationAddedLater_MountsChart() {
        if (!InteractiveChartBrowser.Enabled) return;
        string source = Fragment(false);
        string specification = SpecificationScript.Match(source).Value;
        var pageErrors = new List<string>();
        await using var session = await InteractiveChartBrowser.OpenAsync(Page(SpecificationScript.Replace(source, string.Empty)),
            configurePage: page => page.PageError += (_, error) => pageErrors.Add(error));
        await session.Page.EvaluateAsync("()=>document.querySelector('[data-cfx-responsive-chart]').cfxResponsiveReady");
        Assert.False(await session.Page.EvaluateAsync<bool>("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive"));

        await session.Page.EvaluateAsync("html=>document.querySelector('[data-cfx-responsive-chart]').insertAdjacentHTML('beforeend',html)", specification);
        await session.Page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive");

        Assert.Equal(320, await session.Page.EvaluateAsync<double>(
            "()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)"));
        Assert.Empty(pageErrors);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
