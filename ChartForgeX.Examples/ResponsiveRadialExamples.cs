using System.Text;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Offline pie and donut layouts with native legend facts and interactive slice identities.</summary>
public static class ResponsiveRadialExamples {
    /// <summary>Creates six licence categories, optionally grouping the three smallest into Other.</summary>
    public static Chart Create(VisualThemeMode mode, bool other = false) {
        var chart = Chart.Create().WithSize(1100, 420).WithTheme(VisualTheme.Graphite(), mode)
            .WithTitle("Licences by product").WithSubtitle("Assigned seats, October 2026")
            .WithXLabels("Microsoft 365 E3", "Microsoft 365 E5", "Exchange Online Plan 1", "Power BI Pro", "Visio Plan 2", "Project Plan 3");
        var values = new double[] { 412, 268, 190, 120, 64, 31 };
        chart.AddSeries(new ChartSeries(new ChartSeriesId("licences"), "Licences", other ? ChartSeriesKind.Pie : ChartSeriesKind.Donut,
            values.Select((value, index) => new ChartPoint(index + 1, value))));
        chart.Options.DonutCenterLabel = "Licences";
        if (other) chart.Options.MaximumPieSlices = 4;
        return chart;
    }

    /// <summary>Composes an assetless responsive drawing inside the standard interactive section.</summary>
    public static string Fragment(Chart chart) {
        var renderer = new HtmlResponsiveChartRenderer();
        var drawing = renderer.RenderFragmentWithoutAssets(chart, 240, 1600, printWidth: 1100);
        var size = chart.Options.Size;
        try {
            // Native hit facts and the portable drawing share their maximum-width source frame.
            chart.WithSize(1600, 420);
            var host = new HtmlInteractiveChartRenderer().RenderFragment(chart, options => {
                options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
                options.IncludeResetButton = false;
            });
            var stage = host.IndexOf("class=\"cfx-stage\"", StringComparison.Ordinal);
            var start = host.IndexOf("<svg", stage, StringComparison.Ordinal);
            var end = host.IndexOf("</svg>", start, StringComparison.Ordinal) + "</svg>".Length;
            return host.Substring(0, start) + drawing + host.Substring(end)
                + "<style>" + HtmlResponsiveChartRenderer.Stylesheet + "</style><script>" + HtmlResponsiveChartRenderer.InitializationScript + "</script>";
        } finally { chart.WithSize(size.Width, size.Height); }
    }

    private static string Document(string content, bool dark) => "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Responsive pie and donut charts</title>"
        + "<style>body{margin:0;background:" + (dark ? "#0c0d10" : "#f2f3f4") + ";font:14px system-ui;color:" + (dark ? "#f2f3f5" : "#16181c") + "}"
        + ".panel .cfx-interactive-chart{width:100%;margin:0;padding:0;border:0;box-shadow:none}.cfx-frame{margin:0;border:0;border-radius:0}.cfx-stage{min-width:0}"
        + "header{padding:16px 24px}h1{font-size:20px;margin:0 0 8px}h2{font-size:15px;margin:16px 24px 8px}select{font:inherit;margin-left:8px}article{margin-bottom:24px}</style></head><body>" + content + "</body></html>";

    /// <summary>Writes self-contained live examples and matching static references.</summary>
    public static void Write(string output) {
        Directory.CreateDirectory(output);
        var examples = new StringBuilder();
        foreach (var other in new[] { false, true }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var chart = Create(mode, other);
            var key = (other ? "pie-other" : "donut") + "-" + mode;
            var fragment = Fragment(chart);
            var panel = "<div class='panel' id='" + key + "' style='width:390px;max-width:100%'>" + fragment + "</div>";
            File.WriteAllText(Path.Combine(output, key + ".html"), Document(panel, mode == VisualThemeMode.Dark));
            examples.Append("<article><h2>").Append(other ? "Pie with Other" : "Donut").Append(" · ").Append(mode).Append("</h2>").Append(panel).Append("</article>");
            chart.WithSize(390, 420);
            File.WriteAllBytes(Path.Combine(output, "static-" + key + "-390.png"), chart.ToPng());
        }
        var controls = "<header><h1>Responsive pie and donut charts</h1><label>Maximum chart width<select id='width'><option value='390'>390 px</option><option value='1024'>1024 px</option><option value='1440'>1440 px</option><option value='100%'>Fit available width</option></select></label>"
            + "<p>Resize the window or change the width. Hover a legend entry, use Tab to focus a slice, or press Space to select it.</p></header>";
        var resize = "<script>document.querySelector('#width').addEventListener('change',e=>document.querySelectorAll('.panel').forEach(p=>p.style.width=e.target.value==='100%'?'100%':e.target.value+'px'));</script>";
        File.WriteAllText(Path.Combine(output, "fixture.html"), Document(controls + examples + resize, false));
    }
}
