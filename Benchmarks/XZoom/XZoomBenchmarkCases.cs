using ChartForgeX.Rendering;
using System.Text;
using System.Security.Cryptography;

/// <summary>Candidate-only native X zoom operations; all input and inspection stays outside the measured delegate.</summary>
public static partial class XZoomBenchmarkCases {
    private static readonly HashSet<string> NativeQualified = new(StringComparer.Ordinal);

    /// <summary>Loads canonical tokens and regular/bold native font fixtures before planning or timing.</summary>
    public static void Initialize(string tokenPath, string fontPath, string boldFontPath) => InitializeInputs(tokenPath, fontPath, boldFontPath);

    /// <summary>Creates a fresh typed chart, context, callbacks and bank declaration outside timing.</summary>
    public static object Create(string descriptor) => CreateInput(descriptor);

    /// <summary>Hashes the actual authored series, points, gaps, paint and passive surfaces, without preparing a bank.</summary>
    public static string SourceDigest(string descriptor) => SourceDigest(CreateInput(descriptor));

    /// <summary>Prepares only the prerequisites of At/export outside timing; PrepareBank has no prepared prerequisite.</summary>
    public static void Setup(object model, string operation) {
        var input = (ZoomInput)model;
        if (operation == "PrepareBank") return;
        input.Bank = input.Chart.PrepareXZoom(input.Context, input.Levels);
        input.Frozen = input.Counters;
        if (operation == "PreparedSvg") input.View = input.Bank.At(input.LevelIndex, input.Pan);
        else if (operation != "PreparedAt") throw new ArgumentOutOfRangeException(nameof(operation));
    }

    /// <summary>Runs one public API call. No input creation, reflection, metrics, validation or timing loop occurs here.</summary>
    public static object Execute(object model, string operation) {
        var input = (ZoomInput)model;
        return operation switch {
            "PrepareBank" => input.Chart.PrepareXZoom(input.Context, input.Levels),
            "PreparedAt" => input.Bank!.At(input.LevelIndex, input.Pan),
            "PreparedSvg" => input.View!.ToSvg("xzoom-benchmark"),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    /// <summary>Validates current results and collects exact native payload counts outside timed operations.</summary>
    public static Dictionary<string, long> Validate(object model, object result, string operation) {
        var input = (ZoomInput)model;
        if (operation == "PrepareBank") { input.Bank = (PreparedChartXZoom)result; input.Frozen = input.Counters; }
        else Require(input.Counters == input.Frozen, "At/export invoked an authored formatter.");
        Require(input.SourceSha == SourceDigest(input), "Preparation mutated authored source data/options.");
        var bank = input.Bank ?? throw new InvalidOperationException("A bank was not prepared.");
        ValidateBank(input, bank);
        var view = operation == "PreparedAt" ? (PreparedVisual)result : input.View ?? bank.At(input.LevelIndex, input.Pan);
        ValidateView(input, bank, view, input.LevelIndex, input.Pan);
        var svg = operation == "PreparedSvg" ? (string)result : view.ToSvg("xzoom-benchmark");
        ValidateSvg(input, view, svg);
        Require(svg == view.ToSvg("xzoom-benchmark"), "Native SVG is not deterministic within the lane.");
        if (!NativeQualified.Contains(input.Descriptor)) {
            ValidateNativeExport(input, view);
            NativeQualified.Add(input.Descriptor);
        }
        var metrics = NativeMetrics(bank, input.LevelIndex);
        metrics["AtSvgUtf8Bytes"] = Encoding.UTF8.GetByteCount(svg);
        metrics["SourcePointFacts"] = input.Chart.Series.Sum(series => (long)series.Points.Count);
        metrics["XFormatterCalls"] = input.XCalls;
        metrics["ValueFormatterCalls"] = input.ValueCalls;
        Require(input.Counters == input.Frozen, "Inspection or native export invoked an authored formatter.");
        Require(input.XPerValue.Values.All(count => count == 1), "X values were formatted more than once during one bank preparation.");
        input.LastSvgDigest = Hash(Encoding.UTF8.GetBytes(svg));
        return metrics;
    }

    /// <summary>Gets the complete current-view native SVG digest produced by validation, outside timing.</summary>
    public static string OutputDigest(object model) => ((ZoomInput)model).LastSvgDigest;

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
