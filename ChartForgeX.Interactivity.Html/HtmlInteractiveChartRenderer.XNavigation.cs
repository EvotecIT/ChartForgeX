using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string BuildXNavigation(Chart chart, HtmlChartInteractionOptions options, PreparedVisual prepared,
        VisualRenderContext context, ChartCartesianViewport? viewport, string scope, PreparedVisual? fullDrawing = null) {
        if (!options.XNavigation.Enabled || prepared.XProjection == null) return string.Empty;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("div").Attribute("class", "cfx-x-navigation").Attribute("data-cfx-x-navigation", "true")
            .Attribute("role", "group").Attribute("aria-label", options.Localization[HtmlChartText.XNavigation])
            .BooleanAttribute("hidden").EndStartElement()
            .StartElement("div").Attribute("class", "cfx-x-navigation__header").EndStartElement()
            .StartElement("strong").Text(options.Localization[HtmlChartText.XNavigation]).EndElement()
            .StartElement("div").Attribute("class", "cfx-x-navigation__presets").EndStartElement();
        var index = 0;
        foreach (var preset in options.XNavigation.RangePresets) {
            if (preset == null) throw new ArgumentException("Range presets must not contain null.", nameof(options));
            writer.StartElement("button").Attribute("type", "button").Attribute("class", "cfx-tool")
                .Attribute("data-cfx-x-preset", preset.Span.ToString("R", CultureInfo.InvariantCulture))
                .Attribute("data-cfx-x-control", "preset-" + (index++).ToString(CultureInfo.InvariantCulture))
                .Attribute("aria-pressed", "false").EndStartElement().Text(preset.Label).EndElement();
        }
        writer.StartElement("button").Attribute("type", "button").Attribute("class", "cfx-tool")
            .Attribute("data-cfx-x-full", "true").Attribute("data-cfx-x-control", "full")
            .Attribute("aria-pressed", "true").EndStartElement().Text(options.Localization[HtmlChartText.XFullRange]).EndElement()
            .EndElement().EndElement();
        if (options.XNavigation.IncludeNavigator) {
            // Reuse the real full-domain scene and native mark owners. A data image keeps its
            // semantic catalog and IDs outside the main chart's interaction/query surface.
            var full = fullDrawing ?? (viewport == null ? prepared : chart.Prepare(context));
            var plot = full.XProjection!.PlotBounds;
            var svg = full.ToSvg(new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(scope + "-navigator")));
            writer.StartElement("div").Attribute("class", "cfx-x-navigation__overview").Attribute("aria-hidden", "true").EndStartElement()
                .StartElement("svg").Attribute("xmlns", "http://www.w3.org/2000/svg")
                .Attribute("viewBox", string.Join(" ", new[] { plot.X, plot.Y, plot.Width, plot.Height }.Select(value => value.ToString("R", CultureInfo.InvariantCulture))))
                .Attribute("preserveAspectRatio", "none").Attribute("focusable", "false").EndStartElement()
                .StartElement("image").Attribute("width", full.Scene.Size.Width.ToString("R", CultureInfo.InvariantCulture))
                .Attribute("height", full.Scene.Size.Height.ToString("R", CultureInfo.InvariantCulture))
                .Attribute("href", "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg)))
                .EndStartElement().EndElement().EndElement()
                .StartElement("span").Attribute("class", "cfx-x-navigation__window").Attribute("data-cfx-x-window", "true").EndStartElement().EndElement()
                .EndElement();
        }
        writer.StartElement("div").Attribute("class", "cfx-x-navigation__limits").EndStartElement();
        WriteXNavigationRange(writer, options, "start", HtmlChartText.XRangeStart);
        WriteXNavigationRange(writer, options, "end", HtmlChartText.XRangeEnd);
        writer.EndElement();
        if (options.XNavigation.IncludeScrollbar) WriteXNavigationRange(writer, options, "position", HtmlChartText.XWindowPosition);
        writer.StartElement("span").Attribute("class", "cfx-x-navigation__status").Attribute("data-cfx-x-status", "true")
            .Attribute("role", "status").Attribute("aria-live", "polite").EndStartElement().EndElement().EndElement();
        return writer.Build();
    }

    private static void WriteXNavigationRange(HtmlMarkupWriter writer, HtmlChartInteractionOptions options, string kind, HtmlChartText label) {
        writer.StartElement("label").Attribute("class", "cfx-x-navigation__range").EndStartElement()
            .StartElement("span").EndStartElement().Text(options.Localization[label])
            .StartElement("output").Attribute("data-cfx-x-value", kind).EndStartElement().EndElement().EndElement()
            .StartElement("input").Attribute("type", "range").Attribute("min", "0").Attribute("max", "1").Attribute("step", "any")
            .Attribute("data-cfx-x-range", kind).Attribute("data-cfx-x-control", kind)
            .Attribute("aria-label", options.Localization[label]).BooleanAttribute("disabled").EndVoidElement().EndElement();
    }
}
