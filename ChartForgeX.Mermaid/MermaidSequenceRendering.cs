using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid sequence diagrams.
/// </summary>
public static class MermaidSequenceRendering {
    /// <summary>
    /// Converts a Mermaid sequence document into a product-neutral sequence artifact.
    /// </summary>
    /// <param name="document">The parsed Mermaid sequence document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>A sequence artifact that can render static SVG and PNG previews.</returns>
    public static SequenceArtifact ToSequenceArtifact(this MermaidSequenceDocument document, MermaidSequenceRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidSequenceRenderOptions();
        var artifactId = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-sequence" : options.Id!.Trim();
        var sequence = SequenceArtifact.Create(artifactId)
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height);
        sequence.Padding = options.Padding;
        sequence.Metadata["mermaid.kind"] = document.Kind.ToString();
        sequence.Metadata["mermaid.header"] = document.Header;
        sequence.Metadata["mermaid.participants"] = document.Participants.Count.ToString(CultureInfo.InvariantCulture);
        sequence.Metadata["mermaid.messages"] = document.Messages.Count.ToString(CultureInfo.InvariantCulture);
        sequence.Metadata["mermaid.notes"] = document.Notes.Count.ToString(CultureInfo.InvariantCulture);
        sequence.Metadata["mermaid.blocks"] = document.Blocks.Count.ToString(CultureInfo.InvariantCulture);
        if (document.Autonumber != null) {
            sequence.Metadata["mermaid.autonumber"] = "true";
            if (document.Autonumber.Start != null) sequence.Metadata["mermaid.autonumber.start"] = document.Autonumber.Start;
            if (document.Autonumber.Increment != null) sequence.Metadata["mermaid.autonumber.increment"] = document.Autonumber.Increment;
        }

        foreach (var participant in document.Participants) {
            sequence.AddParticipant(participant.Id, string.IsNullOrWhiteSpace(participant.Alias) ? participant.Id : participant.Alias, ToParticipantKind(participant.Kind));
            var target = sequence.Participants[sequence.Participants.Count - 1];
            target.IsImplicit = participant.IsImplicit;
            target.Metadata["mermaid.id"] = participant.Id;
            target.Metadata["mermaid.kind"] = participant.Kind.ToString();
            target.Metadata["mermaid.source.line"] = participant.Span.Line.ToString(CultureInfo.InvariantCulture);
            if (participant.Configuration != null) target.Metadata["mermaid.configuration"] = participant.Configuration;
        }

        foreach (var link in document.Links) {
            var participant = FindParticipant(sequence, link.ParticipantId);
            if (participant == null) continue;
            if (link.Label != null) participant.Metadata["mermaid.link.label"] = link.Label;
            if (link.Url != null) participant.Metadata["mermaid.link.url"] = link.Url;
            if (link.RawJson != null) participant.Metadata["mermaid.links.json"] = link.RawJson;
        }

        foreach (var message in document.Messages) {
            sequence.AddMessage(message.SourceId, message.TargetId, message.Text, ToLineStyle(message.Operator));
            var target = sequence.Messages[sequence.Messages.Count - 1];
            target.ActivatesTarget = message.ActivatesTarget;
            target.Deactivates = message.Deactivates;
            target.Metadata["mermaid.operator"] = message.Operator;
            target.Metadata["mermaid.source.line"] = message.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = message.Span.Column.ToString(CultureInfo.InvariantCulture);
            if (message.IsCentralConnection) target.Metadata["mermaid.centralConnection"] = "true";
        }

        foreach (var note in document.Notes) {
            sequence.AddNote(ToNotePlacement(note.Placement), note.ParticipantIds, note.Text ?? string.Empty);
            sequence.Notes[sequence.Notes.Count - 1].StepIndex = CountMessagesBefore(document, note.Span.Line);
        }

