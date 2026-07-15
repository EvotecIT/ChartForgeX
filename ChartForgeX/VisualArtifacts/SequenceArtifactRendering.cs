using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Provides static preview rendering helpers for sequence artifacts.
/// </summary>
public static class SequenceArtifactRendering {
    /// <summary>
    /// Wraps a sequence artifact in a product-neutral visual artifact envelope.
    /// </summary>
    /// <param name="sequence">The sequence artifact.</param>
    /// <param name="sourceLanguage">The source language that produced the artifact.</param>
    /// <returns>A visual artifact envelope.</returns>
    public static VisualArtifact ToVisualArtifact(this SequenceArtifact sequence, VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        var layout = SequenceLayout.Calculate(sequence);
        var artifact = VisualArtifact.Create(sequence.Id, VisualArtifactKind.Sequence, sequence);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Title = sequence.Title;
        artifact.Subtitle = sequence.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(layout.Width, layout.Height);
        artifact.ExportFormats = sequence.ExportFormats;
        artifact.Metadata["sequence.participants"] = sequence.Participants.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["sequence.messages"] = sequence.Messages.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["sequence.notes"] = sequence.Notes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(SequenceArtifact);
        for (var index = 0; index < layout.Participants.Count; index++) {
            var participant = layout.Participants[index];
            artifact.Regions.Add(new VisualArtifactRegion {
                Id = participant.Participant.Id,
                Kind = "sequence-participant",
                Label = participant.Participant.Label,
                Bounds = new ChartRect(participant.BoxX, layout.ParticipantBoxY, participant.BoxWidth, SequenceLayout.ParticipantBoxHeight)
            });
        }

        return artifact;
    }

    /// <summary>
    /// Renders a sequence artifact static preview to SVG.
    /// </summary>
    /// <param name="sequence">The sequence artifact.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this SequenceArtifact sequence) {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        var layout = SequenceLayout.Calculate(sequence);
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("viewBox", "0 0 " + F(layout.Width) + " " + F(layout.Height))
            .Attribute("width", layout.Width)
            .Attribute("height", layout.Height)
            .Attribute("role", "img")
            .Attribute("aria-label", sequence.Title.Length == 0 ? sequence.Id : sequence.Title)
            .EndStartElement().Line();

        writer.StartElement("defs").EndStartElement().Line();
        writer.StartElement("marker").Attribute("id", sequence.Id + "-arrow").Attribute("viewBox", "0 0 10 10").Attribute("refX", 9).Attribute("refY", 5).Attribute("markerWidth", 8).Attribute("markerHeight", 8).Attribute("orient", "auto-start-reverse").EndStartElement();
        writer.StartElement("path").Attribute("d", "M 0 0 L 10 5 L 0 10 z").Attribute("fill", "#334155").EndEmptyElement();
        writer.EndElement().Line();
        writer.EndElement().Line();

        writer.StartElement("rect").Attribute("data-cfx-role", "sequence-background").Attribute("x", 0).Attribute("y", 0).Attribute("width", layout.Width).Attribute("height", layout.Height).Attribute("rx", 8).Attribute("fill", "#ffffff").EndEmptyElement().Line();
        writer.StartElement("rect").Attribute("data-cfx-role", "sequence-frame").Attribute("x", 0.5).Attribute("y", 0.5).Attribute("width", layout.Width - 1).Attribute("height", layout.Height - 1).Attribute("rx", 8).Attribute("fill", "none").Attribute("stroke", "#cbd5e1").EndEmptyElement().Line();
        var titleY = sequence.Padding;
        if (sequence.Title.Length > 0) {
            writer.StartElement("text").Attribute("data-cfx-role", "sequence-title").Attribute("x", sequence.Padding).Attribute("y", titleY).Attribute("fill", "#0f172a").Attribute("font-family", FontFamily).Attribute("font-size", 20).Attribute("font-weight", "800").Text(sequence.Title).EndElement().Line();
            titleY += 24;
        }

        if (sequence.Subtitle.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "sequence-subtitle").Attribute("x", sequence.Padding).Attribute("y", titleY).Attribute("fill", "#64748b").Attribute("font-family", FontFamily).Attribute("font-size", 13).Text(sequence.Subtitle).EndElement().Line();

