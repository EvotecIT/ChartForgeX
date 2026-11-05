using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Composes native script-free flow and sequence SVG with the adapter's exploration runtime.</summary>
/// <remarks>Geometry, text and identities come from one native preparation. The runtime only reads the captured
/// facts emitted beside the SVG and paints highlight state; it never recomputes layout.</remarks>
internal sealed class HtmlInteractiveDiagramRenderer {
    internal HtmlChartLocalization Localization { get; } = new();

    internal string Render(FlowArtifact flow, VisualRenderContext? context, bool page) {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        context ??= VisualExportRequest.ForFlow(flow).Context;
        var prepared = flow.Prepare(context);
        var envelope = prepared.ToArtifact(flow.Id, VisualArtifactKind.Flow).ToInterchangeEnvelope();
        return Render(prepared, "flow", FlowModel(envelope, prepared), Title(flow.Title, flow.Id), context.Theme, context.ThemeMode,
            context.Frame.TransparentBackground, context.Layout.PaddingEdges.Left, page);
    }

    internal string Render(SequenceArtifact sequence, VisualRenderContext? context, bool page) {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        var prepared = context == null ? SequencePreparedCompiler.PrepareDefault(sequence) : sequence.Prepare(context);
        var envelope = prepared.ToArtifact(sequence.Id, VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        return Render(prepared, "sequence", SequenceModel(envelope, prepared), Title(sequence.Title, sequence.Id),
            context?.Theme ?? sequence.Theme, context?.ThemeMode ?? sequence.ThemeMode, context?.Frame.TransparentBackground == true,
            context?.Layout.PaddingEdges.Left ?? sequence.Padding, page);
    }

    private string Render(PreparedVisual prepared, string family, string model, string title, VisualTheme theme, VisualThemeMode mode,
        bool transparent, double inset, bool page) {
        var language = Localization.ResolveLanguage(prepared.Accessibility.Language);
        var colors = theme.Resolve(mode);
        var writer = new HtmlMarkupWriter();
        if (page) {
            writer.Doctype().Line()
                .StartElement("html").Attribute("lang", language).Attribute("dir", Localization.DirectionAttribute).EndStartElement().Line()
                .StartElement("head").EndStartElement().Line();
            HtmlChartRenderer.WriteDocumentHead(writer, title, HtmlDiagramAssets.Style
                + "body{margin:0;background:" + (transparent ? "transparent" : colors.Background.ToCss()) + ";" + HtmlSurfacePolish.BrowserTextPolishCss + "}");
            writer.EndElement().Line()
                .StartElement("body").EndStartElement().Line()
                .StartElement("main").Attribute("class", "cfx-diagram-shell").EndStartElement().Line();
        } else {
            writer.StartElement("style").EndStartElement().RawTrusted(HtmlDiagramAssets.Style).EndElement().Line();
        }

        var steps = family == "sequence";
        writer.StartElement("section").Attribute("class", "cfx-diagram").Attribute("data-cfx-look", "graphite")
            .Attribute("data-cfx-diagram", family).Attribute("role", "group").Attribute("aria-label", title)
            .Attribute("lang", page ? null : language).Attribute("dir", page ? null : Localization.DirectionAttribute)
            .Attribute("data-cfx-ui", Localization.ToDiagramJson()).Attribute("data-cfx-diagram-model", model)
            .Attribute("style", "--cfx-diagram-width:" + Number(prepared.Size.Width) + "px;--cfx-diagram-inset:"
                + Number(Math.Max(0, Math.Min(inset, prepared.Size.Width / 4)) * 100 / prepared.Size.Width) + "%;color-scheme:" + (mode == VisualThemeMode.Dark ? "dark" : "light")
                + HtmlInteractiveChartRenderer.ThemeInteractionTokens(theme, mode, transparent))
            .EndStartElement().Line();
        var emission = prepared.ExportSvgEmission(VisualSvgTextPresentation.Automatic);
        writer.StartElement("div").Attribute("class", "cfx-diagram-stage").Attribute("data-cfx-diagram-stage", "true").EndStartElement()
            .RawTrusted(emission.Svg).EndElement().Line();
        HtmlCapturedFontAssets.Write(writer, emission.Fonts);
        if (steps) WritePlayback(writer);
        // Without the runtime the static SVG stays a complete image; the hint appears once exploration is available.
        writer.StartElement("p").Attribute("class", "cfx-diagram-readout").Attribute("data-cfx-diagram-readout", "true")
            .Attribute("role", "status").Attribute("aria-live", "polite").BooleanAttribute("hidden", true).EndStartElement()
            .Text(Localization[HtmlChartText.DiagramHint]).EndElement().Line();
        writer.EndElement().Line();
        writer.StartElement("script").EndStartElement().RawTrusted(HtmlDiagramAssets.Script).EndElement().Line();
        if (page) writer.EndElement().Line().EndElement().Line().EndElement().Line();
        return writer.Build();
    }

    private void WritePlayback(HtmlMarkupWriter writer) {
        writer.StartElement("div").Attribute("class", "cfx-diagram-toolbar").Attribute("role", "toolbar")
            .Attribute("aria-label", Localization[HtmlChartText.DiagramPlayback]).BooleanAttribute("hidden", true).EndStartElement();
        Button(writer, "previous", HtmlChartText.Previous, HtmlChartText.PreviousStep);
        writer.StartElement("button").Attribute("type", "button").Attribute("data-cfx-diagram-step", "play")
            .Attribute("aria-pressed", "false").EndStartElement().Text(Localization[HtmlChartText.Play]).EndElement();
        Button(writer, "next", HtmlChartText.Next, HtmlChartText.NextStep);
        writer.StartElement("span").Attribute("class", "cfx-diagram-progress").Attribute("data-cfx-diagram-progress", "true").EndStartElement().EndElement();
        writer.EndElement().Line();
    }

    private void Button(HtmlMarkupWriter writer, string action, HtmlChartText text, HtmlChartText name) =>
        writer.StartElement("button").Attribute("type", "button").Attribute("data-cfx-diagram-step", action)
            .Attribute("aria-label", Localization[name]).EndStartElement().Text(Localization[text]).EndElement();

    // Traversal walks each step and then the connectors that leave it, so arrow keys read the flow in its authored order.
    private static string FlowModel(VisualArtifactInterchangeEnvelope envelope, PreparedVisual prepared) {
        var regions = RegionRoles(prepared);
        var lanes = envelope.Groups.ToDictionary(group => group.Id, group => group.Label, StringComparer.Ordinal);
        var edges = envelope.Edges.Where(edge => regions.ContainsKey(edge.Id)).OrderBy(edge => edge.Order).ToList();
        var items = new List<string>();
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in envelope.Nodes.Where(node => regions.ContainsKey(node.Id))) {
            items.Add(Item(node.Id, "step", regions[node.Id], node.Label, node.Subtitle,
                node.GroupId != null && lanes.TryGetValue(node.GroupId, out var lane) ? lane : null));
            foreach (var edge in edges.Where(edge => edge.SourceId == node.Id && written.Add(edge.Id)))
                items.Add(Item(edge.Id, "connector", regions[edge.Id], edge.Label ?? string.Empty, null, null, edge.SourceId, edge.TargetId, LabelPart(regions, edge.Id)));
        }
        foreach (var edge in edges.Where(edge => written.Add(edge.Id)))
            items.Add(Item(edge.Id, "connector", regions[edge.Id], edge.Label ?? string.Empty, null, null, edge.SourceId, edge.TargetId, LabelPart(regions, edge.Id)));
        return "{\"items\":[" + string.Join(",", items) + "]}";
    }

