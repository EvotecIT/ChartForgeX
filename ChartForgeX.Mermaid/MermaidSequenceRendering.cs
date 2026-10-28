using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid sequence diagrams.
/// </summary>
public static partial class MermaidSequenceRendering {
    /// <summary>
    /// Converts a Mermaid sequence document into a product-neutral sequence artifact.
    /// </summary>
    /// <param name="document">The parsed Mermaid sequence document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>A sequence artifact that can render static SVG and PNG previews.</returns>
    public static SequenceArtifact ToSequenceArtifact(this MermaidSequenceDocument document, MermaidSequenceRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        using var resourceScope = MermaidResourceGuard.BeginIfAbsent(document.CapturedResourceLimits);
        MermaidResourceGuard.ValidateDocument(document);
        options ??= new MermaidSequenceRenderOptions();
        var artifactId = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-sequence" : options.Id!.Trim();
        var sequence = SequenceArtifact.Create(artifactId)
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width, options.Height);
        sequence.Padding = options.Padding;
        sequence.Metadata["mermaid.kind"] = document.Kind.ToString();
        sequence.Metadata["mermaid.header"] = document.Header;
        sequence.Metadata["mermaid.participants"] = document.Participants.Count.ToString(CultureInfo.InvariantCulture);
        sequence.Metadata["mermaid.messages"] = document.Messages.Count.ToString(CultureInfo.InvariantCulture);
        sequence.Metadata["mermaid.activations"] = document.Activations.Count.ToString(CultureInfo.InvariantCulture);
        sequence.Metadata["mermaid.notes"] = document.Notes.Count.ToString(CultureInfo.InvariantCulture);
        sequence.Metadata["mermaid.blocks"] = document.Blocks.Count.ToString(CultureInfo.InvariantCulture);


        foreach (var participant in document.Participants) {
            sequence.AddParticipant(participant.Id, MermaidLabelText.DisplayCaption(string.IsNullOrWhiteSpace(participant.Alias) ? participant.Id : participant.Alias!), participant.Kind);
            var target = sequence.Participants[sequence.Participants.Count - 1];
            target.IsImplicit = participant.IsImplicit;
            target.Metadata["mermaid.id"] = participant.Id;
            target.Metadata["mermaid.kind"] = participant.Kind.ToString();
            var rawLabel = participant.Alias ?? participant.Id;
            if (target.Label != rawLabel) target.Metadata["mermaid.label"] = rawLabel;
            target.Metadata["mermaid.source.line"] = participant.Span.Line.ToString(CultureInfo.InvariantCulture);
            if (participant.Configuration != null) target.Metadata["mermaid.configuration"] = participant.Configuration;
        }

        AddParticipantLifetimesAndBoxes(document, sequence);

        MermaidSequenceParticipantData.Project(document,
            (id, key, value) => FindParticipant(sequence, id)!.Metadata[key] = value,
            (id, href) => FindParticipant(sequence, id)!.Href = href,
            navigation:(id,links)=>FindParticipant(sequence,id)!.NavigationLinks=links);

        foreach (var message in document.Messages) {
            sequence.AddMessage(message.SourceId, message.TargetId, message.Text == null ? null : MermaidLabelText.DisplayCaption(message.Text), ToLineStyle(message.Operator), ToMessageKind(message.Operator));
            var target = sequence.Messages[sequence.Messages.Count - 1];
            target.ActivatesTarget = message.ActivatesTarget;
            target.Deactivates = message.Deactivates;
            target.Metadata["mermaid.operator"] = message.Operator;
            if (message.Text != null && target.Text != message.Text) target.Metadata["mermaid.text"] = message.Text;
            target.Metadata["mermaid.source.line"] = message.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = message.Span.Column.ToString(CultureInfo.InvariantCulture);
            if (message.IsCentralConnection) target.Metadata["mermaid.centralConnection"] = "true";
        }

        foreach (var activation in document.Activations) {
            sequence.AddActivation(activation.ParticipantId, activation.Active, CountMessagesBefore(document, activation.Span));
            var target = sequence.Activations[sequence.Activations.Count - 1];
            target.Metadata["mermaid.source.line"] = activation.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = activation.Span.Column.ToString(CultureInfo.InvariantCulture);
        }

