#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>Separates typed benchmark fixtures from the PowerForge host through the existing isolated load boundary.</summary>
public sealed class InteractivityBenchmarkLane : IDisposable {
    private static readonly List<InteractivityBenchmarkLane> Lanes = new();
    private readonly AssemblyLoadContext _context;
    private readonly Type _type;
    private readonly int _points;
    private readonly object[] _arguments;
    private readonly string? _receiptName;
    private object? _fixture;

    /// <summary>Loads the fixture without launching a browser, so PowerForge Plan remains read-only.</summary>
    public InteractivityBenchmarkLane(string fixturePath, string binaries, string source, int points, int queries)
        : this(fixturePath, binaries, "InteractivityBenchmarkFixture") {
        _points = points; _arguments = new object[] { points, queries, source };
    }

    /// <summary>Loads a shared or current typed section fixture without preparing a chart or launching a browser.</summary>
    public InteractivityBenchmarkLane(string fixturePath, string binaries, string caseName, string tokens, string font, string boldFont, bool current)
        : this(fixturePath, binaries, current ? "CartesianSectionPreparationBenchmarkFixture" : "SectionPreparationBenchmarkFixture") {
        _arguments = new object[] { caseName, tokens, font, boldFont };
        _receiptName = (current ? "Candidate-" : "Baseline-") + caseName;
    }

    private InteractivityBenchmarkLane(string fixturePath, string binaries, string fixtureType) {
        _context = new AssemblyLoadContext("Interactivity-" + Guid.NewGuid(), isCollectible: true);
        _context.Resolving += (context, name) => {
            var path = Path.Combine(binaries, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
        _type = _context.LoadFromAssemblyPath(Path.GetFullPath(fixturePath)).GetType(fixtureType, throwOnError: true)!;
        _arguments = Array.Empty<object>(); Lanes.Add(this);
    }

    /// <summary>Initializes the typed fixture on first setup and resets results between samples.</summary>
    public void Reset() {
        Initialize();
        Invoke("Reset");
    }
    /// <summary>Runs the declared fixture operation inside PowerForge timing.</summary>
    public void Execute() => Invoke("Execute");
    /// <summary>Runs the fixture's complete integrity proof outside timing.</summary>
    public void Validate() => Invoke("Validate");
    private void Initialize() { _fixture ??= Activator.CreateInstance(_type, _arguments)!; }
    private void Invoke(string method) => _type.GetMethod(method)!.Invoke(_fixture,
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DoNotWrapExceptions, null, null, null);
    /// <summary>Gets the measured batch's checked source-index checksum.</summary>
    public int SourceIndexSum => (int)_type.GetProperty("SourceIndexSum")!.GetValue(_fixture)!;
    /// <summary>Requires actual typed source/policy equality before a shared baseline/current sample.</summary>
    public void ExpectSource(InteractivityBenchmarkLane baseline) {
        Initialize(); baseline.Initialize();
        if ((string)Property("SourceDigest") != (string)baseline.Property("SourceDigest"))
            throw new InvalidOperationException("The paired section source data or presentation policy changed.");
    }
    private object Property(string name) => _type.GetProperty(name)!.GetValue(_fixture)!;
    /// <summary>Gets the validated measured section's UTF-8 payload size.</summary>
    public int HtmlBytes => (int)Property("HtmlBytes");
    /// <summary>Gets the typed source observation count.</summary>
    public int SourceCount => (int)Property("SourceCount");
    /// <summary>Gets the wide native drawing's exported SVG element count.</summary>
    public int WideSvgElementCount => (int)Property("WideSvgElementCount");
    /// <summary>Gets the narrow native drawing's exported SVG element count.</summary>
    public int NarrowSvgElementCount => (int)Property("NarrowSvgElementCount");
    /// <summary>Returns the browser versions without creating uninitialized cases.</summary>
    public static string[] BrowserVersions() => Lanes.FindAll(lane => lane._receiptName == null).ConvertAll(lane => lane._fixture == null ? "Not started"
        : lane._points + ": " + (string)lane._type.GetProperty("BrowserVersion")!.GetValue(lane._fixture)!).ToArray();
    /// <summary>Captures native section receipts only after PowerForge returns, without adding export cost to samples.</summary>
    public static void WriteSectionReceipts(string directory) {
        foreach (var lane in Lanes) {
            if (lane._receiptName == null || lane._fixture == null) continue;
            lane._type.GetMethod("WriteReceipt")!.Invoke(lane._fixture,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DoNotWrapExceptions, null,
                new object[] { Path.Combine(directory, lane._receiptName) }, null);
        }
    }
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
