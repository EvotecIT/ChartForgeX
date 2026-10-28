using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void TopologyTableContentContracts() {
        var table = TableArtifact.Create("facts").AddColumn("name", "Property").AddColumn("value", "Value")
            .AddRow("/count", "count", "0").AddRow("/state", "state", "false");
        table.Rows[0].Cells[1].Metadata["raw.type"] = "Number"; table.Rows[1].Cells[1].Metadata["raw.type"] = "Boolean";
        var chart = TopologyChart.Create().WithViewport(760, 460).WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight);
        chart.AddAutoNode("facts", "Captured facts", width: 220, height: 150); var node = chart.Nodes[0];
        node.Kind = TopologyNodeKind.Generic; node.Shape = TopologyNodeShape.Rectangle; node.ShowStatusBadge = false; node.FitLabelToContent = true; node.TableContent = table;
        chart.Annotations.Add(new TopologyAnnotation("note", "Same graph identity", "facts"));
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(chart.ToVisualArtifact().ToInterchangeJson()); var restored = envelope.ToTopologyChart();
        var copy = restored.Nodes[0].TableContent!; copy.Rows[0].Cells[1].DisplayText = "1";
        PackageAssertions.Require(envelope.Version == 2 && copy.Rows.Count == 2 && copy.Rows[0].Cells[1].Metadata["raw.type"] == "Number"
            && restored.Annotations[0].TargetId == "facts" && !ReferenceEquals(copy, table), "Packed native table content lost typed identity or detached source facts.");
        table.Rows[0].Cells[1].DisplayText = "changed"; node.TableContent = null;
        PackageAssertions.Require(svg == prepared.ToSvg() && png.SequenceEqual(prepared.ToPng()), "Packed table scene retained mutable source models.");
        PackageAssertions.Png(png);
    }
}
