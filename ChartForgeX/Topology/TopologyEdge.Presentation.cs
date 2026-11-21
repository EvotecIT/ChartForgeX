using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>States how a relationship is known.</summary>
public enum TopologyEdgeEvidence {
    /// <summary>No evidence is recorded. The edge renders as before.</summary>
    Unspecified,
    /// <summary>The relationship was observed or configured explicitly.</summary>
    Confirmed,
    /// <summary>The relationship is assumed or inferred. It renders dashed unless the edge sets its own line style.</summary>
    Assumed
}

/// <summary>Names one end of a topology edge.</summary>
public enum TopologyEdgeEnd {
    /// <summary>Neither end.</summary>
    None,
    /// <summary>The source end.</summary>
    Source,
    /// <summary>The target end.</summary>
    Target
}

/// <summary>One colour band of a striped edge, such as a VLAN or a transport on a site link.</summary>
public sealed class TopologyEdgeStripe {
    private string _color = string.Empty;

    /// <summary>Gets or sets the stripe colour.</summary>
    public string Color { get => _color; set => _color = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stripe colours are required.", nameof(value)) : value.Trim(); }

    /// <summary>Gets or sets the legend label for this colour. Unlabelled stripes are drawn but not listed.</summary>
    public string? Label { get; set; }

    internal TopologyEdgeStripe Clone() => new() { Color = Color, Label = Label };
}

/// <summary>A small value chip drawn on an edge, such as PoE watts, replication cost or lag.</summary>
public sealed class TopologyEdgeChip {
    private string _text = string.Empty;
    private Tone _tone = Tone.Neutral;

    /// <summary>Gets or sets the chip text, drawn in a monospace face.</summary>
    public string Text { get => _text; set => _text = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Chip text is required.", nameof(value)) : value.Trim(); }

    /// <summary>Gets or sets an optional icon id drawn before the text.</summary>
    public string? IconId { get; set; }

    /// <summary>Gets or sets the chip tone. <see cref="Tone.None"/> follows the edge status.</summary>
    public Tone Tone { get => _tone; set => _tone = value; }

    internal TopologyEdgeChip Clone() => new() { Text = Text, IconId = IconId, Tone = Tone };
}

public sealed partial class TopologyEdge {
    private TopologyEdgeEvidence _evidence;
    private TopologyEdgeEnd _uplinkEnd;

    /// <summary>Gets or sets whether the relationship is confirmed or assumed.</summary>
    /// <remarks>Assumed edges render with a short dash and a legend entry. An explicit <see cref="LineStyle"/> or
    /// <see cref="DashPattern"/> still wins, and a disabled status keeps its own dotted pattern.</remarks>
    public TopologyEdgeEvidence Evidence {
        get => _evidence;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _evidence = value;
        }
    }

    /// <summary>Gets or sets text explaining the evidence, for example how an assumed link was inferred. It is appended to the tooltip.</summary>
    public string? EvidenceText { get; set; }

    /// <summary>Gets the colours drawn as repeating stripes along the edge. Two or more stripes replace the single edge colour.</summary>
    public List<TopologyEdgeStripe> Stripes { get; } = new();

    /// <summary>Gets value chips drawn along the edge.</summary>
    public List<TopologyEdgeChip> Chips { get; } = new();

    /// <summary>Gets or sets the child end that shows an uplink marker (↑) on its endpoint label.</summary>
    public TopologyEdgeEnd UplinkEnd {
        get => _uplinkEnd;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _uplinkEnd = value;
        }
    }

    internal void CopyPresentationTo(TopologyEdge copy) {
        copy.Evidence = Evidence;
        copy.EvidenceText = EvidenceText;
        copy.UplinkEnd = UplinkEnd;
        foreach (var stripe in Stripes) copy.Stripes.Add(stripe.Clone());
        foreach (var chip in Chips) copy.Chips.Add(chip.Clone());
    }
}
