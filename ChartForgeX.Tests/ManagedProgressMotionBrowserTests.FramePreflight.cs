using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ManagedProgressMotionBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(760, false, false, false)]
    [InlineData(760, false, false, true)]
    [InlineData(390, true, true, false)]
    [InlineData(390, true, true, true)]
    public async Task AllCapturedFramesUsePreparedSectionValidationBeforeAnyNativeAcceptance(int width, bool dark, bool managedReply, bool inactive) {
        if (!Enabled) return;
        var host = new ProgressHost(width, dark); var initial = host.Section("initial");
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 640);
        var page = session.Page; var pageErrors = new List<string>(); page.PageError += (_, error) => pageErrors.Add(error);
        await ConnectAsync(page, host); await PauseAsync(page);
        await page.WaitForFunctionAsync("drawing=>progress.root.dataset.cfxAdaptiveDrawing===drawing", width < 600 ? "narrow" : "wide");
        var revision = await page.EvaluateAsync<string>("()=>progress.sceneRevision");
        ProgressHost.Reply reply;
        if (managedReply) {
            await RequestAsync(page, .75);
            reply = host.Prepare(await page.EvaluateAsync<JsonElement>("()=>pending[0].request"));
        } else {
            using var request = JsonDocument.Parse("{\"basis\":\"initial\",\"seriesKey\":\"delivery\",\"observationId\":\"reviewed\",\"value\":0.75}");
            reply = host.Prepare(request.RootElement);
        }
        await page.EvaluateAsync("""
            ({reply,managedReply,inactive})=>{
                const value=JSON.parse(reply.transition),active=progress.root.dataset.cfxAdaptiveDrawing;
                const drawing=inactive?(active==='wide'?'narrow':'wide'):active;
                const parsed=new DOMParser().parseFromString(value[drawing][1],'image/svg+xml');
                const nonTarget=parsed.querySelector('[data-cfx-role="progress-row"][data-cfx-observation-id="verified"]');
                if(!nonTarget)throw new Error('The actual native non-target row fixture is missing.');
                nonTarget.setAttribute('data-cfx-series','999');
                value[drawing][1]=new XMLSerializer().serializeToString(parsed.documentElement);
                window.preflightCandidate={...reply,transition:JSON.stringify(value)};
                window.preflightThrow=null;window.preflightUpdated=0;
                progress.root.addEventListener('cfxupdated',()=>preflightUpdated++);
                if(managedReply)pending[0].resolve(preflightCandidate);
                else try{progress.update(reply.html,{basis:reply.basis,sceneRevision:progress.sceneRevision,
                    transition:preflightCandidate.transition,motion:{duration:1000}})}catch(error){preflightThrow={name:error.name,message:error.message}}
            }
            """, new { reply, managedReply, inactive });
        await DrainAsync(page);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Frame preflight proof requires absolute task-owned output.");
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "native-frame-preflight-" + width + "-" + managedReply + "-" + inactive);
            foreach (var item in new[] { (Name: "baseline", Section: initial), (Name: "destination", Section: host.Section(reply.basis)), (Name: "midpoint", Section: host.Midpoint()) }) {
                File.WriteAllText(stem + "." + item.Name + ".wide.svg", item.Section.PreparedVisual.ToSvg());
                File.WriteAllBytes(stem + "." + item.Name + ".wide.png", item.Section.PreparedVisual.ToPng());
                if (item.Section.NarrowPreparedVisual is { } narrow) {
                    File.WriteAllText(stem + "." + item.Name + ".narrow.svg", narrow.ToSvg());
                    File.WriteAllBytes(stem + "." + item.Name + ".narrow.png", narrow.ToPng());
                }
            }
            await File.WriteAllTextAsync(stem + ".payload.json", await page.EvaluateAsync<string>("()=>JSON.stringify(preflightCandidate,null,2)"));
            await File.WriteAllTextAsync(stem + ".observed.json", await page.EvaluateAsync<string>("""
                ()=>JSON.stringify({revision:progress.sceneRevision,drawing:progress.root.dataset.cfxAdaptiveDrawing,
                    thrown:preflightThrow,updated:preflightUpdated,requests:pending.map(item=>({request:item.request,aborted:item.signal.aborted})),
                    changes:progressChanges,errors:progressErrors,html:progress.root.outerHTML},null,2)
                """));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        // Diagnostic native/browser artifacts above survive the expected incumbent causal failure.
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>progress.sceneRevision"));
        Assert.Equal("0.5", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.Locator(Layer).CountAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>preflightUpdated"));
        Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>progressChanges"));
        if (managedReply) {
            Assert.Null(await page.EvaluateAsync<JsonElement?>("()=>preflightThrow"));
            Assert.Single(await page.EvaluateAsync<JsonElement[]>("()=>progressErrors"));
        } else {
            Assert.Equal("TypeError", await page.EvaluateAsync<string>("()=>preflightThrow?.name"));
            Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>progressErrors"));
        }
        await RequestAsync(page, .8);
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>pending[pending.length-1].request.basis"));
        await page.EvaluateAsync("()=>progress.dispose()");
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }
}
