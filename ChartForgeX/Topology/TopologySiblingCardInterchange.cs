using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>Carries a sibling card's members through interchange extensions, so a saved grouped chart reopens with its rows.</summary>
/// <remarks>The grouped members exist only on the card, so their facts are written in full. Members are escaped and split into
/// chunks below the extension text limit.</remarks>
internal static class TopologySiblingCardInterchange {
    private const string Prefix = "chartforgex.siblings.";
    private const string HeaderKey = Prefix + "card", MembersKey = Prefix + "members.";
    private const int ChunkCharacters = 60000;

    internal static void Write(TopologyNode node, IDictionary<string, string> extensions) {
        if (node.SiblingGroup is not { } card) return;
        Set(extensions, HeaderKey, string.Join(",", Escape(card.ParentId), card.VisibleCount.ToString(CultureInfo.InvariantCulture), card.Collapsed ? "collapsed" : "open"));
        var rows = card.Members.Select(member => string.Join(",", new[] {
            member.Id, member.Label, member.Subtitle, member.Badge, member.Status.ToString(), member.Kind.ToString(),
            member.IconId, member.Symbol, member.Color, member.Href, member.Tooltip }.Select(Escape))).ToArray();
        var chunk = new List<string>(); var length = 0; var index = 0;
        foreach (var row in rows) {
            if (length + row.Length + 1 > ChunkCharacters && chunk.Count > 0) { Flush(); }
            chunk.Add(row); length += row.Length + 1;
        }
        Flush();

        void Flush() {
            if (chunk.Count == 0) return;
            Set(extensions, MembersKey + index.ToString(CultureInfo.InvariantCulture), string.Join("|", chunk));
            index++; chunk.Clear(); length = 0;
        }
    }

    /// <summary>Moves a saved card out of restored metadata back onto its node.</summary>
    internal static void Read(TopologyNode node) {
        if (!node.Metadata.TryGetValue(HeaderKey, out var header)) return;
        node.Metadata.Remove(HeaderKey);
        var parts = header.Split(',');
        if (parts.Length != 3 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var visible))
            throw new NotSupportedException("Malformed topology sibling card in interchange extensions.");
        var members = new List<TopologySiblingMember>();
        for (var index = 0; node.Metadata.TryGetValue(MembersKey + index.ToString(CultureInfo.InvariantCulture), out var chunk); index++) {
            node.Metadata.Remove(MembersKey + index.ToString(CultureInfo.InvariantCulture));
            foreach (var row in chunk.Split('|').Where(row => row.Length > 0)) {
                var fields = row.Split(',').Select(Unescape).ToArray();
                if (fields.Length != 11 || fields[0] == null || fields[1] == null) throw new NotSupportedException("Malformed topology sibling member in interchange extensions.");
                members.Add(new TopologySiblingMember(fields[0]!, fields[1]!, fields[2], fields[3], Parse<TopologyHealthStatus>(fields[4]), Parse<TopologyNodeKind>(fields[5]),
                    fields[6], fields[7], fields[8], fields[9], fields[10]));
            }
        }
        if (members.Count == 0 || visible < 1 || visible > members.Count) throw new NotSupportedException("Malformed topology sibling card in interchange extensions.");
        node.SiblingGroup = new TopologySiblingCard(Unescape(parts[0]) ?? string.Empty, members, visible, parts[2] == "collapsed");
        node.PreserveDisplayModeSize = true;
    }

    private static void Set(IDictionary<string, string> extensions, string key, string value) {
        if (!VisualArtifactInterchangeMapping.TrySetBoundedExtension(extensions, key, value))
            throw new NotSupportedException("Topology sibling card '" + key + "' exceeds the interchange extension limits.");
    }

    // A missing value is written as "~" so it stays apart from an empty string.
    private static string Escape(string? value) => value == null ? "~" : Uri.EscapeDataString(value);
    private static string? Unescape(string value) => value == "~" ? null : Uri.UnescapeDataString(value);

    private static T Parse<T>(string? value) where T : struct =>
        value != null && Enum.TryParse(value, false, out T parsed) && Enum.IsDefined(typeof(T), parsed) ? parsed
            : throw new NotSupportedException("Unknown topology sibling member value '" + value + "' in interchange extensions.");
}
