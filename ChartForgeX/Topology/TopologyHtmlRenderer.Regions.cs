using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer {
    private string RenderRegionFragment(TopologyChart chart, TopologyRenderOptions options, bool includeAssets,
        string? preparedSvg = null, double? preparedWidth = null, Func<PreparedTopology, string>? renderSvg = null) {
        var html = new StringBuilder();
        var colors = chart.Theme.Resolve(options.RegionPrint ? VisualThemeMode.Light : chart.ThemeMode);
        if (includeAssets) html.Append("<style>").Append(TopologyRegionTemplate.Styles.Value).Append("</style>");
        html.Append("<div class=\"").Append(WebUtility.HtmlEncode(CssClassPrefix(options)))
            .Append("-wrapper cfx-region-chart\" data-cfx-region-chart=\"true\" data-cfx-interactive=\"")
            .Append(options.EnableHtmlInteractions ? "true" : "false").Append("\" data-chart-id=\"").Append(WebUtility.HtmlEncode(chart.Id ?? "topology"))
            .Append("\" style=\"width:100%;max-width:").Append((preparedWidth ?? chart.Viewport.Width).ToString("R", CultureInfo.InvariantCulture))
            .Append("px;--cfx-surface:").Append(colors.ElevatedSurface.ToCss()).Append(";--cfx-text:").Append(colors.Foreground.ToCss())
            .Append(";--cfx-muted:").Append(colors.MutedForeground.ToCss()).Append(";--cfx-border:").Append(colors.Border.ToCss())
            .Append(";--cfx-focus:").Append(colors.Accent.ToCss()).Append(";--cfx-good:").Append(colors.Status.Pass.Fill.ToCss())
            .Append(";--cfx-warn:").Append(colors.Status.Medium.Fill.ToCss()).Append(";--cfx-bad:").Append(colors.Status.Critical.Fill.ToCss())
            .Append(";--cfx-info:").Append(colors.Info.ToCss()).Append(";--cfx-neutral:").Append(colors.Status.Neutral.Fill.ToCss())
            .Append(";font-family:").Append(WebUtility.HtmlEncode(chart.Theme.Typography.Family)).Append("\">");
        if (renderSvg != null || preparedSvg != null) {
            // Producer-owned presentations and prepared artifact snapshots retain their single resolved observation.
            var svg = renderSvg == null ? preparedSvg! : renderSvg(chart.Prepare(options));
            if (string.IsNullOrWhiteSpace(svg)) throw new InvalidOperationException("The region presentation returned no SVG.");
            html.Append("<div class=\"cfx-region-custom\">").Append(svg).Append("</div>");
            if (options.IncludeRegionDetails) html.Append(RegionDetails(chart));
            return html.Append("</div>").ToString();
        }
        var manual = chart.Nodes.Any(node => node.HasPositionOverride) || chart.Groups.Any(group => group.HasPositionOverride);
        var desktopWidth = manual ? chart.Viewport.Width : Math.Max(640, chart.Viewport.Width);
        var bands = new System.Collections.Generic.List<double>();
        if (!manual) {
            bands.Add(640);
            for (var columns = 2; columns <= chart.Groups.Count && columns * 430 <= desktopWidth; columns++) bands.Add(columns * 430);
        }
        if (!bands.Contains(desktopWidth)) bands.Add(desktopWidth);
        var variants = bands.Select(width => (Name: "desktop", Width: width)).ToList();
        if (!manual) variants.Add(("mobile", Math.Min(600, chart.Viewport.Width < 640 ? chart.Viewport.Width : 356)));
        variants.Add(("print", manual ? chart.Viewport.Width : Math.Max(860, chart.Viewport.Width)));
        foreach (var entry in variants) {
            var variant = entry.Name;
            var source = TopologyLayoutEngine.Clone(chart);
            source.Viewport.Width = entry.Width;
            if (variant == "print") source.ThemeMode = VisualThemeMode.Light;
            var policy = options.Clone(); policy.RegionPrint = options.RegionPrint || variant == "print"; policy.UseResponsiveSvg = true;
            policy.IdScope = (options.IdScope ?? chart.Id ?? "region") + "-" + variant + "-" + entry.Width.ToString("R", CultureInfo.InvariantCulture);
            html.Append("<div class=\"cfx-region-").Append(variant).Append("\"");
            if (variant == "desktop") {
                if (!manual) html.Append(" data-cfx-region-band=\"").Append(entry.Width.ToString("R", CultureInfo.InvariantCulture)).Append("\"");
                html.Append(" style=\"max-width:").Append(entry.Width.ToString("R", CultureInfo.InvariantCulture)).Append("px;");
                if (manual) html.Append("display:block;");
                else if (entry.Width != desktopWidth || chart.Viewport.Width < 640) html.Append("display:none;");
                html.Append("\"");
            }
            if (variant == "print") {
                var light = chart.Theme.Resolve(VisualThemeMode.Light);
                html.Append(" style=\"--cfx-good:").Append(light.Status.Pass.Fill.ToCss()).Append(";--cfx-warn:").Append(light.Status.Medium.Fill.ToCss())
                    .Append(";--cfx-bad:").Append(light.Status.Critical.Fill.ToCss()).Append(";--cfx-info:").Append(light.Info.ToCss())
                    .Append(";--cfx-neutral:").Append(light.Status.Neutral.Fill.ToCss()).Append("\"");
            }
            html.Append(">").Append(_svg.Render(source, policy)).Append("</div>");
        }
        if (options.IncludeRegionDetails) html.Append(RegionDetails(chart));
        html.Append("</div>"); return html.ToString();
    }

    private static string RegionDetails(TopologyChart chart) {
        var html = new StringBuilder("<section class=\"cfx-region-details\" aria-label=\"Details\"><h2>Details</h2><table><caption>Nodes</caption><thead><tr><th>Name</th><th>Region</th><th>Details</th><th>Status</th></tr></thead><tbody>");
        string Escape(string? value) => WebUtility.HtmlEncode(value ?? "");
        foreach (var node in chart.Nodes) html.Append("<tr><th>").Append(Escape(node.Label)).Append("</th><td>")
            .Append(Escape(chart.Groups.FirstOrDefault(group => group.Id == node.GroupId)?.Label)).Append("</td><td>")
            .Append(Escape(string.Join(" · ", new[] { node.Subtitle, node.Badge }.Concat(node.Badges.Select(badge => badge.Meaning)).Where(value => !string.IsNullOrWhiteSpace(value)))))
            .Append("</td><td>").Append(Escape(node.StatusText ?? node.Status.ToString())).Append("</td></tr>");
        html.Append("</tbody></table><table><caption>Links</caption><thead><tr><th>From</th><th>To</th><th>Label</th><th>Status</th></tr></thead><tbody>");
        foreach (var edge in chart.Edges) html.Append("<tr><th>").Append(Escape(chart.Nodes.FirstOrDefault(node => node.Id == edge.SourceNodeId)?.Label)).Append("</th><td>")
            .Append(Escape(chart.Nodes.FirstOrDefault(node => node.Id == edge.TargetNodeId)?.Label)).Append("</td><td>").Append(Escape(edge.Label)).Append("</td><td>")
            .Append(Escape(edge.Status.ToString())).Append("</td></tr>");
        return html.Append("</tbody></table></section>").ToString();
    }
}