        foreach (var note in document.Notes) {
            sequence.AddNote(ToNotePlacement(note.Placement), note.ParticipantIds, MermaidLabelText.DisplayCaption(note.Text ?? string.Empty));
            sequence.Notes[sequence.Notes.Count - 1].StepIndex = CountMessagesBefore(document, note.Span);
            sequence.Notes[sequence.Notes.Count - 1].SourceOrder = SequenceOrder(note.Span);
        }

        AddBlocks(document, sequence);
        AddNumbering(document, sequence);
        MermaidPresentation.Apply(sequence, document, options.Presentation);
        if(options.HasExplicitPadding&&sequence.Layout?.CanvasPadding!=null)
            sequence.Layout=sequence.Layout.WithCanvasPadding(ChartForgeX.Primitives.ChartPadding.All(options.Padding));
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
        var artifact = sequence.ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.participants"] = document.Participants.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.messages"] = document.Messages.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.activations"] = document.Activations.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.notes"] = document.Notes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(SequenceArtifact);
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>
    /// Renders a Mermaid sequence document to static SVG through ChartForgeX sequence rendering.
    /// </summary>
    public static string ToSvg(this MermaidSequenceDocument document, MermaidSequenceRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid sequence document to static PNG through ChartForgeX sequence rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidSequenceDocument document, MermaidSequenceRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToPng();

    private static void AddBlocks(MermaidSequenceDocument document, SequenceArtifact sequence) {
        var stack = new Stack<BlockStart>();
        foreach (var block in document.Blocks) {
            if (IsBranch(block.Kind)) {
                if (stack.Count == 0 || !stack.Peek().IsBranching) continue;
                var current = stack.Pop();
                int boundary = CountMessagesBefore(document, block.Span);
                current.CompleteBranch(sequence, boundary, SequenceOrder(block.Span));
                current = current.NextBranch(block.Kind.ToString(), MermaidLabelText.DisplayCaption(block.Text ?? string.Empty), boundary, SequenceOrder(block.Span));
                stack.Push(current);
                continue;
            }
            if (block.Kind == MermaidSequenceBlockKind.End) {
                if (stack.Count == 0) continue;
                var start = stack.Pop();
                int boundary = CountMessagesBefore(document, block.Span);
                start.CompleteBranch(sequence, boundary, SequenceOrder(block.Span));
                bool isEmpty = boundary <= start.StepIndex;
                sequence.AddBlock(start.Kind, start.Text, start.StepIndex, isEmpty ? start.StepIndex : boundary - 1, isEmpty, start.Depth);
                var target = sequence.Blocks[sequence.Blocks.Count - 1];
                target.SourceOrder = start.SourceOrder;
                target.EndSourceOrder = SequenceOrder(block.Span);
                target.CloseStepIndex = boundary;
                if (start.Kind == SequenceArtifactBlockKind.Rect) target.BackgroundColor = RectangleBackground(document, start.Text);
                continue;
            }

            var kind = ToBlockKind(block.Kind);
            if (!kind.HasValue) continue;
            stack.Push(new BlockStart(kind.Value, MermaidLabelText.DisplayCaption(block.Text ?? string.Empty), CountMessagesBefore(document, block.Span), block.Depth, SequenceOrder(block.Span)));
        }
    }

    private static long SequenceOrder(MermaidSourceSpan span) => ((long)span.Line << 32) + (uint)span.Column;

    private static int CountMessagesBefore(MermaidSequenceDocument document, MermaidSourceSpan span) {
        var count = 0;
        foreach (var message in document.Messages) {
            if (message.Span.Line < span.Line || message.Span.Line == span.Line && message.Span.Column < span.Column) count++;
        }

        return count;
    }

    private static SequenceArtifactParticipant? FindParticipant(SequenceArtifact sequence, string id) {
        for (var index = 0; index < sequence.Participants.Count; index++) {
            if (string.Equals(sequence.Participants[index].Id, id, StringComparison.Ordinal)) return sequence.Participants[index];
        }

        return null;
    }

    private static string ResolveTitle(MermaidSequenceDocument document, MermaidSequenceRenderOptions options) =>
        MermaidHeading.Title(document, options.Title, document.Title, "Mermaid sequence");

    private static SequenceArtifactMessageLineStyle ToLineStyle(string messageOperator) =>
        messageOperator.IndexOf("--", StringComparison.Ordinal) >= 0 ? SequenceArtifactMessageLineStyle.Dashed : SequenceArtifactMessageLineStyle.Solid;

    private static SequenceArtifactMessageKind ToMessageKind(string messageOperator) {
        if (messageOperator.IndexOf(')') >= 0) return SequenceArtifactMessageKind.Async;
        // Mermaid dashes and terminal glyphs describe how a message is drawn,
        // not whether its domain purpose is a return or an event. Preserve the
        // exact operator in metadata and use the neutral call semantic unless
        // Mermaid explicitly supplies its asynchronous open-arrow form.
        return SequenceArtifactMessageKind.Call;
    }

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
            case MermaidSequenceBlockKind.ParOver:
                return SequenceArtifactBlockKind.ParOver;
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
        public BlockStart(SequenceArtifactBlockKind kind, string text, int stepIndex, int depth, long sourceOrder) {
            Kind = kind;
            Text = text;
            StepIndex = stepIndex;
            Depth = depth;
            BranchKind = "Primary";
            BranchText = text;
            BranchStepIndex = stepIndex;
            SourceOrder = sourceOrder;
            BranchOrder = sourceOrder;
        }

        private BlockStart(SequenceArtifactBlockKind kind, string text, int stepIndex, int depth, long sourceOrder, string branchKind, string branchText, int branchStepIndex, long branchOrder) {
            Kind = kind;
            Text = text;
            StepIndex = stepIndex;
            Depth = depth;
            BranchKind = branchKind;
            BranchText = branchText;
            BranchStepIndex = branchStepIndex;
            SourceOrder = sourceOrder;
            BranchOrder = branchOrder;
        }

        public long SourceOrder { get; }
        public long BranchOrder { get; }
        public SequenceArtifactBlockKind Kind { get; }
        public string Text { get; }
        public int StepIndex { get; }
        public int Depth { get; }
        public string BranchKind { get; }
        public string BranchText { get; }
        public int BranchStepIndex { get; }
        public bool IsBranching => Kind == SequenceArtifactBlockKind.Alt || Kind == SequenceArtifactBlockKind.Par || Kind == SequenceArtifactBlockKind.ParOver || Kind == SequenceArtifactBlockKind.Critical;

        public void CompleteBranch(SequenceArtifact sequence, int exclusiveEndStepIndex, long endOrder) {
            if (!IsBranching) return;
            bool isEmpty = exclusiveEndStepIndex <= BranchStepIndex;
            sequence.AddBranch(Kind, BranchKind, BranchText, BranchStepIndex, isEmpty ? BranchStepIndex : exclusiveEndStepIndex - 1, Depth, isEmpty);
            var target = sequence.Branches[sequence.Branches.Count - 1];
            target.SourceOrder = BranchOrder;
            target.EndSourceOrder = endOrder;
            target.CloseStepIndex = exclusiveEndStepIndex;
        }

        public BlockStart NextBranch(string branchKind, string branchText, int branchStepIndex, long branchOrder) =>
            new(Kind, Text, StepIndex, Depth, SourceOrder, branchKind, branchText, branchStepIndex, branchOrder);
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid sequence diagrams.
/// </summary>
public sealed class MermaidSequenceRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the natural preview width.</summary>
    public double Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public double Height { get; set; } = 560;

    private double _padding=32;
    internal bool HasExplicitPadding { get; private set; }
    /// <summary>Gets or sets explicit host preview padding. An authored value overrides both source diagram-margin axes.</summary>
    public double Padding { get=>_padding; set { _padding=value;HasExplicitPadding=true; } }
}
