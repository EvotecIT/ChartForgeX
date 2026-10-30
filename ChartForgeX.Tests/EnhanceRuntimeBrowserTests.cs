using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises cfx-enhance.js in a real browser: every family, keyboard, tooltips and linked views.</summary>
public sealed class EnhanceRuntimeBrowserTests {
    private readonly ITestOutputHelper _output;
    public EnhanceRuntimeBrowserTests(ITestOutputHelper output) => _output = output;

    private static VisualRenderContext Context(double width = 640, double height = 300) => new(new VisualLayoutOptions(new VisualSize(width, height), 8));

    private static string Page(string body) => "<!doctype html><html><head><meta charset=\"utf-8\"><style>body{font-family:system-ui;margin:16px}section{max-width:720px}</style>"
        + "</head><body>" + body + "<table id=\"grid\"></table>" + HtmlEnhanceAssets.InlineMarkup() + "</body></html>";

    private static Chart Activity() {
        var day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        ChartPoint Point(int index, double value, string kind) => new ChartPoint(index + 1, value).WithMarkOptions(new ChartMarkOptions(
            keys: Enumerable.Range(0, (int)value).Select(n => kind + "-" + index + "-" + n),
            fields: new Dictionary<string, object> { ["day"] = day.AddDays(index), ["kind"] = kind }));
        return Chart.Create().WithStackedBars().WithLegend(true).WithXLabels("Thu", "Fri", "Sat", "Sun")
            .AddBar("Updates", new[] { 3d, 2, 4, 1 }.Select((v, i) => Point(i, v, "update")))
            .AddBar("Notes", new[] { 1d, 2, 1, 2 }.Select((v, i) => Point(i, v, "note")));
    }

    private static Chart Trend() {
        var start = new DateTime(2026, 10, 1);
        return Chart.Create().WithLegend(true).ConfigureXAxis(axis => axis.Scale = ChartScaleKind.Time)
            .AddLine("Updates", Enumerable.Range(0, 4).Select(i => new ChartPoint(start.AddDays(i).ToOADate(), new[] { 3, 2, 4, 1 }[i])))
            .AddLine("Notes", Enumerable.Range(0, 4).Select(i => new ChartPoint(start.AddDays(i).ToOADate(), new[] { 1, 2, 1, 2 }[i])));
    }

    private const string Smoke = """
        async () => {
          const rows = [];
          for (const s of document.querySelectorAll('svg[data-cfx-enhance]')) {
            const c = s.cfx, m = s.querySelector('[data-cfx-mark]'), name = s.id;
            if (!c || !m) { rows.push({ name, enhanced: !!c, marks: 0 }); continue; }
            s.scrollIntoView({ block: 'center' }); c.refresh();
            const g = c.svg.querySelector('[data-cfx-mark]').getBoundingClientRect(), b = (m.getAttribute('data-cfx-bounds') || '').split(' ').map(Number);
            let x = g.left + g.width / 2, y = g.top + g.height / 2;
            if (!g.width && b.length === 4) { const p = s.createSVGPoint(); p.x = b[0] + b[2] / 2; p.y = b[1] + b[3] / 2; const q = p.matrixTransform(s.getScreenCTM()); x = q.x; y = q.y; }
            const at = { bubbles: true, clientX: x, clientY: y, pointerType: 'mouse', button: 0 };
            m.dispatchEvent(new PointerEvent('pointermove', at));
            const tip = document.getElementById('cfx-e-tip'), tooltip = !!tip && !tip.hidden && tip.textContent.length > 0;
            m.dispatchEvent(new MouseEvent('click', Object.assign({ detail: 1 }, at)));
            const selected = s.querySelectorAll('.cfx-e-selected').length;
            m.focus();
            const seen = new Set([document.activeElement]);
            for (const k of ['ArrowRight', 'ArrowDown', 'ArrowLeft', 'ArrowUp', 'End', 'Home']) {
              document.activeElement.dispatchEvent(new KeyboardEvent('keydown', { key: k, bubbles: true })); seen.add(document.activeElement);
            }
            document.activeElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
            const legend = s.querySelector('[role="button"][aria-pressed]');
            if (legend) { legend.dispatchEvent(new MouseEvent('click', { bubbles: true })); legend.dispatchEvent(new MouseEvent('click', { bubbles: true })); }
            await c.setHighlight(['missing']); await c.clearHighlight();
            rows.push({ name, enhanced: true, marks: s.querySelectorAll('[data-cfx-mark]').length, behaviours: c.behaviours.join('+') || 'core',
              tooltip, selected, focusable: seen.size, legend: s.querySelectorAll('[role="button"][aria-pressed]').length,
              cleared: s.querySelectorAll('.cfx-e-selected').length === 0 });
          }
          return rows;
        }
        """;

