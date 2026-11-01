using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void SequenceCard(VisualArtifactInterchangeJsonWriter writer, SequenceParticipantCard? card) {
        if (card == null) return;
        writer.Property("card");
        writer.StartObject();
        OptionalString(writer, "subtitle", card.Subtitle);
        String(writer, "iconShape", card.IconShape.ToString());
        String(writer, "iconKind", card.IconKind.ToString());
        OptionalString(writer, "iconColor", card.IconColor?.ToCss());
        OptionalString(writer, "statusColor", card.StatusColor?.ToCss());
        writer.EndObject();
    }

    private static SequenceParticipantCard? ReadSequenceCard(Dictionary<string, GeoJsonValue> sequence) {
        var card = OptionalObject(sequence, "card");
        if (card == null) return null;
        var iconColor = OptionalString(card, "iconColor");
        var statusColor = OptionalString(card, "statusColor");
        return new SequenceParticipantCard {
            Subtitle = OptionalString(card, "subtitle"),
            IconShape = RequiredEnum<TopologyIconShape>(card, "iconShape"),
            IconKind = RequiredEnum<TopologyNodeKind>(card, "iconKind"),
            IconColor = iconColor == null ? null : ChartColor.Parse(iconColor),
            StatusColor = statusColor == null ? null : ChartColor.Parse(statusColor)
        };
    }
}
