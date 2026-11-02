using System.Text;
using ChartForgeX.Markup;
using ChartForgeX.VisualArtifacts;

/// <summary>
/// Writes examples generated from ChartForgeX v1 markup fences.
/// </summary>
internal static partial class MarkupExamples {
    public static void Write(string output) {
        foreach (var example in Examples) SaveExample(output, example);
        foreach (var example in ChartFamilyExamples) SaveExample(output, example);
    }

    private static void SaveExample(string output, MarkupExample example) {
        var parser = new VisualMarkupParser();
        var result = parser.Parse(example.Source);
        if (result.HasErrors || result.Artifacts.Count != 1) {
            var diagnostics = string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ": " + diagnostic.Severity + ": " + diagnostic.Message));
            throw new InvalidOperationException("Markup example '" + example.Name + "' did not produce exactly one artifact." + Environment.NewLine + diagnostics);
        }

        var artifact = result.Artifacts[0];
        artifact.SaveSvg(Path.Combine(output, example.Name + ".svg"));
        artifact.SaveHtml(Path.Combine(output, example.Name + ".html"));
        artifact.SavePng(Path.Combine(output, example.Name + ".png"));
        WriteMarkupSource(output, example.Name, example.Source);
        WriteCSharpSource(output, example);
    }

    private static void WriteMarkupSource(string output, string name, string source) =>
        File.WriteAllText(Path.Combine(output, name + ".cfx.md"), Normalize(source), Encoding.UTF8);

    private static void WriteCSharpSource(string output, MarkupExample example) {
        var code = "using System;\n" +
            "using System.Linq;\n" +
            "using ChartForgeX.Markup;\n" +
            "using ChartForgeX.VisualArtifacts;\n\n" +
            "var markdown = @\"" + Normalize(example.Source).Replace("\"", "\"\"", StringComparison.Ordinal) + "\";\n\n" +
            "var result = new VisualMarkupParser().Parse(markdown);\n" +
            "if (result.HasErrors) {\n" +
            "    throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));\n" +
            "}\n\n" +
            "var artifact = result.Artifacts.Single();\n" +
            "artifact.SaveSvg(\"" + example.Name + ".svg\");\n" +
            "artifact.SavePng(\"" + example.Name + ".png\");\n" +
            "artifact.SaveHtml(\"" + example.Name + ".html\");\n";
        File.WriteAllText(Path.Combine(output, example.Name + ".csharp.txt"), code.Replace("\r\n", "\n"), Encoding.UTF8);
    }

    private static string Normalize(string source) => source.Replace("\r\n", "\n").Trim() + "\n";

    private static readonly MarkupExample[] Examples = {
        new(
            "markup-chart-release-trend",
            """
```chartforgex chart v1
id release-trend
title Release Readiness Trend
subtitle Multi-series chart parsed from native v1 markup
type smooth-line
size 920x520
labels Intake, Build, Test, Package, Publish

series Ready type smoothline values 34 48 62 74 88 color #2563EB
series Blocked type smoothline values 18 14 11 7 4 color #DC2626
annotation hline 80 "ship gate" color=#059669
```
"""),
        new(
            "markup-flow-approval",
            """
```chartforgex flow v1
id approval-flow
title Approval Flow
subtitle FlowArtifact parsed from native v1 markup, statically previewed through topology
size 1120x620
layout layered
direction left-to-right

lanes:
| id | label | status |
| --- | --- | --- |
| author | Author | positive |
| review | Review | warning |
| release | Release | neutral |

steps:
| id | label | kind | lane | status | subtitle | badge |
| --- | --- | --- | --- | --- | --- | --- |
| draft | Draft change | start | author | positive | C# or markdown source | 1 |
| validate | Validate contract | process | review | positive | Parser plus render checks | 2 |
| approve | Approved? | decision | review | warning | Human gate | ? |
| publish | Publish package | end | release | positive | Release artifact | 3 |

connectors:
| from | to | label | kind | status |
| --- | --- | --- | --- | --- |
| draft | validate | submit | flow | positive |
| validate | approve | evidence | dependency | positive |
| approve | publish | yes | flow | positive |
| approve | draft | changes | retry | warning |
```
"""),
        new(
            "markup-sequence-incident",
            """
```chartforgex sequence v1
id incident-sequence
title Incident Sequence
subtitle SequenceArtifact parsed from native v1 markup
size 960x560

participants:
| id | label | kind |
| --- | --- | --- |
| user | User | actor |
| api | API | participant |
| db | Database | database |

messages:
| from | to | text | style | activate |
| --- | --- | --- | --- | --- |
| user | api | Submit request | solid | true |
| api | db | Store event | dashed | false |
| db | api | Stored | dashed | false |
| api | user | Accepted | solid | false |

notes:
| placement | participant | text | step |
| --- | --- | --- | --- |
| rightOf | api | Processing and validation | 1 |

blocks:
| kind | text | start | end |
| --- | --- | --- | --- |
| loop | Retry on transient failure | 0 | 2 |
```
"""),
        new(
            "markup-timeline-release-plan",
            """
```chartforgex timeline v1
id release-plan
title Release Plan
subtitle Timeline/Gantt chart parsed from native v1 markup
type gantt
size 980x560
today 2026-02-12

task Design 2026-01-05 2026-01-18 progress=1 color=#2563EB
task Implement 2026-01-15 2026-02-14 progress=0.72 dependsOn=0 color=#14B8A6
task Validate 2026-02-10 2026-02-28 progress=0.35 dependsOn=1 color=#F59E0B
milestone Release 2026-03-05 dependsOn=2 color=#059669
```
"""),
        new(
            "markup-table-release-gates",
            """
```chartforgex table v1
id release-gates
title Release Gates
subtitle TableArtifact parsed from native v1 markup with typed columns
capabilities sort filter export

columns:
| id | label | type | alignment |
| --- | --- | --- | --- |
| gate | Gate | text | left |
| owner | Owner | text | left |
| status | Status | status | center |
| evidence | Evidence | text | left |

rows:
| gate | owner | status | evidence |
| --- | --- | --- | --- |
| API grammar | Markup | positive | v1 reference and parser tests |
| Static output | Core | positive | SVG and PNG parity |
| Package smoke | Release | warning | Build.ps1 release gate |
```
"""),
        new(
            "markup-topology-service-map",
            """
```chartforgex topology v1
id service-map
title Service Map
subtitle TopologyChart parsed from native v1 markup
viewport 1120x620
layout layered left-to-right

groups:
| id | label | status |
| --- | --- | --- |
| edge | Edge | healthy |
| core | Core | warning |
| data | Data | healthy |

nodes:
| id | label | kind | status | group | subtitle | badge |
| --- | --- | --- | --- | --- | --- | --- |
| portal | Portal | application | healthy | edge | Public entry | TLS |
| api | API | service | warning | core | Backpressure watched | P95 |
| worker | Worker | process | healthy | core | Queue consumer | 4 |
| db | Database | database | healthy | data | Primary store | HA |

edges:
| from | to | label | kind | status |
| --- | --- | --- | --- | --- |
| portal | api | HTTPS | data-flow | healthy |
| api | worker | async | dependency | warning |
| worker | db | write | data-flow | healthy |
| api | db | read | data-flow | healthy |
```
""")
    };

    private readonly struct MarkupExample {
        public MarkupExample(string name, string source) {
            Name = name;
            Source = source;
        }

        public string Name { get; }
        public string Source { get; }
    }
}
