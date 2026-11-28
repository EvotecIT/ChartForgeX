using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Sliding-window contracts for the browser data renderer: bounded marks, stable identities and retained state.</summary>
public sealed class DataRendererStreamingBrowserTests {
    internal static string Page => "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">"
        + "<style>body{font-family:system-ui}#chart{width:640px}</style></head><body>"
        + "<div id=\"chart\"><span id=\"retained\">Host content</span></div><script>"
        + HtmlDataChartAssets.Script.Replace("</", "<\\/") + "</script></body></html>";

    // A two-series temporal line with `size` one-second periods; `tick(n)` builds the change for the n-th next period.
    internal const string Window = """
        size => {
          const start = Date.UTC(2026, 0, 1), at = index => new Date(start + index * 1000).toISOString();
          const category = index => ({ id: 't' + index, label: 'T' + index, instant: at(index) });
          const wave = (index, phase) => Math.round(Math.sin(index / 20 + phase) * 400 + 500);
          window.tick = index => ({ categories: [category(index)], values: { a: [wave(index, 0)], b: [wave(index, 1)] } });
          window.selected = [];
          window.chart = ChartForgeXRenderer.create(document.getElementById('chart'), {
            type: 'line', label: 'Replication latency',
            categories: Array.from({ length: size }, (_, index) => category(index)),
            series: [{ id: 'a', name: 'DC1', values: Array.from({ length: size }, (_, index) => wave(index, 0)) },
                     { id: 'b', name: 'DC2', values: Array.from({ length: size }, (_, index) => wave(index, 1)) }]
          }, { onSelect: mark => selected.push(mark) });
        }
        """;

