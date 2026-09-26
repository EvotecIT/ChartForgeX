using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Html;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Provides the interactive runtimes as shared asset files for bundles that reference them through
/// <see cref="HtmlAssetReferences"/> instead of inlining one copy per page.
/// </summary>
public static class HtmlInteractiveAssetFiles {
    private static readonly Lazy<HtmlAssetFile> ChartStyleFile = new(() => new HtmlAssetFile("cfx-interactive", "css", HtmlInteractiveAssets.Style));
    private static readonly Lazy<HtmlAssetFile> ChartScriptFile = new(() => new HtmlAssetFile("cfx-interactive", "js", HtmlInteractiveAssets.Script));
    private static readonly Lazy<HtmlAssetFile> GraphStyleFile = new(() => new HtmlAssetFile("cfx-graph-explorer", "css", HtmlGraphExplorerAssets.Style));
    private static readonly ConcurrentDictionary<string, HtmlAssetFile> TopologyScriptFiles = new(StringComparer.Ordinal);
    private static readonly Lazy<HtmlAssetFile> GraphScriptFile = new(() => new HtmlAssetFile("cfx-graph-explorer", "js", HtmlGraphExplorerRenderer.WrappedInteractionScript()));

    /// <summary>Gets the stylesheet used by interactive chart and dashboard pages.</summary>
    public static HtmlAssetFile ChartStyle => ChartStyleFile.Value;

    /// <summary>Gets the runtime used by interactive chart and dashboard pages.</summary>
    public static HtmlAssetFile ChartScript => ChartScriptFile.Value;

    /// <summary>Gets the stylesheet used by graph explorer pages.</summary>
    public static HtmlAssetFile GraphExplorerStyle => GraphStyleFile.Value;

    /// <summary>Gets the runtime used by graph explorer pages.</summary>
    public static HtmlAssetFile GraphExplorerScript => GraphScriptFile.Value;

    /// <summary>
    /// Gets the topology interaction runtime for the given render options. The runtime is specialised to the CSS class
    /// prefix, so each prefix produces its own file name.
    /// </summary>
    /// <param name="options">The topology render options used for the pages; null uses the defaults.</param>
    /// <returns>The topology runtime file.</returns>
    public static HtmlAssetFile TopologyScript(TopologyRenderOptions? options = null) {
        var script = HtmlInteractiveTopologyRenderer.BuildInteractionScript(HtmlInteractiveTopologyRenderer.Prepare(options));
        return TopologyScriptFiles.GetOrAdd(script, content => new HtmlAssetFile("cfx-topology", "js", content));
    }

    /// <summary>Returns the files referenced by interactive chart and dashboard pages.</summary>
    public static IReadOnlyList<HtmlAssetFile> Charts() => new[] { ChartStyle, ChartScript };

    /// <summary>Returns the files referenced by graph explorer pages.</summary>
    public static IReadOnlyList<HtmlAssetFile> GraphExplorer() => new[] { GraphExplorerStyle, GraphExplorerScript };

    /// <summary>
    /// Writes asset files into <paramref name="directory"/> (created when missing). Files that already exist with the
    /// same content are left untouched, so repeated report runs keep one copy per bundle. New content is written to a
    /// temporary file and moved into place, so readers never see a partially written runtime and concurrent runs that
    /// write the same content succeed.
    /// </summary>
    /// <param name="directory">The bundle's asset folder, matching <see cref="HtmlAssetReferences.BasePath"/>.</param>
    /// <param name="files">The files to write.</param>
    /// <returns>The full paths of the asset files.</returns>
    public static IReadOnlyList<string> WriteTo(string directory, IEnumerable<HtmlAssetFile> files) {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Asset directory must not be empty.", nameof(directory));
        if (files == null) throw new ArgumentNullException(nameof(files));
        Directory.CreateDirectory(directory);
        var paths = new List<string>();
        foreach (var file in files) {
            if (file == null) throw new ArgumentException("Asset files must not contain null entries.", nameof(files));
            var path = Path.Combine(directory, file.FileName);
            var bytes = HtmlAssetFile.Encode(file.Content);
            if (!HasContent(path, bytes)) WriteAtomically(path, bytes);
            paths.Add(path);
        }

        return paths;
    }

    internal static void WriteStylesheet(HtmlMarkupWriter writer, HtmlAssetReferences assets, HtmlAssetFile file) {
        writer.StartElement("link").Attribute("rel", "stylesheet").Attribute("href", assets.Href(file));
        WriteIntegrity(writer, assets, file);
        writer.EndVoidElement().Line();
    }

    internal static void WriteScript(HtmlMarkupWriter writer, HtmlAssetReferences assets, HtmlAssetFile file, string? nonce) {
        writer.StartElement("script").Attribute("src", assets.Href(file)).Attribute("nonce", nonce);
        WriteIntegrity(writer, assets, file);
        writer.EndStartElement().EndElement();
    }

    private static void WriteIntegrity(HtmlMarkupWriter writer, HtmlAssetReferences assets, HtmlAssetFile file) {
        if (!assets.IncludeIntegrity) return;
        writer.Attribute("integrity", file.Integrity).Attribute("crossorigin", "anonymous");
    }

    private static void WriteAtomically(string path, byte[] bytes) {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            File.WriteAllBytes(temporary, bytes);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        } catch (IOException) when (HasContent(path, bytes)) {
            // Another run wrote the same content first.
        } catch (UnauthorizedAccessException) when (HasContent(path, bytes)) {
            // The target is being replaced by a concurrent run with identical content.
        } finally {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static bool HasContent(string path, byte[] bytes) {
        try {
            return File.Exists(path) && SameContent(path, bytes);
        } catch (IOException) {
            return false;
        }
    }

    private static bool SameContent(string path, byte[] bytes) {
        var existing = File.ReadAllBytes(path);
        if (existing.Length != bytes.Length) return false;
        for (var i = 0; i < bytes.Length; i++) {
            if (existing[i] != bytes[i]) return false;
        }

        return true;
    }
}
