using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlTinkerX;
using Microsoft.Playwright;

/// <summary>Exercises the scripts and editor actually embedded in the restored HTML adapter package.</summary>
internal static class PackageBrowserProbe {
    internal static async Task RunDocumentAsync(string html) {
        if (Environment.GetEnvironmentVariable("HFX_TEST_ENABLE_PLAYWRIGHT") != "1") {
            Console.WriteLine("Public HTML document browser execution not requested; set HFX_TEST_ENABLE_PLAYWRIGHT=1 to qualify it.");
            return;
        }
        foreach (var width in new[] { 390, 900 }) {
            await using var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
                Browser = Engine(), Headless = true, ViewportWidth = width, ViewportHeight = 760, Timeout = 15000
            });
            var errors = new System.Collections.Generic.List<string>();
            session.Page.PageError += (_, error) => errors.Add(error);
            await session.Page.SetContentAsync(html);
            PackageAssertions.Require(await session.Page.Locator("svg:visible g[data-cfx-role='pyramid-stage']").CountAsync() == 16,
                "The public HTML adapter did not display every candidate partition at " + width + " pixels.");
            PackageAssertions.Require(await session.Page.Locator("svg:visible [data-cfx-role='pyramid-label']").CountAsync() > 0,
                "The public HTML adapter displayed no readable candidate captions.");
            await Capture(session.Page, "public-html-adapter-" + width + "-" + Engine());
            PackageAssertions.Require(errors.Count == 0 && !session.ConsoleLog.Any(entry => entry.Type == HtmlConsoleMessageType.Error),
                "The public HTML adapter reported a runtime error: " + string.Join("; ", errors));
        }
        Console.WriteLine("Public HTML adapter displayed the staged SVG at compact and wide viewports in " + Engine() + ".");
    }

    internal static async Task RunAsync(string chartHtml, string graphHtml) {
        if (Environment.GetEnvironmentVariable("HFX_TEST_ENABLE_PLAYWRIGHT") != "1") {
            Console.WriteLine("Packed HTML browser execution not requested; set HFX_TEST_ENABLE_PLAYWRIGHT=1 to qualify it.");
            return;
        }
        var engine = Engine();
        foreach (var width in new[] { 390, 900 }) {
            await using var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
                Browser = engine, Headless = true, ViewportWidth = width, ViewportHeight = 760, Timeout = 15000
            });
            var page = session.Page;
            var errors = new System.Collections.Generic.List<string>();
            page.PageError += (_, error) => errors.Add(error);
            await page.SetContentAsync(chartHtml);
            var point = page.Locator("g[data-cfx-role='point'][data-cfx-observation-id='package-bubble']");
            await point.FocusAsync();
            await page.Keyboard.PressAsync("Space");
            PackageAssertions.Require(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-compare-count") == "1",
                "Packed chart keyboard selection did not execute.");
            PackageAssertions.Require(await point.GetAttributeAsync("data-cfx-size") == "7",
                "Packed chart selection lost the retained bubble size fact.");
            await Capture(page, "packed-chart-" + width + "-" + engine);

            await page.SetContentAsync(graphHtml);
            await page.Locator("[data-cfx-graph-action='edit']").ClickAsync();
            await page.Locator("[data-cfx-graph-editor-action='add-node']").ClickAsync();
            var form = page.Locator("[data-cfx-graph-editor-form='node']");
            await form.Locator("[data-cfx-graph-editor-field='id']").FillAsync("packed-worker");
            await form.Locator("[data-cfx-graph-editor-field='label']").FillAsync("Packed worker");
            await form.Locator("button[type='submit']").ClickAsync();
            PackageAssertions.Require(await page.Locator("[data-node-id='packed-worker'][data-cfx-role='graph-node']").CountAsync() == 1,
                "Packed graph editor did not add the authored node.");
            await page.Locator("[data-cfx-graph-editor-close]").ClickAsync();
            await page.Locator("[data-cfx-graph-action='undo']").ClickAsync();
            PackageAssertions.Require(await page.Locator("[data-node-id='packed-worker'][data-cfx-role='graph-node']").CountAsync() == 0,
                "Packed graph editor undo did not restore the previous document.");
            await page.Locator("[data-cfx-graph-action='redo']").ClickAsync();
            PackageAssertions.Require(await page.Locator("[data-node-id='packed-worker'][data-cfx-role='graph-node']").CountAsync() == 1,
                "Packed graph editor redo lost the added node.");
            await Capture(page, "packed-editor-" + width + "-" + engine);
            PackageAssertions.Require(errors.Count == 0 && !session.ConsoleLog.Any(entry => entry.Type == HtmlConsoleMessageType.Error),
                "Packed HTML runtime reported an error: " + string.Join("; ", errors));
        }
        Console.WriteLine("Packed HTML keyboard selection and graph editor add/undo/redo passed in " + engine + ".");
    }

    private static HtmlBrowserEngine Engine() => Environment.GetEnvironmentVariable("CFX_TEST_BROWSER")?.Trim().ToLowerInvariant() switch {
        null or "" or "chromium" => HtmlBrowserEngine.Chromium,
        "firefox" => HtmlBrowserEngine.Firefox,
        "webkit" => HtmlBrowserEngine.WebKit,
        _ => throw new InvalidOperationException("CFX_TEST_BROWSER must be chromium, firefox or webkit.")
    };

    private static async Task Capture(IPage page, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("CFX_BROWSER_CAPTURE_DIRECTORY must be absolute.");
        Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".png"), FullPage = true });
    }
}
