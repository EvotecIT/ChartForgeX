using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static class ResponsiveRadialFixture {
    internal static Chart Model(bool dark, bool other = false, bool longName = false) {
        var names = new[] { "Microsoft 365 E3", "Microsoft 365 E5", "Exchange Online Plan 1", "Power BI Pro", "Visio Plan 2", "Project Plan 3" };
        if (longName) names[2] += " with an extended archive and compliance service description";
        var values = new double[] { 412, 268, 190, 120, 64, 31 };
        var chart = Chart.Create().WithSize(1100, 420).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Licences by product").WithSubtitle("Assigned seats, October 2026").WithXLabels(names);
        chart.AddSeries(new ChartSeries(new ChartSeriesId("licences"), "Licences", other ? ChartSeriesKind.Pie : ChartSeriesKind.Donut,
            values.Select((value, index) => new ChartPoint(index + 1, value))));
        chart.Options.DonutCenterLabel = "Licences";
        if (other) chart.Options.MaximumPieSlices = 4;
        return chart;
    }

    internal static string Fragment(Chart chart, bool interactive = false) {
        var renderer = new HtmlResponsiveChartRenderer();
        var responsive = interactive ? renderer.RenderFragmentWithoutAssets(chart, 240, 1600, printWidth: 1100)
            : renderer.RenderFragment(chart, 240, 1600, printWidth: 1100);
        if (!interactive) return responsive;
        var size = chart.Options.Size;
        try {
            // Both public renderers prepare the same source frame; the host supplies interaction around its drawing.
            chart.WithSize(1600, 420);
            var shell = new HtmlInteractiveChartRenderer().RenderFragment(chart, options => {
                options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
                options.IncludeResetButton = false;
            });
            var stage = shell.IndexOf("class=\"cfx-stage\"", StringComparison.Ordinal);
            var start = shell.IndexOf("<svg", stage, StringComparison.Ordinal);
            var end = shell.IndexOf("</svg>", start, StringComparison.Ordinal) + "</svg>".Length;
            return shell.Substring(0, start) + responsive + shell.Substring(end)
                + "<style>" + HtmlResponsiveChartRenderer.Stylesheet + "</style><script>" + HtmlResponsiveChartRenderer.InitializationScript + "</script>";
        } finally { chart.WithSize(size.Width, size.Height); }
    }

    internal static string Document(string fragment, bool dark) => "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>"
        + "<style>body{margin:0;background:" + (dark ? "#0c0d10" : "#f2f3f4") + "}#panel .cfx-interactive-chart{width:100%;margin:0;padding:0;border:0;box-shadow:none}.cfx-frame{margin:0;border:0;border-radius:0}.cfx-stage{min-width:0}</style></head><body>"
        + "<div id='panel' style='width:390px;max-width:100%'>" + fragment + "</div></body></html>";
}
