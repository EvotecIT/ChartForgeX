using System.Text.Json;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

/// <summary>Mirrors the scalar decoding a native host performs at its JSON transport boundary.</summary>
internal static class NativeWindowTransport {
    internal static ChartAxisWindow Read(JsonElement value) => new(value.GetProperty("minimum").GetDouble(),
        value.GetProperty("maximum").GetDouble(), !value.TryGetProperty("maximumInclusive", out var inclusive) || inclusive.GetBoolean());
}
