global using ChartForgeX.Primitives;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChartForgeX.Tests;
using ChartForgeX.Topology;

/// <summary>Uses the same deterministic public topology fixtures as the routing contract suite.</summary>
public static class TopologyBenchmarkCases {
    /// <summary>Creates a fresh chart and render options before any measured operation.</summary>
    public static (TopologyChart, TopologyRenderOptions) Create(string fixture) => fixture switch {
        "small" => (DenseRouteFixture.Small(), DenseRouteFixture.Options(legend: false)),
        "mesh" => (DenseRouteFixture.Mesh(), DenseRouteFixture.Options(legend: false)),
        "overview" => (DenseRouteFixture.Overview(), DenseRouteFixture.Options(legend: false)),
        "replication" => (DenseReplicationFixture.Sites60(), DenseReplicationFixture.Options()),
        "mixed" => (TopologyRoutingFixtures.Mixed(TopologyEdgeRouting.Orthogonal)
            .WithEdgeWaypoints("fixed", new ChartPoint(380, 142), new ChartPoint(550, 142)), TopologyRoutingFixtures.Options()),
        _ => throw new ArgumentOutOfRangeException(nameof(fixture))
    };

    /// <summary>Checks attached route ends and returns complete geometry and optional SVG digests outside timing.</summary>
    public static string Proof(PreparedTopology prepared, string? svg) {
        var report = prepared.Analyze();
        if (report.Edges.Any(edge => !edge.SourceAttached || !edge.TargetAttached))
            throw new InvalidOperationException("A prepared route no longer attaches to both nodes.");
        return JsonSerializer.Serialize(new {
            prepared.Width, prepared.Height, prepared.NodeCount, prepared.EdgeCount,
            Report = Hash(JsonSerializer.Serialize(report, new JsonSerializerOptions { IncludeFields = true })),
            Svg = svg == null ? null : Hash(svg)
        });
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
