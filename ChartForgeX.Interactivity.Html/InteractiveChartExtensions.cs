using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChartForgeX.Core;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Provides convenience methods for rendering charts with opt-in HTML interactivity.
/// </summary>
public static class InteractiveChartExtensions {
    /// <summary>
    /// Renders a chart to a self-contained interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A complete interactive HTML document.</returns>
    public static string ToInteractiveHtmlPage(this Chart chart) => new HtmlInteractiveChartRenderer().RenderPage(chart);

    /// <summary>
    /// Renders a chart to a self-contained interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML interaction adapter.</param>
    /// <returns>A complete interactive HTML document.</returns>
    public static string ToInteractiveHtmlPage(this Chart chart, Action<HtmlChartInteractionOptions>? configure) => new HtmlInteractiveChartRenderer().RenderPage(chart, configure);

    /// <summary>
    /// Saves a chart as a self-contained interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveInteractiveHtml(this Chart chart, string path) => File.WriteAllText(path, chart.ToInteractiveHtmlPage(), Encoding.UTF8);

    /// <summary>
    /// Saves a chart as a self-contained interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="configure">An optional configuration callback for the HTML interaction adapter.</param>
    public static void SaveInteractiveHtml(this Chart chart, string path, Action<HtmlChartInteractionOptions>? configure) => File.WriteAllText(path, chart.ToInteractiveHtmlPage(configure), Encoding.UTF8);

    /// <summary>
    /// Renders several charts to one self-contained interactive dashboard HTML document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <returns>A complete interactive dashboard document.</returns>
    public static string ToInteractiveHtmlDashboardPage(this IEnumerable<Chart> charts) => new HtmlInteractiveDashboardRenderer().RenderPage(charts);

    /// <summary>
    /// Renders several charts to one self-contained interactive dashboard HTML document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML dashboard adapter.</param>
    /// <returns>A complete interactive dashboard document.</returns>
    public static string ToInteractiveHtmlDashboardPage(this IEnumerable<Chart> charts, Action<HtmlInteractiveDashboardOptions>? configure) => new HtmlInteractiveDashboardRenderer().RenderPage(charts, configure);

    /// <summary>
    /// Saves several charts as one self-contained interactive dashboard HTML document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveInteractiveHtmlDashboard(this IEnumerable<Chart> charts, string path) => File.WriteAllText(path, charts.ToInteractiveHtmlDashboardPage(), Encoding.UTF8);

    /// <summary>
    /// Saves several charts as one self-contained interactive dashboard HTML document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="configure">An optional configuration callback for the HTML dashboard adapter.</param>
    public static void SaveInteractiveHtmlDashboard(this IEnumerable<Chart> charts, string path, Action<HtmlInteractiveDashboardOptions>? configure) => File.WriteAllText(path, charts.ToInteractiveHtmlDashboardPage(configure), Encoding.UTF8);
}
