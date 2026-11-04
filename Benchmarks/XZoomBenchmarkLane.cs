#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>One isolated candidate product/fixture context. Shared runner owns every sample's timing and ordering.</summary>
public sealed class XZoomBenchmarkLane {
    private readonly Func<string, object> _create;
    private readonly Func<string, string> _source;
    private readonly Action<object, string> _setup;
    private readonly Func<object, string, object> _execute;
    private readonly Func<object, object, string, Dictionary<string, long>> _validate;
    private readonly Func<object, string> _digest;
    private readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal);
    private object? _model, _result;
    private string _operation = "", _descriptor = "";

    /// <summary>Loads only the candidate and new fixture; historical fixture assemblies are not involved.</summary>
    public XZoomBenchmarkLane(string core, string fixtures, string tokens, string font, string bold) {
        var context = new AssemblyLoadContext("XZoom-" + Guid.NewGuid(), true);
        context.Resolving += (ctx, name) => name.Name == "ChartForgeX" ? ctx.LoadFromAssemblyPath(Path.GetFullPath(core)) : null;
        context.LoadFromAssemblyPath(Path.GetFullPath(core));
        var type = context.LoadFromAssemblyPath(Path.GetFullPath(fixtures)).GetType("XZoomBenchmarkCases", true)!;
        type.GetMethod("Initialize")!.Invoke(null, new object[] { tokens, font, bold });
        _create = (Func<string, object>)type.GetMethod("Create")!.CreateDelegate(typeof(Func<string, object>));
        _source = (Func<string, string>)type.GetMethod("SourceDigest", new[] { typeof(string) })!.CreateDelegate(typeof(Func<string, string>));
        _setup = (Action<object, string>)type.GetMethod("Setup")!.CreateDelegate(typeof(Action<object, string>));
        _execute = (Func<object, string, object>)type.GetMethod("Execute")!.CreateDelegate(typeof(Func<object, string, object>));
        _validate = (Func<object, object, string, Dictionary<string, long>>)type.GetMethod("Validate")!.CreateDelegate(typeof(Func<object, object, string, Dictionary<string, long>>));
        _digest = (Func<object, string>)type.GetMethod("OutputDigest")!.CreateDelegate(typeof(Func<object, string>));
    }

    /// <summary>Gets an authored-input digest outside timing, without preparing a bank.</summary>
    public string SourceDigest(string descriptor) => _source(descriptor);

    /// <summary>Creates one fresh input and only the prepared prerequisite required by this operation.</summary>
    public void Reset(string descriptor, string operation) {
        _descriptor = descriptor; _operation = operation; _result = null;
        Metrics = new Dictionary<string, long>();
        _model = _create(descriptor);
        _setup(_model, operation);
    }

    /// <summary>Invokes one fixture operation. There is no local timing, loop, reflection or counting here.</summary>
    public void Execute() => _result = _execute(_model!, _operation);

    /// <summary>Checks this result and within-input determinism; missing output fails visibly.</summary>
    public void Validate() {
        if (_model == null || _result == null) throw new InvalidOperationException("The native X zoom operation produced no result.");
        Metrics = _validate(_model, _result, _operation);
        var digest = _digest(_model); var key = _descriptor + "|" + _operation;
        if (_digests.TryGetValue(key, out var expected) && expected != digest)
            throw new InvalidOperationException("Native X zoom output changed within one candidate/input/operation lane.");
        _digests[key] = digest;
    }

    /// <summary>Exact current native metrics collected after timing; units are named in the suite metadata.</summary>
    public Dictionary<string, long> Metrics { get; private set; } = new();
}
