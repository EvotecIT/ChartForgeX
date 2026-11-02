using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Saved reader pages that combine the standalone interactions on each supported Cartesian family.</summary>
/// <remarks>Each page animates on load, eases toolbar and legend rescaling, shows crosshair axis values,
/// downloads or copies its data from the context menu, offers a host menu item, isolates a series on
/// double-click or double-tap and uses touch-sized targets on coarse pointers.</remarks>
public static class HtmlInteractionShowcaseExamples {
    /// <summary>Gets the chart families shown by <see cref="CreatePage"/>.</summary>
    public static IReadOnlyList<ChartSeriesKind> Kinds { get; } = new[] {
        ChartSeriesKind.Line, ChartSeriesKind.Bar, ChartSeriesKind.Area, ChartSeriesKind.Scatter, ChartSeriesKind.Candlestick
    };

    /// <summary>Creates the saved reader page for one family.</summary>
    /// <param name="kind">One of <see cref="Kinds"/>.</param>
    /// <param name="dark">Whether to use the dark Graphite palette.</param>
    /// <returns>A complete interactive HTML page.</returns>
    public static string CreatePage(ChartSeriesKind kind, bool dark = false) {
        var chart = CreateChart(kind, dark);
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.ChartId = "showcase-" + kind.ToString().ToLowerInvariant();
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan
                | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.IncludeContextMenu = true;
            options.ContextMenuItems.Add(new HtmlChartContextMenuItem("flag", "Flag selection for review"));
            // The bar page shows the C# template; the others use a page callback below.
            if (kind == ChartSeriesKind.Bar) options.Tooltip.Template = "{series} on {category}: {value} requests";
        });
        if (kind == ChartSeriesKind.Bar) return html.Replace("</body>", Status + "</body>", StringComparison.Ordinal);
        return html.Replace("</body>", Status + Renderer + "</body>", StringComparison.Ordinal);
    }

    // The host owns the menu action; the page only reports what it received.
    private const string Status = """
        <p class="cfx-showcase-status" role="status" style="max-width:800px;margin:12px auto;font:13px/18px system-ui;opacity:.75"></p>
        <script>
        document.querySelector('.cfx-interactive-chart').addEventListener('cfxcontextaction', event => {
          document.querySelector('.cfx-showcase-status').textContent =
            'Flagged ' + event.detail.selection.length + ' selected mark(s) out of ' + event.detail.data.length + ' rows.';
        });
        </script>
        """;

    private const string Renderer = """
        <script>
        document.querySelector('.cfx-interactive-chart').cfxTooltipRenderer = context => {
          const box = document.createElement('div'), title = document.createElement('strong'), value = document.createElement('div');
          title.textContent = context.series || context.title; value.textContent = (context.category ? context.category + ': ' : '') + context.value;
          box.append(title, value); return box;
        };
        </script>
        """;

    internal static Chart CreateChart(ChartSeriesKind kind, bool dark) {
        var chart = Chart.Create().WithSize(800, 440)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithLegend().WithDataLabels(false);
        switch (kind) {
            case ChartSeriesKind.Line:
                return chart.WithTitle("Response time by service").WithSubtitle("Median milliseconds per hour; double-click a line to isolate it")
                    .AddLine("API", Hours(index => 120 + 30 * Math.Sin(index / 3.0) + index * 2))
                    .AddLine("Web", Hours(index => 180 + 25 * Math.Cos(index / 4.0) + index))
                    .AddLine("Batch", Hours(index => 260 - index * 3 + 20 * Math.Sin(index / 2.0)));
            case ChartSeriesKind.Area:
                return chart.WithTitle("Storage in use").WithSubtitle("Terabytes per day; toggle a series to rescale")
                    .AddArea("Primary", Hours(index => 40 + index * 1.5 + 4 * Math.Sin(index / 2.0)))
                    .AddArea("Replica", Hours(index => 22 + index * .8 + 3 * Math.Cos(index / 3.0)));
            case ChartSeriesKind.Scatter:
                return chart.WithTitle("Request size and latency").WithSubtitle("Each point is one sampled request")
                    .WithXAxisBounds(0, 100).WithYAxisBounds(0, 500)
                    .AddScatter("Cached", Samples(17, 0, 40))
                    .AddScatter("Uncached", Samples(29, 120, 60));
            case ChartSeriesKind.Bar:
                return chart.WithTitle("Requests by weekday").WithSubtitle("Thousands of requests; brush or zoom to compare days")
                    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
                    .AddBar("Search", Week(42, 48, 51, 47, 55, 31, 27))
                    .AddBar("Checkout", Week(18, 21, 24, 22, 29, 15, 12));
            case ChartSeriesKind.Candlestick:
                var random = new Random(7);
                var price = 120d;
                var candles = Enumerable.Range(0, 30).Select(day => {
                    var open = price; var close = open + (random.NextDouble() - .45) * 6;
                    var high = Math.Max(open, close) + random.NextDouble() * 3; var low = Math.Min(open, close) - random.NextDouble() * 3;
                    price = close;
                    return new ChartCandlestick(day, Math.Round(open, 2), Math.Round(high, 2), Math.Round(low, 2), Math.Round(close, 2));
                }).ToArray();
                return chart.WithTitle("Share price, last 30 sessions").WithSubtitle("Open, high, low and close; zoom to a week")
                    .WithYAxisBounds(100, 145)
                    .AddSeries(ChartSeries.CreateCandlestick(new ChartSeriesId("price"), "Price", candles))
                    .AddLine("Moving average", candles.Select((candle, index) => new ChartPoint(candle.X,
                        Math.Round(candles.Skip(Math.Max(0, index - 4)).Take(Math.Min(5, index + 1)).Average(item => item.Close), 2))));
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "The showcase covers Line, Bar, Area, Scatter and Candlestick.");
        }
    }

    private static IEnumerable<ChartPoint> Hours(Func<int, double> value) =>
        Enumerable.Range(0, 24).Select(index => new ChartPoint(index, Math.Round(value(index), 1)));

    // Category labels sit at one-based positions.
    private static IEnumerable<ChartPoint> Week(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value));

    private static IEnumerable<ChartPoint> Samples(int seed, double offset, double spread) {
        var random = new Random(seed);
        return Enumerable.Range(0, 24).Select(_ => {
            var size = Math.Round(5 + random.NextDouble() * 90, 1);
            return new ChartPoint(size, Math.Round(offset + 60 + size * 2.4 + (random.NextDouble() - .5) * spread, 1));
        });
    }
}
