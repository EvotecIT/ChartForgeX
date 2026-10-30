using System.Globalization;
using System.Text.Json;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>
/// The linked-views example in a real browser: enhanced charts of several families and a real CanopyX grid (CanopyX.Html,
/// referenced by the examples only) follow each other through the page's CfxLink.connect calls.
/// </summary>
public sealed class EnhanceCanopyXBrowserTests {
    private const string Grid = "#records [data-canopyx-host]";
    private const string Segment = "#week [data-cfx-mark][data-cfx-series='0'][data-cfx-point='5']";

    private static async Task<HtmlBrowserSession> OpenExampleAsync(string output, int width, int height, string? theme = null) {
        var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
            Browser = Engine, Headless = true, Timeout = 15000, ViewportWidth = width, ViewportHeight = height
        });
        try {
            var errors = new List<string>();
            session.Page.PageError += (_, error) => errors.Add(error);
            session.Page.Console += (_, message) => { if (message.Type == "error") errors.Add(message.Text); };
            if (theme != null) await session.Page.EmulateMediaAsync(new() { ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light });
            await session.Page.GotoAsync(new Uri(Path.Combine(output, "enhance", "index.html")).AbsoluteUri);
            await session.Page.WaitForFunctionAsync("s => { const h = document.querySelector(s); return h && h.canopyx && h.querySelector('.cx-row'); }", Grid);
            Assert.Empty(errors);
            return session;
        } catch {
            await session.DisposeAsync();
            throw;
        }
    }

    private static string Output() {
        var output = Path.Combine(Path.GetTempPath(), "cfx-canopyx-" + Guid.NewGuid().ToString("N"));
        EnhanceExamples.Write(output);
        return output;
    }

    private static Task<JsonElement> StateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        s => {
          const host = document.querySelector(s), grid = host.canopyx;
          const chart = id => { const svg = document.querySelector('#' + id + ' > svg');
            return { selected: svg.querySelectorAll('.cfx-e-selected').length, selectedKeys: [...new Set([...svg.querySelectorAll('.cfx-e-selected')].flatMap(m => JSON.parse(m.getAttribute('data-cfx-keys') || '[]')))], linked: [...svg.querySelectorAll('.cfx-e-linked')].map(m => JSON.parse(m.getAttribute('data-cfx-keys') || '[]')) }; };
          return { filters: grid.getExternalFilters(), chips: [...host.querySelectorAll('.cx-link-chip')].map(c => c.textContent.replace('×', '').trim()),
            rows: [...host.querySelectorAll('.cx-row')].map(r => r.getAttribute('data-cx-id')),
            week: chart('week'), trend: chart('trend'), heatmap: chart('heatmap'), support: chart('support') };
        }
        """, Grid);

    private static async Task ClickCenterAsync(IPage page, string selector) {
        var mark = page.Locator(selector).First;
        await mark.ScrollIntoViewIfNeededAsync();
        var box = await mark.BoundingBoxAsync() ?? throw new InvalidOperationException("No box for " + selector);
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    private static async Task BrushTrendAsync(IPage page, float from, float to) {
        var svg = page.Locator("#trend > svg");
        await svg.ScrollIntoViewIfNeededAsync();
        var box = await svg.BoundingBoxAsync() ?? throw new InvalidOperationException("No trend chart box.");
        await page.Mouse.MoveAsync(box.X + box.Width * from, box.Y + box.Height * .5f);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width * to, box.Y + box.Height * .5f, new() { Steps = 8 });
        await page.Mouse.UpAsync();
    }

    private static Task WaitForChipAsync(IPage page, bool present) =>
        page.WaitForFunctionAsync("([s, present]) => !!document.querySelector(s).querySelector('.cx-link-chip') === present", new object[] { Grid, present });

    private static string[] Strings(JsonElement array) => array.EnumerateArray().Select(item => item.GetString()!).ToArray();

    /// <summary>A column segment filters the grid by its day and kind with CanopyX's chip, grid rows highlight marks in
    /// several families, brushing the trend applies a date range and highlights the heatmap, and removing a chip clears the chart.</summary>
    [BrowserFact]
    public async Task ChartsAndCanopyXGrid_FollowEachOther() {
        if (!Enabled) return;
        var output = Output();
        await using var session = await OpenExampleAsync(output, 1200, 900);
        var page = session.Page;

        // Chart to grid: the Updates segment of Thu 29 Oct filters the grid to exactly its records.
        var keys = JsonSerializer.Deserialize<string[]>(await page.Locator(Segment).First.GetAttributeAsync("data-cfx-keys") ?? "[]")!;
        Assert.NotEmpty(keys);
        await ClickCenterAsync(page, Segment);
        await WaitForChipAsync(page, true);
        var state = await StateAsync(page);
        var filters = state.GetProperty("filters").EnumerateArray().ToArray();
        Assert.Equal(new[] { "day", "kind" }, filters.Select(f => f.GetProperty("field").GetString()));
        Assert.All(filters, f => Assert.Equal("week", f.GetProperty("source").GetString()));
        Assert.Equal("2026-10-29T00:00:00.000Z", filters[0].GetProperty("values")[0].GetString());
        Assert.Equal("Update", filters[1].GetProperty("values")[0].GetString());
        Assert.Equal("Filtered by Changes this week: Thu 29; Updates", Assert.Single(Strings(state.GetProperty("chips"))));
        Assert.Equal(keys.OrderBy(k => k, StringComparer.Ordinal), Strings(state.GetProperty("rows")).OrderBy(k => k, StringComparer.Ordinal));
        Assert.Equal(1, state.GetProperty("week").GetProperty("selected").GetInt32());

        // Removing the chip clears the chart's selection, and the grid shows every record again.
        await page.Locator(Grid + " .cx-link-chip").ClickAsync();
        await WaitForChipAsync(page, false);
        state = await StateAsync(page);
        Assert.Empty(state.GetProperty("filters").EnumerateArray());
        Assert.Equal(0, state.GetProperty("week").GetProperty("selected").GetInt32());

        // Grid to charts: two selected rows mark the marks that hold them in the trend, heatmap and schedule families.
        // Rows are pooled, so pick them by their row index rather than their DOM order.
        ILocator Check(int row) => page.Locator(Grid + " .cx-row[aria-rowindex=\"" + row + "\"] input[type=checkbox]");
        await Check(2).ClickAsync();
        await Check(5).ClickAsync();
        await page.WaitForFunctionAsync("s => document.querySelector(s).querySelectorAll('.cx-row[aria-selected=\"true\"]').length === 2", Grid);
        var picked = await page.EvaluateAsync<string[]>("s => [...document.querySelector(s).querySelectorAll('.cx-row[aria-selected=\"true\"]')].map(r => r.getAttribute('data-cx-id'))", Grid);
        Assert.Equal(2, picked.Length);
        state = await StateAsync(page);
        foreach (var family in new[] { "trend", "heatmap", "support" }) {
            var linked = state.GetProperty(family).GetProperty("linked").EnumerateArray().Select(Strings).ToArray();
            Assert.NotEmpty(linked);
            Assert.All(linked, marks => Assert.True(marks.Intersect(picked).Any(), family + " marks a mark without a selected record."));
            Assert.Equal(picked.OrderBy(k => k, StringComparer.Ordinal), linked.SelectMany(m => m).Intersect(picked).Distinct().OrderBy(k => k, StringComparer.Ordinal));
        }
        Assert.Empty(state.GetProperty("filters").EnumerateArray());

        // Brushing the trend filters the grid by an inclusive date range and highlights the same records in the heatmap.
        await Check(2).ClickAsync();
        await Check(5).ClickAsync();
        await BrushTrendAsync(page, .3f, .45f);
        await WaitForChipAsync(page, true);
        state = await StateAsync(page);
        var range = Assert.Single(state.GetProperty("filters").EnumerateArray());
        Assert.Equal("trend", range.GetProperty("source").GetString());
        Assert.Equal("day", range.GetProperty("field").GetString());
        var bounds = range.GetProperty("range").EnumerateArray().Select(v => DateTime.Parse(v.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)).ToArray();
        Assert.True(bounds[0] < bounds[1]);
        Assert.Equal(bounds[1].Date.AddDays(1).AddMilliseconds(-1), bounds[1]);
        Assert.StartsWith("Filtered by Daily trend: ", Assert.Single(Strings(state.GetProperty("chips"))), StringComparison.Ordinal);
        // The grid renders only its visible rows; each one is a brushed record, and every highlighted cell holds brushed records.
        var brushed = Strings(state.GetProperty("trend").GetProperty("selectedKeys"));
        var shown = Strings(state.GetProperty("rows"));
        Assert.NotEmpty(shown);
        Assert.All(shown, key => Assert.Contains(key, brushed));
        var heat = state.GetProperty("heatmap").GetProperty("linked").EnumerateArray().Select(Strings).ToArray();
        Assert.NotEmpty(heat);
        Assert.All(heat, cell => Assert.True(cell.Intersect(brushed).Any(), "A highlighted heatmap cell holds no brushed record."));
        Assert.True(state.GetProperty("trend").GetProperty("selected").GetInt32() > 0);

        // No echo: filters and highlights applied to the grid never come back as its own link change.
        var echoes = await page.EvaluateAsync<int>("s => { let n = 0; const h = document.querySelector(s); h.addEventListener('canopyx:linkchange', () => n++); "
            + "return h.canopyx.setExternalFilter('trend', null).then(() => h.canopyx.setHighlight(['r001'])).then(() => n); }", Grid);
        Assert.Equal(0, echoes);
        Directory.Delete(output, true);
    }

    /// <summary>Writes review screenshots at 1200px and 390px in both schemes when CFX_CANOPYX_SHOTS names an absolute folder.</summary>
    [BrowserFact]
    public async Task ReviewScreenshots() {
        var folder = Environment.GetEnvironmentVariable("CFX_CANOPYX_SHOTS");
        if (!Enabled || string.IsNullOrWhiteSpace(folder)) return;
        if (!Path.IsPathRooted(folder)) throw new InvalidOperationException("CFX_CANOPYX_SHOTS must be an absolute folder.");
        Directory.CreateDirectory(folder);
        var output = Output();
        foreach (var theme in new[] { "light", "dark" }) foreach (var width in new[] { 1200, 390 }) {
            await using var session = await OpenExampleAsync(output, width, 900, theme);
            var page = session.Page;
            var name = Path.Combine(folder, width + "-" + theme);
            await page.ScreenshotAsync(new() { Path = name + "-rest.png", FullPage = true });
            await ClickCenterAsync(page, Segment);
            await WaitForChipAsync(page, true);
            await page.Mouse.MoveAsync(1, 1);
            await page.ScreenshotAsync(new() { Path = name + "-segment.png", FullPage = true });
            await page.Locator(Grid + " .cx-link-chip").ClickAsync();
            // Rows are pooled, so pick them by their row index rather than their DOM order.
        ILocator Check(int row) => page.Locator(Grid + " .cx-row[aria-rowindex=\"" + row + "\"] input[type=checkbox]");
            await Check(2).ClickAsync();
            await Check(5).ClickAsync();
            await page.Mouse.MoveAsync(1, 1);
            await page.ScreenshotAsync(new() { Path = name + "-rows.png", FullPage = true });
            await Check(2).ClickAsync();
            await Check(5).ClickAsync();
            await BrushTrendAsync(page, .3f, .45f);
            await page.Mouse.MoveAsync(1, 1);
            await page.ScreenshotAsync(new() { Path = name + "-brush.png", FullPage = true });
        }
        Directory.Delete(output, true);
    }
}
