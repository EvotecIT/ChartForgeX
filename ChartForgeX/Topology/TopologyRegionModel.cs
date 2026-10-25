using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Topology;

/// <summary>Semantic presentation tones. Automatic uses the existing health status.</summary>
public enum TopologyTone {
    /// <summary>Use the model's health status.</summary>
    Automatic,
    /// <summary>A positive state.</summary>
    Good,
    /// <summary>A state requiring attention.</summary>
    Warn,
    /// <summary>A failing state.</summary>
    Bad,
    /// <summary>Informational context.</summary>
    Info,
    /// <summary>A neutral relationship or state.</summary>
    Neutral
}

internal static class TopologyRegionMetadata {
    internal static TopologyTone Tone(Dictionary<string, string> metadata) => metadata.TryGetValue("chartforgex.region.tone", out var text)
        && Enum.TryParse(text, out TopologyTone tone) && Enum.IsDefined(typeof(TopologyTone), tone) ? tone : TopologyTone.Automatic;
    internal static void Tone(Dictionary<string, string> metadata, TopologyTone tone) {
        TopologyModelGuards.EnumDefined(tone, nameof(tone));
        if (tone == TopologyTone.Automatic) metadata.Remove("chartforgex.region.tone"); else metadata["chartforgex.region.tone"] = tone.ToString();
    }
}

/// <summary>Relative importance of a circular node in the region presentation.</summary>
public enum TopologyNodeWeight {
    /// <summary>A leading node, placed first in an automatically arranged region.</summary>
    Primary,
    /// <summary>A normal node.</summary>
    Standard,
    /// <summary>A smaller supporting node.</summary>
    Minor
}

/// <summary>A named, domain-neutral glyph displayed above a node.</summary>
public sealed class TopologyNodeBadge {
    /// <summary>Creates a badge with a visible glyph and a full accessible meaning.</summary>
    public TopologyNodeBadge(string glyph, string meaning, bool informational = false) {
        Glyph = !string.IsNullOrWhiteSpace(glyph) ? glyph : throw new ArgumentException("A badge glyph is required.", nameof(glyph));
        Meaning = !string.IsNullOrWhiteSpace(meaning) ? meaning : throw new ArgumentException("A badge meaning is required.", nameof(meaning));
        Informational = informational;
    }
    /// <summary>Gets the shared glyph name: crown, star or lock.</summary>
    public string Glyph { get; }
    /// <summary>Gets the full host-provided badge meaning.</summary>
    public string Meaning { get; }
    /// <summary>Gets whether the badge uses the informational tone instead of the role tone.</summary>
    public bool Informational { get; }
}

public sealed partial class TopologyNode {
    /// <summary>Gets or sets the semantic ring tone, independent of the full health status wording.</summary>
    public TopologyTone Tone { get => TopologyRegionMetadata.Tone(Metadata); set => TopologyRegionMetadata.Tone(Metadata, value); }
    // Presentation facts live in the existing bounded metadata contract. Cloning, detached
    // preparation and older interchange readers preserve them without a second source of truth.
    private const string RegionKey = "chartforgex.region.";
    /// <summary>Gets or sets the region node's visual weight. Defaults to standard.</summary>
    public TopologyNodeWeight Weight {
        get => Metadata.TryGetValue(RegionKey + "weight", out var value) && Enum.TryParse(value, out TopologyNodeWeight weight)
            && Enum.IsDefined(typeof(TopologyNodeWeight), weight) ? weight : TopologyNodeWeight.Standard;
        set { TopologyModelGuards.EnumDefined(value, nameof(value)); Metadata[RegionKey + "weight"] = value.ToString(); }
    }
    /// <summary>Gets or sets host wording for the ring status, including diagnostic detail.</summary>
    public string? StatusText {
        get => Metadata.TryGetValue(RegionKey + "status", out var value) ? value : null;
        set { if (value == null) Metadata.Remove(RegionKey + "status"); else Metadata[RegionKey + "status"] = value; }
    }
    /// <summary>Gets named badges in insertion order. Only the first two are drawn; all appear in tooltips and details.</summary>
    public IReadOnlyList<TopologyNodeBadge> Badges {
        get {
            var badges = new List<TopologyNodeBadge>();
            for (var i = 0; Metadata.TryGetValue(RegionKey + "badge." + i.ToString(CultureInfo.InvariantCulture) + ".glyph", out var glyph); i++) {
                var prefix = RegionKey + "badge." + i.ToString(CultureInfo.InvariantCulture);
                if (Metadata.TryGetValue(prefix + ".meaning", out var meaning))
                    badges.Add(new TopologyNodeBadge(glyph, meaning, Metadata.TryGetValue(prefix + ".info", out var info) && info == "true"));
            }
            return badges.AsReadOnly();
        }
    }
    /// <summary>Adds a named badge, preserving every meaning in static and interactive exports.</summary>
    public TopologyNode AddBadge(TopologyNodeBadge badge) {
        if (badge == null) throw new ArgumentNullException(nameof(badge));
        var prefix = RegionKey + "badge." + Badges.Count.ToString(CultureInfo.InvariantCulture);
        Metadata[prefix + ".glyph"] = badge.Glyph; Metadata[prefix + ".meaning"] = badge.Meaning;
        Metadata[prefix + ".info"] = badge.Informational ? "true" : "false";
        return this;
    }
}

public sealed partial class TopologyEdge {
    /// <summary>Gets or sets the relationship tone. Automatic uses health, or information for overlays.</summary>
    public TopologyTone Tone { get => TopologyRegionMetadata.Tone(Metadata); set => TopologyRegionMetadata.Tone(Metadata, value); }
    /// <summary>Gets or sets whether this relationship spans regions in an underlay curve, or a narrow-screen chip.</summary>
    public bool IsOverlay {
        get => Metadata.TryGetValue("chartforgex.region.overlay", out var value) && value == "true";
        set => Metadata["chartforgex.region.overlay"] = value ? "true" : "false";
    }
}

public sealed partial class TopologyGroup {
    /// <summary>Gets or sets optional wording for a status chip in the region header. Null omits the chip.</summary>
    public string? StatusText {
        get => Metadata.TryGetValue("chartforgex.region.status", out var value) ? value : null;
        set { if (value == null) Metadata.Remove("chartforgex.region.status"); else Metadata["chartforgex.region.status"] = value; }
    }
}
