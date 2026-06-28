using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static string StableSceneHash(GraphScene scene) {
        unchecked {
            var hash = 2166136261u;
            AddStableHash(ref hash, scene.Id);
            AddStableHash(ref hash, scene.Title);
            AddStableHash(ref hash, scene.Options.Layout.Mode.ToString());
            AddStableHash(ref hash, scene.Options.Layout.Direction.ToString());
            foreach (var node in scene.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal)) {
                AddStableHash(ref hash, node.Id);
                AddStableHash(ref hash, node.Label);
                AddStableHash(ref hash, node.Kind);
                AddStableHash(ref hash, node.Status);
                AddStableHash(ref hash, node.ClusterId);
                AddStableHash(ref hash, node.Shape.ToString());
                AddStableHash(ref hash, node.Level?.ToString(CultureInfo.InvariantCulture));
                AddStableHash(ref hash, node.Style.BackgroundColor);
                AddStableHash(ref hash, node.Style.BorderColor);
                AddStableHash(ref hash, node.Style.LabelColor);
                AddStableHash(ref hash, node.Size.ToString("R", CultureInfo.InvariantCulture));
                if (node.HasExplicitPosition) {
                    AddStableHash(ref hash, node.X.ToString("R", CultureInfo.InvariantCulture));
                    AddStableHash(ref hash, node.Y.ToString("R", CultureInfo.InvariantCulture));
                }
            }

            foreach (var edge in scene.Edges.OrderBy(edge => edge.Id, StringComparer.Ordinal)) {
                AddStableHash(ref hash, edge.Id);
                AddStableHash(ref hash, edge.SourceNodeId);
                AddStableHash(ref hash, edge.TargetNodeId);
                AddStableHash(ref hash, edge.Label);
                AddStableHash(ref hash, edge.Kind);
                AddStableHash(ref hash, edge.Shape.ToString());
                AddStableHash(ref hash, edge.Style.Color);
                AddStableHash(ref hash, edge.Style.Width?.ToString("R", CultureInfo.InvariantCulture));
                AddStableHash(ref hash, edge.Curvature.ToString("R", CultureInfo.InvariantCulture));
            }

            foreach (var cluster in scene.GetEffectiveClusters().OrderBy(cluster => cluster.Id, StringComparer.Ordinal)) {
                AddStableHash(ref hash, cluster.Id);
                AddStableHash(ref hash, cluster.Label);
                foreach (var nodeId in cluster.NodeIds.OrderBy(nodeId => nodeId, StringComparer.Ordinal)) AddStableHash(ref hash, nodeId);
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void AddStableHash(ref uint hash, string? value) {
        foreach (var ch in value ?? string.Empty) {
            hash ^= ch;
            hash *= 16777619u;
        }

        hash ^= 31u;
        hash *= 16777619u;
    }
}
