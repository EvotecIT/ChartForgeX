using System;
using System.IO;

namespace ChartForgeX.Topology;

public static partial class TopologyIconPackJson {
    private static void ResolvePackArtworkFiles(TopologyIconPack pack, string manifestDirectory) {
        foreach (var icon in pack.Icons) {
            var artwork = icon.Artwork;
            if (artwork == null || artwork.HasSvgBody || !artwork.HasSvgPath) continue;
            var svgPath = ResolvePackAssetPath(manifestDirectory, artwork.SvgPath!, icon.Id + ".artwork.svgPath");
            var manifestViewBox = artwork.SvgViewBox;
            var loaded = TopologyIconSvgPackImporter.LoadSvgArtworkFromFile(svgPath, pack.Id, icon.Id);
            artwork.SvgBody = loaded.SvgBody;
            artwork.SvgViewBox = string.IsNullOrWhiteSpace(manifestViewBox) ? loaded.SvgViewBox : manifestViewBox;
        }
    }

    private static string ResolvePackAssetPath(string manifestDirectory, string assetPath, string pathName) {
        if (!TopologyIconArtwork.IsSafeAssetPath(assetPath)) throw new ArgumentException("Unsafe topology icon asset path: " + pathName);
        var baseDirectory = Path.GetFullPath(manifestDirectory);
        var normalizedAssetPath = assetPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, normalizedAssetPath));
        var baseWithSeparator = AppendDirectorySeparator(baseDirectory);
        if (!fullPath.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Topology icon asset path escapes its manifest directory: " + pathName);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Topology icon SVG asset was not found.", fullPath);
        RejectReparsePointPath(baseDirectory, fullPath, pathName);
        return fullPath;
    }

    private static void RejectReparsePointPath(string baseDirectory, string fullPath, string pathName) {
        var current = Path.GetFullPath(baseDirectory);
        var relative = MakeRelativePath(current, fullPath);
        foreach (var part in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)) {
            current = Path.Combine(current, part);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Topology icon asset path uses a reparse point: " + pathName);
        }
    }

    private static string MakeRelativePath(string baseDirectory, string fullPath) {
        var baseUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(baseDirectory)), UriKind.Absolute);
        var fileUri = new Uri(Path.GetFullPath(fullPath), UriKind.Absolute);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path) {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + Path.DirectorySeparatorChar;
    }
}
