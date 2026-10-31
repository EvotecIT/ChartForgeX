using System;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

internal static class EmbeddingSmoke {
    internal static void Run(string fontFamily) {
        var series = ChartSeries.CreateGanttTask(new ChartSeriesId("support"), "Support", 2, 3,
            color: ChartColor.FromHex("#2563EB").WithCssVariable("--accent"));
        series.ReplaceGanttItems(new[] { ChartGanttItem.OpenEnded(2).WithMarkOptions(new ChartMarkOptions(
            href: "/versions/4", linkTarget: VisualSvgLinkTarget.NewContext,
            tooltip: "Supported version", accessibleLabel: "View version 4", current: true)) });
        var chart = Chart.Create().AddSeries(series).WithSize(400, 160).WithXAxisBounds(0, 10)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(fontFamily))
            .WithGanttOpenEndStyle(ChartGanttOpenEndStyle.Fade);
        var scale = new ChartStripScale(new ChartAxis { Minimum = 0, Maximum = 10 });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 40), 0),
            font: FontSpec.FromFamily(fontFamily));
        var prepared = chart.PrepareStrip(context, scale);
        var svg = prepared.ToSvg(new VisualSvgOptions("aot-support").WithFluidWidth().WithInheritedFonts());
        foreach (var contract in new[] { "width=\"100%\"", "href=\"/versions/4\"", "noopener noreferrer", "var(--accent", "aria-current=\"true\"" })
            if (!svg.Contains(contract)) throw new InvalidOperationException("Embedded chart AOT export omitted " + contract);
        if (prepared.ToPng().Length < 32) throw new InvalidOperationException("Embedded chart AOT raster failed.");
        if (!scale.PrepareAxis(context).ToSvg("aot-axis").Contains("strip-axis-tick"))
            throw new InvalidOperationException("Embedded chart AOT shared axis failed.");
        var html = new HtmlChartRenderer().RenderFragment(chart, new VisualSvgOptions("aot-table"), ChartDataTableMode.Details);
        if (!html.Contains("<table>") || !html.Contains("<summary>") || !html.Contains(chart.Options.Labels.Ongoing))
            throw new InvalidOperationException("Embedded chart AOT data table failed.");
    }
}
