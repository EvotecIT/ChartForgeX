using System;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteCircularImageNodeMark(StringBuilder writer, GraphSceneNode node, double size) {
        writer.Append("<circle r=\"");
        writer.Append(Number(size + 4));
        writer.Append('"');
        WriteNodeMarkStyle(writer, node);
        writer.Append("></circle><image");
        Attribute(writer, "href", node.ImageUrl);
        Attribute(writer, "aria-label", node.ImageAlt);
        Attribute(writer, "x", Number(-size));
        Attribute(writer, "y", Number(-size));
        Attribute(writer, "width", Number(size * 2));
        Attribute(writer, "height", Number(size * 2));
        writer.Append("></image>");
    }

    private static void WriteRectangularImageNodeMark(StringBuilder writer, GraphSceneNode node, double size) {
        var width = size * 2.6;
        var height = size * 1.8;
        writer.Append("<rect x=\"");
        writer.Append(Number(-width / 2));
        writer.Append("\" y=\"");
        writer.Append(Number(-height / 2));
        writer.Append("\" width=\"");
        writer.Append(Number(width));
        writer.Append("\" height=\"");
        writer.Append(Number(height));
        writer.Append("\" rx=\"");
        writer.Append(Number(Math.Min(8, size * 0.35)));
        writer.Append('"');
        WriteNodeMarkStyle(writer, node);
        writer.Append("></rect><image");
        Attribute(writer, "href", node.ImageUrl);
        Attribute(writer, "aria-label", node.ImageAlt);
        Attribute(writer, "x", Number(-width / 2 + 3));
        Attribute(writer, "y", Number(-height / 2 + 3));
        Attribute(writer, "width", Number(Math.Max(1, width - 6)));
        Attribute(writer, "height", Number(Math.Max(1, height - 6)));
        writer.Append("></image>");
    }
}
