using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class ScenarioReadabilityBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 1440)]
    [InlineData(true, false, 390)]
    [InlineData(false, true, 390)]
    [InlineData(true, true, 1440)]
    public async Task ScenarioHeadingsAndProgressRemainReadableInStandaloneAndThemedHosts(bool dark, bool host, int width) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(640, 280)
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("counts"), "Counts", new[] { new ChartPoint(1, 7), new ChartPoint(2, 3) }));
        var interactive = new HtmlInteractiveChartRenderer().RenderFragment(chart, options => {
            options.Interaction.Enable(ChartInteractionFeatures.Scenarios | ChartInteractionFeatures.StepPlayback);
            options.Interaction.AddScenario("route", "Review route", scenario => scenario
                .AddSeriesStep("counts", "Observe").AddSeriesStep("counts", "Confirm"));
        });
        var topology = TopologyChart.Create().WithTheme(VisualTheme.Graphite(), mode).WithViewport(640, 240, 20)
            .AddNode("a", "API", 80, 100).AddNode("b", "Worker", 340, 100)
            .AddScenario("route", "Topology route", scenario => scenario.AddNodeStep("a").AddNodeStep("b"));
        var topologyHtml = new HtmlInteractiveTopologyRenderer().RenderFragment(topology,
            new TopologyRenderOptions { ActiveScenarioId = "route", EnableHtmlSelectionPanel = false });
        var background = dark ? "#0f172a" : "#ffffff";
        var foreground = dark ? "#e5e7eb" : "#0f172a";
        var hostCard = dark ? "#203247" : "#fff5e6";
        var hostText = dark ? "#f0f4ff" : "#2a1c10";
        var hostMuted = dark ? "#c0cedf" : "#635041";
        var hostStyle = host ? $".host{{--cfx-surface-card:{hostCard};--cfx-text-primary:{hostText};--cfx-text-muted:{hostMuted}}}" : "";
        var html = $"<!doctype html><html><head><style>body{{margin:0;background:{background};color:{foreground}}}main{{max-width:760px;margin:auto;padding:12px}}{hostStyle}</style></head><body><main class='host'>{topologyHtml}{interactive}</main></body></html>";
        await using var session = await OpenAsync(html, width, 1000);
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = dark ? ColorScheme.Dark : ColorScheme.Light });
        if (host) {
            Assert.Equal(dark ? "rgb(32, 50, 71)" : "rgb(255, 245, 230)", await page.Locator(".cfx-scenario-panel").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
            Assert.Equal(dark ? "rgb(240, 244, 255)" : "rgb(42, 28, 16)", await page.Locator(".cfx-scenario-panel__title").EvaluateAsync<string>("node => getComputedStyle(node).color"));
            Assert.Equal(dark ? "rgb(192, 206, 223)" : "rgb(99, 80, 65)", await page.Locator(".cfx-scenario-panel__meta").EvaluateAsync<string>("node => getComputedStyle(node).color"));
        }
        await page.Locator("[data-cfx-scenario='route']").ClickAsync();
        var observations = new List<JsonElement>();
        for (var step = 0; step <= 2; step++) {
            if (step > 0) {
                await page.Locator(".cfx-topology-scenario-panel [data-cfx-scenario-panel-steps] li").Nth(step - 1).ClickAsync();
                await page.Locator(".cfx-scenario-panel__steps li").Nth(step - 1).ClickAsync();
                var progress = page.Locator(".cfx-scenario-progress__text");
                await Microsoft.Playwright.Assertions.Expect(progress).ToBeVisibleAsync();
                await Microsoft.Playwright.Assertions.Expect(progress).ToHaveTextAsync($"Step {step} / 2");
            }
            await page.Locator(".cfx-scenario-progress__bar").EvaluateAsync("node => { void getComputedStyle(node).width; }");
            await page.WaitForFunctionAsync("() => document.querySelector('.cfx-scenario-progress__bar').getAnimations().every(animation => animation.playState !== 'running')");
            observations.Add(await page.EvaluateAsync<JsonElement>("""
                step => {
                  const canvas=document.createElement('canvas'); canvas.width=canvas.height=1;
                  const ctx=canvas.getContext('2d',{willReadFrequently:true,colorSpace:'srgb'});
                  function rgba(color) {ctx.clearRect(0,0,1,1);ctx.fillStyle=color;ctx.fillRect(0,0,1,1);return [...ctx.getImageData(0,0,1,1).data].map((v,i)=>i===3?v/255:v);}
                  function over(top,base) {return top.slice(0,3).map((v,i)=>v*top[3]+base[i]*(1-top[3]));}
                  function luminance(c) {return c.map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4}).reduce((s,v,i)=>s+v*[.2126,.7152,.0722][i],0);}
                  function facts(selector,progress,node=document.querySelector(selector)) {
                    const chain=[];for(let p=node;p;p=p.parentElement)chain.unshift(p);
                    let bg=[255,255,255];for(const p of chain)bg=over(rgba(getComputedStyle(p).backgroundColor),bg);
                    const ink=rgba(getComputedStyle(node).color),fg=over(ink,bg),l1=luminance(fg),l2=luminance(bg);
                    let ratio=(Math.max(l1,l2)+.05)/(Math.min(l1,l2)+.05);
                    let progressOverlapsText=false;
                    if(progress){const bar=document.querySelector('.cfx-scenario-progress__bar'),fillBox=bar.getBoundingClientRect(),range=document.createRange();range.selectNodeContents(node);const textBox=range.getBoundingClientRect();progressOverlapsText=Math.min(fillBox.right,textBox.right)>Math.max(fillBox.left,textBox.left)&&Math.min(fillBox.bottom,textBox.bottom)>Math.max(fillBox.top,textBox.top);if(progressOverlapsText){const fill=over(rgba(getComputedStyle(bar).backgroundColor),bg),fl=luminance(over(ink,fill)),bl=luminance(fill);ratio=Math.min(ratio,(Math.max(fl,bl)+.05)/(Math.min(fl,bl)+.05));}}
                    const box=node.getBoundingClientRect(),visible=box.width>0&&box.height>0&&chain.every(p=>{const s=getComputedStyle(p);return s.display!=='none'&&s.visibility!=='hidden'&&s.visibility!=='collapse'&&Number(s.opacity)>0;});
                    return {selector,text:node.textContent,ratio,ink,background:bg,progressOverlapsText,visible};
                  }
                  return {step,labels:[facts('.cfx-topology-scenario-panel__title',false),facts('.cfx-topology-scenario-panel__meta',false),...Array.from(document.querySelectorAll('.cfx-topology-scenario-panel [data-cfx-scenario-panel-steps] li'),node=>facts('.cfx-topology-scenario-panel [data-cfx-scenario-panel-steps] li',false,node)),facts('.cfx-scenario-panel__title',false),facts('.cfx-scenario-panel__meta',false),...Array.from(document.querySelectorAll('.cfx-scenario-panel__steps li'),node=>facts('.cfx-scenario-panel__steps li',false,node)),facts('.cfx-scenario-progress__text',true)]};
                }
                """, step));
        }
        var root = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(root)) {
            var folder = Path.Combine(root, "scenario-readability"); Directory.CreateDirectory(folder);
            var name = (host ? "host" : "standalone") + (dark ? "-dark-" : "-light-") + width;
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(folder, name + ".png"), FullPage = true });
            File.WriteAllText(Path.Combine(folder, name + ".html"), html);
            File.WriteAllText(Path.Combine(folder, name + ".json"), JsonSerializer.Serialize(observations));
        }
        foreach (var observation in observations) {
            var labels = observation.GetProperty("labels").EnumerateArray().ToArray();
            var stepCaptions = labels.Where(label => label.GetProperty("selector").GetString() == ".cfx-scenario-panel__steps li").ToArray();
            Assert.Equal(new[] { "Observe", "Confirm" }, stepCaptions.Select(label => label.GetProperty("text").GetString()?.Trim()).ToArray());
            var topologyCaptions = labels.Where(label => label.GetProperty("selector").GetString() == ".cfx-topology-scenario-panel [data-cfx-scenario-panel-steps] li").ToArray();
            Assert.Equal(new[] { "a", "b" }, topologyCaptions.Select(label => label.GetProperty("text").GetString()?.Trim()).ToArray());
            foreach (var label in labels) {
                Assert.True(label.GetProperty("visible").GetBoolean(), label.GetRawText());
                Assert.True(label.GetProperty("ratio").GetDouble() >= 4.5, label.GetRawText());
            }
        }
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }
}
