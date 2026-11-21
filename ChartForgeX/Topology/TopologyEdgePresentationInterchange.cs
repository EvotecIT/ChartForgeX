using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>Carries edge evidence, stripes, chips and the uplink end through interchange extensions and back.</summary>
/// <remarks>Each fact is one bounded <c>chartforgex.edge.*</c> entry. List parts are escaped, so colours, labels and chip text keep any character.</remarks>
internal static class TopologyEdgePresentationInterchange {
    private const string Prefix = "chartforgex.edge.";
    private const string EvidenceKey = Prefix + "evidence", EvidenceTextKey = Prefix + "evidenceText", UplinkKey = Prefix + "uplink";
    private const string StripesKey = Prefix + "stripes", ChipsKey = Prefix + "chips";

    internal static void Write(TopologyEdge edge, IDictionary<string, string> extensions) {
        if (edge.Evidence != TopologyEdgeEvidence.Unspecified) Set(extensions, EvidenceKey, edge.Evidence.ToString());
        if (!string.IsNullOrWhiteSpace(edge.EvidenceText)) Set(extensions, EvidenceTextKey, edge.EvidenceText!);
        if (edge.UplinkEnd != TopologyEdgeEnd.None) Set(extensions, UplinkKey, edge.UplinkEnd.ToString());
        if (edge.Stripes.Count > 0) Set(extensions, StripesKey, Join(edge.Stripes.Select(stripe => new[] { stripe.Color, stripe.Label ?? string.Empty })));
        if (edge.Chips.Count > 0) Set(extensions, ChipsKey, Join(edge.Chips.Select(chip => new[] { chip.Text, chip.Tone.Name, chip.IconId ?? string.Empty })));
    }

    /// <summary>Moves the facts out of restored metadata onto the edge.</summary>
    internal static void Read(TopologyEdge edge) {
        var metadata = edge.Metadata;
        if (Take(metadata, EvidenceKey) is { } evidence) edge.Evidence = Parse<TopologyEdgeEvidence>(evidence, "edge evidence");
        if (Take(metadata, EvidenceTextKey) is { } text) edge.EvidenceText = text;
        if (Take(metadata, UplinkKey) is { } uplink) edge.UplinkEnd = Parse<TopologyEdgeEnd>(uplink, "edge uplink end");
        if (Take(metadata, StripesKey) is { } stripes)
            foreach (var parts in Split(stripes, 2)) edge.Stripes.Add(new TopologyEdgeStripe { Color = parts[0], Label = parts[1].Length == 0 ? null : parts[1] });
        if (Take(metadata, ChipsKey) is { } chips)
            foreach (var parts in Split(chips, 3)) edge.Chips.Add(new TopologyEdgeChip { Text = parts[0], Tone = ParseTone(parts[1]), IconId = parts[2].Length == 0 ? null : parts[2] });
    }

    private static void Set(IDictionary<string, string> extensions, string key, string value) {
        if (!VisualArtifactInterchangeMapping.TrySetBoundedExtension(extensions, key, value))
            throw new NotSupportedException("Topology edge presentation '" + key + "' exceeds the interchange extension limits.");
    }

    private static string? Take(Dictionary<string, string> metadata, string key) {
        if (!metadata.TryGetValue(key, out var value)) return null;
        metadata.Remove(key);
        return value;
    }

    private static T Parse<T>(string value, string name) where T : struct, Enum =>
        Enum.TryParse(value, false, out T parsed) && Enum.IsDefined(typeof(T), parsed) ? parsed
            : throw new NotSupportedException("Unknown topology " + name + " '" + value + "' in interchange extensions.");

    private static Tone ParseTone(string value) => Tone.TryParse(value, out var tone) ? tone
        : throw new NotSupportedException("Unknown topology chip tone '" + value + "' in interchange extensions.");

    private static string Join(IEnumerable<string[]> items) => string.Join("|", items.Select(parts => string.Join(",", parts.Select(Uri.EscapeDataString))));

    private static IEnumerable<string[]> Split(string value, int count) => value.Split('|').Where(item => item.Length > 0).Select(item => {
        var parts = item.Split(',').Select(Uri.UnescapeDataString).ToArray();
        if (parts.Length != count) throw new NotSupportedException("Malformed topology edge presentation in interchange extensions.");
        return parts;
    });
}
