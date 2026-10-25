using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    // Flat immutable rows retain native tree depth without increasing the common JSON nesting limit.
    private static void TreeView(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTreeViewArtifact? tree) {
        if (tree == null) return;
        writer.Property("treeView"); writer.StartObject();
        String(writer, "themeJson", tree.Theme.ToJson());
        String(writer, "themeMode", tree.ThemeMode.ToString());
        Number(writer, "paddingLeft", tree.Padding.Left); Number(writer, "paddingTop", tree.Padding.Top);
        Number(writer, "paddingRight", tree.Padding.Right); Number(writer, "paddingBottom", tree.Padding.Bottom);
        Boolean(writer, "transparentBackground", tree.TransparentBackground); Boolean(writer, "showCard", tree.ShowCard);
        Boolean(writer, "hostOwnsFrame", tree.HostOwnsFrame); Number(writer, "pngOutputScale", tree.PngOutputScale);
        Boolean(writer, "showIcons", tree.ShowIcons); Number(writer, "rowIndent", tree.RowIndent); Number(writer, "rowPadding", tree.RowPadding);
        writer.Property("treeViewItems"); writer.StartArray();
        foreach (var item in tree.Items) {
            writer.StartObject(); String(writer, "id", item.Id); String(writer, "label", item.Label);
            OptionalString(writer, "parentId", item.ParentId); String(writer, "description", item.Description);
            String(writer, "kind", item.Kind.ToString()); String(writer, "icon", item.Icon.ToString());
            Boolean(writer, "highlighted", item.Highlighted); OptionalString(writer, "iconReference", item.IconReference);
            writer.Property("treeViewClasses"); writer.StartArray(); foreach (var name in item.Classes) writer.String(name);
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }

    private static VisualArtifactInterchangeTreeViewArtifact? ReadTreeView(Dictionary<string, GeoJsonValue> root) {
        var fields = OptionalObject(root, "treeView"); if (fields == null) return null;
        var tree = new VisualArtifactInterchangeTreeViewArtifact {
            ThemeMode = OptionalEnum<VisualThemeMode>(fields, "themeMode") ?? VisualThemeMode.Light,
            Padding = new ChartPadding(OptionalNumber(fields, "paddingLeft") ?? 18, OptionalNumber(fields, "paddingTop") ?? 16,
                OptionalNumber(fields, "paddingRight") ?? 18, OptionalNumber(fields, "paddingBottom") ?? 12),
            TransparentBackground = OptionalBool(fields, "transparentBackground") ?? false,
            ShowCard = OptionalBool(fields, "showCard") ?? true, HostOwnsFrame = OptionalBool(fields, "hostOwnsFrame") ?? false,
            PngOutputScale = OptionalInt(fields, "pngOutputScale") ?? 1,
            ShowIcons = OptionalBool(fields, "showIcons") ?? false,
            RowIndent = OptionalNumber(fields, "rowIndent") ?? 18, RowPadding = OptionalNumber(fields, "rowPadding") ?? 5
        };
        var theme = OptionalString(fields, "themeJson"); if (theme != null) tree.Theme = VisualTheme.FromJson(theme);
        var items = OptionalArray(fields, "treeViewItems");
        if (items.Count > VisualBlockRendering.MaximumTreeViewItems) throw new ArgumentException("Tree-view rows exceed the native limit.");
        foreach (var value in items) {
            var item = value.AsObject("tree-view row"); var classes = new List<string>();
            foreach (var name in OptionalArray(item, "treeViewClasses")) classes.Add(name.AsString("tree-view class"));
            tree.Items.Add(new TreeViewItem(RequiredString(item, "id"), RequiredString(item, "label"),
                OptionalString(item, "parentId"), RequiredStringAllowEmpty(item, "description"),
                RequiredEnum<TreeViewItemKind>(item, "kind"), RequiredEnum<TreeViewIcon>(item, "icon"),
                OptionalBool(item, "highlighted") ?? false, OptionalString(item, "iconReference"), classes));
        }
        return tree;
    }
}
