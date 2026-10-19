using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using HtmlTinkerX;
using Microsoft.Playwright;

/// <summary>Measures the actual private acquisition implementation through a benchmark-only HTML export.</summary>
public sealed class InteractivityBenchmarkFixture : IDisposable {
    private readonly HtmlBrowserSession _session;
    private readonly int _count;
    private readonly int _queries;
    private int[]? _last;
    private readonly int[] _expected;
    private bool _publicHoverProven;

    /// <summary>Prepares the chart and persistent browser outside PowerForge's measured operation.</summary>
    public InteractivityBenchmarkFixture(int count, int queries, string sourcePath) {
        _count = count; _queries = queries;
        var chart = Chart.Create().WithSize(1000, 600).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddScatter("Measurements", Enumerable.Range(0, count).Select(index => new ChartPoint(index, Math.Sin(index * .19) * 10 + index % 37)));
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Range = HtmlChartTooltipRange.Nearest;
        });
        const string marker = "  const pointerActivationTarget =";
        var source = File.ReadAllText(sourcePath);
        var acquisition = source[source.IndexOf("  const pointerCandidates =", StringComparison.Ordinal)..source.IndexOf(marker, StringComparison.Ordinal)];
        if (!html.Contains(acquisition, StringComparison.Ordinal)) throw new InvalidOperationException("The embedded acquisition code differs from the recorded source file. Rebuild the product before benchmarking.");
        if (html.IndexOf(marker, StringComparison.Ordinal) != html.LastIndexOf(marker, StringComparison.Ordinal))
            throw new InvalidOperationException("The benchmark hook must have exactly one insertion point.");
        // This hook exists only in the disposable benchmark page; the shipped runtime has no benchmark export.
        html = html.Replace(marker, "  window.cfxBenchmarkAcquire = pointerCandidates;\n" + marker, StringComparison.Ordinal);
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

    /// <summary>Executes one deterministic in-page query batch without consumer-owned timers.</summary>
    public void Execute() => _last = _session.Page.EvaluateAsync<int[]>("""
        () => {
          const fixture = window.cfxBenchmark;
          return fixture.probes.map(probe => {
            const point = window.cfxBenchmarkAcquire(fixture.root,
              { target: fixture.stage, clientX: probe.x, clientY: probe.y }, true, fixture.stage)?.observation?.node;
            return point ? Number(point.dataset.cfxSourcePoint ?? point.dataset.cfxPoint) : -1;
          });
        }
        """).GetAwaiter().GetResult();

    /// <summary>Checks every inferred result against an independent geometry oracle and retains console failures.</summary>
    public void Validate() {
        if (_last == null || _last.Length != _queries || !_last.SequenceEqual(_expected))
            throw new InvalidOperationException("Acquisition results differ from the independently prepared geometry oracle.");
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
