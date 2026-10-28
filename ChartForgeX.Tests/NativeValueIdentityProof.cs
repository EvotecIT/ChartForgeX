using System.Text.Json;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Retains causal default-identity and native text binding evidence before asserting exact native midpoint output.</summary>
internal static class NativeValueIdentityProof {
    internal static void AssertEqual(PreparedVisual expected, PreparedVisual captured, string name) {
        var before = expected.ToSvg(); var after = captured.ToSvg();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "native-value-identity-" + name);
            File.WriteAllText(stem + ".expected.default.svg", before); File.WriteAllText(stem + ".captured.default.svg", after);
            File.WriteAllText(stem + ".expected.fixed.svg", expected.ToSvg("native-identity-body"));
            File.WriteAllText(stem + ".captured.fixed.svg", captured.ToSvg("native-identity-body"));
            object Bindings(PreparedVisual frame) => frame.Scene.Nodes.OfType<VisualSceneText>().Select(node => new {
                node.Id, node.Role, color = node.Color.ToCss(), alignment = node.Alignment.ToString(),
                preparedColor = node.Text.Style.Color.ToCss(), preparedAlignment = node.Text.Style.Alignment.ToString(),
                node.Text.Style.FontSize, node.Text.Size, node.Text.Style.LineHeight,
                underline = node.Text.Style.UnderlineStyle.ToString(), strike = node.Text.Style.StrikethroughStyle.ToString(),
                lines = node.Text.Lines.Select(line => line.Text).ToArray()
            }).ToArray();
            File.WriteAllText(stem + ".bindings.json", JsonSerializer.Serialize(new { expected = Bindings(expected), captured = Bindings(captured) },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Equal(before, after);
    }
}
