using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidSequenceRendering {
    private static ChartColor RectangleBackground(MermaidSequenceDocument document, string text) {
        if (text.Length == 0) return document.Configuration.Sequence?.RectangleBackground ?? ChartColor.FromRgba(128, 128, 128, 128);
        if (SvgRasterColor.TryParse(text, out var color)) return color;
        throw new ArgumentException("Sequence rect background must be a supported native color.", nameof(document));
    }

    private static void AddNumbering(MermaidSequenceDocument document, SequenceArtifact sequence) {
        var control = document.Autonumber;
        if (control == null) return;
        sequence.Metadata["mermaid.autonumber"] = control.Enabled ? "true" : "false";
        if (control.Start != null) sequence.Metadata["mermaid.autonumber.start"] = control.Start;
        if (control.Increment != null) sequence.Metadata["mermaid.autonumber.increment"] = control.Increment;
        if (!MermaidSequenceNumbering.TryResolve(document, out var values, out _))
            throw new ArgumentException("Sequence autonumber exceeds the supported decimal range across its messages.", nameof(document));
        for (var index = 0; index < values.Count; index++) {
            var target = sequence.Messages[index]; var value = values[index];
            target.Metadata["mermaid.autonumber.visible"] = value.Visible ? "true" : "false";
            target.Metadata["mermaid.autonumber.value"] = value.Number.ToString("0.############################", CultureInfo.InvariantCulture);
        }
    }
}
