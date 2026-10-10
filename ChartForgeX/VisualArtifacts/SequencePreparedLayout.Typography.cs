using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    // Wrapping may split between text elements, but every lane must fit its widest element.
    internal static double MinimumLabelWidth(SequenceArtifact model, VisualSceneBuilder builder, double size) {
        var texts = model.Participants.Select(item => item.Label).Concat(model.Messages.Select(item => item.Text))
            .Concat(model.Notes.Select(item => item.Text)).Concat(model.Blocks.Select(item => item.Kind + " " + item.Text))
            .Concat(model.Branches.Select(item => item.Kind + " " + item.Text));
        var width = 0d;
        foreach (var text in texts) {
            var elements = StringInfo.GetTextElementEnumerator(text);
            while (elements.MoveNext()) {
                var element = elements.GetTextElement();
                width = Math.Max(width, Math.Max(builder.MeasureText(element, size).Width, builder.MeasureText(element, size, 700).Width));
            }
        }
        return width;
    }
}
