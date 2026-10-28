using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Validates source board dimensions and the ticket template for the existing native topology owners.</summary>
internal sealed class MermaidKanbanSettings {
    internal string? TicketBaseUrl { get; private set; }
    internal double? Padding { get; private set; }
    internal double? SectionWidth { get; private set; }
    internal static bool IsScalarPath(string path) => path is "kanban.ticketBaseUrl" or "kanban.padding" or "kanban.sectionWidth";
    internal bool Handles(string path) => IsScalarPath(path) || path.StartsWith("kanban.ticketBaseUrl.", StringComparison.Ordinal)
        || path.StartsWith("kanban.padding.", StringComparison.Ordinal) || path.StartsWith("kanban.sectionWidth.", StringComparison.Ordinal);
    internal static MermaidKanbanSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidKanbanSettings();
        settings.Padding = Number("padding", true); settings.SectionWidth = Number("sectionWidth", false);
        var value = configuration.Last("kanban.ticketBaseUrl");
        if (value == null) return settings;
        var probe = value.Value?.Replace("#TICKET#", "CFX-TICKET");
        if (!value.IsString || probe == null || probe.Length > 4096 || TopologyRenderPrimitives.SafeHref(probe) == null
            || !Uri.TryCreate(probe, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            error(value, "Mermaid configuration 'kanban.ticketBaseUrl' must be a safe absolute HTTP(S) template of at most 4096 characters.");
        else settings.TicketBaseUrl = value.Value;
        return settings;

        double? Number(string name, bool zero) {
            var setting = configuration.Last("kanban." + name);
            if (setting == null) return null;
            if (setting.TryGetNumber(out var number) && (zero ? number >= 0 : number > 0)) return number;
            error(setting, "Mermaid configuration '" + setting.Path + "' must be a finite " + (zero ? "non-negative" : "positive") + " number.");
            return null;
        }
    }
    internal string? Link(string? ticket) {
        if (TicketBaseUrl == null || string.IsNullOrWhiteSpace(ticket)) return null;
        var marker = TicketBaseUrl.IndexOf("#TICKET#", StringComparison.Ordinal);
        var target = marker < 0 ? TicketBaseUrl : TicketBaseUrl.Substring(0, marker) + Uri.EscapeDataString(ticket!) + TicketBaseUrl.Substring(marker + 8);
        return TopologyRenderPrimitives.SafeHref(target);
    }
}
