#nullable enable
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>Isolates measured assemblies so baseline and candidate render identical report charts in one rotated run.</summary>
public sealed class ChartBenchmarkLane {
    private readonly Func<object> _create;
    private readonly Func<object, string, string> _svg;
    private readonly Func<object, byte[]> _png;
    private readonly MethodInfo _digest;
    private object _chart;
    private string? _lastSvg;
    private byte[]? _lastPng;
    private string _expectedSvg, _expectedPng;

    /// <summary>Loads one product binary and records the fixture's SVG and PNG digests outside timing.</summary>
    public ChartBenchmarkLane(string core, string fixtures, string fixture) {
        var context = new AssemblyLoadContext("Charts-" + Guid.NewGuid(), true);
        context.Resolving += (ctx, name) => name.Name == "ChartForgeX" ? ctx.LoadFromAssemblyPath(Path.GetFullPath(core)) : null;
        var library = context.LoadFromAssemblyPath(Path.GetFullPath(core));
        var cases = context.LoadFromAssemblyPath(Path.GetFullPath(fixtures)).GetType("ChartBenchmarkCases", throwOnError: true)!;
        var create = cases.GetMethod("Create")!;
        _create = () => create.Invoke(null, new object[] { fixture })!;
        _digest = cases.GetMethod("Digest")!;
        var chartType = library.GetType("ChartForgeX.Core.Chart", throwOnError: true)!;
        var extensions = library.GetType("ChartForgeX.ChartExtensions", throwOnError: true)!;
        var c = Expression.Parameter(typeof(object)); var scope = Expression.Parameter(typeof(string));
        _svg = Expression.Lambda<Func<object, string, string>>(Expression.Call(extensions.GetMethod("ToSvg", new[] { chartType, typeof(string) })!, Expression.Convert(c, chartType), scope), c, scope).Compile();
        _png = Expression.Lambda<Func<object, byte[]>>(Expression.Call(extensions.GetMethod("ToPng", new[] { chartType })!, Expression.Convert(c, chartType)), c).Compile();
        _chart = _create();
        _expectedSvg = Digest(_svg(_chart, "bench"), null);
        _expectedPng = Digest(null, _png(_chart));
    }

    private string Digest(string? svg, byte[]? png) => (string)_digest.Invoke(null, new object?[] { svg, png })!;

    /// <summary>Requires a comparison lane to retain the baseline's complete output.</summary>
    public void Expect(ChartBenchmarkLane baseline) {
        _expectedSvg = baseline._expectedSvg; _expectedPng = baseline._expectedPng;
    }

    /// <summary>Creates a fresh chart outside timing, so caches on the chart instance cannot carry over.</summary>
    public void Reset() {
        _chart = _create(); _lastSvg = null; _lastPng = null;
    }

    /// <summary>Renders the chart to SVG with a host id scope.</summary>
    public int Svg() {
        _lastSvg = _svg(_chart, "bench"); return _lastSvg.Length;
    }

    /// <summary>Renders the chart to PNG.</summary>
    public int Png() {
        _lastPng = _png(_chart); return _lastPng.Length;
    }

    /// <summary>Fails when the SVG or PNG bytes differ from the declared baseline.</summary>
    public void Validate() {
        if (_lastSvg != null && Digest(_lastSvg, null) != _expectedSvg) throw new InvalidOperationException("SVG changed from the baseline.");
        if (_lastPng != null && Digest(null, _lastPng) != _expectedPng) throw new InvalidOperationException("PNG changed from the baseline.");
        if (_lastSvg == null && _lastPng == null) throw new InvalidOperationException("Nothing was rendered.");
    }
}

/// <summary>Loads old and prepared-scene fixture adapters without loading either product into the runner context.</summary>
public sealed class SceneBenchmarkLane {
    private readonly Func<string, object> _create;
    private readonly Func<object, object> _prepare;
    private readonly Func<object, string, object> _execute;
    private readonly Func<object, string, string, long> _validate;
    private readonly string _fixture;
    private readonly System.Collections.Generic.Dictionary<string, string> _digests = new();
    private object _model;
    private object? _result;
    private string _operation = "";
    private long _outputLength;

    /// <summary>Loads a fixture adapter, canonical theme and fixed model before samples start.</summary>
    public SceneBenchmarkLane(string core, string fixtures, string tokenPath, string fontPath, string boldFontPath, string fixture, bool prepared) {
        var context = new AssemblyLoadContext("Scene-" + Guid.NewGuid(), true);
        context.Resolving += (ctx, name) => name.Name == "ChartForgeX" ? ctx.LoadFromAssemblyPath(Path.GetFullPath(core)) : null;
        context.LoadFromAssemblyPath(Path.GetFullPath(core));
        var type = context.LoadFromAssemblyPath(Path.GetFullPath(fixtures)).GetType(prepared ? "DirectSceneBenchmarkCases" : "LegacySceneBenchmarkCases", true)!;
        type.GetMethod("Initialize")!.Invoke(null, new object[] { tokenPath, fontPath, boldFontPath });
        _create = (Func<string, object>)type.GetMethod("Create")!.CreateDelegate(typeof(Func<string, object>));
        _prepare = (Func<object, object>)type.GetMethod("Prepare")!.CreateDelegate(typeof(Func<object, object>));
        _execute = (Func<object, string, object>)type.GetMethod("Execute")!.CreateDelegate(typeof(Func<object, string, object>));
        _validate = (Func<object, string, string, long>)type.GetMethod("Validate")!.CreateDelegate(typeof(Func<object, string, string, long>));
        _fixture = fixture;
        _model = _create(_fixture);
        SourceDigest = (string)type.GetMethod("SourceDigest")!.Invoke(null, new object[] { fixture })!;
        _validate(_execute(_model, "Svg"), "Svg", _fixture);
    }

    /// <summary>Creates the model and, for export-only cases, prepares its scene outside timing.</summary>
    public void Reset(string operation) {
        _operation = operation; _result = null; _outputLength = 0;
        _model = _create(_fixture);
        if (operation.StartsWith("Prepared", StringComparison.Ordinal)) _model = _prepare(_model);
    }

    /// <summary>Executes exactly the named rendering boundary.</summary>
    public void Execute() => _result = _execute(_model, _operation);

    /// <summary>Checks dimensions/content and within-lane deterministic output after timing.</summary>
    public void Validate() {
        if (_result == null) throw new InvalidOperationException("Nothing was rendered.");
        _outputLength = _validate(_result, _operation, _fixture);
        // Byte identity is required within an unchanged lane, while approved v2 layout differs from legacy output.
        if (_result is string || _result is byte[]) {
            var bytes = _result is byte[] image ? image : System.Text.Encoding.UTF8.GetBytes((string)_result);
            var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            if (_digests.TryGetValue(_operation, out var expected) && expected != digest)
                throw new InvalidOperationException("Non-deterministic " + _operation + " output.");
            _digests[_operation] = digest;
        }
    }

    /// <summary>Gets output bytes, SVG characters or compiled semantic-region count, as named by each operation.</summary>
    public long OutputLength => _outputLength;

    /// <summary>Gets the source series/value/break digest to require identical paired input.</summary>
    public string SourceDigest { get; }
}
