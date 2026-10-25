using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal enum SequenceTextRole { Participant, Message, Note, Fragment, Number }

/// <summary>One captured role/style policy for both native measurement and paint; no backend computes another layout.</summary>
internal sealed class SequencePreparedPresentation {
    private readonly TextStyle[] _styles = new TextStyle[5];
    private readonly bool[] _explicitColor = new bool[5];
    private readonly TextStyleOverride? _numberOverride;
    private readonly double _badgeClearance;
    private double _maximumBadgeDiameter;
    internal bool UsesNumberBadges => Layout?.NumberPresentation == SequenceNumberPresentation.Badge;
    internal double BadgeClearance => _badgeClearance;
    internal SequenceLayoutPolicy? Layout { get; }
    internal bool Customized { get; }
    internal bool SeparateNumber { get; }
    internal IReadOnlyList<int> VisibleParticipantIndices { get; }
    internal bool MirrorsParticipants => Layout?.MirrorParticipants == true;
    internal SequencePreparedPresentation(SequenceArtifact model, VisualRenderContext context) {
        Layout = model.Layout?.HasOverrides == true ? model.Layout : null;
        Customized = Layout != null || model.Typography?.HasOverrides == true;
        VisibleParticipantIndices = SelectVisibleParticipants(model, Layout?.HideUnusedParticipants == true);
        var typography = model.Typography;
        _numberOverride = typography?.Number; _badgeClearance = Math.Max(1, context.Theme.AxisStrokeWidth);
        var overrides = new[] { typography?.Participant, typography?.Message, typography?.Note, typography?.Fragment, typography?.Number };
        for (var index = 0; index < _styles.Length; index++) {
            var fallback = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground, LineHeight = 1 };
            if (index == (int)SequenceTextRole.Number) fallback = _styles[(int)SequenceTextRole.Message].Clone();
            _styles[index] = overrides[index]?.Resolve(fallback) ?? fallback;
            _explicitColor[index] = overrides[index]?.Color.HasValue == true;
            if(index==(int)SequenceTextRole.Number&&!_explicitColor[index])_explicitColor[index]=_explicitColor[(int)SequenceTextRole.Message];
        }
        SeparateNumber = UsesNumberBadges || typography?.Number?.HasOverrides == true;
    }
    internal TextStyle Style(SequenceTextRole role, ChartColor color, double ratio = 1) {
        var result = _styles[(int)role].Clone();
        if (!_explicitColor[(int)role]) result.Color = color;
        if (ratio != 1) result.FontSize *= ratio;
        return result;
    }
    internal TextMetrics Measure(string text, VisualSceneBuilder builder, SequenceTextRole role, double ordinarySize, double ratio = 1) =>
        Customized ? builder.MeasureText(text, Style(role, ChartColor.Black, ratio)) : builder.MeasureText(text, ordinarySize * ratio);
    internal string Wrap(string text, double width, VisualSceneBuilder builder, SequenceTextRole role, double ordinarySize, double ratio = 1) {
        if (!Customized) return SequencePreparedLayout.Wrap(text, width, builder, ordinarySize * ratio);
        return SequencePreparedLayout.Wrap(text, width, builder, Style(role, ChartColor.Black, ratio), Layout?.WrapLabels != false);
    }

    internal double MinimumLabelWidth(SequenceArtifact model, VisualSceneBuilder builder, double ordinarySize) {
        if (!Customized) return SequencePreparedLayout.MinimumLabelWidth(model, builder, ordinarySize);
        var width = 0d;
        void Include(string text, SequenceTextRole role, double ratio = 1) {
            var style = Style(role, ChartColor.Black, ratio);
            if (Layout?.WrapLabels == false) {
                foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) width = Math.Max(width, builder.MeasureText(line, style).Width);
            } else {
                var elements = StringInfo.GetTextElementEnumerator(text);
                while (elements.MoveNext()) width = Math.Max(width, builder.MeasureText(elements.GetTextElement(), style).Width);
            }
        }
        foreach (var item in VisibleParticipantIndices.Select(index => model.Participants[index])) { Include(SequencePreparedLayout.ParticipantCaption(item), SequenceTextRole.Participant); if (!string.IsNullOrEmpty(item.Stereotype)) Include("«" + item.Stereotype + "»", SequenceTextRole.Participant, .85); }
        for (var index = 0; index < model.Messages.Count; index++) {
            var prefix = SequencePreparedLayout.Number(model, index);
            if (UsesNumberBadges) {
                var label = NumberLabel(prefix);
                if (label.Length > 0) _maximumBadgeDiameter = Math.Max(_maximumBadgeDiameter, BadgeRadius(label, builder) * 2);
                Include(model.Messages[index].Text, SequenceTextRole.Message); continue;
            }
            if (SeparateNumber && prefix.Length > 0 && Layout?.WrapLabels == false) {
                var body = builder.MeasureText(model.Messages[index].Text, Style(SequenceTextRole.Message, ChartColor.Black)).Width;
                var number = builder.MeasureText(prefix, Style(SequenceTextRole.Number, ChartColor.Black)).Width;
                width = Math.Max(width, body + number);
            } else Include(prefix + model.Messages[index].Text, SequenceTextRole.Message);
        }
        foreach (var item in model.Notes) Include(item.Text, SequenceTextRole.Note);
        foreach (var item in model.Blocks) Include(item.Kind + " " + item.Text, SequenceTextRole.Fragment);
        foreach (var item in model.Branches) Include(item.Kind + " " + item.Text, SequenceTextRole.Fragment);
        foreach (var item in model.ParticipantGroups.Where(group => VisibleParticipantIndices.Any(index => index >= group.FirstParticipantIndex && index <= group.LastParticipantIndex))) Include(item.Label, SequenceTextRole.Fragment);
        return width;
    }
    internal SequenceTypography CaptureTypography(SequenceTypography? authored) {
        TextStyleOverride Capture(SequenceTextRole role,TextStyleOverride? original) {
            var resolved=_styles[(int)role].Clone();
            // The uncustomized scalar builder selects weight400 even when the caller's
            // base FontSpec requests another weight. Capture the face actually painted.
            if (!Customized) resolved.Font.Weight = 400;
            var copy=original?.Clone()??new TextStyleOverride();
            copy.FontFamily=resolved.Font.Family;copy.FontSize=resolved.FontSize;copy.FontWeight=resolved.Font.Weight.ToString(CultureInfo.InvariantCulture);
            copy.Italic=resolved.Font.Italic;copy.OpenTypeLanguageTag=resolved.OpenTypeLanguageTag;
            copy.Variations=resolved.Font.Variations;copy.ColorPaletteIndex=resolved.Font.ColorPaletteIndex;
            return copy;
        }
        return new SequenceTypography(Capture(SequenceTextRole.Participant,authored?.Participant),Capture(SequenceTextRole.Message,authored?.Message),
            Capture(SequenceTextRole.Note,authored?.Note),Capture(SequenceTextRole.Fragment,authored?.Fragment),
            UsesNumberBadges ? _numberOverride?.Clone() : SeparateNumber?Capture(SequenceTextRole.Number,authored?.Number):null);
    }
    private static IReadOnlyList<int> SelectVisibleParticipants(SequenceArtifact model, bool hideUnused) {
        if (!hideUnused) return Array.AsReadOnly(Enumerable.Range(0, model.Participants.Count).ToArray());
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var message in model.Messages) { used.Add(message.SourceId); used.Add(message.TargetId); }
        foreach (var note in model.Notes) foreach (var id in note.ParticipantIds) used.Add(id);
        foreach (var activation in model.Activations) used.Add(activation.ParticipantId);
        foreach (var block in model.Blocks) foreach (var id in block.ParticipantIds) used.Add(id);
        return Array.AsReadOnly(Enumerable.Range(0, model.Participants.Count).Where(index => used.Contains(model.Participants[index].Id)).ToArray());
    }
    internal double SelfMessageReserve(SequenceArtifact model, double minimumLabelWidth) {
        var ordinary = SequencePreparedLayout.SelfMessageReserve(minimumLabelWidth);
        if (UsesNumberBadges && _maximumBadgeDiameter > 0) ordinary += _maximumBadgeDiameter + BadgeClearance * 2 + WrapSides;
        if (!MirrorsParticipants || !model.Messages.Select((message,index) => new {message,index}).Any(item => item.message.SourceId == item.message.TargetId
            && model.Participants.Any(participant => participant.Id == item.message.SourceId && participant.DestroyedAtMessageIndex == item.index))) return ordinary;
        var header = Math.Max(Layout?.ParticipantMinimumWidth ?? 160, minimumLabelWidth + WrapSides * 2);
        header = Math.Max(header, VisibleParticipantIndices.Select(index => model.Participants[index].MinimumWidth ?? 0).DefaultIfEmpty(0).Max());
        return ordinary + header / 2 + Math.Max(160, minimumLabelWidth) + 8;
    }
    internal static string NumberLabel(string prefix) => prefix.EndsWith(". ", StringComparison.Ordinal) ? prefix.Substring(0, prefix.Length - 2) : prefix;
    internal TextStyle BadgeStyle(string label, ChartColor ink) {
        var size = label.Length > 5 ? 7 : label.Length > 3 ? 9 : 12;
        var fallback = new TextStyle { Font = new FontSpec { Family = "sans-serif" }, FontSize = size, Color = ink, LineHeight = 1 };
        return _numberOverride?.Resolve(fallback) ?? fallback;
    }
    internal double BadgeRadius(string label, VisualSceneBuilder builder) {
        var style = BadgeStyle(label, ChartColor.Black); var metrics = builder.MeasureText(label, style);
        var vertical = metrics.Height / 2 + Math.Abs(VisualSceneTextFace.BaselineOffset(style));
        return Math.Max(6, Math.Sqrt(metrics.Width * metrics.Width / 4 + vertical * vertical) + BadgeClearance);
    }
    internal double WrapSides => Layout?.WrapPadding ?? 8;
    internal double NoteWidth(double minimumLabelWidth) => Math.Max(120, minimumLabelWidth + (Layout?.NoteSpacing is double margin ? margin * 2 + 14 : 24));
    internal double MinimumLane(SequenceArtifact model, double ordinary) {
        if(!Customized)return SequencePreparedLayout.MinimumParticipantLane(model,ordinary);
        var spacing = Layout?.ParticipantSpacing ?? 12;
        var width = Math.Max(Layout?.ParticipantMinimumWidth ?? 0, VisibleParticipantIndices.Select(index => model.Participants[index].MinimumWidth ?? 0).DefaultIfEmpty(0).Max());
        return Math.Max(ordinary + (UsesNumberBadges && _maximumBadgeDiameter > 0 ? _maximumBadgeDiameter + BadgeClearance * 2 + WrapSides : 0), width + spacing);
    }
}