        foreach (var block in layout.Blocks) {
            writer.StartElement("rect").Attribute("data-cfx-role", "sequence-block").Attribute("data-kind", block.Block.Kind.ToString()).Attribute("x", sequence.Padding).Attribute("y", block.Y).Attribute("width", layout.Width - sequence.Padding * 2).Attribute("height", block.Height).Attribute("rx", 6).Attribute("fill", "#f8fafc").Attribute("stroke", "#dbe4ef").EndEmptyElement().Line();
            if (block.Block.Text.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "sequence-block-label").Attribute("x", sequence.Padding + 10).Attribute("y", block.Y + 18).Attribute("fill", "#475569").Attribute("font-family", FontFamily).Attribute("font-size", 12).Attribute("font-weight", "700").Text(block.Block.Kind + ": " + block.Block.Text).EndElement().Line();
        }

        foreach (var participant in layout.Participants) WriteParticipantSvg(writer, layout, participant);
        foreach (var message in layout.Messages) WriteMessageSvg(writer, sequence, layout, message);
        foreach (var note in layout.Notes) WriteNoteSvg(writer, note);

        writer.EndElement();
        return writer.Build();
    }

    /// <summary>
    /// Renders a sequence artifact static preview to PNG.
    /// </summary>
    /// <param name="sequence">The sequence artifact.</param>
    /// <returns>PNG bytes.</returns>
    public static byte[] ToPng(this SequenceArtifact sequence) {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        var layout = SequenceLayout.Calculate(sequence);
        var canvas = new RgbaCanvas((int)Math.Ceiling(layout.Width), (int)Math.Ceiling(layout.Height), 2, null);
        canvas.Clear(ChartColor.White);
        var border = Color("#cbd5e1");
        const double surfaceInset = 8;
        canvas.FillRoundedRect(surfaceInset, surfaceInset, layout.Width - surfaceInset * 2, layout.Height - surfaceInset * 2, 8, ChartColor.White);
        canvas.StrokeRoundedRect(surfaceInset + 0.5, surfaceInset + 0.5, layout.Width - surfaceInset * 2 - 1, layout.Height - surfaceInset * 2 - 1, 8, border, 1);
        var titleY = sequence.Padding - 17;
        if (sequence.Title.Length > 0) {
            canvas.DrawTextEmphasized(sequence.Padding, titleY, Fit(sequence.Title, 20, layout.Width - sequence.Padding * 2), Color("#0f172a"), 20);
            titleY += 24;
        }

        if (sequence.Subtitle.Length > 0) canvas.DrawText(sequence.Padding, titleY, Fit(sequence.Subtitle, 13, layout.Width - sequence.Padding * 2), Color("#64748b"), 13);
        foreach (var block in layout.Blocks) {
            canvas.FillRoundedRect(sequence.Padding, block.Y, layout.Width - sequence.Padding * 2, block.Height, 6, Color("#f8fafc"));
            canvas.StrokeRoundedRect(sequence.Padding, block.Y, layout.Width - sequence.Padding * 2, block.Height, 6, Color("#dbe4ef"), 1);
            if (block.Block.Text.Length > 0) canvas.DrawText(sequence.Padding + 10, block.Y + 5, Fit(block.Block.Kind + ": " + block.Block.Text, 12, layout.Width - sequence.Padding * 2 - 20), Color("#475569"), 12);
        }

        foreach (var participant in layout.Participants) DrawParticipantPng(canvas, layout, participant);
        foreach (var message in layout.Messages) DrawMessagePng(canvas, message);
        foreach (var note in layout.Notes) DrawNotePng(canvas, note);
        return PngWriter.WriteRgba(canvas.ToImage());
    }

    private static void WriteParticipantSvg(SvgMarkupWriter writer, SequenceLayout layout, ParticipantLayout participant) {
        writer.StartElement("line").Attribute("data-cfx-role", "sequence-lifeline").Attribute("data-participant-id", participant.Participant.Id).Attribute("x1", participant.CenterX).Attribute("y1", layout.ParticipantBoxY + SequenceLayout.ParticipantBoxHeight).Attribute("x2", participant.CenterX).Attribute("y2", layout.Height - 34).Attribute("stroke", "#cbd5e1").Attribute("stroke-dasharray", "6 6").EndEmptyElement().Line();
        writer.StartElement("rect").Attribute("data-cfx-role", "sequence-participant").Attribute("data-participant-id", participant.Participant.Id).Attribute("data-kind", participant.Participant.Kind.ToString()).Attribute("x", participant.BoxX).Attribute("y", layout.ParticipantBoxY).Attribute("width", participant.BoxWidth).Attribute("height", SequenceLayout.ParticipantBoxHeight).Attribute("rx", 6).Attribute("fill", "#eff6ff").Attribute("stroke", "#60a5fa").EndEmptyElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "sequence-participant-label").Attribute("x", participant.CenterX).Attribute("y", layout.ParticipantBoxY + 23).Attribute("text-anchor", "middle").Attribute("fill", "#0f172a").Attribute("font-family", FontFamily).Attribute("font-size", 12).Attribute("font-weight", "700").Text(Fit(participant.Participant.Label, 12, participant.BoxWidth - 12)).EndElement().Line();
    }

    private static void WriteMessageSvg(SvgMarkupWriter writer, SequenceArtifact sequence, SequenceLayout layout, MessageLayout message) {
        var dash = message.Message.LineStyle == SequenceArtifactMessageLineStyle.Dashed ? "6 5" : null;
        writer.StartElement("line").Attribute("data-cfx-role", "sequence-message").Attribute("data-source", message.Message.SourceId).Attribute("data-target", message.Message.TargetId).Attribute("x1", message.X1).Attribute("y1", message.Y).Attribute("x2", message.X2).Attribute("y2", message.Y).Attribute("stroke", "#334155").Attribute("stroke-width", 1.5).Attribute("marker-end", "url(#" + sequence.Id + "-arrow)").Attribute("stroke-dasharray", dash).EndEmptyElement().Line();
        if (message.Message.Text.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "sequence-message-label").Attribute("x", (message.X1 + message.X2) / 2).Attribute("y", message.Y - 8).Attribute("text-anchor", "middle").Attribute("fill", "#334155").Attribute("font-family", FontFamily).Attribute("font-size", 12).Text(Fit(message.Message.Text, 12, Math.Abs(message.X2 - message.X1) - 16)).EndElement().Line();
    }

    private static void WriteNoteSvg(SvgMarkupWriter writer, NoteLayout note) {
        writer.StartElement("rect").Attribute("data-cfx-role", "sequence-note").Attribute("x", note.X).Attribute("y", note.Y).Attribute("width", note.Width).Attribute("height", note.Height).Attribute("rx", 6).Attribute("fill", "#fef9c3").Attribute("stroke", "#eab308").EndEmptyElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "sequence-note-text").Attribute("x", note.X + 10).Attribute("y", note.Y + 21).Attribute("fill", "#713f12").Attribute("font-family", FontFamily).Attribute("font-size", 12).Text(Fit(note.Note.Text, 12, note.Width - 20)).EndElement().Line();
    }

    private static void DrawParticipantPng(RgbaCanvas canvas, SequenceLayout layout, ParticipantLayout participant) {
        canvas.DrawDashedLine(participant.CenterX, layout.ParticipantBoxY + SequenceLayout.ParticipantBoxHeight, participant.CenterX, layout.Height - 34, Color("#cbd5e1"), 1, 6, 6);
        canvas.FillRoundedRect(participant.BoxX, layout.ParticipantBoxY, participant.BoxWidth, SequenceLayout.ParticipantBoxHeight, 6, Color("#eff6ff"));
        canvas.StrokeRoundedRect(participant.BoxX, layout.ParticipantBoxY, participant.BoxWidth, SequenceLayout.ParticipantBoxHeight, 6, Color("#60a5fa"), 1);
        DrawCenteredText(canvas, participant.Participant.Label, participant.CenterX, layout.ParticipantBoxY + 12, participant.BoxWidth - 12, 12, Color("#0f172a"), true);
    }

    private static void DrawMessagePng(RgbaCanvas canvas, MessageLayout message) {
        if (message.Message.LineStyle == SequenceArtifactMessageLineStyle.Dashed) canvas.DrawDashedLine(message.X1, message.Y, message.X2, message.Y, Color("#334155"), 1.5, 6, 5);
        else canvas.DrawLine(message.X1, message.Y, message.X2, message.Y, Color("#334155"), 1.5);
        var direction = message.X2 >= message.X1 ? 1 : -1;
        canvas.FillPolygon(new[] {
            new ChartPoint(message.X2, message.Y),
            new ChartPoint(message.X2 - direction * 9, message.Y - 5),
            new ChartPoint(message.X2 - direction * 9, message.Y + 5)
        }, Color("#334155"));
        if (message.Message.Text.Length > 0) DrawCenteredText(canvas, message.Message.Text, (message.X1 + message.X2) / 2, message.Y - 24, Math.Abs(message.X2 - message.X1) - 16, 12, Color("#334155"), false);
    }

    private static void DrawNotePng(RgbaCanvas canvas, NoteLayout note) {
        canvas.FillRoundedRect(note.X, note.Y, note.Width, note.Height, 6, Color("#fef9c3"));
        canvas.StrokeRoundedRect(note.X, note.Y, note.Width, note.Height, 6, Color("#eab308"), 1);
        canvas.DrawText(note.X + 10, note.Y + 8, Fit(note.Note.Text, 12, note.Width - 20), Color("#713f12"), 12);
    }

    private static void DrawCenteredText(RgbaCanvas canvas, string text, double centerX, double y, double width, double fontSize, ChartColor color, bool emphasized) {
        var fitted = Fit(text, fontSize, width);
        var textWidth = RgbaCanvas.MeasureTextWidth(fitted, fontSize, null);
        if (emphasized) canvas.DrawTextEmphasized(centerX - textWidth / 2, y, fitted, color, fontSize);
        else canvas.DrawText(centerX - textWidth / 2, y, fitted, color, fontSize);
    }

    private static string Fit(string value, double fontSize, double maxWidth) {
        if (value.Length == 0 || maxWidth <= 8) return string.Empty;
        var text = value.Replace("\r", " ").Replace("\n", " ");
        while (text.Length > 1 && RgbaCanvas.MeasureTextWidth(text + "...", fontSize, null) > maxWidth) text = text.Substring(0, text.Length - 1);
        return text.Length == value.Length ? text : text.TrimEnd() + "...";
    }

    private static ChartColor Color(string hex) => ChartColor.FromHex(hex);

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private const string FontFamily = "Segoe UI, Arial, sans-serif";

    private sealed class SequenceLayout {
        public const double ParticipantBoxHeight = 38;
        public double Width { get; set; }
        public double Height { get; set; }
        public double ParticipantBoxY { get; set; }
        public List<ParticipantLayout> Participants { get; } = new();
        public List<MessageLayout> Messages { get; } = new();
        public List<NoteLayout> Notes { get; } = new();
        public List<BlockLayout> Blocks { get; } = new();

        public static SequenceLayout Calculate(SequenceArtifact sequence) {
            var layout = new SequenceLayout();
            var participantCount = Math.Max(1, sequence.Participants.Count);
            layout.Width = Math.Max(sequence.Width, sequence.Padding * 2 + participantCount * 140);
            layout.ParticipantBoxY = sequence.Padding + (sequence.Title.Length > 0 ? 42 : 8) + (sequence.Subtitle.Length > 0 ? 18 : 0);
            var laneTop = layout.ParticipantBoxY + ParticipantBoxHeight;
            var stepGap = 58.0;
            var totalSteps = Math.Max(sequence.Messages.Count, sequence.Notes.Count);
            for (var index = 0; index < sequence.Notes.Count; index++) totalSteps = Math.Max(totalSteps, sequence.Notes[index].StepIndex + 1);
            layout.Height = Math.Max(sequence.Height, laneTop + 52 + Math.Max(1, totalSteps) * stepGap + sequence.Padding);
            var laneAreaLeft = sequence.Padding + 24;
            var laneAreaRight = layout.Width - sequence.Padding - 24;
            var laneGapBase = participantCount == 1 ? 0 : (laneAreaRight - laneAreaLeft) / (participantCount - 1);
            var boxWidth = Math.Min(150, Math.Max(96, participantCount == 1 ? 130 : laneGapBase * 0.72));
            var left = laneAreaLeft + boxWidth / 2;
            var right = laneAreaRight - boxWidth / 2;
            var laneGap = participantCount == 1 ? 0 : (right - left) / (participantCount - 1);
            if (sequence.Participants.Count == 0) layout.Participants.Add(new ParticipantLayout(new SequenceArtifactParticipant("participant", "Participant"), (left + right) / 2, boxWidth));
            else for (var index = 0; index < sequence.Participants.Count; index++) layout.Participants.Add(new ParticipantLayout(sequence.Participants[index], left + laneGap * index, boxWidth));
            for (var index = 0; index < sequence.Messages.Count; index++) {
                var message = sequence.Messages[index];
                var source = layout.FindParticipant(message.SourceId);
                var target = layout.FindParticipant(message.TargetId);
                if (source == null || target == null) continue;
                layout.Messages.Add(new MessageLayout(message, source.CenterX, target.CenterX, laneTop + 44 + index * stepGap));
            }

            for (var index = 0; index < sequence.Notes.Count; index++) layout.Notes.Add(layout.PlaceNote(sequence.Notes[index], laneTop + 24 + sequence.Notes[index].StepIndex * stepGap));
            for (var index = 0; index < sequence.Blocks.Count; index++) {
                var block = sequence.Blocks[index];
                var start = laneTop + 18 + Math.Max(0, block.StartStepIndex) * stepGap;
                var end = laneTop + 64 + Math.Max(block.StartStepIndex, block.EndStepIndex) * stepGap;
                layout.Blocks.Add(new BlockLayout(block, start, Math.Max(42, end - start)));
            }

            return layout;
        }

        private ParticipantLayout? FindParticipant(string id) {
            for (var index = 0; index < Participants.Count; index++) if (string.Equals(Participants[index].Participant.Id, id, StringComparison.Ordinal)) return Participants[index];
            return null;
        }

        private NoteLayout PlaceNote(SequenceArtifactNote note, double y) {
            var first = note.ParticipantIds.Count > 0 ? FindParticipant(note.ParticipantIds[0]) : null;
            var last = note.ParticipantIds.Count > 1 ? FindParticipant(note.ParticipantIds[note.ParticipantIds.Count - 1]) : first;
            var width = Math.Min(240, Math.Max(150, RgbaCanvas.MeasureTextWidth(note.Text, 12, null) + 24));
            var x = first == null ? 40 : first.CenterX + 18;
            if (note.Placement == SequenceArtifactNotePlacement.LeftOf && first != null) x = first.CenterX - width - 18;
            if (note.Placement == SequenceArtifactNotePlacement.Over && first != null && last != null) {
                x = Math.Min(first.CenterX, last.CenterX) - width / 2;
                width = Math.Max(width, Math.Abs(last.CenterX - first.CenterX) + 80);
            }

            x = Math.Max(12, Math.Min(Width - width - 12, x));
            return new NoteLayout(note, x, y, width, 36);
        }
    }

    private sealed class ParticipantLayout {
        public ParticipantLayout(SequenceArtifactParticipant participant, double centerX, double boxWidth) {
            Participant = participant;
            CenterX = centerX;
            BoxWidth = boxWidth;
            BoxX = centerX - boxWidth / 2;
        }

        public SequenceArtifactParticipant Participant { get; }
        public double CenterX { get; }
        public double BoxX { get; }
        public double BoxWidth { get; }
    }

    private sealed class MessageLayout {
        public MessageLayout(SequenceArtifactMessage message, double x1, double x2, double y) {
            Message = message;
            X1 = x1;
            X2 = x2;
            Y = y;
        }

        public SequenceArtifactMessage Message { get; }
        public double X1 { get; }
        public double X2 { get; }
        public double Y { get; }
    }

    private sealed class NoteLayout {
        public NoteLayout(SequenceArtifactNote note, double x, double y, double width, double height) {
            Note = note;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public SequenceArtifactNote Note { get; }
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
    }

    private sealed class BlockLayout {
        public BlockLayout(SequenceArtifactBlock block, double y, double height) {
            Block = block;
            Y = y;
            Height = height;
        }

        public SequenceArtifactBlock Block { get; }
        public double Y { get; }
        public double Height { get; }
    }
}
