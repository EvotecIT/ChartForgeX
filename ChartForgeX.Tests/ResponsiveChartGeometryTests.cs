using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveChartGeometryTests {
    [BrowserFact]
    public async Task RemovingCompressedHostBeforeDecodeCompletesDoesNotCreateAnObserver() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 280).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddBar("Count", new[] { new ChartPoint(1, 1) });
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 800, compressLayouts: true);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body><script>" + HtmlResponsiveChartRenderer.InitializationScript + "</script></body></html>");
        await session.Page.EvaluateAsync("""
            fragment=>{
              const text=Response.prototype.text, Observer=ResizeObserver;
              Response.prototype.text=async function(){const result=await text.call(this);await new Promise(resolve=>window.releaseDecode=resolve);return result;};
              window.observedCharts=0;
              window.ResizeObserver=class extends Observer {observe(host,...args){if(host.matches('[data-cfx-responsive-chart]'))window.observedCharts++;super.observe(host,...args);}};
              const template=document.createElement('template');template.innerHTML=fragment;document.body.append(template.content);
              window.removedChart=document.querySelector('[data-cfx-responsive-chart]');
            }
            """, fragment);
        await session.Page.WaitForFunctionAsync("()=>typeof releaseDecode==='function'");
        await session.Page.EvaluateAsync("async()=>{const ready=removedChart.cfxResponsiveReady;removedChart.remove();releaseDecode();await ready;}");
        Assert.Equal(0, await session.Page.EvaluateAsync<int>("()=>observedCharts"));
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>!removedChart.cfxResponsive && !removedChart.cfxResponsiveReady"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task InertFragmentsMountAndSparseBarsRespectTheirCapAcrossTheNativeClampTransition() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 280).WithPadding(0, 0, 0, 0).WithHeader(false).WithLegend(false)
            .WithAxes(false).WithDataLabels(false).WithCard(false).WithBarStyle(ChartBarStyle.Flat).WithMaximumBarWidth(220)
            .AddBar("A", new[] { new ChartPoint(1, 1) });
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600);
        var html = "<html><body style='margin:0'><div id='panel' style='width:320px'></div><script>" + HtmlResponsiveChartRenderer.InitializationScript + "</script></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 600);
        // Mirrors a compressed chapter: script elements parsed through innerHTML stay inert.
        await session.Page.EvaluateAsync("html=>{const t=document.createElement('template');t.innerHTML=html;document.querySelector('#panel').append(t.content);}", fragment);
        double? previous = null;
        foreach (var width in new[] { 320, 323, 324, 340, 360, 361, 400 }) {
            await session.Page.EvaluateAsync("width=>document.querySelector('#panel').style.width=width+'px'", width);
            await session.Page.WaitForFunctionAsync("width=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===width", width);
            var geometry = await session.Page.EvaluateAsync<double[]>("()=>{const b=document.querySelector('[data-cfx-role=bar]');return [Number(b.getAttribute('width')),Number(b.getAttribute('x'))];}");
            Assert.InRange(geometry[0], 0, 220.001);
            Assert.Equal(width / 2d, geometry[1] + geometry[0] / 2, .1);
            if (width == 361) Assert.InRange(Math.Abs(geometry[0] - previous!.Value), 0, .1);
            previous = geometry[0];
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContainerResizeKeepsCssPixelGeometryAndTypographyWithPlainOrCompressedSpecifications(bool compressed) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 280).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddLine("Count", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3), new ChartPoint(3, 2) }).WithYAxisIntegerTicks();
        var html = "<html><body style='margin:0'><div id='panel' style='width:1120px'>" +
            new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600, compressLayouts: compressed) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 600);
        await session.Page.EvaluateAsync("async()=>await document.querySelector('[data-cfx-responsive-chart]').cfxResponsiveReady");
        foreach (var width in new[] { 1120, 320, 779, 1354, 1700 }) {
            await session.Page.EvaluateAsync("width=>document.querySelector('#panel').style.width=width+'px'", width);
            await session.Page.WaitForFunctionAsync("width=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===Math.min(width,1600)", width);
            var dimensions = await session.Page.EvaluateAsync<double[]>("""
                async width=>{document.querySelector('#panel').style.width=width+'px';await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));const svg=document.querySelector('svg'),t=svg.querySelector('[data-cfx-role="axis-y-label"]'),m=t.getScreenCTM();return [svg.getBoundingClientRect().width,svg.getBoundingClientRect().height,svg.viewBox.baseVal.width,parseFloat(getComputedStyle(t).fontSize)*Math.hypot(m.c,m.d),parseFloat(getComputedStyle(t).fontSize)];}
                """, width);
            Assert.Equal(Math.Min(width, 1600), dimensions[0], .1); Assert.Equal(280, dimensions[1], .1);
            Assert.Equal(dimensions[0], dimensions[2], .1); Assert.Equal(dimensions[4], dimensions[3], .01);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LayoutSpecificationRetainsSourceValuesAndRestoresAuthoredAxisPolicies(bool bars) {
        var chart=Chart.Create().WithSize(760,280).WithHeader(false).WithLegend(false).WithDataLabels(false);
        var values=new[]{new ChartPoint(1,1),new ChartPoint(2,3),new ChartPoint(3,2)};
        if(bars)chart.WithBarStyle(ChartBarStyle.Flat).WithStackedBars().AddBar("A",values).AddBar("B",values);else chart.AddLine("A",values);
        var axis=chart.Options.XAxis;
        var fragment=new HtmlResponsiveChartRenderer().RenderFragment(chart,480,1600,printWidth:320);
        Assert.Equal(760,chart.Options.Size.Width);Assert.Same(axis,chart.Options.XAxis);
        var match=Regex.Match(fragment,"<script type=\"application/json\" data-cfx-spec>(.*?)</script>",RegexOptions.Singleline);
        using var spec=JsonDocument.Parse(match.Groups[1].Value);
        Assert.Equal("chartforgex/layout-1",spec.RootElement.GetProperty("schema").GetString());
        Assert.Equal(320,spec.RootElement.GetProperty("printWidth").GetInt32());
        var series=spec.RootElement.GetProperty("series");Assert.Equal(bars?2:1,series.GetArrayLength());
        Assert.Equal(new[]{1d,3d,2d},series[0].GetProperty("points").EnumerateArray().Select(p=>p.GetProperty("y").GetDouble()));
        var fallback=XElement.Parse(Regex.Match(fragment,"<svg.*?</svg>",RegexOptions.Singleline).Value);
        Assert.Equal("320",(string?)fallback.Attribute("width"));Assert.Equal("280",(string?)fallback.Attribute("height"));
    }

    [BrowserFact]
    public async Task ArbitraryFractionalWidthsDebounceAndPrintAtTheActualPanelWidth() {
        if(!InteractiveChartBrowser.Enabled)return;
        var chart=Chart.Create().WithSize(760,280).WithHeader(false).WithLegend(false).WithDataLabels(false).WithYAxisIntegerTicks()
            .AddBar("Count",new[]{new ChartPoint(1,1),new ChartPoint(2,3),new ChartPoint(3,2)});
        var html="<html><head><style>@media print{#panel{width:640px!important}}</style></head><body style='margin:0'><div id='panel' style='width:1120px'>"+
            new HtmlResponsiveChartRenderer().RenderFragment(chart,240,1600)+"</div></body></html>";
        await using var session=await InteractiveChartBrowser.OpenAsync(html,1440,600);var page=session.Page;
        await page.EvaluateAsync("async()=>{await document.querySelector('[data-cfx-responsive-chart]').cfxResponsiveReady;await document.fonts.ready;}");
        foreach(var width in new[]{313.25,779.5,1354.125}){
            await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'",width);
            await page.WaitForFunctionAsync("w=>Math.abs(Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)-w)<1",width);
            var result=await page.EvaluateAsync<double[]>("()=>{const svg=document.querySelector('svg'),label=svg.querySelector('[data-cfx-role=axis-y-label] text');return[svg.getBoundingClientRect().width,svg.getBoundingClientRect().height,parseFloat(getComputedStyle(label).fontSize)*Math.hypot(label.getScreenCTM().c,label.getScreenCTM().d),document.documentElement.scrollWidth];}");
            Assert.InRange(Math.Abs(result[0]-width),0,.5);Assert.Equal(280,result[1],.1);Assert.Equal(12,result[2],.1);Assert.InRange(result[3],0,1440);
        }
        await page.WaitForTimeoutAsync(150);
        await page.EvaluateAsync("()=>{window.layoutWrites=0;new MutationObserver(records=>window.layoutWrites+=records.filter(r=>r.attributeName==='viewBox').length).observe(document.querySelector('svg'),{attributes:true});}");
        await page.EvaluateAsync("async()=>{for(const w of [610,620,630]){document.querySelector('#panel').style.width=w+'px';await new Promise(requestAnimationFrame);}}");
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===630");
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>layoutWrites"));
        await page.EmulateMediaAsync(new(){Media=Microsoft.Playwright.Media.Print});
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===640");
        Assert.Equal(280,await page.Locator("svg").EvaluateAsync<double>("svg=>svg.getBoundingClientRect().height"),.1);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
    [BrowserFact]
    public async Task HiddenChartMeasuresItsLegendAndTicksWhenRevealedAndWrapsWithoutScalingText() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 280).WithHeader(false).WithDataLabels(false).WithMaximumBarWidth(40)
            .AddBar("Verified observations", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) })
            .AddBar("Pending observations", new[] { new ChartPoint(1, 2), new ChartPoint(2, 1) });
        var html = "<html><body style='margin:0'><div id='panel' style='display:none;width:320px'>" +
            new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 390, 600);
        await session.Page.EvaluateAsync("async()=>{await document.querySelector('[data-cfx-responsive-chart]').cfxResponsiveReady;document.querySelector('#panel').style.display='block';}");
        await session.Page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===320");
        foreach (var width in new[] { 320, 1120, 320 }) {
            await session.Page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await session.Page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            var boxes = await session.Page.EvaluateAsync<double[][]>("()=>[...document.querySelectorAll('[data-cfx-role=legend-entry]')].map(n=>{const b=n.getBoundingClientRect();return[b.x,b.y,b.right,b.bottom];})");
            Assert.Equal(2, boxes.Length);
            foreach (var box in boxes) { Assert.InRange(box[0], 0, width); Assert.InRange(box[2], 0, width + .5); }
            Assert.True(boxes[0][2] <= boxes[1][0] + .5 || boxes[0][3] <= boxes[1][1] + .5);
            var bars = await session.Page.Locator("[data-cfx-role=bar]").EvaluateAllAsync<double[]>("nodes=>nodes.map(n=>n.getBoundingClientRect().width)");
            Assert.All(bars, bar => Assert.InRange(bar, .1, 40.1));
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

}