    [BrowserFact]
    public async Task SlidingWindowKeepsMarksBoundedAndRetainsIdsSelectionAndFocus() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync(Window, 40);
        var target = page.Locator("[data-cfx-series='b'][data-cfx-category='t30']");
        await target.FocusAsync();
        await target.PressAsync("Enter");
        var nodes = await page.EvaluateAsync<int>("()=>document.getElementById('chart').getElementsByTagName('*').length");
        // Each append drops the oldest period; t30 stays in the window for the first 30 updates.
        var state = await page.EvaluateAsync<string>("""
            () => {
              for (let index = 40; index < 70; index++) chart.append(tick(index), { maximum: 40 });
              const ids = [...document.querySelectorAll("[data-cfx-series='a']")].map(node => node.dataset.cfxCategory);
              const pressed = [...document.querySelectorAll("[aria-pressed='true']")].map(node => node.dataset.cfxSeries + '/' + node.dataset.cfxCategory);
              return [ids.length, ids[0], ids[39], pressed.join(), chart.selection()?.categoryId,
                document.activeElement.dataset.cfxSeries + '/' + document.activeElement.dataset.cfxCategory].join('|');
            }
            """);
        Assert.Equal("40|t30|t69|b/t30|t30|b/t30", state);
        Assert.Equal(nodes, await page.EvaluateAsync<int>("()=>document.getElementById('chart').getElementsByTagName('*').length"));
        // Once t30 leaves the window the selection is released and focus moves to the oldest DC2 point.
        state = await page.EvaluateAsync<string>("""
            () => {
              for (let index = 70; index < 300; index++) chart.append(tick(index), { maximum: 40 });
              const stops = [...document.querySelectorAll("[tabindex='0']")];
              return [document.querySelectorAll('[data-cfx-category]').length, document.querySelectorAll("[aria-pressed='true']").length,
                chart.selection(), document.activeElement.dataset.cfxSeries + '/' + document.activeElement.dataset.cfxCategory,
                stops.length, document.getElementById('chart').children.length].join('|');
            }
            """);
        Assert.Equal("80|0||b/t260|1|2", state);
        Assert.Equal(nodes, await page.EvaluateAsync<int>("()=>document.getElementById('chart').getElementsByTagName('*').length"));
        Assert.Equal("Host content", await page.Locator("#retained").TextContentAsync());
        Assert.Single(await page.EvaluateAsync<string[]>("()=>selected.map(mark=>mark.categoryId)"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task RovingTabStopSurvivesUpdatesWhenFocusIsElsewhere() {
        await using var session = await OpenAsync(Page + "<button id=\"outside\">Outside</button>");
        var page = session.Page;
        await page.EvaluateAsync(Window, 10);
        await page.Locator("[data-cfx-series='a'][data-cfx-category='t0']").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Locator("#outside").FocusAsync();
        await page.EvaluateAsync("()=>{chart.append(tick(10),{maximum:10});chart.append(tick(11),{maximum:10});}");
        Assert.Equal(new[] { "a/t2" }, await page.EvaluateAsync<string[]>(
            "()=>[...document.querySelectorAll(\"#chart [tabindex='0']\")].map(node=>node.dataset.cfxSeries+'/'+node.dataset.cfxCategory)"));

        await page.Locator("[data-cfx-series='b'][data-cfx-category='t4']").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal(1, await page.Locator("#chart [tabindex='0']").CountAsync());
        await page.Locator("#outside").FocusAsync();
        await page.EvaluateAsync("()=>chart.append(tick(12),{maximum:10})");
        Assert.Equal(new[] { "b/t5" }, await page.EvaluateAsync<string[]>(
            "()=>[...document.querySelectorAll(\"#chart [tabindex='0']\")].map(node=>node.dataset.cfxSeries+'/'+node.dataset.cfxCategory)"));

        await page.EvaluateAsync("""
            () => {
              chart.batch(live => {
                for (const id of ['a', 'b']) live.replaceSeries({ id, name: id, values: Array(10).fill(null) });
              });
              chart.append({ categories: tick(13).categories, values: {} }, { maximum: 10 });
            }
            """);
        Assert.Equal(1, await page.Locator("#chart svg[tabindex='0']").CountAsync());
        Assert.Equal("outside", await page.Locator(":focus").GetAttributeAsync("id"));
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.Equal(1, await page.Locator("#chart svg:focus").CountAsync());
        await page.EvaluateAsync("()=>chart.append(tick(14),{span:1})");
        Assert.Equal(1, await page.Locator("#chart [tabindex='0']").CountAsync());
        Assert.Equal("t14", await page.Locator(":focus").GetAttributeAsync("data-cfx-category"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SpanDropsPeriodsOlderThanTheNewestInstant() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync(Window, 20);
        var result = await page.EvaluateAsync<string>("""
            () => {
              chart.append(tick(25), { span: 10000 });
              const ids = [...document.querySelectorAll("[data-cfx-series='a']")].map(node => node.dataset.cfxCategory);
              const rejected = [{ span: 0 }, { span: Infinity }, { span: '5' }, { maximum: 0 }].every(settings => {
                try { chart.append(tick(26), settings); return false; } catch (error) { return error instanceof RangeError; }
              });
              const original = document.querySelector('#chart svg');
              const rejectedDiscarded = [null, 'invalid', tick(24).categories[0].instant].every(instant => {
                const oldest = { id: 'late', label: 'Late sample', instant };
                const newest = tick(26).categories[0];
                try { chart.append({ categories: [oldest, newest], values: { a: [1, 2] } }, { span: 10000, maximum: 1 }); return false; }
                catch (error) { return error instanceof TypeError && document.querySelector('#chart svg') === original; }
              });
              return [ids.length, ids[0], ids.at(-1), rejected, rejectedDiscarded].join('|');
            }
            """);
        // Periods t15..t19 lie within 10 s of t25; earlier ones are dropped.
        Assert.Equal("6|t15|t25|true|true", result);
        var untimed = await page.EvaluateAsync<bool>("""
            () => {
              const host = document.createElement('div'); document.body.appendChild(host);
              const plain = ChartForgeXRenderer.create(host, { type: 'column', label: 'Plain',
                categories: [{ id: 'x', label: 'X' }], series: [{ id: 's', name: 'S', values: [1] }] });
              try { plain.append({ categories: [{ id: 'y', label: 'Y' }], values: { s: [2] } }, { span: 1000 }); return false; }
              catch (error) { return error instanceof TypeError && host.querySelectorAll('[data-cfx-category]').length === 1; }
            }
            """);
        Assert.True(untimed);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task BatchDrawsOnceAndDiscardsChangesWhenTheCallbackThrows() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync(Window, 10);
        var result = await page.EvaluateAsync<string>("""
            () => {
              const host = document.getElementById('chart'), original = host.querySelector('svg');
              let draws = 0;
              const observer = new MutationObserver(records => draws += records.filter(record => record.addedNodes.length).length);
              observer.observe(host, { childList: true });
              let rejectedStep = false, rejectedDomain = false;
              chart.batch(live => {
                live.append(tick(10), { maximum: 10 }).append(tick(11), { maximum: 10 });
                try { live.append(tick(12), { maximum: -1 }); } catch { rejectedStep = true; }
                try { live.replaceSeries({ id: 'a', name: 'DC1', values: [Number.MAX_VALUE, -Number.MAX_VALUE, ...Array(8).fill(0)] }); }
                catch (error) { rejectedDomain = error instanceof RangeError; }
                live.replaceSeries({ id: 'b', name: 'DC2 (ms)', values: Array(10).fill(7) });
              });
              const afterBatch = [...host.querySelectorAll("[data-cfx-series='b']")].map(node => node.getAttribute('aria-label'));
              const kept = host.querySelector('svg');
              let thrown = false;
              try { chart.batch(live => { live.append(tick(12), { maximum: 10 }); throw new Error('host failure'); }); }
              catch (error) { thrown = error.message === 'host failure'; }
              const after = document.querySelectorAll("[data-cfx-series='a']");
              return new Promise(done => queueMicrotask(() => {
                observer.disconnect();
                done([draws, kept !== original, rejectedStep, rejectedDomain, afterBatch[0], thrown, host.querySelector('svg') === kept,
                  after[0].dataset.cfxCategory, after[9].dataset.cfxCategory].join('|'));
              }));
            }
            """);
        Assert.Equal("1|true|true|true|DC2 (ms), T2: 7|true|true|t2|t11", result);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SlidingWindowRendersUnderStrictContentSecurityPolicy() {
        const string pageHtml = """
            <!doctype html><html><head>
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src https://assets.example; style-src 'none'">
            <script src="https://assets.example/cfx-renderer.js"></script>
            </head><body><div id="chart"></div></body></html>
            """;
        await using var session = await OpenAsync(pageHtml, configurePageAsync: page =>
            page.RouteAsync("https://assets.example/cfx-renderer.js", route => route.FulfillAsync(new() {
                ContentType = "text/javascript", Body = HtmlDataChartAssets.Script
            })));
        var page = session.Page;
        await page.EvaluateAsync(Window, 2);
        await page.EvaluateAsync("()=>{chart.append(tick(2),{maximum:2});}");
        Assert.Equal("block|rgb(51, 65, 85)|pointer|0", await page.EvaluateAsync<string>("""
            () => {
              const svg = document.querySelector('#chart svg'), mark = svg.querySelector('[role=button]');
              return [getComputedStyle(svg).display, getComputedStyle(svg).color, getComputedStyle(mark).cursor,
                document.querySelectorAll('#chart [style]').length].join('|');
            }
            """));
        AssertNoConsoleErrors(session);
    }
}
