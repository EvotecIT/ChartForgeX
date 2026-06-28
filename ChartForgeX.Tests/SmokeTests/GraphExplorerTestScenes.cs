using ChartForgeX.Interactivity;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static GraphScene SampleGraphScene() {
        return GraphScene.Create("service-map", "Service map")
            .AddNode("api", "API", node => {
                node.Kind = "service";
                node.GroupId = "platform";
                node.ClusterId = "core";
                node.Status = "healthy";
                node.Size = 11;
                node.Shape = GraphNodeShape.Image;
                node.ImageUrl = "data:image/svg+xml,%3Csvg viewBox='0 0 64 64'%3E%3Crect width='64' height='64' rx='16' fill='%232563eb'/%3E%3C/svg%3E";
                node.ImageAlt = "API service icon";
                node.IconText = "A";
                node.Metadata["owner"] = "identity";
            })
            .AddNode("db", "Database", node => {
                node.Kind = "database";
                node.GroupId = "platform";
                node.ClusterId = "core";
                node.Status = "warning";
                node.Size = 13;
            })
            .AddNode("worker", "Worker", node => {
                node.Kind = "service";
                node.GroupId = "jobs";
                node.Status = "healthy";
                node.Fixed = true;
                node.X = 760;
                node.Y = 340;
            })
            .AddEdge("api-db", "api", "db", "queries", edge => {
                edge.Kind = "dependency";
                edge.Status = "warning";
                edge.Weight = 2;
                edge.Length = 140;
                edge.Directed = true;
                edge.Shape = GraphEdgeShape.Curve;
                edge.Curvature = 32;
                edge.Dashed = true;
                edge.Metadata["evidence"] = "privileged-path";
            })
            .AddEdge("api-worker", "api", "worker", "enqueues", edge => {
                edge.Kind = "queue";
                edge.Status = "healthy";
            })
            .AddCluster("core", "Core services", new[] { "api", "db" }, cluster => {
                cluster.Kind = "community";
                cluster.Collapsed = true;
                cluster.Metadata["tier"] = "core";
            });
    }
}
