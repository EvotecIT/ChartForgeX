using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Responsive heatmaps keep whole column captions, an unsquashed colour legend and regular caption spacing.</summary>
public sealed class ResponsiveHeatmapNarrowBrowserTests {
    private const string Audit = """
        ()=>{
          const host=document.querySelector('[data-cfx-responsive-chart]'),svg=host.querySelector('svg');
          const hidden=node=>{for(let n=node;n&&n!==svg;n=n.parentElement)if(getComputedStyle(n).display==='none')return true;return false;};
          const texts=[...svg.querySelectorAll('text')].filter(text=>!hidden(text)&&text.getBoundingClientRect().width>0);
          const rects=texts.map(text=>text.getBoundingClientRect());
          const overlaps=[];
          for(let i=0;i<rects.length;i++)for(let j=i+1;j<rects.length;j++){const a=rects[i],b=rects[j];
            if(a.left<b.right-1&&b.left<a.right-1&&a.top<b.bottom-1&&b.top<a.bottom-1)overlaps.push(texts[i].textContent+' / '+texts[j].textContent);}
          // A caption is clipped when its own clip box is narrower than the drawn text.
          const clipped=[...svg.querySelectorAll('[data-cfx-role="heatmap-column-label"] text,[data-cfx-role="heatmap-scale-label"] text')].filter(text=>!hidden(text)).filter(text=>{
            const owner=text.closest('[clip-path]');if(!owner)return false;
            const rect=svg.getElementById(owner.getAttribute('clip-path').match(/#([^)]*)/)[1]).querySelector('rect');
            const box=text.getBBox(),x=Number(rect.getAttribute('x')),w=Number(rect.getAttribute('width'));
            return box.x<x-.5||box.x+box.width>x+w+.5;}).map(text=>text.textContent);
          const columns=[...svg.querySelectorAll('[data-cfx-role="heatmap-column-label"]')].filter(group=>!hidden(group)).map(group=>Number(group.dataset.cfxSourceId?.match(/(\d+)$/)?.[1]));
          const steps=[...svg.querySelectorAll('[data-cfx-role="heatmap-scale-step"]')].map(step=>step.getBoundingClientRect().width);
          return {overlaps,clipped,columns,smallestStep:Math.min(...steps)};
        }
        """;

    private sealed class Result {
        public string[] Overlaps { get; set; } = Array.Empty<string>();
        public string[] Clipped { get; set; } = Array.Empty<string>();
        public double[] Columns { get; set; } = Array.Empty<double>();
        public double SmallestStep { get; set; }
    }

    [BrowserTheory]
    [InlineData(320)]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task Layout_HeatmapAtWidth_KeepsCaptionsWholeAndRegular(int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(ReadableDefaultsTests.Availability(1100), 300, 1920);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div style='width:" + width + "px'>" + fragment + "</div></body></html>", Math.Max(width, 400), 600);
        await session.Page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive");
        var result = await session.Page.EvaluateAsync<Result>(Audit);

        Assert.Empty(result.Overlaps);
        Assert.Empty(result.Clipped);
        Assert.True(result.Columns.Length >= 3, "At least three column captions should remain.");
        var strides = result.Columns.Zip(result.Columns.Skip(1), (left, right) => right - left).Distinct().ToArray();
        Assert.Single(strides);
        Assert.True(result.SmallestStep >= 20, "Colour legend steps must keep their native width, not " + result.SmallestStep + " px.");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
