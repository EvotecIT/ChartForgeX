using System.Text.RegularExpressions;
using ChartForgeX.Interactivity.Html;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Serves a page and its asset files from one origin under a files-only content security policy, records every policy
/// violation and every inline script or style that appears in the document, and drives the common interactions.
/// </summary>
internal static class ExternalCspBrowserHarness {
    internal const string Origin = "https://chartforgex.test";
    internal const string AssetPath = "assets/";
    private const string BrowserCaptureVariable = "CFX_BROWSER_CAPTURE_DIRECTORY";

    /// <summary>The supported policy: scripts, styles and workers from files on the page's origin only. Exports draw
    /// through data: and blob: images, and the embedded fonts are data: URLs.</summary>
    internal const string Policy = "default-src 'none'; script-src 'self'; style-src 'self'; worker-src 'self'; "
        + "img-src 'self' data: blob:; font-src 'self' data:; base-uri 'none'; form-action 'none'";

    // Recorded from document start, so transient elements (print surfaces, export probes) count too.
    private const string Recorder = """
        (() => {
          const log = window.__cfxCsp = { violations: [], inline: [] };
          document.addEventListener('securitypolicyviolation', e =>
            log.violations.push(`${e.effectiveDirective} ${e.blockedURI} ${(e.sample || '').slice(0, 80)} @${e.sourceFile}:${e.lineNumber}:${e.columnNumber}`));
          const executable = s => !s.hasAttribute('src') && (!s.type || /^(module|(text|application)\/(java|ecma)script)$/i.test(s.type.trim()));
          const inspect = node => {
            if (node.nodeType !== 1) return;
            for (const element of [node, ...node.querySelectorAll('style, script')]) {
              if (element.localName === 'style') log.inline.push('style: ' + (element.textContent || '').slice(0, 80));
              else if (element.localName === 'script' && executable(element)) log.inline.push('script: ' + (element.textContent || '').slice(0, 80));
            }
          };
          new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(inspect)))
            .observe(document, { childList: true, subtree: true });
        })();
        """;

    // The document after interaction: inline elements, handler attributes and script URLs anywhere, templates included.
    private const string Scan = """
        () => {
          const found = [];
          const executable = s => !s.hasAttribute('src') && (!s.type || /^(module|(text|application)\/(java|ecma)script)$/i.test(s.type.trim()));
          const visit = scope => {
            for (const element of scope.querySelectorAll('*')) {
              if (element.localName === 'style') found.push('style element');
              if (element.localName === 'script' && executable(element)) found.push('inline script');
              for (const attribute of element.attributes) {
                if (/^on/i.test(attribute.name)) found.push('handler ' + attribute.name + ' on ' + element.localName);
                if (/^\s*javascript:/i.test(attribute.value)) found.push('script URL in ' + attribute.name);
              }
              if (element.localName === 'template') visit(element.content);
              if (element.shadowRoot) visit(element.shadowRoot);
            }
          };
          visit(document);
          return found;
        }
        """;

    /// <summary>Renders the page through <see cref="HtmlExternalAssets"/> into page markup and asset files.</summary>
    internal static HtmlExternalPage Split(string html) => HtmlExternalAssets.Split(html, new HtmlAssetReferences(AssetPath));

