#nullable enable
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>Isolates measured assemblies so baseline and candidate use identical fixtures in one rotated run.</summary>
public sealed class TopologyBenchmarkLane {
    private readonly object _chart, _options, _prepared;
    private readonly Func<object, object, object> _prepare;
    private readonly Func<object, string> _svg;
    private readonly Func<object, object> _analyze;
    private readonly MethodInfo _proof;
    private readonly PropertyInfo _height;
    private readonly Action _clearPlanCache;
    private object _lastPrepared;
    private string? _lastSvg;
    private string _expectedPrepare, _expectedSvg;

    /// <summary>Loads one product binary and prepares fixture and validation baselines outside timing.</summary>
    public TopologyBenchmarkLane(string core, string fixtures, string fixture) {
        var context = new AssemblyLoadContext("Topology-" + Guid.NewGuid(), true);
        context.Resolving += (ctx, name) => name.Name == "ChartForgeX" ? ctx.LoadFromAssemblyPath(Path.GetFullPath(core)) : null;
        var library = context.LoadFromAssemblyPath(Path.GetFullPath(core));
        var fixtureLibrary = context.LoadFromAssemblyPath(Path.GetFullPath(fixtures));
        var cases = fixtureLibrary.GetType("TopologyBenchmarkCases", throwOnError: true)!;
        var pair = cases.GetMethod("Create")!.Invoke(null, new object[] { fixture })!;
        _proof = cases.GetMethod("Proof")!;
        _chart = pair.GetType().GetField("Item1")!.GetValue(pair)!;
        _options = pair.GetType().GetField("Item2")!.GetValue(pair)!;
        var chartType = library.GetType("ChartForgeX.Topology.TopologyChart", throwOnError: true)!;
        var optionsType = library.GetType("ChartForgeX.Topology.TopologyRenderOptions", throwOnError: true)!;
        var method = library.GetType("ChartForgeX.Topology.TopologyChartExtensions", throwOnError: true)!.GetMethod("Prepare", new[] { chartType, optionsType })!;
        var a = Expression.Parameter(typeof(object)); var b = Expression.Parameter(typeof(object));
        _prepare = Expression.Lambda<Func<object, object, object>>(Expression.Convert(Expression.Call(method, Expression.Convert(a, chartType), Expression.Convert(b, optionsType)), typeof(object)), a, b).Compile();
        var preparedType = library.GetType("ChartForgeX.Topology.PreparedTopology", throwOnError: true)!;
        var p = Expression.Parameter(typeof(object));
        _svg = Expression.Lambda<Func<object, string>>(Expression.Call(Expression.Convert(p, preparedType), preparedType.GetMethod("ToSvg")!), p).Compile();
        _analyze = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Call(Expression.Convert(p, preparedType), preparedType.GetMethod("Analyze")!), typeof(object)), p).Compile();
        _height = preparedType.GetProperty("Height")!;
        // Binaries with a shared plan cache would serve every repeated preparation from it; measured operations start cold.
        var clear = library.GetType("ChartForgeX.Topology.TopologyDenseRoutePlanner")?.GetMethod("ClearPlanCache", BindingFlags.NonPublic | BindingFlags.Static);
        _clearPlanCache = clear == null ? () => { } : (Action)Delegate.CreateDelegate(typeof(Action), clear);
        _prepared = _lastPrepared = _prepare(_chart, _options);
        _expectedPrepare = Proof(_prepared, null);
        _expectedSvg = Proof(_prepared, _svg(_prepared));
    }

    private string Proof(object prepared, string? svg) => (string)_proof.Invoke(null, new object?[] { prepared, svg })!;

    /// <summary>Requires a comparison lane to retain the baseline's complete output.</summary>
    public void Expect(TopologyBenchmarkLane baseline) {
        _expectedPrepare = baseline._expectedPrepare; _expectedSvg = baseline._expectedSvg;
    }

    /// <summary>Measures detached node/group layout, which may defer dense route planning.</summary>
    public double Prepare() {
        _clearPlanCache();
        _lastPrepared = _prepare(_chart, _options); _lastSvg = null;
        return (double)_height.GetValue(_lastPrepared)!;
    }

    /// <summary>Includes route planning and diagnostics inside the measured operation.</summary>
    public double CompletePrepare() {
        var height = Prepare(); _analyze(_lastPrepared); return height;
    }

    /// <summary>Creates a fresh snapshot and renders it.</summary>
    public int Svg() {
        _clearPlanCache();
        _lastPrepared = _prepare(_chart, _options); _lastSvg = _svg(_lastPrepared); return _lastSvg.Length;
    }

    /// <summary>Creates a fresh snapshot and renders it, as a host's second drawing of the same chart does: a plan of equal geometry may come from the cache.</summary>
    public int RepeatSvg() {
        _lastPrepared = _prepare(_chart, _options); _lastSvg = _svg(_lastPrepared); return _lastSvg.Length;
    }

    /// <summary>Renders the already fully planned snapshot created in setup.</summary>
    public int PreparedSvg() {
        _lastPrepared = _prepared; _lastSvg = _svg(_prepared); return _lastSvg.Length;
    }

    /// <summary>Fails when geometry, counts, diagnostics or SVG differ from the declared baseline.</summary>
    public void Validate(bool svg) {
        if (Proof(_lastPrepared, svg ? _lastSvg : null) != (svg ? _expectedSvg : _expectedPrepare))
            throw new InvalidOperationException("Dimensions, counts, route diagnostics or SVG changed from the baseline.");
    }
}
