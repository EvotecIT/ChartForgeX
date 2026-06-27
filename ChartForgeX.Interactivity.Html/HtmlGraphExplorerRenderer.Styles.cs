using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static string Backend(HtmlGraphRenderBackend backend) => backend switch {
        HtmlGraphRenderBackend.Svg => "svg",
        HtmlGraphRenderBackend.Canvas => "canvas",
        HtmlGraphRenderBackend.WebGl => "canvas",
        _ => throw new InvalidOperationException("Graph explorer render backend is unsupported: " + backend)
    };

    private static string LayoutMode(GraphLayoutMode mode) => mode switch {
        GraphLayoutMode.StructuredPrepared => "structured-prepared",
        GraphLayoutMode.Hierarchical => "hierarchical",
        _ => throw new InvalidOperationException("Graph explorer layout mode is unsupported: " + mode)
    };

    private static GraphNodeShape EffectiveNodeShape(GraphSceneNode? node) {
        if (node == null) return GraphNodeShape.Circle;
        return node.Shape == GraphNodeShape.Image && string.IsNullOrWhiteSpace(node.ImageUrl) ? GraphNodeShape.Circle : node.Shape;
    }

    private static string NodeShape(GraphSceneNode node) => EffectiveNodeShape(node) switch {
        GraphNodeShape.Box => "box",
        GraphNodeShape.Image => "image",
        GraphNodeShape.Ellipse => "ellipse",
        GraphNodeShape.Square => "square",
        GraphNodeShape.Diamond => "diamond",
        GraphNodeShape.Triangle => "triangle",
        GraphNodeShape.TriangleDown => "triangleDown",
        GraphNodeShape.Star => "star",
        GraphNodeShape.Database => "database",
        GraphNodeShape.Text => "text",
        _ => "circle"
    };

    private static string EdgeShape(GraphEdgeShape shape) => shape switch {
        GraphEdgeShape.Curve => "curve",
        GraphEdgeShape.DynamicCurve => "dynamic",
        GraphEdgeShape.ContinuousCurve => "continuous",
        GraphEdgeShape.SelfReference => "selfReference",
        _ => "line"
    };

    private static void WriteNodeMarkStyle(StringBuilder writer, GraphSceneNode node) {
        var style = NodeMarkStyle(node);
        if (!string.IsNullOrWhiteSpace(style)) Attribute(writer, "style", style);
    }

    private static string NodeMarkStyle(GraphSceneNode node) {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(node.Style.BackgroundColor)) parts.Add("fill:" + node.Style.BackgroundColor);
        if (!string.IsNullOrWhiteSpace(node.Style.BorderColor)) parts.Add("stroke:" + node.Style.BorderColor);
        if (node.Style.Shadow) parts.Add("filter:drop-shadow(0 5px 10px rgba(15,23,42,.18))");
        return string.Join(";", parts);
    }

    private static string NodeLabelStyle(GraphSceneNode node) {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(node.Style.LabelColor)) parts.Add("fill:" + node.Style.LabelColor);
        return string.Join(";", parts);
    }

    private static string EdgeStyle(GraphSceneEdge edge) {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(edge.Style.Color)) parts.Add("stroke:" + edge.Style.Color);
        if (edge.Style.Width.HasValue) parts.Add("stroke-width:" + Number(edge.Style.Width.Value));
        if (!string.IsNullOrWhiteSpace(edge.Style.DashPattern)) parts.Add("stroke-dasharray:" + edge.Style.DashPattern);
        if (edge.Style.Hidden) parts.Add("display:none");
        return string.Join(";", parts);
    }

    private static string EdgeLabelStyle(GraphSceneEdge edge) {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(edge.Style.LabelColor)) parts.Add("fill:" + edge.Style.LabelColor);
        if (edge.Style.Hidden) parts.Add("display:none");
        return string.Join(";", parts);
    }

    private static void WriteDatabaseNodeMark(StringBuilder writer, GraphSceneNode node, double size) {
        var width = size * 1.25;
        var top = -size * 0.55;
        var bottom = size * 0.55;
        var radius = size * 0.38;
        writer.Append("<path d=\"M ");
        writer.Append(Number(-width));
        writer.Append(' ');
        writer.Append(Number(top));
        writer.Append(" C ");
        writer.Append(Number(-width));
        writer.Append(' ');
        writer.Append(Number(top - radius));
        writer.Append(' ');
        writer.Append(Number(width));
        writer.Append(' ');
        writer.Append(Number(top - radius));
        writer.Append(' ');
        writer.Append(Number(width));
        writer.Append(' ');
        writer.Append(Number(top));
        writer.Append(" L ");
        writer.Append(Number(width));
        writer.Append(' ');
        writer.Append(Number(bottom));
        writer.Append(" C ");
        writer.Append(Number(width));
        writer.Append(' ');
        writer.Append(Number(bottom + radius));
        writer.Append(' ');
        writer.Append(Number(-width));
        writer.Append(' ');
        writer.Append(Number(bottom + radius));
        writer.Append(' ');
        writer.Append(Number(-width));
        writer.Append(' ');
        writer.Append(Number(bottom));
        writer.Append(" Z M ");
        writer.Append(Number(-width));
        writer.Append(' ');
        writer.Append(Number(top));
        writer.Append(" C ");
        writer.Append(Number(-width));
        writer.Append(' ');
        writer.Append(Number(top + radius));
        writer.Append(' ');
        writer.Append(Number(width));
        writer.Append(' ');
        writer.Append(Number(top + radius));
        writer.Append(' ');
        writer.Append(Number(width));
        writer.Append(' ');
        writer.Append(Number(top));
        writer.Append('"');
        WriteNodeMarkStyle(writer, node);
        writer.Append("></path>");
    }

    private static string PolygonPoints(GraphNodeShape shape, double size) {
        if (shape == GraphNodeShape.Diamond) return "0," + Number(-size * 1.35) + " " + Number(size * 1.35) + ",0 0," + Number(size * 1.35) + " " + Number(-size * 1.35) + ",0";
        if (shape == GraphNodeShape.Triangle) return "0," + Number(-size * 1.35) + " " + Number(size * 1.25) + "," + Number(size * 1.05) + " " + Number(-size * 1.25) + "," + Number(size * 1.05);
        if (shape == GraphNodeShape.TriangleDown) return Number(-size * 1.25) + "," + Number(-size * 1.05) + " " + Number(size * 1.25) + "," + Number(-size * 1.05) + " 0," + Number(size * 1.35);
        return "0," + Number(-size * 1.45) + " " + Number(size * 0.42) + "," + Number(-size * 0.45) + " " + Number(size * 1.4) + "," + Number(-size * 0.45) + " " + Number(size * 0.62) + "," + Number(size * 0.18) + " " + Number(size * 0.9) + "," + Number(size * 1.25) + " 0," + Number(size * 0.64) + " " + Number(-size * 0.9) + "," + Number(size * 1.25) + " " + Number(-size * 0.62) + "," + Number(size * 0.18) + " " + Number(-size * 1.4) + "," + Number(-size * 0.45) + " " + Number(-size * 0.42) + "," + Number(-size * 0.45);
    }
}
