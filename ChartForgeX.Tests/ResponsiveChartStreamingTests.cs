using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveChartStreamingTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParserAddedChartsWaitForCompleteLayoutDataAndDynamicChartsStillMount(bool compressed) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(320, 160).WithHeader(false).WithLegend(false)
            .AddLine("Count", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) });
        string fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 480, compressLayouts: compressed);
        string marker = compressed ? "data-cfx-spec-gzip>" : "data-cfx-spec>";
        int start = fragment.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = fragment.IndexOf("</script>", start, StringComparison.Ordinal);
        Assert.True(start >= marker.Length && end > start);
        int split = start + (end - start) / 2;
        var pageErrors = new List<string>();
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body></body></html>",
            configurePage: page => page.PageError += (_, error) => pageErrors.Add(error));
        await session.Page.EvaluateAsync("html=>{document.open();document.write(html);}",
            "<html><body style='width:320px'><script>" + HtmlResponsiveChartRenderer.InitializationScript + "</script>" + fragment[..split]);
        // Flush the observer while the parser is still inside the layout-data script.
        await session.Page.EvaluateAsync("()=>new Promise(resolve=>setTimeout(resolve,0))");
        Assert.Equal("loading", await session.Page.EvaluateAsync<string>("()=>document.readyState"));
        await session.Page.EvaluateAsync("html=>{document.write(html);document.close();}",
            fragment[split..] + "</body></html>");
        await session.Page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-responsive-chart]')?.cfxResponsive");
        Assert.Equal(320, await session.Page.EvaluateAsync<double>(
            "()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)"));
        // The same public lifecycle must still handle fragments inserted after parsing.
        await session.Page.EvaluateAsync("html=>document.body.insertAdjacentHTML('beforeend',html)", fragment);
        await session.Page.WaitForFunctionAsync("()=>[...document.querySelectorAll('[data-cfx-responsive-chart]')].length===2&&[...document.querySelectorAll('[data-cfx-responsive-chart]')].every(host=>!!host.cfxResponsive)");
        Assert.Empty(pageErrors);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
