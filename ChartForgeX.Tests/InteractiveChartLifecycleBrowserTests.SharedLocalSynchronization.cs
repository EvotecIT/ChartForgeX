using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData("native-replace")]
    [InlineData("native-single")]
    [InlineData("native-toggle-remove")]
    [InlineData("native-local-first")]
    [InlineData("command-replace")]
    [InlineData("command-remove")]
    [InlineData("command-add")]
    [InlineData("command-clear")]
    [InlineData("command-local-add-only")]
    [InlineData("command-local-remove-only")]
    [InlineData("command-local-replace-only")]
    [InlineData("native-lasso-replace")]
    [InlineData("native-lasso-replace-compare")]
    [InlineData("native-lasso-append")]
    [InlineData("native-lasso-append-compare")]
    public async Task SharedToLocalTransitionsRetireSharedPeerSelectionWithoutTransportingLocalFacts(string profile) {
        if (!Enabled) return;
        var source = Chart.Create().WithSize(640, 360).WithLegend(false)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("shared"), "Shared", new[] { new ChartPoint(1, 4) })
                .WithObservationId(0, new ChartObservationId("shared-a")))
            .AddLine("PRIVATE LOCAL SERIES", new[] { new ChartPoint(987654, 876543) });
        source.Series[1].WithObservationId(0, new ChartObservationId("local-b")).WithPointLabel(0, "PRIVATE LOCAL CAPTION");
        var peer = Chart.Create().WithSize(640, 360).WithLegend(false)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("shared"), "Shared", new[] { new ChartPoint(1, 4) })
                .WithObservationId(0, new ChartObservationId("shared-a")))
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("unrelated"), "Unrelated", new[] { new ChartPoint(3, 8) })
                .WithObservationId(0, new ChartObservationId("unrelated")));
        var mode = profile == "native-single" ? ChartSelectionMode.Single : profile == "native-replace" ? ChartSelectionMode.Replace : ChartSelectionMode.Toggle;
        var html = new[] { source, peer }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.Interaction.GroupName = "shared-local-transition";
            options.Interaction.SelectionMode = mode;
            options.Interaction.Features = ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
            if (profile.Contains("lasso", StringComparison.Ordinal)) options.Interaction.Enable(ChartInteractionFeatures.Brush);
            if (profile.EndsWith("-compare", StringComparison.Ordinal)) options.Interaction.Enable(ChartInteractionFeatures.CompareMarkers);
        });
        await using var session = await OpenAsync(html, 900, 1300);
        var page = session.Page; var roots = page.Locator(Root);
        await page.EvaluateAsync("""
            ()=>{window.scopeControllers=[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>ChartForgeXCharts.get(root));
                window.scopeTransport=[];window.scopeChanges=[];window.scopeLassos=[];
                scopeControllers[0].root.addEventListener('cfxsync',event=>scopeTransport.push(event.detail));
                document.body.addEventListener('cfxselectionchange',event=>scopeChanges.push(event.detail));
                document.body.addEventListener('cfxlasso',event=>scopeLassos.push(event.detail));
                scopeControllers[1].select({observationIds:['unrelated']});}
            """);
        async Task Activate(string id) {
            await roots.First.Locator("[data-cfx-observation-id='" + id + "']").FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        if (profile.Contains("lasso", StringComparison.Ordinal)) {
            await page.EvaluateAsync("()=>{scopeControllers[0].select({observationIds:['shared-a']});scopeControllers[1].select({observationIds:['shared-a','unrelated']});}");
            await roots.First.Locator("[data-cfx-mode-button=brush]").ClickAsync();
            var bounds = await roots.First.Locator("[data-cfx-observation-id=local-b]").BoundingBoxAsync()
                ?? throw new InvalidOperationException("The native local observation has no bounds.");
            var x = (float)(bounds.X + bounds.Width / 2); var y = (float)(bounds.Y + bounds.Height / 2);
            var append = profile.Contains("append", StringComparison.Ordinal);
            if (append) await page.Keyboard.DownAsync("Shift");
            await page.Mouse.MoveAsync(x - 10, y - 10); await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(x + 10, y + 10, new Microsoft.Playwright.MouseMoveOptions { Steps = 5 });
            await page.Mouse.UpAsync();
            if (append) await page.Keyboard.UpAsync("Shift");
        } else if (profile == "native-local-first") {
            await Activate("local-b");
            Assert.Equal(0, await page.EvaluateAsync<int>("()=>scopeTransport.length"));
            await Activate("shared-a");
        } else if (profile.StartsWith("native", StringComparison.Ordinal)) {
            await Activate("shared-a"); await Activate("local-b");
            if (profile == "native-toggle-remove") await Activate("shared-a");
        } else {
            await page.EvaluateAsync("""
                profile=>{
                    const [source,peer]=scopeControllers;
                    if(profile==='command-local-add-only') source.select({observationIds:['local-b']},{mode:'add',sync:true});
                    else if(profile==='command-local-remove-only') {
                        source.select({observationIds:['local-b']});source.select({observationIds:['local-b']},{mode:'remove',sync:true});
                    } else if(profile==='command-local-replace-only')source.select({observationIds:['local-b']},{sync:true});
                    else {
                        source.select({observationIds:profile==='command-remove'?['shared-a','local-b']:['shared-a']});
                        peer.select({observationIds:['shared-a','unrelated']});
                        if(profile==='command-clear')source.clearSelection({sync:true});
                        else if(profile==='command-remove')source.select({observationIds:['shared-a']},{mode:'remove',sync:true});
                        else source.select({observationIds:['local-b']},{mode:profile==='command-add'?'add':'replace',sync:true});
                    }
                }
                """, profile);
        }
        var state = await page.EvaluateAsync<JsonElement>("""
            ()=>({source:scopeControllers[0].getSelection().map(target=>({id:target.observationId,scope:target.seriesScope})),
                peer:scopeControllers[1].getSelection().map(target=>target.observationId),transport:scopeTransport,changes:scopeChanges.length,lassos:scopeLassos})
            """);
        await CaptureAsync(page, "shared-local-" + profile, state);
        var expectedPeer = profile is "native-single" or "command-replace" or "command-clear" or "command-local-replace-only" ? Array.Empty<string>()
            : profile is "command-add" or "native-local-first" || profile.Contains("lasso-append", StringComparison.Ordinal) ? new[] { "shared-a", "unrelated" } : new[] { "unrelated" };
        Assert.Equal(expectedPeer, state.GetProperty("peer").EnumerateArray().Select(item => item.GetString()));
        var transport = state.GetProperty("transport");
        Assert.DoesNotContain("local-b", transport.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE LOCAL", transport.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("987654", transport.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("876543", transport.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("\"seriesScope\":\"local\"", transport.GetRawText(), StringComparison.Ordinal);
        foreach (var notification in transport.EnumerateArray())
            if (notification.GetProperty("action").GetString() == "compare")
                Assert.Equal(notification.GetProperty("targets").GetArrayLength(), notification.GetProperty("count").GetInt32());
        if (profile is "command-local-add-only" or "command-local-remove-only") Assert.Empty(transport.EnumerateArray());
        if (profile.StartsWith("command", StringComparison.Ordinal)) Assert.Equal(0, state.GetProperty("changes").GetInt32());
        if (profile.Contains("lasso", StringComparison.Ordinal)) {
            Assert.Equal(1, state.GetProperty("changes").GetInt32());
            var lasso = Assert.Single(state.GetProperty("lassos").EnumerateArray());
            Assert.Equal("local-b", Assert.Single(lasso.GetProperty("targets").EnumerateArray()).GetProperty("observationId").GetString());
            Assert.Single(transport.EnumerateArray(), notification => notification.GetProperty("action").GetString() == "lasso");
            Assert.DoesNotContain(transport.EnumerateArray(), notification => notification.GetProperty("action").GetString() == "compare");
        }
        if (profile == "native-single") Assert.Single(state.GetProperty("source").EnumerateArray());
        if (profile == "native-local-first") {
            await Activate("shared-a");
            Assert.Equal(new[] { "unrelated" }, await page.EvaluateAsync<string[]>("()=>scopeControllers[1].getSelection().map(target=>target.observationId)"));
        }
        if (state.GetProperty("source").GetArrayLength() == 0) await Activate("local-b");
        await roots.First.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal(new[] { 0, 0 }, await page.EvaluateAsync<int[]>("()=>scopeControllers.map(controller=>controller.getSelection().length)"));
        AssertNoConsoleErrors(session);
    }
}
