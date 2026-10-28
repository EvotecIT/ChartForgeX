using System;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidEventModelingRendering {
    /// <summary>Converts one data block into detached Field/Type/Value facts using the existing table contract.</summary>
    /// <remarks>Nested paths use JSON Pointer escaping. Cells remain inert display/export text with explicit kind metadata;
    /// objects, arrays and unsupported formats never become invented numeric values or executable content.</remarks>
    public static TableArtifact ToDataTable(this MermaidEventModelingDataBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        using var scope = MermaidResourceGuard.BeginIfAbsent(block.CapturedResourceLimits);
        if (!MermaidEventModelingParser.Identifier(block.Id)) throw new ArgumentException("Event Modeling data blocks require a bounded identifier.", nameof(block));
        return MermaidEventModelingData.Create("eventmodeling-data-" + block.Id, block.DataType, block.Content, block.Span).WithTitle(block.Id);
    }

    /// <summary>Converts a frame's inline and/or referenced data to one detached typed-fact table.</summary>
    /// <param name="document">The source timeline and data declarations.</param>
    /// <param name="frameNumber">The exact authored frame identity, including leading zeroes.</param>
    /// <returns>Existing table facts; an empty table when the frame has no data.</returns>
    public static TableArtifact ToDataTable(this MermaidEventModelingDocument document, string frameNumber) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        using var scope = MermaidResourceGuard.BeginIfAbsent(document.CapturedResourceLimits);
        ValidateModel(document);
        var frame = document.Frames.SingleOrDefault(item => item.Number == frameNumber)
            ?? throw new ArgumentException("An existing Event Modeling frame number is required.", nameof(frameNumber));
        return FrameData(document, frame);
    }

    private static TableArtifact FrameData(MermaidEventModelingDocument document, MermaidEventModelingFrame frame) {
        var table = MermaidEventModelingData.NewTable("eventmodeling-data-frame-" + frame.Number).WithTitle(frame.EntityIdentifier);
        table.Metadata["mermaid.frame"] = frame.Number;
        var both = frame.InlineData != null && frame.DataReference != null;
        if (frame.InlineData != null) Append(MermaidEventModelingData.Create(table.Id, frame.DataType, frame.InlineData, frame.Span), both ? "/inline" : "", "inline");
        if (frame.DataReference != null) {
            var block = document.DataBlocks.Single(item => item.Id == frame.DataReference);
            Append(block.ToDataTable(), both ? "/data/" + MermaidEventModelingData.Escape(block.Id) : "", "reference");
        }
        return table;

        void Append(TableArtifact source, string prefix, string sourceKind) {
            foreach (var pair in source.Metadata) {
                var key = pair.Key.StartsWith("mermaid.data.", StringComparison.Ordinal) ? pair.Key.Substring("mermaid.data.".Length)
                    : pair.Key.StartsWith("mermaid.", StringComparison.Ordinal) ? pair.Key.Substring("mermaid.".Length) : pair.Key;
                table.Metadata["mermaid.data." + sourceKind + "." + key] = pair.Value;
            }
            MermaidResourceGuard.ReserveElements(source.Rows.Count, frame.Span);
            foreach (var row in source.Rows) {
                MermaidResourceGuard.Checkpoint();
                MermaidEventModelingData.CheckPortableFactLength((long)prefix.Length + row.Key.Length, frame.Span);
                MermaidEventModelingData.CheckPortableFactLength((long)prefix.Length + row.Cells[0].DisplayText.Length, frame.Span);
                table.AddRow(prefix + row.Key, prefix + row.Cells[0].DisplayText, row.Cells[1].DisplayText, row.Cells[2].DisplayText);
                var copy = table.Rows[table.Rows.Count - 1];
                foreach (var pair in row.Metadata) copy.Metadata[pair.Key] = pair.Value;
                for (var index = 0; index < row.Cells.Count; index++)
                    foreach (var pair in row.Cells[index].Metadata) copy.Cells[index].Metadata[pair.Key] = pair.Value;
                copy.Metadata["mermaid.data.sourceKind"] = sourceKind; copy.Metadata["mermaid.data.path"] = copy.Key;
            }
        }
    }
}
