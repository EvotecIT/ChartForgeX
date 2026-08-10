using System.Collections.Generic;

namespace ChartForgeX.Topology;

public sealed partial class TopologyIconCatalog {
    /// <summary>Creates an independent deep copy of the catalog, its packs, icons, artwork, tags, and metadata.</summary>
    public TopologyIconCatalog Clone() {
        var clone = new TopologyIconCatalog();
        foreach (var pack in _packs) clone._packs.Add(ClonePack(pack));
        return clone;
    }

    private static TopologyIconPack ClonePack(TopologyIconPack source) {
        var clone = new TopologyIconPack(source.Id, source.Label, source.Vendor, source.Version, source.IsBuiltIn);
        Copy(source.Metadata, clone.Metadata);
        clone.Tags.AddRange(source.Tags);
        foreach (var icon in source.Icons) clone.AddIconUnchecked(CloneIcon(icon));
        return clone;
    }

    private static TopologyIconDefinition CloneIcon(TopologyIconDefinition source) {
        var clone = new TopologyIconDefinition(source.PackId, source.Id, source.Label, source.NodeKind, source.Shape) {
            Symbol = source.Symbol,
            Color = source.Color,
            Category = source.Category,
            DisplayMode = source.DisplayMode,
            Artwork = CloneArtwork(source.Artwork)
        };
        Copy(source.Metadata, clone.Metadata);
        clone.Tags.AddRange(source.Tags);
        return clone;
    }

    private static TopologyIconArtwork? CloneArtwork(TopologyIconArtwork? source) {
        if (source == null) return null;
        return new TopologyIconArtwork {
            SvgViewBox = source.SvgViewBox,
            SvgBody = source.SvgBody,
            SvgPath = source.SvgPath,
            PreviewPath = source.PreviewPath,
            ImageHref = source.ImageHref,
            PreserveAspectRatio = source.PreserveAspectRatio
        };
    }

    private static void Copy(IReadOnlyDictionary<string, string> source, IDictionary<string, string> target) {
        foreach (var item in source) target[item.Key] = item.Value;
    }
}
