using System.Text.Json;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Links two native charts through the public browser events and controllers.</summary>
public static class LinkedChartViewsExamples {
    private static readonly DateTime FirstDay = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly int[] Api = { 24, 31, 27, 36 };
    private static readonly int[] Worker = { 13, 18, 15, 21 };

    /// <summary>Writes a linked-chart page and a native preview for the selected theme.</summary>
    /// <param name="output">The generated example directory.</param>
    /// <param name="dark">Whether to use the dark theme.</param>
    public static void Write(string output, bool dark = false) {
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, "graphite-" + (dark ? "dark" : "light") + "-linked-chart-views");
        File.WriteAllText(path + ".html", CreatePage(dark));
        var chart = CreateTimeChart(dark);
        chart.SaveSvg(path + ".svg");
        chart.SavePng(path + ".png");
    }

    /// <summary>Creates a self-contained time-range and service-summary example.</summary>
    /// <param name="dark">Whether to use the dark theme.</param>
    /// <returns>A page that uses ChartForgeX's public browser API without another library.</returns>
    public static string CreatePage(bool dark = false) {
        void ConfigureTime(HtmlChartInteractionOptions options) {
            Configure(options, "linked-time", "linked-time");
            options.Interaction.Enable(ChartInteractionFeatures.Brush);
            options.Interaction.BrushAxis = ChartBrushAxis.X;
        }
        void ConfigureTotals(HtmlChartInteractionOptions options) => Configure(options, "linked-totals", "linked-totals");

        // A static host can retain native prepared responses just as a server host
        // can return a freshly rendered fragment from its update endpoint.
        var windows = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var first = 0; first < Api.Length; first++) {
            for (var last = first; last < Api.Length; last++) {
                var key = first + ":" + last;
                windows[key] = CreateTotalsChart(first, last, dark).ToInteractiveHtmlFragmentWithoutAssets(options =>
                    Configure(options, "linked-totals", "linked-totals-" + first + "-" + last));
            }
        }
        windows["empty"] = CreateTotalsChart(0, -1, dark).ToInteractiveHtmlFragmentWithoutAssets(options =>
            Configure(options, "linked-totals", "linked-totals-empty"));
        var days = Enumerable.Range(0, Api.Length).Select(day => FirstDay.AddDays(day).ToString("O")).ToArray();
        var page = CreateTimeChart(dark).ToInteractiveHtmlPage(ConfigureTime);
        var summary = CreateTotalsChart(0, Api.Length - 1, dark).ToInteractiveHtmlFragmentWithoutAssets(ConfigureTotals);
        var controls = "<div class='linked-controls'><button type='button' id='linked-reset'>Show every day</button>"
            + "<p id='linked-status' role='status'>Showing every day. Brush a date range, then select a service below.</p></div>";
        var style = "<style>main{display:grid;gap:20px;width:min(100%,1100px);min-width:0}"
            + ".linked-controls{display:flex;flex-wrap:wrap;align-items:center;gap:12px}"
            + ".linked-controls p{margin:0;font-size:14px}.linked-controls button{font:inherit;padding:8px 12px;"
            + "border:1px solid currentColor;border-radius:6px;background:transparent;color:inherit;cursor:pointer}"
            + ".cfx-interactive-chart{min-width:0;max-width:100%}</style>";
        var script = "<script>const linkedWindows=" + JsonSerializer.Serialize(windows)
            + ",linkedDays=" + JsonSerializer.Serialize(days) + ";" + """
            (()=>{
              const timeRoot=document.querySelector('[data-cfx-chart-id="linked-time"]');
              const totalsRoot=document.querySelector('[data-cfx-chart-id="linked-totals"]');
              const time=ChartForgeXCharts.get(timeRoot),totals=ChartForgeXCharts.get(totalsRoot);
              const status=document.getElementById('linked-status');
              const restore=()=>{
                totals.update(linkedWindows['0:3']);
                time.clearSelection();time.clearHighlight();totals.clearSelection();
                status.textContent='Showing every day. Brush a date range, then select a service below.';
              };
              timeRoot.addEventListener('cfxbrush',event=>{
                const range=event.detail.range?.x;
                if(!range)return;
                const from=Date.parse(range[0]),to=Date.parse(range[1]);
                const days=linkedDays.map((day,index)=>({time:Date.parse(day),index}));
                const inside=days.filter(day=>day.time>=from&&day.time<=to);
                const key=inside.length?inside[0].index+':'+inside[inside.length-1].index:'empty';
                totals.update(linkedWindows[key]);
                status.textContent=inside.length?'Showing '+inside.length+' days in the brushed range.':'No observations in this range.';
              });
              totalsRoot.addEventListener('cfxselectionchange',event=>{
                const categories=event.detail.targets.map(target=>target.category);
                const seriesKeys=categories.map(category=>category==='API'?'api':category==='Worker'?'worker':null).filter(Boolean);
                if(seriesKeys.length)time.highlight({seriesKeys},{dim:true});
                else time.clearHighlight();
              });
              timeRoot.addEventListener('cfxreset',restore);
              document.getElementById('linked-reset').addEventListener('click',restore);
            })();
            </script>
            """;
        return page.Replace("</head>", style + "</head>", StringComparison.Ordinal)
            .Replace("</main>", summary + controls + "</main>", StringComparison.Ordinal)
            .Replace("</body>", script + "</body>", StringComparison.Ordinal);
    }

    private static void Configure(HtmlChartInteractionOptions options, string id, string scope) {
        options.PageTitle = "Linked date range and service totals";
        options.Interaction.ChartId = id;
        options.IdScope = scope;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Toolbar = HtmlChartToolbarLayout.Compact;
        options.Tooltip.PinOnSelect = false;
    }

    private static Chart CreateTimeChart(bool dark) {
        ChartSeries Series(string id, string name, int[] values) {
            var series = ChartSeries.CreateLine(new ChartSeriesId(id), name,
                values.Select((value, index) => new ChartPoint(FirstDay.AddDays(index), value)));
            for (var index = 0; index < values.Length; index++)
                series.WithObservationId(index, new ChartObservationId(id + "-" + index));
            return series;
        }
        return Frame(dark).WithTitle("Requests over time")
            .WithSubtitle("Choose Brush, then drag across the dates to update the totals")
            .WithXAxisTimeScale(TimeZoneInfo.Utc, showTimeZone: true, label: "UTC")
            .AddSeries(Series("api", "API", Api)).AddSeries(Series("worker", "Worker", Worker));
    }

    private static Chart CreateTotalsChart(int first, int last, bool dark) {
        var api = Api.Skip(first).Take(last - first + 1).Sum();
        var worker = Worker.Skip(first).Take(last - first + 1).Sum();
        var series = ChartSeries.CreateBar(new ChartSeriesId("totals"), "Requests",
            new[] { new ChartPoint(1, api), new ChartPoint(2, worker) })
            .WithObservationId(0, new ChartObservationId("api-total"))
            .WithObservationId(1, new ChartObservationId("worker-total"));
        return Frame(dark).WithTitle("Requests by service")
            .WithSubtitle("Select a service to highlight its readings above; Escape clears the selection")
            .WithXLabels("API", "Worker").AddSeries(series);
    }

    private static Chart Frame(bool dark) => Chart.Create().WithSize(800, 360)
        .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
        .WithYAxis("Requests").WithDataLabels(false);
}
