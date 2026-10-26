using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    /// <summary>Admits the raster URIs actually emitted by this SVG/HTML export before hashing scene pixels.</summary>
    private static long AdmitSvgImageUris(VisualScene scene, int maximumCharacters) {
        long characters = 0;
        string? owner = null;
        var owners = new Stack<string?>();
        for (var i = 0; i < scene.Nodes.Count; i++) {
            var node = scene.Nodes[i];
            if (node is VisualSceneGroup group) {
                owners.Push(owner);
                if (group.Metadata.TryGetValue("data-node-id", out var identity)) owner = identity;
                if (group.ImageResource == null) continue;
                var beforeResource = characters;
                var resourceOwner = owner;
                characters += group.ImageResource.BoundedHref((int)(maximumCharacters - beforeResource),
                    observed => new VisualResourceLimitException("MaximumOutputBytes", maximumCharacters, beforeResource + observed, resourceOwner)).Length;
                // SVG emits the authored resource instead of this native fallback subtree.
                SkipImageFallback(scene, ref i); owner = owners.Pop();
            } else if (node is VisualSceneEndGroup) owner = owners.Pop();
            else if (node is VisualSceneImage image) {
                var before = characters;
                var resourceOwner = owner;
                var uri = image.BoundedDataUri((int)(maximumCharacters - before),
                    observed => new VisualResourceLimitException("MaximumOutputBytes", maximumCharacters, before + observed, resourceOwner));
                characters += uri.Length;
            }
        }
        return characters;
    }

    /// <summary>Counts the exact RGBA array visits made by the incumbent identity stream, including fallback and repeated placements.</summary>
    private static void AdmitImageIdentityWork(VisualScene scene) {
        long work = 0;
        string? owner = null;
        var owners = new Stack<string?>();
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) {
                owners.Push(owner);
                if (group.Metadata.TryGetValue("data-node-id", out var identity)) owner = identity;
            } else if (node is VisualSceneEndGroup) owner = owners.Pop();
            else if (node is VisualSceneImage image) {
                work += image.Image.Pixels.LongLength;
                if (work > RasterEffectPreflight.MaximumWorkSamples) throw new VisualResourceLimitException(
                    "SvgImageIdentityWorkSamples", RasterEffectPreflight.MaximumWorkSamples, work, owner);
            }
        }
    }
}