    /// <summary>Coverage matrix, part 2: the runtime attaches to every family and leaves plain SVG alone.</summary>
    [BrowserFact]
    public async Task EveryFamily_GetsTheGenericBehavioursWithoutErrors() {
        if (!Enabled) return;
        var body = new StringBuilder();
        var plain = Chart.Create().AddBar("Plain", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) }).Prepare(Context()).ToSvg(new VisualSvgOptions("plain"));
        body.Append("<section id=\"plain-host\">").Append(plain).Append("</section>");
        foreach (var family in ExampleRecipes.ChartFamilies) {
            var id = "family-" + family.Key.ToString().ToLowerInvariant();
            var svg = family.Value.Create("wide", VisualThemeMode.Light).Prepare(Context(720, 420)).ToSvg(new VisualSvgOptions(id).WithEnhanceHooks(includeData: true));
            body.Append("<section>").Append(svg.Replace("<svg ", "<svg id=\"" + id + "\" ")).Append("</section>");
        }
        await using var session = await OpenAsync(Page(body.ToString()), 900, 700);
        var page = session.Page;
        var plainBefore = await page.EvaluateAsync<string>("() => document.querySelector('#plain-host svg').outerHTML");
        var rows = await page.EvaluateAsync<EnhanceRow[]>(Smoke);
        Assert.Equal(ExampleRecipes.ChartFamilies.Count(), rows.Length);
        foreach (var row in rows) {
            _output.WriteLine($"{row.Name,-26} marks {row.Marks,4}  {row.Behaviours,-10} tooltip {row.Tooltip}  select {row.Selected}  keyboard {row.Focusable}  legend {row.Legend}");
            Assert.True(row.Enhanced && row.Marks > 0, row.Name + " was not enhanced.");
            Assert.True(row.Tooltip, row.Name + " showed no tooltip.");
            Assert.True(row.Selected > 0 && row.Cleared, row.Name + " did not select and clear.");
            Assert.True(row.Focusable >= 1, row.Name + " has no keyboard target.");
        }
        Assert.Equal(plainBefore, await page.EvaluateAsync<string>("() => document.querySelector('#plain-host svg').outerHTML"));
        Assert.False(await page.EvaluateAsync<bool>("() => !!document.querySelector('#plain-host svg').cfx"));
        AssertNoConsoleErrors(session);
    }

    private const string StandIn = """
        () => {
          // A minimal LinkedView with CanopyX's method names, recording what the chart asks of it.
          const host = document.getElementById('grid');
          const grid = window.grid = { host, calls: [], filters: [],
            setExternalFilter(source, f) { this.calls.push(['filter', source, f]); this.filters = f ? [Object.assign({ source }, f)] : []; return Promise.resolve(true); },
            clearExternalFilters() { this.calls.push(['clearFilters']); return Promise.resolve(true); }, getExternalFilters() { return this.filters; },
            setHighlight(keys, o) { this.calls.push(['highlight', keys, o]); return Promise.resolve(keys.length); },
            clearHighlight() { this.calls.push(['clearHighlight']); return Promise.resolve(0); }, getHighlight() { return []; },
            startLinkEvents() { this.calls.push(['start']); return Promise.resolve(); } };
          window.echo = 0;
          const chart = CfxEnhance.get(document.getElementById('activity-host'));
          chart.svg.addEventListener('cfx:linkchange', () => window.echo++);
          CfxLink.connect(chart, grid, { field: 'day', sourceLabel: 'Activity chart', targetLabel: 'Records' });
          return chart.view;
        }
        """;

    [BrowserFact]
    public async Task LinkedViews_FilterBothWaysWithoutEcho() {
        if (!Enabled) return;
        var activity = Activity().Prepare(Context()).ToSvg(new VisualSvgOptions("activity").WithEnhanceHooks());
        await using var session = await OpenAsync(Page("<section id=\"activity-host\">" + activity + "</section>"), 900, 600);
        var page = session.Page;
        Assert.Equal("activity", await page.EvaluateAsync<string>(StandIn));
        const string segment = "#activity-host [data-cfx-mark][data-cfx-series='1'][data-cfx-point='1']";

        await page.Locator(segment).ClickAsync();
        var call = await page.EvaluateAsync<string>("() => JSON.stringify(grid.calls.filter(c => c[0] === 'filter').pop())");
        Assert.Contains("\"field\":\"day\"", call, StringComparison.Ordinal);
        Assert.Contains("2026-10-02T00:00:00.000Z", call, StringComparison.Ordinal);
        Assert.Contains("Fri · Notes", call, StringComparison.Ordinal);
        Assert.Equal("true", await page.Locator(segment).EvaluateAsync<string>("e => String(e.classList.contains('cfx-e-selected'))"));
        Assert.Equal(1, await page.Locator("#activity-host .cfx-e-sel").CountAsync());

        // Grid selection highlights the marks holding those rows, and applied links never re-emit.
        var before = await page.EvaluateAsync<int>("() => echo");
        await page.EvaluateAsync("() => grid.host.dispatchEvent(new CustomEvent('canopyx:linkchange', { detail: { view: 'records', action: 'select', scope: 'select', keys: ['update-2-0', 'note-2-0'], values: {}, label: '' } }))");
        Assert.Equal(2, await page.Locator("#activity-host .cfx-e-linked").CountAsync());
        Assert.Equal(2, await page.Locator("#activity-host .cfx-e-bar").CountAsync());
        await page.EvaluateAsync("() => grid.host.dispatchEvent(new CustomEvent('canopyx:linkchange', { detail: { view: 'records', action: 'filter', scope: 'filter', keys: [], values: { kind: ['note'] }, label: 'note' } }))");
        Assert.Equal(4, await page.Locator("#activity-host .cfx-e-filtered").CountAsync());
        Assert.Equal(before, await page.EvaluateAsync<int>("() => echo"));

        // Removing the chart's chip in the grid is the reader's choice; the chart drops its selection silently.
        await page.EvaluateAsync("() => grid.host.dispatchEvent(new CustomEvent('canopyx:externalfilter', { detail: { filters: [], reason: 'user' } }))");
        Assert.Equal(0, await page.Locator("#activity-host .cfx-e-selected").CountAsync());
        Assert.Equal(before, await page.EvaluateAsync<int>("() => echo"));

        // Keyboard: one tab stop, arrows move, Space selects, Escape clears.
        await page.Locator(segment).FocusAsync();
        Assert.Equal(1, await page.Locator("#activity-host [data-cfx-mark][tabindex='0']").CountAsync());
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("2", await page.EvaluateAsync<string>("() => document.activeElement.getAttribute('data-cfx-point')"));
        Assert.Equal("cfx-e-tip", await page.EvaluateAsync<string>("() => document.activeElement.getAttribute('aria-describedby')"));
        Assert.Equal("tooltip", await page.EvaluateAsync<string>("() => document.getElementById('cfx-e-tip').getAttribute('role')"));
        Assert.Contains("Notes", await page.EvaluateAsync<string>("() => document.getElementById('cfx-e-tip').textContent"), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(1, await page.Locator("#activity-host .cfx-e-selected").CountAsync());
        await page.Keyboard.PressAsync("Escape");
        Assert.Equal(0, await page.Locator("#activity-host .cfx-e-selected").CountAsync());
        Assert.Equal("[\"filter\",\"activity\",null]", await page.EvaluateAsync<string>("() => JSON.stringify(grid.calls.filter(c => c[0] === 'filter').pop())"));

        // Legend entries are toggle buttons; Shift isolates.
        var legend = page.Locator("#activity-host [data-cfx-role='legend-entry'][data-cfx-legend-for='0']");
        await legend.ClickAsync();
        Assert.Equal("false", await legend.GetAttributeAsync("aria-pressed"));
        await legend.ClickAsync(new LocatorClickOptions { Modifiers = new[] { KeyboardModifier.Shift } });
        Assert.Equal("true", await legend.GetAttributeAsync("aria-pressed"));
        Assert.Equal("false", await page.Locator("#activity-host [data-cfx-legend-for='1']").GetAttributeAsync("aria-pressed"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task Brush_OnOneChartFiltersAnotherChart() {
        if (!Enabled) return;
        var trend = Trend().Prepare(Context()).ToSvg(new VisualSvgOptions("trend").WithEnhanceHooks(includeData: true));
        var activity = Activity().Prepare(Context()).ToSvg(new VisualSvgOptions("activity").WithEnhanceHooks());
        await using var session = await OpenAsync(Page("<section id=\"trend-host\">" + trend + "</section><section id=\"activity-host\">" + activity + "</section>"), 900, 900);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.disconnect = CfxLink.connect(CfxEnhance.get(document.getElementById('trend-host')), CfxEnhance.get(document.getElementById('activity-host')), { field: 'day' }); }");
        var plot = await page.EvaluateAsync<double[]>("() => { const s = document.querySelector('#trend-host svg'), r = s.getBoundingClientRect(), p = s.getAttribute('data-cfx-plot').split(' ').map(Number), w = Number(s.getAttribute('data-cfx-size').split(' ')[0]); return [r.left + p[0] * r.width / w, r.top + (p[1] + p[3] / 2) * r.height / Number(s.getAttribute('data-cfx-size').split(' ')[1]), p[2] * r.width / w]; }");

        // Hovering shows the shared-X tooltip with both series and a crosshair.
        await page.Mouse.MoveAsync((float)(plot[0] + plot[2] / 3), (float)plot[1], new MouseMoveOptions { Steps = 3 });
        Assert.Equal(1, await page.Locator("#trend-host .cfx-e-cross").CountAsync());
        var tip = await page.EvaluateAsync<string>("() => document.getElementById('cfx-e-tip').textContent");
        Assert.Contains("Updates", tip, StringComparison.Ordinal); Assert.Contains("Notes", tip, StringComparison.Ordinal);

        // Drag across the first two days.
        await page.Mouse.MoveAsync((float)(plot[0] - 4), (float)plot[1]);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(plot[0] + plot[2] * .4), (float)plot[1], new MouseMoveOptions { Steps = 6 });
        await page.Mouse.UpAsync();
        Assert.Equal("visible", await page.Locator("#trend-host .cfx-e-brush").GetAttributeAsync("visibility"));
        var filter = await page.EvaluateAsync<string>("() => JSON.stringify(CfxEnhance.get(document.getElementById('activity-host')).getExternalFilters())");
        Assert.Contains("\"range\":[\"2026-10-01T00:00:00.000Z\",\"2026-10-02T23:59:59.999Z\"]", filter, StringComparison.Ordinal);
        // Thursday and Friday stay; Saturday and Sunday dim in both series.
        Assert.Equal(4, await page.Locator("#activity-host .cfx-e-filtered").CountAsync());
        Assert.Equal(0, await page.Locator("#activity-host [data-cfx-point='0'].cfx-e-filtered").CountAsync());
        AssertNoConsoleErrors(session);
    }

    /// <summary>The documented worked example: a host behaviour registered after load reaches charts already enhanced.</summary>
    [BrowserFact]
    public async Task HostBehaviour_AttachesThroughThePublicRegistry() {
        if (!Enabled) return;
        var calendar = ExampleRecipes.ForKind(ChartSeriesKind.CalendarHeatmap).Create("wide", VisualThemeMode.Light).Prepare(Context(720, 300))
            .ToSvg(new VisualSvgOptions("calendar").WithEnhanceHooks());
        await using var session = await OpenAsync(Page("<section id=\"calendar-host\">" + calendar + "</section>"), 900, 600);
        var page = session.Page;
        var attached = await page.EvaluateAsync<string>("""
            () => {
              CfxEnhance.behaviour('broken', { match: () => true, attach: () => { throw new Error('host bug'); } });
              CfxEnhance.behaviour('week-jump', {
                match: kit => !!kit.roles['calendar-cell'],
                attach: kit => ({ key(e, mark) {
                  if (e.key !== 'PageDown' && e.key !== 'PageUp') return;
                  const cells = kit.bySeries[mark.s].filter(kit.visible), i = cells.indexOf(mark);
                  return cells[Math.max(0, Math.min(cells.length - 1, i + (e.key === 'PageDown' ? 7 : -7)))];
                } })
              });
              return CfxEnhance.get(document.getElementById('calendar-host')).behaviours.join('+');
            }
            """);
        Assert.Equal("grid+week-jump", attached);
        await page.Locator("#calendar-host [data-cfx-mark]").First.FocusAsync();
        var start = await page.EvaluateAsync<string>("() => document.activeElement.getAttribute('data-cfx-date')");
        await page.Keyboard.PressAsync("PageDown");
        var moved = await page.EvaluateAsync<string>("() => document.activeElement.getAttribute('data-cfx-date')");
        Assert.Equal(DateTime.Parse(start).AddDays(7), DateTime.Parse(moved));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.NotEqual(moved, await page.EvaluateAsync<string>("() => document.activeElement.getAttribute('data-cfx-date')"));
    }

    private sealed class EnhanceRow {
        public string Name { get; set; } = "";
        public bool Enhanced { get; set; }
        public int Marks { get; set; }
        public string Behaviours { get; set; } = "";
        public bool Tooltip { get; set; }
        public int Selected { get; set; }
        public int Focusable { get; set; }
        public int Legend { get; set; }
        public bool Cleared { get; set; }
    }
}
