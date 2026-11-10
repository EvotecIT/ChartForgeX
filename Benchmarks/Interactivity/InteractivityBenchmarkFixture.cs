using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using HtmlTinkerX;
using Microsoft.Playwright;

/// <summary>Measures public pointer-move hover acquisition in the shipped runtime, with its animation frames flushed in-page.</summary>
public sealed class InteractivityBenchmarkFixture : IDisposable {
    private readonly HtmlBrowserSession _session;
    private readonly int _count;
    private readonly int _queries;
    private int[]? _last;
    private readonly int[] _expected;
    private bool _publicHoverProven;

    /// <summary>Prepares the chart and persistent browser outside PowerForge's measured operation.</summary>
    public InteractivityBenchmarkFixture(int count, int queries) {
        _count = count; _queries = queries;
        var chart = Chart.Create().WithSize(1000, 600).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddScatter("Measurements", Enumerable.Range(0, count).Select(index => new ChartPoint(index, Math.Sin(index * .19) * 10 + index % 37)));
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Range = HtmlChartTooltipRange.Nearest;
        });
        // The shipped runtime is minified and private; the page is measured unchanged through its public pointer events.
        _session = HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
            Browser = HtmlBrowserEngine.Chromium, Headless = true, Timeout = 30000,
            ViewportWidth = 1100, ViewportHeight = 800
        }).GetAwaiter().GetResult();
        try {
            _session.Page.SetContentAsync(html).GetAwaiter().GetResult();
            _session.Page.WaitForFunctionAsync("() => document.querySelector('.cfx-interactive-chart')?.dataset.cfxMounted === 'true' || !!document.querySelector('.cfx-tooltip')").GetAwaiter().GetResult();
            _expected = _session.Page.EvaluateAsync<int[]>("""
                ({ count, queries }) => {
                  const root = document.querySelector('.cfx-interactive-chart'), stage = root.querySelector('.cfx-stage');
                  const points = [...root.querySelectorAll('g[data-cfx-role="point"][data-cfx-point]')].map(node => {
                    const box = node.getBoundingClientRect();
                    return { node, source: Number(node.dataset.cfxSourcePoint ?? node.dataset.cfxPoint),
                      x: box.left + box.width / 2, y: box.top + box.height / 2 };
                  });
                  if (points.length !== count || points.some((point, index) => point.source !== index)) throw new Error('Point count or source identity changed');
                  const probes = Array.from({ length: queries }, (_, index) => {
                    const point = points[(index * 997 + 137) % count];
                    return { x: point.x + 2, y: point.y + 3 };
                  });
                  const expected = probes.map(probe => points.reduce((best, point) => {
                    const distance = Math.hypot(point.x - probe.x, point.y - probe.y);
                    return !best || distance < best.distance ? { source: point.source, distance } : best;
                  }, null).source);
                  window.cfxBenchmark = { root, stage, probes, expected, points };
                  root.addEventListener('cfxhover', event => { window.cfxBenchmark.hover = event.detail.target; });
                  root.addEventListener('cfxhoverclear', () => { window.cfxBenchmark.hover = null; });
                  return expected;
                }
                """, new { count, queries }).GetAwaiter().GetResult();
            ValidatePublicHover();
        } catch {
            _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }
    }

    /// <summary>Resets only the result buffer; source data and browser startup remain outside timing.</summary>
    public void Reset() => _last = null;

    /// <summary>
    /// Dispatches one deterministic pointer move per query on the plot stage, so the runtime searches for the nearest observation instead of
    /// taking an exact marker hit, and runs the frames it requests synchronously. Each query covers acquisition and hover commit.
    /// </summary>
    public void Execute() => _last = _session.Page.EvaluateAsync<int[]>("""
        () => {
          const fixture = window.cfxBenchmark, request = window.requestAnimationFrame, cancel = window.cancelAnimationFrame;
          const frames = new Map(); let next = 0;
          window.requestAnimationFrame = callback => { frames.set(++next, callback); return next; };
          window.cancelAnimationFrame = id => { frames.delete(id); };
          try {
            return fixture.probes.map(probe => {
              fixture.stage.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, composed: true, cancelable: true,
                clientX: probe.x, clientY: probe.y, pointerId: 1, pointerType: 'mouse', isPrimary: true }));
              for (let pass = 0; pass < 8 && frames.size; pass++) {
                const pending = [...frames.values()], time = performance.now();
                frames.clear(); pending.forEach(callback => callback(time));
              }
              const hover = fixture.hover;
              return hover ? Number(hover.sourcePoint) : -1;
            });
          } finally {
            window.requestAnimationFrame = request; window.cancelAnimationFrame = cancel;
          }
        }
        """).GetAwaiter().GetResult();

    /// <summary>Checks every inferred result against an independent geometry oracle and retains console failures.</summary>
    public void Validate() {
        if (_last == null || _last.Length != _queries || !_last.SequenceEqual(_expected))
            throw new InvalidOperationException("Pointer hover results differ from the independently prepared geometry oracle: expected ["
                + string.Join(",", _expected) + "], actual [" + string.Join(",", _last ?? Array.Empty<int>()) + "].");
        if (!_publicHoverProven) throw new InvalidOperationException("Public pointer hover was not proven.");
        var errors = _session.ConsoleLog.Where(message => message.Type == HtmlConsoleMessageType.Error).Select(message => message.Text).ToArray();
        if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors));
    }

    /// <summary>Gets the independently checked observation count.</summary>
    public int Points => _count;
    /// <summary>Gets the number of acquisition queries per timed operation.</summary>
    public int Queries => _queries;
    /// <summary>Gets a compact result checksum without replacing complete per-query validation.</summary>
    public int SourceIndexSum => _last?.Sum() ?? 0;
    /// <summary>Gets the actual browser build used for this case.</summary>
    public string BrowserVersion => _session.Browser?.Version ?? "Unknown";

    private void ValidatePublicHover() {
        var proof = _session.Page.EvaluateAsync<JsonElement>("""
            () => {
              const fixture = window.cfxBenchmark, point = fixture.points[Math.floor(fixture.points.length / 3)];
              const native = document.elementFromPoint(point.x, point.y)?.closest('g[data-cfx-role="point"][data-cfx-point]');
              if (!native) throw new Error('The public pointer fixture has no native painted target');
              return { x: point.x, y: point.y, source: Number(native.dataset.cfxSourcePoint ?? native.dataset.cfxPoint) };
            }
            """).GetAwaiter().GetResult();
        _session.Page.Mouse.MoveAsync(1, 1).GetAwaiter().GetResult();
        _session.Page.Mouse.MoveAsync((float)proof.GetProperty("x").GetDouble(), (float)proof.GetProperty("y").GetDouble()).GetAwaiter().GetResult();
        _session.Page.WaitForFunctionAsync("source => Number(window.cfxBenchmark.hover?.sourcePoint) === source && !document.querySelector('.cfx-tooltip').hidden",
            proof.GetProperty("source").GetInt32()).GetAwaiter().GetResult();
        _publicHoverProven = true;
        _session.Page.Mouse.MoveAsync(1, 1).GetAwaiter().GetResult();
    }

    /// <summary>Closes the HtmlTinkerX-owned session before returning to engineering work.</summary>
    public void Dispose() => _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