        AddBlocks(document, sequence);
        return sequence;
    }

    /// <summary>
    /// Wraps a Mermaid sequence document in a product-neutral visual artifact envelope backed by a sequence artifact.
    /// </summary>
    /// <param name="document">The parsed Mermaid sequence document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>A visual artifact envelope.</returns>
    public static VisualArtifact ToVisualArtifact(this MermaidSequenceDocument document, MermaidSequenceRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var sequence = document.ToSequenceArtifact(options);
        var artifact = VisualArtifact.Create(sequence.Id, VisualArtifactKind.Mermaid, sequence);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = sequence.Title;
        artifact.Subtitle = sequence.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(sequence.Width, sequence.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.participants"] = document.Participants.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.messages"] = document.Messages.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.notes"] = document.Notes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(SequenceArtifact);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid sequence document to static SVG through ChartForgeX sequence rendering.
    /// </summary>
    public static string ToSvg(this MermaidSequenceDocument document, MermaidSequenceRenderOptions? options = null) =>
        document.ToSequenceArtifact(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid sequence document to static PNG through ChartForgeX sequence rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidSequenceDocument document, MermaidSequenceRenderOptions? options = null) =>
        document.ToSequenceArtifact(options).ToPng();

    private static void AddBlocks(MermaidSequenceDocument document, SequenceArtifact sequence) {
        var stack = new Stack<BlockStart>();
        foreach (var block in document.Blocks) {
            if (IsBranch(block.Kind)) continue;
            if (block.Kind == MermaidSequenceBlockKind.End) {
                if (stack.Count == 0) continue;
                var start = stack.Pop();
                sequence.AddBlock(start.Kind, start.Text, start.StepIndex, CountMessagesBefore(document, block.Span.Line));
                continue;
            }

            var kind = ToBlockKind(block.Kind);
            if (!kind.HasValue) continue;
            stack.Push(new BlockStart(kind.Value, block.Text ?? string.Empty, CountMessagesBefore(document, block.Span.Line)));
        }
    }

    private static int CountMessagesBefore(MermaidSequenceDocument document, int line) {
        var count = 0;
        foreach (var message in document.Messages) {
            if (message.Span.Line < line) count++;
        }

        return count;
    }

    private static SequenceArtifactParticipant? FindParticipant(SequenceArtifact sequence, string id) {
        for (var index = 0; index < sequence.Participants.Count; index++) {
            if (string.Equals(sequence.Participants[index].Id, id, StringComparison.Ordinal)) return sequence.Participants[index];
        }

        return null;
    }

    private static string ResolveTitle(MermaidSequenceDocument document, MermaidSequenceRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        var frontMatterTitle = FindFrontMatterValue(document.FrontMatter, "title");
        return string.IsNullOrWhiteSpace(frontMatterTitle) ? "Mermaid sequence" : frontMatterTitle!;
    }

    private static string ResolveSubtitle(MermaidSequenceDocument document, MermaidSequenceRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static string? FindFrontMatterValue(string? frontMatter, string key) {
        if (string.IsNullOrWhiteSpace(frontMatter)) return null;
        var lines = frontMatter!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var prefix = key + ":";
        foreach (var line in lines) {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var value = trimmed.Substring(prefix.Length).Trim();
            return value.Trim('"', '\'');
        }

        return null;
    }

    private static SequenceArtifactParticipantKind ToParticipantKind(MermaidSequenceParticipantKind kind) {
        switch (kind) {
            case MermaidSequenceParticipantKind.Actor:
                return SequenceArtifactParticipantKind.Actor;
            case MermaidSequenceParticipantKind.Boundary:
                return SequenceArtifactParticipantKind.Boundary;
            case MermaidSequenceParticipantKind.Control:
                return SequenceArtifactParticipantKind.Control;
            case MermaidSequenceParticipantKind.Entity:
                return SequenceArtifactParticipantKind.Entity;
            case MermaidSequenceParticipantKind.Database:
                return SequenceArtifactParticipantKind.Database;
            case MermaidSequenceParticipantKind.Collections:
                return SequenceArtifactParticipantKind.Collections;
            case MermaidSequenceParticipantKind.Queue:
                return SequenceArtifactParticipantKind.Queue;
            default:
                return SequenceArtifactParticipantKind.Participant;
        }
    }

    private static SequenceArtifactMessageLineStyle ToLineStyle(string messageOperator) =>
        messageOperator.StartsWith("--", StringComparison.Ordinal) ? SequenceArtifactMessageLineStyle.Dashed : SequenceArtifactMessageLineStyle.Solid;

    private static SequenceArtifactNotePlacement ToNotePlacement(string placement) {
        switch (placement.Trim().ToLowerInvariant()) {
            case "left of":
                return SequenceArtifactNotePlacement.LeftOf;
            case "over":
                return SequenceArtifactNotePlacement.Over;
            default:
                return SequenceArtifactNotePlacement.RightOf;
        }
    }

    private static bool IsBranch(MermaidSequenceBlockKind kind) => kind == MermaidSequenceBlockKind.Else || kind == MermaidSequenceBlockKind.And || kind == MermaidSequenceBlockKind.Option;

    private static SequenceArtifactBlockKind? ToBlockKind(MermaidSequenceBlockKind kind) {
        switch (kind) {
            case MermaidSequenceBlockKind.Loop:
                return SequenceArtifactBlockKind.Loop;
            case MermaidSequenceBlockKind.Alt:
                return SequenceArtifactBlockKind.Alt;
            case MermaidSequenceBlockKind.Opt:
                return SequenceArtifactBlockKind.Opt;
            case MermaidSequenceBlockKind.Par:
                return SequenceArtifactBlockKind.Par;
            case MermaidSequenceBlockKind.Critical:
                return SequenceArtifactBlockKind.Critical;
            case MermaidSequenceBlockKind.Rect:
                return SequenceArtifactBlockKind.Rect;
            case MermaidSequenceBlockKind.Break:
                return SequenceArtifactBlockKind.Break;
            default:
                return null;
        }
    }

    private readonly struct BlockStart {
        public BlockStart(SequenceArtifactBlockKind kind, string text, int stepIndex) {
            Kind = kind;
            Text = text;
            StepIndex = stepIndex;
        }

        public SequenceArtifactBlockKind Kind { get; }
        public string Text { get; }
        public int StepIndex { get; }
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid sequence diagrams.
/// </summary>
public sealed class MermaidSequenceRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public double Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public double Height { get; set; } = 560;

    /// <summary>Gets or sets the preview padding.</summary>
    public double Padding { get; set; } = 32;
}
