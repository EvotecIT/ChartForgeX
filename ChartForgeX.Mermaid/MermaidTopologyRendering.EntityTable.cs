using System.Globalization;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    private static TableArtifact EntityTable(MermaidEntityNode entity) {
        var comments = entity.Attributes.Any(attribute => !string.IsNullOrWhiteSpace(attribute.Comment));
        var table = TableArtifact.Create(entity.Id + "-attributes")
            .AddColumn("type", "Type").AddColumn("name", "Name").AddColumn("key", "Key");
        table.ShowGridLines = true;
        if (comments) table.AddColumn("comment", "Description");
        for (var index = 0; index < entity.Attributes.Count; index++) {
            var attribute = entity.Attributes[index];
            var key = index.ToString(CultureInfo.InvariantCulture);
            var values = comments ? new object?[] { attribute.Type, attribute.Name, attribute.Key ?? "", MermaidLabelText.EntityCaption(attribute.Comment ?? "") }
                : new object?[] { attribute.Type, attribute.Name, attribute.Key ?? "" };
            table.AddRow(key, values);
            table.ConfigureRow(index, row => {
                if (comments) row.Cells[3].Content = MermaidInlineCaption.Content(attribute.Comment ?? "", markdownByDefault: true);
                row.Metadata["mermaid.attribute.type"] = attribute.Type;
                row.Metadata["mermaid.attribute.name"] = attribute.Name;
                if (attribute.Key != null) row.Metadata["mermaid.attribute.key"] = attribute.Key;
                if (attribute.Comment != null) row.Metadata["mermaid.attribute.comment"] = attribute.Comment;
            });
        }
        return table;
    }
}
