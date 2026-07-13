using System;
using System.IO;
using System.Text;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Provides adapter-owned interactive HTML output for topology charts.</summary>
public static class InteractiveTopologyExtensions {
    /// <summary>Renders a self-contained interactive topology HTML fragment.</summary>
    public static string ToInteractiveHtmlFragment(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return new HtmlInteractiveTopologyRenderer().RenderFragment(chart, options);
    }

    /// <summary>Renders interactive topology markup without renderer-owned assets.</summary>
    public static string ToInteractiveHtmlFragmentWithoutAssets(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return new HtmlInteractiveTopologyRenderer().RenderFragmentWithoutAssets(chart, options);
    }

    /// <summary>Renders a complete self-contained interactive topology HTML page.</summary>
    public static string ToInteractiveHtmlPage(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return new HtmlInteractiveTopologyRenderer().RenderPage(chart, options);
    }

    /// <summary>Saves a complete self-contained interactive topology HTML page.</summary>
    public static void SaveInteractiveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, chart.ToInteractiveHtmlPage(options), Encoding.UTF8);
    }
}
