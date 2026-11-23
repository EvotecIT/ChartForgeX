using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Html;

public sealed partial class HtmlChartRenderer {
    /// <summary>The standard dashboard widths: desktop card, tablet and phone.</summary>
    public static IReadOnlyList<int> StandardWidths { get; } = Array.AsReadOnly(new[] { 800, 600, 390 });

    /// <summary>Renders one native drawing per width and lets the container choose between them, without script.</summary>
    /// <param name="chart">The chart; its authored size is restored afterwards.</param>
    /// <param name="idScope">A stable scope; each drawing gets its own ID scope inside it.</param>
    /// <param name="widths">Drawing widths in CSS pixels, at least two, each from 160 to 8192.
    /// Defaults to <see cref="StandardWidths"/>.</param>
    /// <returns>A fragment that shows the widest drawing that fits its container, or the narrowest one scaled down.
    /// Every drawing is prepared natively at its own width, so ticks, captions and legends are laid out for that width.</returns>
    /// <remarks>The container query needs a browser from 2023 or later; older browsers show the narrowest drawing.
    /// Print shows the widest drawing.</remarks>
    public string RenderWidths(Chart chart, string idScope, params int[] widths) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var ordered = (widths == null || widths.Length == 0 ? StandardWidths.ToArray() : widths).Distinct().OrderByDescending(width => width).ToArray();
        if (ordered.Length < 2 || ordered.Any(width => width < 160 || width > 8192))
            throw new ArgumentOutOfRangeException(nameof(widths), "A width set needs at least two widths from 160 to 8192 pixels.");
        var size = chart.Options.Size;
        var writer = new HtmlMarkupWriter();
        // An Auto chart follows the viewer's colour scheme in every drawing, unless the caller supplied colour variables.
        var auto = AutoThemeStyle.ForHtml(chart.Options.Theme, chart.Options.ThemeMode, chart.Options.SvgColorVariables);
        try {
            writer.StartElement("style").EndStartElement().RawTrusted(WidthSetCss(ordered)).EndElement().RawTrusted(auto?.StyleElement);
            writer.StartElement("div").Attribute("class", "chartforgex-widths").Attribute("data-cfx-widths", string.Join(" ", ordered.Select(N)))
                .Attribute(AutoThemeStyle.ScopeAttribute, auto?.Id).Attribute(AutoThemeStyle.Attribute, auto == null ? null : AutoThemeStyle.AttributeValue).EndStartElement();
            for (var i = 0; i < ordered.Length; i++) {
                var width = ordered[i];
                chart.Size(width, size.Height);
                var scope = VisualSvgOptions.NamespaceFromExternalId(idScope + "|w" + N(width));
                var options = new VisualSvgOptions(scope, chart.Options.SvgColorVariables, responsive: true);
                var svg = chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg(auto == null ? options : options.WithColorScope(auto.Variables));
                writer.StartElement("div").Attribute("class", "chartforgex-chart")
                    .Attribute("data-cfx-width", N(width))
                    .Attribute("data-cfx-from", i == ordered.Length - 1 ? "0" : N(width))
                    .Attribute("data-cfx-until", i == 0 ? null : N(ordered[i - 1]))
                    .EndStartElement().RawTrusted(svg).EndElement();
            }
            writer.EndElement();
        } finally {
            chart.Size(size.Width, size.Height);
        }
        return HtmlStrictStyles.Apply(writer.Build(), _nonce);
    }

    // The switching rules name their complete width set, so different sets on one page cannot select each other's
    // drawings, while equal sets share identical rules.
    private static string WidthSetCss(IReadOnlyList<int> descending) {
        var set = ".chartforgex-widths[data-cfx-widths=\"" + string.Join(" ", descending.Select(N)) + "\"]>";
        var css = new System.Text.StringBuilder(".chartforgex-widths{container-type:inline-size;width:100%}")
            .Append(".chartforgex-widths>[data-cfx-width]{display:none;box-sizing:border-box;overflow:visible}")
            .Append(".chartforgex-widths>[data-cfx-width] svg{display:block;width:100%;height:auto;max-width:100%}")
            .Append(".chartforgex-widths>[data-cfx-from=\"0\"]{display:block}");
        // Like an ordinary fragment, a drawing fills narrower containers and never grows past its native width.
        foreach (var width in descending)
            css.Append(".chartforgex-widths>[data-cfx-width=\"").Append(N(width)).Append("\"]{max-width:").Append(N(width)).Append("px}");
        foreach (var width in descending.Take(descending.Count - 1).Reverse())
            css.Append("@container (min-width:").Append(N(width)).Append("px){")
                .Append(set).Append("[data-cfx-from=\"").Append(N(width)).Append("\"]{display:block}")
                .Append(set).Append("[data-cfx-until=\"").Append(N(width)).Append("\"]{display:none}}");
        css.Append("@media print{").Append(set).Append("[data-cfx-width]{display:none}").Append(set).Append("[data-cfx-width=\"")
            .Append(N(descending[0])).Append("\"]{display:block}}");
        return css.ToString();
    }

    private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
}
