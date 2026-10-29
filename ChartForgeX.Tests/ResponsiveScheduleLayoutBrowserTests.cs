using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Responsive state timelines keep the title, lanes and summaries on the canvas with constant text at every width.</summary>
public sealed class ResponsiveScheduleLayoutBrowserTests {
    private const string Audit = """
        ()=>{
          const host=document.querySelector('[data-cfx-responsive-chart]'),svg=host.querySelector('svg'),box=svg.getBoundingClientRect();
          const hidden=node=>{for(let n=node;n&&n!==svg;n=n.parentElement)if(getComputedStyle(n).display==='none')return true;return false;};
          const texts=[...svg.querySelectorAll('text')].filter(text=>!hidden(text)&&text.getBoundingClientRect().width>0);
          const rects=texts.map(text=>text.getBoundingClientRect());
          const overlaps=[];
          for(let i=0;i<rects.length;i++)for(let j=i+1;j<rects.length;j++){const a=rects[i],b=rects[j];
            if(a.left<b.right-1&&b.left<a.right-1&&a.top<b.bottom-1&&b.top<a.bottom-1)overlaps.push(texts[i].textContent+' / '+texts[j].textContent);}
          const clipped=[...svg.querySelectorAll('[data-cfx-role="schedule-row-label"] text,[data-cfx-role="lane-summary"] text')].filter(text=>{
            let node=text,rect=null;
            for(;node&&node!==svg;node=node.parentElement){const id=node.getAttribute('clip-path')?.match(/url\(#([^)]*)\)/)?.[1];if(id){rect=svg.getElementById(id).querySelector('rect');break;}}
            if(!rect)return false;const b=text.getBBox(),x=Number(rect.getAttribute('x')),w=Number(rect.getAttribute('width'));
            return b.x<x-.5||b.x+b.width>x+w+.5;}).map(text=>{const b=text.getBBox();const r=svg.getElementById(text.closest('g[clip-path]').getAttribute('clip-path').match(/#([^)]*)/)[1]).querySelector('rect');return text.textContent+' '+b.x.toFixed(1)+'+'+b.width.toFixed(1)+' x='+text.getAttribute('x')+' a='+text.getAttribute('text-anchor')+' clip '+r.getAttribute('x')+'+'+r.getAttribute('width');});
          return {
            width:Number(host.dataset.cfxLayoutWidth),
            outside:texts.filter((_,i)=>rects[i].left<box.left-1||rects[i].right>box.right+1).map(text=>text.textContent),
            overlaps,clipped,
            smallest:Math.min(...texts.map(text=>parseFloat(getComputedStyle(text).fontSize))),
            labels:svg.querySelectorAll('[data-cfx-role="schedule-row-label"] text').length,
            groups:svg.querySelectorAll('[data-cfx-role="state-timeline-group"] text').length,
            summaries:svg.querySelectorAll('[data-cfx-role="lane-summary"] text').length,
            truncated:[...svg.querySelectorAll('text')].filter(text=>text.firstChild?.textContent.includes('…')).length,
            truncatedWithTitle:[...svg.querySelectorAll('text')].filter(text=>text.firstChild?.textContent.includes('…')).every(text=>!!text.querySelector('title'))
          };
        }
        """;

    private sealed class Result {
        public double Width { get; set; }
        public string[] Outside { get; set; } = Array.Empty<string>();
        public string[] Overlaps { get; set; } = Array.Empty<string>();
        public string[] Clipped { get; set; } = Array.Empty<string>();
        public double Smallest { get; set; }
        public int Labels { get; set; }
        public int Groups { get; set; }
        public int Summaries { get; set; }
        public int Truncated { get; set; }
        public bool TruncatedWithTitle { get; set; }
    }

    [BrowserTheory]
    [InlineData(320)]
    [InlineData(390)]
    [InlineData(1440)]
    [InlineData(1920)]
    public async Task Layout_StateTimelineAtWidth_KeepsTextInsideReadableAndUnclipped(int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(StateTimelineLabelPolicyTests.DenseLanes(), 300, 1920);
        var page = "<html><body style='margin:0'><div style='width:" + width + "px'>" + fragment + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(page, Math.Max(width, 400), 600);
        await session.Page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive");
        var result = await session.Page.EvaluateAsync<Result>(Audit);

        Assert.Equal(width, result.Width);
        Assert.Empty(result.Outside);
        Assert.Empty(result.Overlaps);
        Assert.Empty(result.Clipped);
        Assert.True(result.Smallest >= 10, "Smallest text was " + result.Smallest + " px.");
        Assert.Equal(12, result.Labels);
        Assert.Equal(3, result.Groups);
        Assert.Equal(12, result.Summaries);
        if (width <= 390) Assert.True(result.Truncated > 0, "Long lane names and the heading should truncate at " + width + " px.");
        Assert.True(result.TruncatedWithTitle, "Every truncated caption must expose its complete text.");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task Layout_StateTimelineNarrowsItsLaneGutterAtSmallWidths() {
        if (!InteractiveChartBrowser.Enabled) return;
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(StateTimelineLabelPolicyTests.DenseLanes(), 300, 1920);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div id=box style='width:1440px'>" + fragment + "</div></body></html>", 1500, 600);
        await session.Page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive");
        const string plotLeft = "()=>{const host=document.querySelector('[data-cfx-responsive-chart]');return document.querySelector('[data-cfx-role=\"state-timeline-segment-shape\"]').getBoundingClientRect().left-host.getBoundingClientRect().left;}";
        var wide = await session.Page.EvaluateAsync<double>(plotLeft);
        await session.Page.EvaluateAsync("()=>{document.getElementById('box').style.width='320px';document.querySelector('[data-cfx-responsive-chart]').cfxResponsive.layout(320);}");
        var narrow = await session.Page.EvaluateAsync<double>(plotLeft);

        Assert.True(narrow < wide, $"The lane gutter should narrow at 320 px ({narrow} vs {wide}).");
        Assert.True(narrow <= 320 * .45, $"Lanes should keep most of a 320 px container; the plot starts at {narrow} px.");
    }
}
