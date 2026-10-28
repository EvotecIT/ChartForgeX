using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void SequencePresentation(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeSequenceArtifact sequence) {
        SequenceLifelineStroke(writer,sequence.LifelineStroke);
        DropShadow(writer, "noteShadow", sequence.NoteShadow);
        DropShadow(writer, "fragmentTabShadow", sequence.FragmentTabShadow);
        DropShadow(writer, "backgroundShadow", sequence.BackgroundShadow);
        if(sequence.Theme!=null)String(writer,"themeJson",sequence.Theme.ToJson());
        OptionalString(writer,"themeMode",sequence.ThemeMode?.ToString());
        if (sequence.Layout?.HasOverrides == true) {
            var layout = sequence.Layout;
            writer.Property("layout"); writer.StartObject();
            if(layout.ParticipantBands is {} bands){writer.Property("participantBands");writer.StartObject();Number(writer,"glyphHeight",bands.GlyphHeight);Number(writer,"glyphLabelGap",bands.GlyphLabelGap);Number(writer,"labelLifelineGap",bands.LabelLifelineGap);writer.EndObject();}
            if (layout.CanvasPadding is ChartPadding padding) {
                writer.Property("canvasPadding"); writer.StartObject(); Number(writer, "left", padding.Left); Number(writer, "top", padding.Top);
                Number(writer, "right", padding.Right); Number(writer, "bottom", padding.Bottom); writer.EndObject();
            }
            OptionalNumber(writer, "participantMinimumWidth", layout.ParticipantMinimumWidth);
            OptionalNumber(writer, "participantMinimumHeight", layout.ParticipantMinimumHeight);
            OptionalNumber(writer, "participantSpacing", layout.ParticipantSpacing);
            OptionalNumber(writer, "messageSpacing", layout.MessageSpacing);
            OptionalNumber(writer, "noteSpacing", layout.NoteSpacing);
            OptionalNumber(writer, "fragmentMargin", layout.FragmentMargin);
            OptionalNumber(writer, "fragmentLabelPadding", layout.FragmentLabelPadding);
            OptionalNumber(writer, "activationWidth", layout.ActivationWidth);
            OptionalNumber(writer, "wrapPadding", layout.WrapPadding);
            if (layout.WrapLabels.HasValue) Boolean(writer, "wrapLabels", layout.WrapLabels.Value);
            if (layout.MirrorParticipants.HasValue) Boolean(writer, "mirrorParticipants", layout.MirrorParticipants.Value);
            if (layout.HideUnusedParticipants.HasValue) Boolean(writer, "hideUnusedParticipants", layout.HideUnusedParticipants.Value);
            OptionalString(writer, "messageAlignment", layout.MessageAlignment?.ToString());
            OptionalString(writer, "noteAlignment", layout.NoteAlignment?.ToString());
            OptionalString(writer, "numberPresentation", layout.NumberPresentation?.ToString());
            OptionalString(writer, "selfMessageRouting", layout.SelfMessageRouting?.ToString());
            OptionalNumber(writer,"footerBottomMargin",layout.FooterBottomMargin);OptionalNumber(writer,"fragmentTabMinimumWidth",layout.FragmentTabMinimumWidth);OptionalNumber(writer,"fragmentTabMinimumHeight",layout.FragmentTabMinimumHeight);
            writer.EndObject();
        }
        if (sequence.Typography?.HasOverrides == true) {
            var typography = sequence.Typography;
            writer.Property("typography"); writer.StartObject();
            DetailTextStyle(writer, typography.Participant, "participant"); DetailTextStyle(writer, typography.Message, "message");
            DetailTextStyle(writer, typography.Note, "note"); DetailTextStyle(writer, typography.Fragment, "fragment"); DetailTextStyle(writer, typography.Number, "number");
            writer.EndObject();
        }
    }
    private static SequenceLayoutPolicy? ReadSequenceLayout(Dictionary<string, GeoJsonValue> sequence) {
        var item = OptionalObject(sequence, "layout"); if (item == null) return null;
        RequireSequenceMembers(item, new[] { "canvasPadding", "participantMinimumWidth", "participantMinimumHeight", "participantSpacing", "messageSpacing", "noteSpacing", "fragmentMargin", "fragmentLabelPadding", "activationWidth", "wrapPadding", "wrapLabels", "mirrorParticipants", "hideUnusedParticipants", "messageAlignment", "noteAlignment", "numberPresentation", "selfMessageRouting", "footerBottomMargin", "fragmentTabMinimumWidth", "fragmentTabMinimumHeight", "participantBands" });
        ChartPadding? padding = null;
        if (OptionalObject(item, "canvasPadding") is {} edges) {
            RequireSequenceMembers(edges, new[] { "left", "top", "right", "bottom" });
            padding = new ChartPadding(RequiredNumber(edges, "left"), RequiredNumber(edges, "top"), RequiredNumber(edges, "right"), RequiredNumber(edges, "bottom"));
        }
        var policy = new SequenceLayoutPolicy(padding,
            OptionalNumber(item, "participantMinimumWidth"), OptionalNumber(item, "participantMinimumHeight"), OptionalNumber(item, "participantSpacing"), OptionalNumber(item, "messageSpacing"), OptionalNumber(item, "noteSpacing"), OptionalNumber(item, "fragmentMargin"), OptionalNumber(item, "fragmentLabelPadding"), OptionalNumber(item, "activationWidth"), OptionalNumber(item, "wrapPadding"), OptionalBool(item, "wrapLabels"), OptionalBool(item, "mirrorParticipants"), OptionalBool(item, "hideUnusedParticipants"), OptionalEnum<TextAlignment>(item, "messageAlignment"), OptionalEnum<TextAlignment>(item, "noteAlignment"), OptionalEnum<SequenceNumberPresentation>(item, "numberPresentation"), OptionalEnum<SequenceSelfMessageRouting>(item, "selfMessageRouting"),OptionalNumber(item,"footerBottomMargin"),OptionalNumber(item,"fragmentTabMinimumWidth"),OptionalNumber(item,"fragmentTabMinimumHeight"),ReadParticipantBands(item));
        return policy.HasOverrides ? policy : null;
    }
    private static SequenceParticipantBands? ReadParticipantBands(Dictionary<string,GeoJsonValue> layout) {
        var item=OptionalObject(layout,"participantBands");if(item==null)return null;RequireSequenceMembers(item,new[]{"glyphHeight","glyphLabelGap","labelLifelineGap"});
        return new SequenceParticipantBands(RequiredNumber(item,"glyphHeight"),RequiredNumber(item,"glyphLabelGap"),RequiredNumber(item,"labelLifelineGap"));
    }
    private static SequenceTypography? ReadSequenceTypography(Dictionary<string, GeoJsonValue> sequence) {
        var item = OptionalObject(sequence, "typography"); if (item == null) return null;
        RequireSequenceMembers(item, new[] { "participant", "message", "note", "fragment", "number" });
        var typography = new SequenceTypography(ReadDetailTextStyle(item, "participant"), ReadDetailTextStyle(item, "message"),
            ReadDetailTextStyle(item, "note"), ReadDetailTextStyle(item, "fragment"), ReadDetailTextStyle(item, "number"));
        return typography.HasOverrides ? typography : null;
    }
    private static void RequireSequenceMembers(Dictionary<string, GeoJsonValue> values, string[] names) {
        foreach (var name in values.Keys) if (Array.IndexOf(names, name) < 0) throw new ArgumentException("Unknown sequence presentation member: " + name);
    }
}
