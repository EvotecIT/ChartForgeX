using System.Text.Json;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class TopologyHierarchyNativeBrowserTests {
    [BrowserTheory]
    [InlineData(true, 390)] [InlineData(false, 1100)]
    public async Task ActualNativeHierarchyResetWaitsForIgnoredAbortWithoutInventingViewportAxes(bool dark, int width) {
        if (!Enabled) return;
        var host = new Host(dark); await using var session = await OpenAsync(host.Page(), width, 800); var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.ExposeFunctionAsync("prepareAdmissionOrganization", (JsonElement request) => host.Reply(request));
        await page.EvaluateAsync("""
            () => {window.org=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.orgPending=[];
                window.orgRequests=[];window.orgChanges=[];window.orgErrors=[];
                org.root.addEventListener('cfxhierarchychange',event=>orgChanges.push(event.detail));
                org.root.addEventListener('cfxhierarchyerror',event=>orgErrors.push(event.detail));
                org.connectNative((request,signal)=>{orgRequests.push(request);return new Promise(resolve=>orgPending.push({request,signal,resolve}));},
                    {basis:'initial',axes:null,hierarchy:true,preparationBackpressure:'latest-window'});}
            """);
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-hierarchy-control=toggle][data-cfx-hierarchy-node=engineering]"));
        await page.EvaluateAsync("() => org.setHierarchyView({collapsedNodeIds:['engineering']})");
        await page.WaitForFunctionAsync("() => orgPending.length===1");
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-reset]"));
        await page.Keyboard.PressAsync("Space");
        // A second Reset still uses the existing hierarchy owner after logical pending was canceled.
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-reset]"));
        await page.Keyboard.PressAsync("Space");
        await CaptureHierarchyBoundary(page, width, dark, "admission-native-reset-queued");
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            var initial = await ActiveDrawing(page, host); var stem = Path.Combine(output, "native-hierarchy-admission-queued-" + width);
            File.WriteAllText(stem + ".svg", initial.ToSvg()); File.WriteAllBytes(stem + ".native.png", initial.ToPng());
        }
        Assert.Equal(1, await page.EvaluateAsync<int>("() => orgPending.length"));
        Assert.True(await page.EvaluateAsync<bool>("() => orgPending[0].signal.aborted&&[...org.root.querySelectorAll('[data-cfx-export],[data-cfx-hierarchy-control]')].every(button=>button.disabled)"));
        Assert.Equal("TypeError", await page.EvaluateAsync<string>("() => {try{org.setHierarchyView({collapsedNodeIds:['engineering']});return 'accepted';}catch(error){return error.name;}}"));
        await page.EvaluateAsync("async () => orgPending[0].resolve(await prepareAdmissionOrganization(orgPending[0].request))");
        await page.WaitForFunctionAsync("() => orgPending.length===2");
        Assert.Equal("hierarchy", await page.EvaluateAsync<string>("() => orgPending[1].request.channel"));
        Assert.Equal("reset", await page.EvaluateAsync<string>("() => orgPending[1].request.kind"));
        Assert.Equal("initial", await page.EvaluateAsync<string>("() => orgPending[1].request.basis"));
        Assert.Equal(5, await page.Locator(".cfx-stage [data-cfx-role=topology-node][data-node-id]").CountAsync());
        await page.EvaluateAsync("async () => orgPending[1].resolve(await prepareAdmissionOrganization(orgPending[1].request))");
        await page.WaitForFunctionAsync("() => orgChanges.length===1");
        await CaptureHierarchyBoundary(page, width, dark, "admission-native-reset-accepted");
        if (!string.IsNullOrWhiteSpace(output)) {
            var accepted = await ActiveDrawing(page, host); var stem = Path.Combine(output, "native-hierarchy-admission-accepted-" + width);
            File.WriteAllText(stem + ".svg", accepted.ToSvg()); File.WriteAllBytes(stem + ".native.png", accepted.ToPng());
        }
        Assert.Empty(await page.EvaluateAsync<string[]>("() => org.getHierarchyView().collapsedNodeIds"));
        Assert.Equal(5, await page.Locator(".cfx-stage [data-cfx-role=topology-node][data-node-id]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement===org.root"));
        Assert.Equal(5, host.SourceCount); Assert.Empty(await page.EvaluateAsync<string[]>("() => orgErrors.map(error=>error.message)"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
