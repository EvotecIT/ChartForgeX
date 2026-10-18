#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>Separates benchmark dependencies from the PowerForge host and retains one browser per case.</summary>
public sealed class InteractivityBenchmarkLane : IDisposable {
    private static readonly List<InteractivityBenchmarkLane> Lanes = new();
    private readonly AssemblyLoadContext _context;
    private readonly Type _type;
    private readonly int _points, _queries;
    private readonly string _source;
    private object? _fixture;

    /// <summary>Loads the fixture without launching a browser, so PowerForge Plan remains read-only.</summary>
    public InteractivityBenchmarkLane(string fixturePath, string binaries, string source, int points, int queries) {
        _context = new AssemblyLoadContext("Interactivity-" + Guid.NewGuid(), isCollectible: true);
        _context.Resolving += (context, name) => {
            var path = Path.Combine(binaries, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
        _type = _context.LoadFromAssemblyPath(Path.GetFullPath(fixturePath)).GetType("InteractivityBenchmarkFixture", throwOnError: true)!;
        _points = points; _queries = queries; _source = source; Lanes.Add(this);
    }

    /// <summary>Initializes the persistent browser on first setup and resets results between samples.</summary>
    public void Reset() {
        _fixture ??= Activator.CreateInstance(_type, _points, _queries, _source)!;
        _type.GetMethod("Reset")!.Invoke(_fixture, null);
    }
    /// <summary>Runs the declared acquisition batch inside PowerForge timing.</summary>
    public void Execute() => _type.GetMethod("Execute")!.Invoke(_fixture, null);
    /// <summary>Runs source-identity and nearest-observation integrity proof outside timing.</summary>
    public void Validate() => _type.GetMethod("Validate")!.Invoke(_fixture, null);
    /// <summary>Gets the measured batch's checked source-index checksum.</summary>
    public int SourceIndexSum => (int)_type.GetProperty("SourceIndexSum")!.GetValue(_fixture)!;
    /// <summary>Returns the browser versions without creating uninitialized cases.</summary>
    public static string[] BrowserVersions() => Lanes.ConvertAll(lane => lane._fixture == null ? "Not started"
        : lane._points + ": " + (string)lane._type.GetProperty("BrowserVersion")!.GetValue(lane._fixture)!).ToArray();
    /// <summary>Closes this case's browser and releases its isolated load context.</summary>
    public void Dispose() {
        if (_fixture is IDisposable disposable) disposable.Dispose();
        _fixture = null; _context.Unload();
    }
    /// <summary>Closes all initialized sessions after success, failure, or planning.</summary>
    public static void DisposeAll() {
        foreach (var lane in Lanes) lane.Dispose();
        Lanes.Clear();
    }
}
