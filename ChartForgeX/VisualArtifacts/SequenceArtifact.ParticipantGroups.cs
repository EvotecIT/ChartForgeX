using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class SequenceArtifact {
    private readonly List<SequenceArtifactParticipantGroup> _participantGroups = new();

    /// <summary>Gets vertical groups of contiguous participants.</summary>
    public IReadOnlyList<SequenceArtifactParticipantGroup> ParticipantGroups => _participantGroups;

    /// <summary>Adds a vertical group spanning an inclusive, contiguous range of participants.</summary>
    public SequenceArtifact AddParticipantGroup(string id, string label, int firstParticipantIndex, int lastParticipantIndex, ChartColor? fill = null) {
        _participantGroups.Add(new SequenceArtifactParticipantGroup(id, label, firstParticipantIndex, lastParticipantIndex) { Fill = fill });
        return this;
    }
}

/// <summary>Describes an inclusive, contiguous participant range shown as a vertical group.</summary>
public sealed class SequenceArtifactParticipantGroup {
    /// <summary>Initializes a participant group.</summary>
    public SequenceArtifactParticipantGroup(string id, string label, int firstParticipantIndex, int lastParticipantIndex) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Group id is required.", nameof(id)) : id;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        FirstParticipantIndex = firstParticipantIndex;
        LastParticipantIndex = lastParticipantIndex;
    }

    /// <summary>Gets the stable group identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the visible group label.</summary>
    public string Label { get; }

    /// <summary>Gets the first participant index, inclusive.</summary>
    public int FirstParticipantIndex { get; }

    /// <summary>Gets the last participant index, inclusive.</summary>
    public int LastParticipantIndex { get; }

    /// <summary>Gets or sets the background fill; null produces a transparent group.</summary>
    public ChartColor? Fill { get; set; }

    /// <summary>Gets host metadata such as source locations.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}
