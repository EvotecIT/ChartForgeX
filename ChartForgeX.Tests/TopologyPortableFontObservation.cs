using System.Text.Json;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

/// <summary>Records real selected native request/glyph roots before portable serialization can reject their budgets.</summary>
internal static class TopologyPortableFontObservation {
    internal static void Capture(string name, PreparedVisual prepared) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        var safeName = string.Concat(name.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_'));
        var path = Path.Combine(output, "portable-font-" + safeName);
        File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng());
        var captured = prepared.CapturedSemanticSnapshot?.Presentation?.ContextFont;
        var requests = captured?.Text?.Requests.Select(request => new {
            request.PublicKey, Family = request.Style.Font.Family, request.Style.Font.Weight, request.Style.Font.Italic,
            Primary = Root(request.Face.Font), Clusters = request.Clusters.Decisions.Select(decision => new {
                Input = decision.Raw, Output = decision.Output, Selected = Root(decision.Face)
            }).ToArray()
        }).ToArray();
        var glyphs = prepared.Scene.Nodes.OfType<VisualSceneText>().Select(mark => new {
            mark.Id, mark.Role, mark.X, mark.Baseline, mark.Text.Size,
            Lines = mark.Text.Lines.Select(line => new { line.Text, line.Width,
                Roots = line.Glyphs.Select(glyph => Root(glyph.Face)).DistinctBy(root => root!.Identity).ToArray() }).ToArray()
        }).ToArray();
        File.WriteAllText(path + ".font-facts.json", JsonSerializer.Serialize(new { Requests = requests, Glyphs = glyphs },
            new JsonSerializerOptions { WriteIndented = true }));
    }
    private static FontRoot? Root(TrueTypeFont? face) => face == null ? null
        : new FontRoot(face.Root.CapturedByteIdentity, face.Root.CapturedByteLength, face.SelectedFamily, face.DisplayName, face.CollectionIndex);
    private sealed record FontRoot(string Identity, int Length, string? Family, string? Face, int? Collection);
}
