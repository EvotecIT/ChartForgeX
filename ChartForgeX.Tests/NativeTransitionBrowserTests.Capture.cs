using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    private static async Task CaptureAsync(IPage page, HtmlChartSection from, HtmlChartSection to, HtmlNativeTransitionOptions options, string payload, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Native transition proof requires an absolute capture directory.");
        Directory.CreateDirectory(output); var prefix = Path.Combine(output, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(prefix + ".observed.json", await page.EvaluateAsync<string>("()=>JSON.stringify({html:document.querySelector('.cfx-interactive-chart').outerHTML,revision:ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).sceneRevision,pending:window.nativePending?.map(item=>item.request),errors:window.nativeErrors,events:window.nativeEvents,frame:document.querySelector('.cfx-native-motion-frame')?.outerHTML},null,2)"));
        await File.WriteAllTextAsync(prefix + ".transition.json", payload); File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html);
        void Save(string suffix, PreparedVisual native) { File.WriteAllText(prefix + suffix + ".svg", native.ToSvg()); File.WriteAllBytes(prefix + suffix + ".png", native.ToPng()); }
        Save(".from.wide", from.PreparedVisual); Save(".from.narrow", from.NarrowPreparedVisual!); Save(".to.wide", to.PreparedVisual); Save(".to.narrow", to.NarrowPreparedVisual!);
        var legend = options.Effect == HtmlNativeTransitionEffect.LegendVisibility;
        Save(".middle.wide", legend ? VisualSeriesVisibilityCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3)[1] : VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, options.FlashColor)[1]);
        Save(".middle.narrow", legend ? VisualSeriesVisibilityCapture.Frames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 3)[1] : VisualValueCapture.NativeFrames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 3, options.FlashColor)[1]);
    }
}
