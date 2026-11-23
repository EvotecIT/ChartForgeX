using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer {
    private string RenderRegionFragment(TopologyChart chart, TopologyRenderOptions options, bool includeAssets,
        string? preparedSvg = null, double? preparedWidth = null, Func<PreparedTopology, string>? renderSvg = null, AutoThemeStyle? auto = null, bool writeAutoStyle = false) {
        var html = new StringBuilder();
        if (options.RegionPrint) auto = null;
        string Colors(ThemeMode mode) => RegionColors(chart.Theme.Resolve(options.RegionPrint ? ThemeMode.Light : mode));
        if (includeAssets) html.Append("<style>").Append(TopologyRegionTemplate.Styles.Value).Append("</style>");
        html.Append("<div class=\"").Append(WebUtility.HtmlEncode(CssClassPrefix(options)))
            .Append("-wrapper cfx-region-chart\"").Append(auto?.RootAttributes)
            .Append(" data-cfx-region-chart=\"true\" data-cfx-interactive=\"")
            .Append(options.Html.Interactions ? "true" : "false").Append("\" data-chart-id=\"").Append(WebUtility.HtmlEncode(chart.Id ?? "topology"))
            .Append("\" style=\"width:100%;max-width:").Append((preparedWidth ?? chart.Viewport.Width).ToString("R", CultureInfo.InvariantCulture))
            .Append("px").Append(auto == null ? Colors(chart.ThemeMode) : "")
            .Append(";font-family:").Append(WebUtility.HtmlEncode(chart.Theme.Typography.Family)).Append("\">");
        if (auto != null && writeAutoStyle) html.Append(auto.StyleElement);
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
            if (variant == "print") source.ThemeMode = ThemeMode.Light;
            var policy = options.Clone(); policy.RegionPrint = options.RegionPrint || variant == "print"; policy.UseResponsiveSvg = true;
            // Print stays light: the print drawing paints literal colours rather than the Auto properties a dark preference changes.
            if (variant == "print" && auto != null) policy.SvgColorVariables = null;
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
                var light = chart.Theme.Resolve(ThemeMode.Light);
                html.Append(" style=\"--cfx-good:").Append(light.Status.Pass.Fill.ToCss()).Append(";--cfx-warn:").Append(light.Status.Medium.Fill.ToCss())
                    .Append(";--cfx-bad:").Append(light.Status.Critical.Fill.ToCss()).Append(";--cfx-info:").Append(light.Info.ToCss())
                    .Append(";--cfx-neutral:").Append(light.Status.Neutral.Fill.ToCss()).Append("\"");
            }
            html.Append(">").Append(source.Prepare(policy).ToSvg()).Append("</div>");
        }
        if (options.IncludeRegionDetails) html.Append(RegionDetails(chart));
        html.Append("</div>"); return html.ToString();
    }

    // Inline custom properties of a region chart; Auto writes the same declarations per mode in its scoped stylesheet.
    private static string RegionColors(VisualThemeColors colors) => ";--cfx-surface:" + colors.ElevatedSurface.ToCss() + ";--cfx-text:" + colors.Foreground.ToCss()
        + ";--cfx-muted:" + colors.MutedForeground.ToCss() + ";--cfx-border:" + colors.Border.ToCss()
        + ";--cfx-focus:" + colors.Accent.ToCss() + ";--cfx-good:" + colors.Status.Pass.Fill.ToCss()
        + ";--cfx-warn:" + colors.Status.Medium.Fill.ToCss() + ";--cfx-bad:" + colors.Status.Critical.Fill.ToCss()
        + ";--cfx-info:" + colors.Info.ToCss() + ";--cfx-neutral:" + colors.Status.Neutral.Fill.ToCss();

    private static string RegionAutoColors(VisualThemeColors light) {
        string V(VisualThemeColorToken token, ChartForgeX.Primitives.ChartColor color) => AutoThemeStyle.TokenVar(token, color);
        return "--cfx-surface:" + V(VisualThemeColorToken.ElevatedSurface, light.ElevatedSurface) + ";--cfx-text:" + V(VisualThemeColorToken.Foreground, light.Foreground)
            + ";--cfx-muted:" + V(VisualThemeColorToken.MutedForeground, light.MutedForeground) + ";--cfx-border:" + V(VisualThemeColorToken.Border, light.Border)
            + ";--cfx-focus:" + V(VisualThemeColorToken.Accent, light.Accent) + ";--cfx-good:" + V(VisualThemeColorToken.Positive, light.Status.Pass.Fill)
            + ";--cfx-warn:" + V(VisualThemeColorToken.Warning, light.Status.Medium.Fill) + ";--cfx-bad:" + V(VisualThemeColorToken.Negative, light.Status.Critical.Fill)
            + ";--cfx-info:" + V(VisualThemeColorToken.Info, light.Info) + ";--cfx-neutral:" + V(VisualThemeColorToken.Neutral, light.Status.Neutral.Fill);
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
