using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory] [InlineData(760, false)] [InlineData(390, true)]
    public async Task AutomaticOwnedFacesLoadFromBinaryUnderSelfFontCspAndDetachedExportsKeepNativeInk(int width, bool dark) {
        if (!Enabled) return;
        var html = OwnedPage(Model(width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light).ToInteractiveHtmlFragment(OwnedOptions));
        await using var session = await OpenAsync(html, width + 24, 760); var page = session.Page;
        await page.WaitForFunctionAsync("()=>[...document.fonts].some(face=>face.family.includes('cfx-owned-')&&face.status==='loaded')&&[...document.querySelectorAll('[data-cfx-owned-font-text]')].some(node=>!node.hasAttribute('display'))");
        var observed = await page.EvaluateAsync<JsonElement>("()=>({violations:ownedFontViolations,fonts:[...document.fonts].map(face=>({family:face.family,status:face.status})),labels:[...document.querySelectorAll('[data-cfx-owned-font-text]')].map(node=>({text:node.textContent,family:getComputedStyle(node).fontFamily,display:getComputedStyle(node).display,width:node.getBBox().width})),resources:performance.getEntriesByType('resource').map(row=>row.name)})");
        HtmlOwnedFontDeliveryTests.Save("binary-csp-"+width+".page.html",html);HtmlOwnedFontDeliveryTests.Save("binary-csp-"+width+".observed.json",observed.GetRawText());
        var exported = await CaptureExports(page,"[data-cfx-export=svg]","[data-cfx-export=png]","owned-font-csp-"+width);
        Assert.Empty(observed.GetProperty("violations").EnumerateArray());
        Assert.Contains(observed.GetProperty("labels").EnumerateArray(), row=>row.GetProperty("display").GetString()!="none"&&row.GetProperty("width").GetDouble()>0);
        Assert.DoesNotContain(observed.GetProperty("resources").EnumerateArray(),row=>row.GetString()!.StartsWith("data:font",StringComparison.Ordinal)||row.GetString()!.StartsWith("blob:",StringComparison.Ordinal));
        Assert.DoesNotContain(XDocument.Parse(exported.Svg).Descendants(),node=>node.Attribute("data-cfx-owned-font-text")!=null);
        Assert.Contains(XDocument.Parse(exported.Svg).Descendants(),node=>node.Name.LocalName=="use");
        Assert.Equal(0,exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Equal(0,exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData(false)] [InlineData(true)]
    public async Task UnsupportedOrRejectedBinaryFontLoadingKeepsTheAcceptedNativeContourFallback(bool rejected) {
        if (!Enabled) return;
        var failure = rejected
            ? "const Actual=FontFace;window.FontFace=function(...args){const face=new Actual(...args);face.load=()=>Promise.reject(new Error('Intentional owned-font rejection'));return face;};"
            : "window.FontFace=undefined;";
        var html=OwnedPage(Model(760,VisualThemeMode.Light).ToInteractiveHtmlFragment(OwnedOptions),failure);
        await using var session=await OpenAsync(html,784,760,waitForOwnedFonts:false);var page=session.Page;
        var observed=await page.EvaluateAsync<JsonElement>("()=>({violations:ownedFontViolations,fonts:document.fonts.size,visible:[...document.querySelectorAll('[data-cfx-owned-font-native]')].filter(node=>getComputedStyle(node).display!=='none').length,text:[...document.querySelectorAll('[data-cfx-owned-font-text]')].filter(node=>getComputedStyle(node).display!=='none').length})");
        HtmlOwnedFontDeliveryTests.Save("binary-failure-"+rejected+".page.html",html);HtmlOwnedFontDeliveryTests.Save("binary-failure-"+rejected+".observed.json",observed.GetRawText());
        await page.ScreenshotAsync(new PageScreenshotOptions{Path=CapturePath("owned-font-failure-"+rejected+".png"),FullPage=true});
        Assert.Empty(observed.GetProperty("violations").EnumerateArray());Assert.Equal(0,observed.GetProperty("fonts").GetInt32());Assert.True(observed.GetProperty("visible").GetInt32()>0);Assert.Equal(0,observed.GetProperty("text").GetInt32());AssertNoConsoleErrors(session);
    }
    [BrowserFact]
    public async Task BinaryRegistrationDetachesCallerBytesAndChartDisposalKeepsAnIndependentlyOwnedFace() {
        if (!Enabled) return;
        var html=OwnedPage(Model(760,VisualThemeMode.Light).ToInteractiveHtmlFragment(OwnedOptions));
        await using var session=await OpenAsync(html,784,760,waitForOwnedFonts:false);var page=session.Page;
        await page.WaitForFunctionAsync("()=>[...document.fonts].some(face=>face.family.includes('cfx-owned-')&&face.status==='loaded')");
        var observed=await page.EvaluateAsync<JsonElement>("async()=>{const module=window[Symbol.for('ChartForgeX.nativeTextExport')],payload=document.querySelector('template[data-cfx-font-root]').content.textContent;const bytes=Uint8Array.from(atob(payload),c=>c.charCodeAt(0));const owner=module.registerBinary('Independent caller',bytes,{weight:'400'});bytes.fill(0);await owner.ready;const root=document.querySelector('.cfx-interactive-chart');ChartForgeXCharts.get(root).dispose();const after=[...document.fonts].map(face=>({family:face.family,status:face.status}));owner.release();return{after,final:document.fonts.size,violations:ownedFontViolations};}");
        HtmlOwnedFontDeliveryTests.Save("caller-mutation-lifetime.observed.json",observed.GetRawText());
        Assert.Empty(observed.GetProperty("violations").EnumerateArray());Assert.Equal(0,observed.GetProperty("final").GetInt32());
        Assert.Contains("Independent caller",Assert.Single(observed.GetProperty("after").EnumerateArray()).GetProperty("family").GetString()!,StringComparison.Ordinal);AssertNoConsoleErrors(session);
    }
    [BrowserFact]
    public async Task ReplacementBeforeAFontLoadCompletesCannotSwitchTheDetachedDrawingOrRetainItsFace() {
        if (!Enabled) return;
        const string delayed="window.pendingOwnedLoads=[];const Actual=FontFace;window.FontFace=function(...args){const face=new Actual(...args),load=face.load.bind(face);face.load=()=>new Promise((resolve,reject)=>pendingOwnedLoads.push(()=>load().then(resolve,reject)));return face;};";
        var first=Model(760,VisualThemeMode.Light).ToInteractiveHtmlFragment(OwnedOptions);
        var next=Model(760,VisualThemeMode.Light).WithTitle("A replacement drawing").ToInteractiveHtmlFragmentWithoutAssets(OwnedOptions);
        await using var session=await OpenAsync(OwnedPage(first,delayed),784,760,waitForOwnedFonts:false);var page=session.Page;
        await page.WaitForFunctionAsync("()=>pendingOwnedLoads.length>0");
        await page.EvaluateAsync("html=>{window.oldOwnedSvg=document.querySelector('.cfx-stage svg');window.oldOwnedCount=pendingOwnedLoads.length;ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).update(html);}",next);
        var observed=await page.EvaluateAsync<JsonElement>("async()=>{await Promise.all(pendingOwnedLoads.splice(0,oldOwnedCount).map(load=>load()));await Promise.resolve();return{detached:!oldOwnedSvg.isConnected,oldVisible:[...oldOwnedSvg.querySelectorAll('[data-cfx-owned-font-text]')].filter(node=>!node.hasAttribute('display')).length,faces:document.fonts.size,newVisible:[...document.querySelectorAll('[data-cfx-owned-font-text]')].filter(node=>!node.hasAttribute('display')).length};}");
        HtmlOwnedFontDeliveryTests.Save("late-load-replacement.observed.json",observed.GetRawText());
        Assert.True(observed.GetProperty("detached").GetBoolean());Assert.Equal(0,observed.GetProperty("oldVisible").GetInt32());Assert.Equal(0,observed.GetProperty("newVisible").GetInt32());Assert.Equal(0,observed.GetProperty("faces").GetInt32());
        await page.EvaluateAsync("async()=>{await Promise.all(pendingOwnedLoads.splice(0).map(load=>load()));await document.fonts.ready;}");
        await page.WaitForFunctionAsync("()=>[...document.querySelectorAll('[data-cfx-owned-font-text]')].some(node=>!node.hasAttribute('display'))");
        await page.EvaluateAsync("()=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).dispose()");Assert.Equal(0,await page.EvaluateAsync<int>("()=>document.fonts.size"));AssertNoConsoleErrors(session);
    }
    [BrowserFact]
    public async Task FontObservationRejectsForeignRecordsAndNewMissingIdentitiesRetainContoursWithoutBorrowingTheOldFace() {
        if (!Enabled) return;
        await using var session=await OpenAsync(OwnedPage(Model(760,VisualThemeMode.Light).ToInteractiveHtmlFragment(OwnedOptions)),784,760);var page=session.Page;
        await page.WaitForFunctionAsync("()=>[...document.fonts].some(face=>face.family.includes('cfx-owned-')&&face.status==='loaded')");
        var observed=await page.EvaluateAsync<JsonElement>("async()=>{const module=window[Symbol.for('ChartForgeX.nativeTextExport')],root=document.querySelector('.cfx-interactive-chart'),normal=module.observation(root,false),failures=[];for(const change of ['attribute','base64','empty','dangling']){const copy=root.cloneNode(true),record=copy.querySelector('template[data-cfx-font-root]');if(change==='attribute')record.setAttribute('onclick','foreign');if(change==='base64')record.content.textContent='?'+record.content.textContent.slice(1);if(change==='empty')record.content.replaceChildren();if(change==='dangling')record.dataset.cfxFontRoot='0'.repeat(64);try{module.observation(copy,false);failures.push(false);}catch(error){failures.push(error instanceof TypeError);}}const missing=root.cloneNode(true),record=missing.querySelector('template[data-cfx-font-root]'),old=record.dataset.cfxFontRoot;record.dataset.cfxFontRoot='0'.repeat(64);record.content.replaceChildren();for(const face of missing.querySelectorAll('template[data-cfx-font-face]'))if(face.dataset.cfxFontResource===old)face.dataset.cfxFontResource='0'.repeat(64);const different=!normal.isEqualNode(module.observation(missing,true));document.body.appendChild(missing);const releases=[];await module.mount(missing,release=>{releases.push(release);return release;});const aliases=new Set([...missing.querySelectorAll('template[data-cfx-font-face]')].filter(face=>face.dataset.cfxFontResource==='0'.repeat(64)).map(face=>face.dataset.cfxFontFace));const borrowed=[...missing.querySelectorAll('[data-cfx-owned-font-text]')].filter(node=>aliases.has(node.dataset.cfxOwnedFontText)&&!node.hasAttribute('display')).length;const native=[...missing.querySelectorAll('[data-cfx-owned-font-native]')].filter(node=>aliases.has(node.dataset.cfxOwnedFontNative)&&!node.hasAttribute('display')).length;for(const release of releases)release();missing.remove();return{failures,different,borrowed,native,violations:ownedFontViolations};}");
        HtmlOwnedFontDeliveryTests.Save("font-observation-malformed-and-missing.observed.json",observed.GetRawText());
        Assert.All(observed.GetProperty("failures").EnumerateArray(),row=>Assert.True(row.GetBoolean()));Assert.True(observed.GetProperty("different").GetBoolean());
        Assert.Equal(0,observed.GetProperty("borrowed").GetInt32());Assert.True(observed.GetProperty("native").GetInt32()>0);Assert.Empty(observed.GetProperty("violations").EnumerateArray());AssertNoConsoleErrors(session);
    }
    private static void OwnedOptions(HtmlChartInteractionOptions options) { options.TextPresentation=HtmlChartTextPresentation.Automatic;options.ResponsiveLayout=HtmlChartResponsiveLayout.Fit;options.Interaction.ChartId="owned-font-proof";options.Interaction.Enable(ChartInteractionFeatures.Export); }
    private static string OwnedPage(string body,string before="")=>@"<!doctype html><html><head><meta http-equiv=""Content-Security-Policy"" content=""default-src 'none';script-src 'unsafe-inline';style-src 'unsafe-inline';img-src data:;font-src 'self';connect-src 'none';base-uri 'none'""></head><body><script>window.ownedFontViolations=[];addEventListener('securitypolicyviolation',event=>ownedFontViolations.push({directive:event.effectiveDirective,blocked:event.blockedURI}));"+before+"</script>"+body+"</body></html>";
    private static string CapturePath(string name) { var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output)||!Path.IsPathRooted(output))throw new InvalidOperationException("Owned font browser proof requires an absolute capture path.");Directory.CreateDirectory(output);return Path.Combine(output,name); }
}