    private static string SequenceModel(VisualArtifactInterchangeEnvelope envelope, PreparedVisual prepared) {
        var regions = RegionRoles(prepared);
        var items = envelope.Nodes.Where(node => node.Role == VisualArtifactInterchangeNodeRole.SequenceParticipant && regions.ContainsKey(node.Id))
            .OrderBy(node => node.Sequence?.Order ?? 0)
            .Select(node => Item(node.Id, "participant", regions[node.Id], node.Label, null, null)).ToList();
        var messages = envelope.Edges.Where(edge => edge.Role == VisualArtifactInterchangeEdgeRole.SequenceMessage && regions.ContainsKey(edge.Id))
            .OrderBy(edge => edge.Order).ToList();
        items.AddRange(messages.Select(edge => Item(edge.Id, "message", regions[edge.Id], edge.Label ?? string.Empty, null, null, edge.SourceId, edge.TargetId)));
        return "{\"items\":[" + string.Join(",", items) + "]}";
    }

    private static Dictionary<string, string> RegionRoles(PreparedVisual prepared) {
        var roles = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var region in prepared.Regions) if (!roles.ContainsKey(region.Id)) roles[region.Id] = region.Role;
        return roles;
    }

    // Connector captions are separate native groups; they share the connector's hover and highlight.
    private static string? LabelPart(Dictionary<string, string> regions, string id) => regions.ContainsKey(id + "-label") ? id + "-label" : null;

    private static string Item(string id, string kind, string role, string label, string? detail, string? group,
        string? source = null, string? target = null, string? part = null) {
        var json = new StringBuilder("{\"id\":").Append(HtmlJsonString.Encode(id))
            .Append(",\"kind\":").Append(HtmlJsonString.Encode(kind))
            .Append(",\"role\":").Append(HtmlJsonString.Encode(role))
            .Append(",\"label\":").Append(HtmlJsonString.Encode(label));
        if (!string.IsNullOrWhiteSpace(detail)) json.Append(",\"detail\":").Append(HtmlJsonString.Encode(detail!));
        if (!string.IsNullOrWhiteSpace(group)) json.Append(",\"group\":").Append(HtmlJsonString.Encode(group!));
        if (source != null) json.Append(",\"source\":").Append(HtmlJsonString.Encode(source));
        if (target != null) json.Append(",\"target\":").Append(HtmlJsonString.Encode(target));
        if (part != null) json.Append(",\"part\":").Append(HtmlJsonString.Encode(part));
        return json.Append('}').ToString();
    }

    private static string Title(string title, string id) => string.IsNullOrWhiteSpace(title) ? id : title;
    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
