using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ApiLedger;

/// <summary>Checks ledger reachability and overload fidelity using an independent miniature public API.</summary>
internal static class SelfTest {
    internal static void Run(string? scratchDirectory = null) {
        const string source = """
            namespace Fixture;
            public partial class Api<T> where T : class {
                public int Value { get; private set; }
                public void Add(int count = 3) { }
                public void Add(string label) { }
                protected virtual void Configure() { }
                public sealed class Nested { }
                private class Hidden { public class Child { } }
            }
            public partial class Api<T> {
                public event System.Action? Changed;
                internal void InternalOnly() { Changed?.Invoke(); }
            }
            public enum State : byte { Ready = 7, Done = 9 }
            public readonly struct Value { }
            public interface IContract { int Read(); }
            public sealed class Explicit : IContract { int IContract.Read() => 1; }
            public delegate void Callback(int value);
            """;
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("Fixture", [CSharpSyntaxTree.ParseText(source, path: "fixture.cs")], refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        Require(errors.Length == 0, string.Join("\n", errors.Select(item => item.ToString())));
        var rows = Inventory.Extract(compilation);
        Require(rows.Count(row => row.DocId == "T:Fixture.Api`1") == 1, "Partial type must occur exactly once.");
        Require(rows.Count(row => row.Name == "Add") == 2, "Overloads must remain distinct.");
        Require(rows.Single(row => row.DocId == "M:Fixture.Api`1.Add(System.Int32)").Signature.Contains("3", StringComparison.Ordinal), "Optional default must be captured.");
        Require(rows.Any(row => row.DocId == "M:Fixture.Api`1.Configure" && row.Accessibility == "Protected"), "Subclass API must be captured.");
        Require(rows.Any(row => row.DocId == "T:Fixture.Api`1.Nested"), "Public nested type must be captured.");
        Require(rows.All(row => !row.Type.Contains("Hidden", StringComparison.Ordinal) && row.Name != "InternalOnly"), "Nested reachability must exclude implementation members.");
        Require(rows.Any(row => row.DocId == "M:Fixture.Api`1.#ctor" && row.Implicit), "Implicit public constructors are API.");
        Require(rows.Single(row => row.DocId == "F:Fixture.State.Ready").Constant == "7", "Enum numeric values must be captured.");
        Require(rows.Single(row => row.DocId == "T:Fixture.State").Signature.Contains("byte", StringComparison.Ordinal), "Enum underlying type must be captured.");
        Require(rows.Single(row => row.DocId == "T:Fixture.Value").Signature.Contains("readonly", StringComparison.Ordinal), "Readonly type contract must be captured.");
        Require(rows.Any(row => row.DocId == "E:Fixture.Api`1.Changed") && rows.All(row => !row.Name.StartsWith("add_", StringComparison.Ordinal)), "Events must not duplicate accessors.");
        Require(rows.Any(row => row.DocId == "M:Fixture.IContract.Read"), "Explicit implementation must remain reachable through interface contract.");
        Require(rows.Any(row => row.DocId == "T:Fixture.Callback") && rows.Any(row => row.Name == "Invoke"), "Delegate invocation contract must be captured.");
        Require(rows.Single(row => row.DocId == "P:Fixture.Api`1.Value").Signature.Contains("private set", StringComparison.Ordinal), "Accessor visibility must be retained.");
        EvidenceSelfTest.Run(scratchDirectory, rows.Single(row => row.DocId == "M:Fixture.Api`1.Add(System.Int32)"));
        Console.WriteLine("API ledger self-test passed: symbol fidelity, private evidence anonymization, external mapping validation and LF CSV serialization.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
