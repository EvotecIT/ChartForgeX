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
