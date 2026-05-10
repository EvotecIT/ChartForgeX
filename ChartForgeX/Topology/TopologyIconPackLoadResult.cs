using System;
using System.IO;

namespace ChartForgeX.Topology;

/// <summary>
/// Describes the result of loading one topology icon-pack manifest from disk.
/// </summary>
public sealed class TopologyIconPackLoadResult {
    internal TopologyIconPackLoadResult(string path, TopologyIconPack pack) {
        SourcePath = path;
        FileName = Path.GetFileName(path);
        Pack = pack;
    }

    internal TopologyIconPackLoadResult(string path, TopologyIconPack pack, string skipReason) {
        SourcePath = path;
        FileName = Path.GetFileName(path);
        Pack = pack;
        SkipReason = skipReason;
    }

    internal TopologyIconPackLoadResult(string path, Exception exception) {
        SourcePath = path;
        FileName = Path.GetFileName(path);
        ErrorMessage = exception.Message;
        Exception = exception;
    }

    /// <summary>Gets the source manifest path.</summary>
    public string SourcePath { get; }

    /// <summary>Gets the source manifest file name.</summary>
    public string FileName { get; }

    /// <summary>Gets the loaded icon pack when loading succeeded.</summary>
    public TopologyIconPack? Pack { get; }

    /// <summary>Gets the load error message when loading failed.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets the skip reason when a valid manifest was not added to the catalog.</summary>
    public string? SkipReason { get; }

    /// <summary>Gets the load exception when loading failed.</summary>
    public Exception? Exception { get; }

    /// <summary>Gets whether the manifest loaded successfully.</summary>
    public bool Succeeded => Pack != null && ErrorMessage == null && SkipReason == null;

    /// <summary>Gets whether the manifest loaded but was intentionally skipped.</summary>
    public bool Skipped => Pack != null && SkipReason != null;
}