    /// <summary>Fails when the served markup carries an inline script, style element, style attribute, handler or script URL.</summary>
    internal static void AssertSourceLoadsFilesOnly(HtmlExternalPage page) {
        var html = page.Html;
        var problems = new List<string>();
        foreach (Match match in Regex.Matches(html, "<script\\b([^>]*)>(.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase)) {
            var attributes = match.Groups[1].Value;
            var type = Regex.Match(attributes, "\\btype\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            var executable = !type.Success || Regex.IsMatch(type.Groups[1].Value.Trim(), "^(module|(text|application)/(java|ecma)script)$", RegexOptions.IgnoreCase);
            if (executable && !Regex.IsMatch(attributes, "\\bsrc\\s*=", RegexOptions.IgnoreCase)) problems.Add("inline script");
            if (executable && match.Groups[2].Value.Trim().Length > 0) problems.Add("script with src and content");
        }
        var markup = Regex.Replace(html, "<script\\b[^>]*>.*?</script>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (Regex.IsMatch(markup, "<style\\b", RegexOptions.IgnoreCase)) problems.Add("style element");
        if (Regex.IsMatch(markup, "<[a-zA-Z][^>]*\\sstyle\\s*=", RegexOptions.IgnoreCase)) problems.Add("style attribute");
        if (Regex.IsMatch(markup, "<[a-zA-Z][^>]*\\son[a-z]+\\s*=", RegexOptions.IgnoreCase)) problems.Add("event handler attribute");
        if (Regex.IsMatch(markup, "=\\s*[\"']?\\s*javascript:", RegexOptions.IgnoreCase)) problems.Add("script URL");
        foreach (var file in page.Files.Where(file => file.ContentType == "text/javascript"))
            if (Regex.IsMatch(file.Content, "\\beval\\s*\\(|\\bnew\\s+Function\\s*\\(")) problems.Add("eval in " + file.FileName);
        Assert.True(problems.Count == 0, string.Join(", ", problems.Distinct()));
    }

    /// <summary>Opens the page with the policy header; asset requests are answered from <see cref="HtmlExternalPage.Files"/>.</summary>
    /// <param name="page">The split page.</param>
    /// <param name="shared">Shared runtime files the page references through its own asset references.</param>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    internal static async Task<HtmlBrowserSession> OpenAsync(HtmlExternalPage page, IEnumerable<HtmlAssetFile>? shared = null, int width = 1100, int height = 760) {
        var session = await HtmlBrowser.OpenSessionForNavigationAsync(new HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 20000, ViewportWidth = width, ViewportHeight = height,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        try {
            var files = page.Files.Concat(shared ?? Array.Empty<HtmlAssetFile>()).GroupBy(file => file.FileName)
                .ToDictionary(group => "/" + AssetPath + group.Key, group => group.First(), StringComparer.Ordinal);
            // Violations name a file and line; keep the files to read them.
            if (Environment.GetEnvironmentVariable(BrowserCaptureVariable) is { Length: > 0 } capture) {
                var folder = Path.Combine(capture, "external-csp");
                HtmlInteractiveAssetFiles.WriteTo(Path.Combine(folder, "assets"), files.Values);
                File.WriteAllText(Path.Combine(folder, "page.html"), page.Html);
            }
            await session.Page.AddInitScriptAsync(Recorder);
            await session.Page.Context.RouteAsync(Origin + "/**", route => {
                var path = new Uri(route.Request.Url).AbsolutePath;
                if (path == "/page.html")
                    return route.FulfillAsync(new() {
                        Body = page.Html,
                        Headers = new Dictionary<string, string> { ["Content-Type"] = "text/html; charset=utf-8", ["Content-Security-Policy"] = Policy }
                    });
                if (files.TryGetValue(path, out var file))
                    return route.FulfillAsync(new() {
                        Body = file.Content,
                        Headers = new Dictionary<string, string> { ["Content-Type"] = file.ContentType + "; charset=utf-8", ["Content-Security-Policy"] = Policy }
                    });
                return route.FulfillAsync(new() { Status = 404, Body = "missing" });
            });
            await session.Page.GotoAsync(Origin + "/page.html", new() { WaitUntil = WaitUntilState.Load, Timeout = 20000 });
            await InteractiveChartBrowser.SettlePresentationAsync(session.Page);
            return session;
        } catch {
            await session.DisposeAsync();
            throw;
        }
    }

    /// <summary>Hovers, clicks, uses the keyboard, zooms, exports and enters the print view on the first
    /// <paramref name="rootSelector"/> element.</summary>
    internal static async Task InteractAsync(IPage page, string rootSelector) {
        var root = page.Locator(rootSelector).First;
        await root.ScrollIntoViewIfNeededAsync();
        var box = await root.BoundingBoxAsync() ?? throw new InvalidOperationException("No box for " + rootSelector);
        for (var row = 1; row <= 4; row++)
            for (var column = 1; column <= 6; column++)
                await page.Mouse.MoveAsync((float)(box.X + box.Width * column / 7), (float)(box.Y + box.Height * row / 5), new() { Steps = 2 });
        await page.Mouse.ClickAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        var focusable = root.Locator("[tabindex='0']").First;
        if (await focusable.CountAsync() > 0) {
            await focusable.FocusAsync();
            foreach (var key in new[] { "ArrowRight", "ArrowRight", "ArrowDown", "Enter", "Escape" }) await page.Keyboard.PressAsync(key);
        }
        await page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        await page.Mouse.WheelAsync(0, -240);
        await page.WaitForTimeoutAsync(150);
        // Export controls may sit in a closed menu; their click handlers are what matters here.
        await page.EvaluateAsync("""
            selector => document.querySelectorAll(selector + ' [data-cfx-export], ' + selector + ' [data-cfx-topology-export], ' + selector + ' [data-cfx-graph-action^="export-"]')
              .forEach(button => button.click())
            """, rootSelector);
        await page.WaitForTimeoutAsync(400);
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await page.WaitForTimeoutAsync(400);
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForTimeoutAsync(150);
    }

    /// <summary>Fails on any policy violation, any inline script or style that appeared at any time, and any inline
    /// element, handler or script URL in the current document.</summary>
    internal static async Task AssertCleanAsync(IPage page) {
        var violations = await page.EvaluateAsync<string[]>("() => window.__cfxCsp.violations");
        var inserted = await page.EvaluateAsync<string[]>("() => window.__cfxCsp.inline");
        var present = await page.EvaluateAsync<string[]>(Scan);
        var problems = violations.Concat(inserted).Concat(present).Distinct().ToArray();
        Assert.True(problems.Length == 0, string.Join(" | ", problems));
    }
}
