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
    public async Task ContainerResizeKeepsCssPixelGeometryAndTypographyWithPlainOrCompressedBands(bool compressed) {
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
    public void NativeCoordinateBandsRetainTypographyAndAgreeWithNativePreparationAtIntermediateWidth(bool bars) {
        var chart = Chart.Create().WithSize(760, 280).WithHeader(false).WithLegend(false).WithDataLabels(false);
        var values = new[] { new ChartPoint(1, 1), new ChartPoint(2, 3), new ChartPoint(3, 2) };
        if (bars) chart.WithBarStyle(ChartBarStyle.Flat).WithStackedBars().AddBar("A", values).AddBar("B", values);
        else chart.AddLine("A", values);
        var ticks = chart.Options.XAxis.TickCount;
        var explicitTicks = chart.Options.XAxis.HasExplicitTickCount;
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600);
        Assert.Equal(760, chart.Options.Size.Width); Assert.Equal(280, chart.Options.Size.Height); Assert.Equal(ticks, chart.Options.XAxis.TickCount);
        Assert.Equal(explicitTicks, chart.Options.XAxis.HasExplicitTickCount);
        var match = Regex.Match(fragment, "<script type=\"application/json\" data-cfx-layouts>(.*?)</script>", RegexOptions.Singleline);
        using var frames = JsonDocument.Parse(match.Groups[1].Value);
        Assert.InRange(frames.RootElement.GetArrayLength(), 2, 256);
        const int width = 779;
        var frame = frames.RootElement.EnumerateArray().Last(item => item.GetProperty("width").GetInt32() <= width);
        var sourceWidth = frame.GetProperty("width").GetInt32();
        var drawing = XElement.Parse(frame.GetProperty("svg").GetString()!);
        var elements = drawing.Descendants().ToArray();
        foreach (var change in frame.GetProperty("coordinates").EnumerateArray()) {
            var index = change[0].GetInt32(); var name = change[1].GetString()!; var n = 0;
            elements[index].SetAttributeValue(name, Regex.Replace(change[2].GetString()!, @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?", token =>
                (double.Parse(token.Value, System.Globalization.CultureInfo.InvariantCulture) + change[3][n++].GetDouble() * (width - sourceWidth)).ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
        }
        var expected = XElement.Parse(chart.WithSize(width, 280).ToSvg("expected")).Descendants().ToArray();
        Assert.Equal(expected.Length, elements.Length);
        foreach (var index in Enumerable.Range(0, expected.Length)) {
            Assert.Equal((string?)expected[index].Attribute("font-size"), (string?)elements[index].Attribute("font-size"));
            foreach (var attribute in new[] { "x", "x1", "x2", "y", "width", "height" }) {
                if (!double.TryParse((string?)expected[index].Attribute(attribute), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)) continue;
                var actual = double.Parse((string)elements[index].Attribute(attribute)!, System.Globalization.CultureInfo.InvariantCulture);
                Assert.InRange(Math.Abs(actual - value), 0, .25);
            }
        }
    }
}
