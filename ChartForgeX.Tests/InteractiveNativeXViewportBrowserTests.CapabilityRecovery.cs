using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LinkedCartesianPeersKeepCompatibleXWhenYIsMissingOrUsesAnotherScale(bool differentYScale) {
        if (!Enabled) return;
        var sender = new CapabilityHost("capability-source");
        var peer = new CapabilityHost("capability-peer", secondary: !differentYScale, logarithmicY: differentYScale);
        await using var session = await BrowserProofFonts.OpenAsync(sender.Page(peer), 1000, 1450);
        var page = session.Page;
        await ConnectCapabilitiesAsync(page, new[] { sender, peer }, new[] { "x,y", differentYScale ? "x,y" : "x" });
        await page.EvaluateAsync("()=>chartController.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}})");
        await page.WaitForFunctionAsync("()=>capabilityChanges.length>=1");
        await CapabilityDrainAsync(page);
        var requests = await page.EvaluateAsync<JsonElement>("()=>capabilityRequests");
        var received = Assert.Single(requests.EnumerateArray(), request => request.GetProperty("chartId").GetString() == peer.Id);
        AssertWindow(received.GetProperty("sourceViewport").GetProperty("x"), 2, 10);
        Assert.False(received.GetProperty("sourceViewport").TryGetProperty("y", out _));
        await page.WaitForFunctionAsync("()=>capabilityChanges.length===2");
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>capabilityControllers[0].getXWindow()"), 2, 10);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>capabilityControllers[1].getXWindow()"), 2, 10);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>capabilityControllers[0].getYWindow()"), 20, 80);
        var peerY = await page.EvaluateAsync<JsonElement>("()=>capabilityControllers[1].getYWindow()");
        if (differentYScale) AssertWindow(peerY, 1, 1000);
        else AssertWindow(peerY, 0, 100);
        var peerRoot = page.Locator(".cfx-interactive-chart[data-cfx-chart-id='" + peer.Id + "']");
        var download = await page.RunAndWaitForDownloadAsync(() => peerRoot.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The compatible peer did not export.");
        var exported = XDocument.Load(path);
        Assert.Equal(await BrowserProofCaptions.TextsAsync(peerRoot.Locator("[data-cfx-role=axis-x-label]")), await BrowserProofCaptions.ExportedAsync(page, exported, "axis-x-label", peerRoot.Locator(".cfx-stage svg")));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>capabilityErrors.map(error=>error.message)"));
        await CaptureAsync(page, "peer-compatible-x-" + differentYScale, new { differentYScale, requests });
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("x,secondaryY", false)]
    [InlineData("x,secondaryY", true)]
    [InlineData("secondaryY", false)]
    public async Task ResetAfterLosingSecondaryYStillPreparesFullNativeAxesAndCommitsOnlyAfterAcceptance(string gestureAxes, bool contextMenu) {
        if (!Enabled) return;
        var host = new CapabilityHost("reset-capability", secondary: true);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page(), 1000, 800);
        var page = session.Page;
        await ConnectCapabilitiesAsync(page, new[] { host }, new[] { gestureAxes });
        // Programmatic windows are explicit independently of the configured gesture axes.
        await page.EvaluateAsync("()=>chartController.setViewport({x:{minimum:2,maximum:10},secondaryY:{minimum:20,maximum:80}})");
        await page.WaitForFunctionAsync("()=>capabilityChanges.length===1");
        await page.EvaluateAsync("""
            axes=>{window.capabilityControlled=[];window.capabilityResetEvents=0;window.capabilitySelectionEvents=0;
                chartController.root.addEventListener('cfxreset',()=>capabilityResetEvents++);
                chartController.root.addEventListener('cfxselectionchange',()=>capabilitySelectionEvents++);
                chartController.connectViewport((request,signal)=>new Promise((resolve,reject)=>capabilityControlled.push({request,signal,resolve,reject})),{axes:axes.split(',')});}
            """, gestureAxes);
        await page.EvaluateAsync("""
            replacement=>{window.capabilityReplacementBasis=replacement.token;chartController.update(replacement.html);
                capabilityBases.set(chartController.sceneRevision,replacement.token);}
            """, host.WithoutSecondaryY(2, 10));
        Assert.Equal(JsonValueKind.Null, (await page.EvaluateAsync<JsonElement>("()=>chartController.getViewport().secondaryY")).ValueKind);
        Assert.True(await page.Locator("[data-cfx-zoom=in]").IsDisabledAsync());
        await page.Locator(Middle).FocusAsync(); await page.Keyboard.PressAsync("Space");
        await page.EvaluateAsync("()=>{capabilityResetEvents=0;capabilitySelectionEvents=0;}");
        var selected = await page.EvaluateAsync<JsonElement>("()=>chartController.getSelection()");
        Assert.Single(selected.EnumerateArray());
        var revision = await page.EvaluateAsync<string>("()=>chartController.sceneRevision");
        await ClickCapabilityResetAsync(page, contextMenu); await CapabilityDrainAsync(page);
        Assert.Single((await page.EvaluateAsync<JsonElement>("()=>capabilityControlled.map(item=>item.request)")).EnumerateArray());
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 2, 10);
        Assert.Equal(selected.GetRawText(), (await page.EvaluateAsync<JsonElement>("()=>chartController.getSelection()")).GetRawText());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>capabilityResetEvents+capabilitySelectionEvents"));
        await page.EvaluateAsync("()=>capabilityControlled[0].reject(new Error('Keep the committed view'))");
        await page.WaitForFunctionAsync("()=>capabilityErrors.length===1");
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>chartController.sceneRevision"));
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 2, 10);
        Assert.Equal(selected.GetRawText(), (await page.EvaluateAsync<JsonElement>("()=>chartController.getSelection()")).GetRawText());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>capabilityResetEvents+capabilitySelectionEvents"));
        await ClickCapabilityResetAsync(page, contextMenu); await CapabilityDrainAsync(page);
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>capabilityControlled.length"));
        var reset = await page.EvaluateAsync<JsonElement>("()=>capabilityControlled[1].request");
        Assert.Equal("reset", reset.GetProperty("kind").GetString());
        Assert.Equal(new[] { "x", "y" }, reset.GetProperty("axes").EnumerateArray().Select(axis => axis.GetString()));
        Assert.False(reset.TryGetProperty("sourceViewport", out _));
        Assert.False(reset.TryGetProperty("left", out _));
        await page.EvaluateAsync("""
            async()=>{const pending=capabilityControlled[1],reply=await capabilityHostPrepare({...pending.request,basis:capabilityReplacementBasis});
                capabilityCandidates.set(pending.request.requestId,reply.token);pending.resolve(reply.html);}
            """);
        await page.WaitForFunctionAsync("()=>capabilityChanges.length===2");
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 0, 12);
        Assert.Equal(JsonValueKind.Null, (await page.EvaluateAsync<JsonElement>("()=>chartController.getViewport().secondaryY")).ValueKind);
        Assert.Empty((await page.EvaluateAsync<JsonElement>("()=>chartController.getSelection()")).EnumerateArray());
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>capabilityResetEvents"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>capabilitySelectionEvents"));
        Assert.True(await page.Locator("[data-cfx-reset]").IsHiddenAsync());
        Assert.True(await page.Locator("[data-cfx-zoom=in]").IsDisabledAsync());
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The accepted full native view did not export.");
        var svg = XDocument.Load(path);
        foreach (var id in new[] { "sample-0", "sample-12" }) {
            var source = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-observation-id") == id);
            Assert.Contains(source.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "marker");
        }
        Assert.Equal(await BrowserProofCaptions.TextsAsync(page.Locator("[data-cfx-role=axis-x-label]")), await BrowserProofCaptions.ExportedAsync(page, svg, "axis-x-label"));
        await CaptureAsync(page, "reset-lost-y-" + gestureAxes + "-" + contextMenu, new { gestureAxes, contextMenu, reset });
        AssertNoConsoleErrors(session);
    }

    private static Task CapabilityDrainAsync(IPage page) => page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
    private static async Task ClickCapabilityResetAsync(IPage page, bool contextMenu) {
        if (!contextMenu) { await page.Locator("[data-cfx-reset]").ClickAsync(); return; }
        await page.Locator(Middle).FocusAsync(); await page.Keyboard.PressAsync("Shift+F10");
        await page.Locator(".cfx-chart-context-menu [data-cfx-context-action=reset]").ClickAsync();
    }
    private static async Task ConnectCapabilitiesAsync(IPage page, CapabilityHost[] hosts, string[] axes) {
        await page.ExposeFunctionAsync("capabilityHostPrepare", (JsonElement request) => hosts.Single(host => host.Id == request.GetProperty("chartId").GetString()).Prepare(request));
        await page.EvaluateAsync("""
            axes=>{window.capabilityControllers=[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>ChartForgeXCharts.get(root));
                window.chartController=capabilityControllers[0];window.capabilityRequests=[];window.capabilityChanges=[];window.capabilityErrors=[];
                window.capabilityBases=new Map(capabilityControllers.map(controller=>[controller.sceneRevision,'initial']));window.capabilityCandidates=new Map();
                capabilityControllers.forEach((controller,index)=>{
                    controller.root.addEventListener('cfxviewportrequest',event=>capabilityRequests.push(event.detail));
                    controller.root.addEventListener('cfxviewporterror',event=>capabilityErrors.push(event.detail));
                    controller.root.addEventListener('cfxviewportchange',event=>{
                        capabilityBases.set(event.detail.sceneRevision,capabilityCandidates.get(event.detail.requestId));capabilityChanges.push(event.detail);});
                    controller.connectViewport(async(request,signal)=>{
                        const result=await capabilityHostPrepare({...request,basis:capabilityBases.get(request.sceneRevision)});
                        if(signal.aborted)throw new DOMException('Canceled','AbortError');
                        capabilityCandidates.set(request.requestId,result.token);return result.html;
                    },{axes:axes[index].split(',')});
                });}
            """, axes);
    }

    private sealed class CapabilityHost {
        private readonly bool _logarithmicY;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, CapabilitySnapshot> _sections = new();
        internal CapabilityHost(string id, bool secondary = false, bool logarithmicY = false) {
            Id = id; _logarithmicY = logarithmicY;
            _sections.Add("initial", new CapabilitySnapshot(_renderer.PrepareSection(Model(secondary), configure: ConfigureSection), secondary));
        }
        internal string Id { get; }
        internal string Page(params CapabilityHost[] peers) => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>"
            + _sections["initial"].Section.Html + string.Concat(peers.Select(peer => peer._sections["initial"].Section.Html))
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal object WithoutSecondaryY(double minimum, double maximum) {
            lock (_sections) return Store(false, new ChartAxisWindow(minimum, maximum), null);
        }
        internal object Prepare(JsonElement request) {
            lock (_sections) {
                var basis = _sections[request.GetProperty("basis").GetString()!];
                var prepared = basis.Section.PreparedVisual;
                ChartAxisWindow? x = null; ChartAxisWindow? y = null;
                if (request.GetProperty("kind").GetString() != "reset") {
                    var projection = prepared.XProjection!;
                    if (projection.Minimum != projection.FullMinimum || projection.Maximum != projection.FullMaximum || !projection.MaximumInclusive)
                        x = new ChartAxisWindow(projection.Minimum, projection.Maximum, projection.MaximumInclusive);
                    var value = basis.Secondary ? prepared.SecondaryYProjection : prepared.YProjection;
                    if (value != null && (value.Minimum != value.FullMinimum || value.Maximum != value.FullMaximum || !value.MaximumInclusive))
                        y = new ChartAxisWindow(value.Minimum, value.Maximum, value.MaximumInclusive);
                    var requested = request.GetProperty("sourceViewport");
                    if (requested.TryGetProperty("x", out var sourceX)) x = projection.ConstrainViewport(NativeWindowTransport.Read(sourceX));
                    if (requested.TryGetProperty(basis.Secondary ? "secondaryY" : "y", out var sourceY))
                        y = value!.ConstrainViewport(NativeWindowTransport.Read(sourceY));
                }
                return Store(basis.Secondary, x, y);
            }
        }
        private object Store(bool secondary, ChartAxisWindow? x, ChartAxisWindow? y) {
            var section = x == null && y == null ? _renderer.PrepareSection(Model(secondary), configure: ConfigureSection)
                : _renderer.PrepareSection(Model(secondary), new ChartCartesianViewport(x: x, y: secondary ? null : y, secondaryY: secondary ? y : null), ConfigureSection);
            var token = "capability-" + _sections.Count;
            _sections.Add(token, new CapabilitySnapshot(section, secondary));
            return new { token, html = section.Html };
        }
        private Chart Model(bool secondary) {
            var primary = new ChartSeries(new ChartSeriesId("samples"), "Samples", ChartSeriesKind.Line,
                Enumerable.Range(0, 13).Select(index => new ChartPoint(index, 20 + index * 4)));
            for (var index = 0; index < 13; index++) primary.WithObservationId(index, new ChartObservationId("sample-" + index));
            primary.ConfigureMarkers(options => options.Enabled = true);
            var reference = new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.StepLine,
                new[] { new ChartPoint(0, 30), new ChartPoint(12, 30) });
            if (secondary) reference.UseSecondaryValueAxis();
            var chart = Chart.Create().WithSize(800, 440).WithFontFamily(BrowserProofFonts.Family).WithTitle(Id)
                .WithXAxisBounds(0, 12).WithYAxisBounds(_logarithmicY ? 1 : 0, _logarithmicY ? 1000 : 100)
                .WithYAxisScale(_logarithmicY ? ChartScaleKind.Logarithmic : ChartScaleKind.Linear)
                .AddSeries(primary).AddSeries(reference);
            if (secondary) chart.WithSecondaryYAxisBounds(0, 100);
            return chart;
        }
        private void ConfigureSection(HtmlChartInteractionOptions options) {
            Configure(options); options.Interaction.ChartId = Id; options.Interaction.GroupName = "capability-peers";
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
            options.IncludeContextMenu = true;
        }
        private sealed record CapabilitySnapshot(HtmlChartSection Section, bool Secondary);
    }
}
