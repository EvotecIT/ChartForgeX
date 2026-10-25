namespace ChartForgeX.Mermaid;

/// <summary>Maps only explicit source built-ins with an existing dependency-free native catalog owner.</summary>
internal static class MermaidArchitectureIcons {
    internal static string? NativeId(string? source) => source switch {
        "database" => "common:database",
        "server" => "common:server",
        "cloud" => "cloud:cloud",
        _ => null
    };
}
