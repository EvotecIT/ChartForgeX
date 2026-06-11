using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Parses Mermaid source into source-preserving document models.
/// </summary>
public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source and detects the diagram family.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidDocument> Parse(string source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var result = new MermaidParseResult<MermaidDocument>();
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var frontMatter = ReadFrontMatter(lines, result);
        var header = FindHeader(lines, frontMatter.EndLine + 1, result);
        if (!header.HasValue) {
            Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source must declare a diagram type.");
            return result;
        }

        var descriptor = ResolveDiagramKind(header.Value.Text);
        if (descriptor.Kind == MermaidDiagramKind.Unknown) {
            Add(result, header.Value.Line, header.Value.Column, header.Value.Text.Length, MermaidDiagnosticSeverity.Error, "Unknown Mermaid diagram type '" + FirstToken(header.Value.Text) + "'.");
            return result;
        }

        MermaidDocument document;
        if (descriptor.Kind == MermaidDiagramKind.Flowchart) {
            document = ParseFlowchart(source, lines, frontMatter, header.Value, descriptor, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Sequence) {
            document = ParseSequence(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Class) {
            document = ParseClass(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.State) {
            document = ParseState(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.EntityRelationship) {
            document = ParseEntityRelationship(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Pie) {
            document = ParsePie(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Journey) {
            document = ParseJourney(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.GitGraph) {
            document = ParseGitGraph(source, lines, frontMatter, header.Value, descriptor, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Gantt) {
            document = ParseGantt(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.MindMap) {
            document = ParseMindMap(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Timeline) {
            document = ParseTimeline(source, lines, frontMatter, header.Value, descriptor, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Requirement) {
            document = ParseRequirement(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Quadrant) {
            document = ParseQuadrant(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.XYChart) {
            document = ParseXYChart(source, lines, frontMatter, header.Value, descriptor, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Sankey) {
            document = ParseSankey(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Radar) {
            document = ParseRadar(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Treemap) {
            document = ParseTreemap(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Kanban) {
            document = ParseKanban(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Architecture) {
            document = ParseArchitecture(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.C4) {
            document = ParseC4(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Block) {
            document = ParseBlock(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Packet) {
            document = ParsePacket(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Venn) {
            document = ParseVenn(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Ishikawa) {
            document = ParseIshikawa(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.Wardley) {
            document = ParseWardley(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.TreeView) {
            document = ParseTreeView(source, lines, frontMatter, header.Value, result);
        } else if (descriptor.Kind == MermaidDiagramKind.EventModeling) {
            document = ParseEventModeling(source, lines, frontMatter, header.Value, result);
        } else {
            document = new MermaidDocument {
                SourceText = source,
                Kind = descriptor.Kind,
                Header = header.Value.Text,
                HeaderSpan = new MermaidSourceSpan(header.Value.Line, header.Value.Column, header.Value.Text.Length),
                FrontMatter = frontMatter.Text
            };
            ReadRawBodyStatements(document, lines, header.Value.Line + 1);
            Add(result, header.Value.Line, header.Value.Column, header.Value.Text.Length, MermaidDiagnosticSeverity.Warning, "Mermaid diagram kind '" + descriptor.HeaderKind + "' is recognized but not implemented yet.");
        }

        AddDirectives(document, lines, frontMatter.EndLine + 1, header.Value.Line - 1);
        result.Document = document;
        return result;
    }

    private static MermaidFlowchartDocument ParseFlowchart(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, DiagramDescriptor descriptor, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidFlowchartDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Flowchart,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text,
            Direction = ParseFlowchartDirection(descriptor.Direction)
        };

        MermaidFlowchartParser.ParseStatements(document, lines, header.Line + 1, result);

        return document;
    }

    private static MermaidSequenceDocument ParseSequence(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidSequenceDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Sequence,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidSequenceParser.ParseStatements(document, lines, header.Line + 1, result);

        return document;
    }

    private static MermaidPieDocument ParsePie(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidPieDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Pie,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        foreach (var token in Tokenize(header.Text)) {
            if (string.Equals(token, "showData", StringComparison.OrdinalIgnoreCase)) {
                document.ShowData = true;
                break;
            }
        }

        MermaidPieParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidJourneyDocument ParseJourney(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidJourneyDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Journey,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidJourneyParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidClassDocument ParseClass(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidClassDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Class,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidClassParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidStateDocument ParseState(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidStateDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.State,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidStateParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidEntityRelationshipDocument ParseEntityRelationship(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidEntityRelationshipDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.EntityRelationship,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidEntityRelationshipParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidGanttDocument ParseGantt(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidGanttDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Gantt,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidGanttParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidTimelineDocument ParseTimeline(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, DiagramDescriptor descriptor, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidTimelineDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Timeline,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text,
            Direction = Normalize(descriptor.Direction) == "td" ? MermaidTimelineDirection.TopDown : MermaidTimelineDirection.LeftToRight
        };

        MermaidTimelineParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidQuadrantDocument ParseQuadrant(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidQuadrantDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Quadrant,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidQuadrantParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidRequirementDocument ParseRequirement(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidRequirementDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Requirement,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidRequirementParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidMindMapDocument ParseMindMap(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidMindMapDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.MindMap,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidMindMapParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidXYChartDocument ParseXYChart(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, DiagramDescriptor descriptor, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidXYChartDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.XYChart,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text,
            Orientation = Normalize(descriptor.Direction) == "horizontal" ? MermaidXYChartOrientation.Horizontal : MermaidXYChartOrientation.Vertical
        };

        MermaidXYChartParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidSankeyDocument ParseSankey(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidSankeyDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Sankey,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidSankeyParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidRadarDocument ParseRadar(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidRadarDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Radar,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidRadarParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidTreemapDocument ParseTreemap(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidTreemapDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Treemap,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidTreemapParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidKanbanDocument ParseKanban(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidKanbanDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Kanban,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidKanbanParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static MermaidArchitectureDocument ParseArchitecture(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidArchitectureDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Architecture,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidArchitectureParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }

    private static FrontMatterResult ReadFrontMatter(string[] lines, MermaidParseResult<MermaidDocument> result) {
        if (lines.Length == 0 || lines[0].Trim() != "---") return new FrontMatterResult(null, 0);
        var content = new List<string>();
        for (var index = 1; index < lines.Length; index++) {
            if (lines[index].Trim() == "---") return new FrontMatterResult(string.Join("\n", content), index + 1);
            content.Add(lines[index]);
        }

        Add(result, 1, 1, 3, MermaidDiagnosticSeverity.Error, "Mermaid frontmatter must close with '---'.");
        return new FrontMatterResult(string.Join("\n", content), lines.Length);
    }

    private static HeaderLine? FindHeader(string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0) continue;
            if (IsDirective(trimmed) || IsComment(trimmed)) continue;
            return new HeaderLine(trimmed, index + 1, LeadingWhitespace(raw) + 1);
        }

        return null;
    }

    private static void AddDirectives(MermaidDocument document, string[] lines, int startLine, int endLine) {
        for (var line = startLine; line <= endLine && line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = raw.Trim();
            if (!IsDirective(trimmed)) continue;
            document.Directives.Add(new MermaidDirective(trimmed, new MermaidSourceSpan(line, LeadingWhitespace(raw) + 1, trimmed.Length)));
        }
    }

    private static void ReadRawBodyStatements(MermaidDocument document, string[] lines, int startLine) {
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || IsComment(trimmed)) continue;
            document.RawStatements.Add(new MermaidRawStatement(trimmed, new MermaidSourceSpan(line, LeadingWhitespace(raw) + 1, trimmed.Length)));
        }
    }

    private static DiagramDescriptor ResolveDiagramKind(string header) {
        var tokens = Tokenize(header);
        if (tokens.Count == 0) return new DiagramDescriptor(MermaidDiagramKind.Unknown, string.Empty, string.Empty);
        var first = Normalize(tokens[0]);
        var direction = tokens.Count > 1 ? tokens[1] : string.Empty;
        switch (first) {
            case "flowchart":
            case "graph":
                return new DiagramDescriptor(MermaidDiagramKind.Flowchart, tokens[0], direction);
            case "sequencediagram":
                return new DiagramDescriptor(MermaidDiagramKind.Sequence, tokens[0], string.Empty);
            case "classdiagram":
                return new DiagramDescriptor(MermaidDiagramKind.Class, tokens[0], string.Empty);
            case "statediagram":
            case "statediagramv2":
                return new DiagramDescriptor(MermaidDiagramKind.State, tokens[0], string.Empty);
            case "erdiagram":
                return new DiagramDescriptor(MermaidDiagramKind.EntityRelationship, tokens[0], string.Empty);
            case "gantt":
                return new DiagramDescriptor(MermaidDiagramKind.Gantt, tokens[0], string.Empty);
            case "pie":
                return new DiagramDescriptor(MermaidDiagramKind.Pie, tokens[0], string.Empty);
            case "journey":
                return new DiagramDescriptor(MermaidDiagramKind.Journey, tokens[0], string.Empty);
            case "gitgraph":
                return new DiagramDescriptor(MermaidDiagramKind.GitGraph, tokens[0], direction);
            case "mindmap":
                return new DiagramDescriptor(MermaidDiagramKind.MindMap, tokens[0], string.Empty);
            case "timeline":
                return new DiagramDescriptor(MermaidDiagramKind.Timeline, tokens[0], direction);
            case "requirementdiagram":
                return new DiagramDescriptor(MermaidDiagramKind.Requirement, tokens[0], string.Empty);
            case "quadrantchart":
                return new DiagramDescriptor(MermaidDiagramKind.Quadrant, tokens[0], string.Empty);
            case "sankey":
            case "sankeybeta":
                return new DiagramDescriptor(MermaidDiagramKind.Sankey, tokens[0], string.Empty);
            case "xychartbeta":
                return new DiagramDescriptor(MermaidDiagramKind.XYChart, tokens[0], direction);
            case "blockbeta":
                return new DiagramDescriptor(MermaidDiagramKind.Block, tokens[0], string.Empty);
            case "packetbeta":
                return new DiagramDescriptor(MermaidDiagramKind.Packet, tokens[0], string.Empty);
            case "architecturebeta":
                return new DiagramDescriptor(MermaidDiagramKind.Architecture, tokens[0], string.Empty);
            case "kanban":
                return new DiagramDescriptor(MermaidDiagramKind.Kanban, tokens[0], string.Empty);
            case "radarbeta":
                return new DiagramDescriptor(MermaidDiagramKind.Radar, tokens[0], string.Empty);
            case "treemapbeta":
                return new DiagramDescriptor(MermaidDiagramKind.Treemap, tokens[0], string.Empty);
            case "c4context":
            case "c4container":
            case "c4component":
            case "c4dynamic":
            case "c4deployment":
                return new DiagramDescriptor(MermaidDiagramKind.C4, tokens[0], string.Empty);
            case "venn":
            case "vennbeta":
                return new DiagramDescriptor(MermaidDiagramKind.Venn, tokens[0], string.Empty);
            case "ishikawa":
            case "ishikawabeta":
                return new DiagramDescriptor(MermaidDiagramKind.Ishikawa, tokens[0], string.Empty);
            case "wardleybeta":
                return new DiagramDescriptor(MermaidDiagramKind.Wardley, tokens[0], string.Empty);
            case "eventmodeling":
                return new DiagramDescriptor(MermaidDiagramKind.EventModeling, tokens[0], string.Empty);
            case "treeview":
            case "treeviewbeta":
                return new DiagramDescriptor(MermaidDiagramKind.TreeView, tokens[0], string.Empty);
            case "zenuml":
                return new DiagramDescriptor(MermaidDiagramKind.ZenUml, tokens[0], string.Empty);
            default:
                return new DiagramDescriptor(MermaidDiagramKind.Unknown, tokens[0], string.Empty);
        }
    }

    private static MermaidFlowchartDirection ParseFlowchartDirection(string value) {
        switch (Normalize(value)) {
            case "":
                return MermaidFlowchartDirection.None;
            case "tb":
                return MermaidFlowchartDirection.TopToBottom;
            case "td":
                return MermaidFlowchartDirection.TopDown;
            case "bt":
                return MermaidFlowchartDirection.BottomToTop;
            case "lr":
                return MermaidFlowchartDirection.LeftToRight;
            case "rl":
                return MermaidFlowchartDirection.RightToLeft;
            default:
                return MermaidFlowchartDirection.None;
        }
    }

    private static bool IsDirective(string text) => text.StartsWith("%%{", StringComparison.Ordinal) && text.EndsWith("}%%", StringComparison.Ordinal);

    private static bool IsComment(string text) => text.StartsWith("%%", StringComparison.Ordinal);

    private static List<string> Tokenize(string text) => text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

    private static string FirstToken(string text) => Tokenize(text).Count == 0 ? string.Empty : Tokenize(text)[0];

    private static string Normalize(string value) {
        var chars = new List<char>(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        }

        return new string(chars.ToArray());
    }

    private static int LeadingWhitespace(string text) {
        var count = 0;
        while (count < text.Length && char.IsWhiteSpace(text[count])) count++;
        return count;
    }

    private static void Add<TDocument>(MermaidParseResult<TDocument> result, int line, int column, int length, MermaidDiagnosticSeverity severity, string message) where TDocument : MermaidDocument {
        result.Diagnostics.Add(new MermaidDiagnostic {
            Span = new MermaidSourceSpan(line, column, length),
            Severity = severity,
            Message = message
        });
    }

    private readonly struct FrontMatterResult {
        public FrontMatterResult(string? text, int endLine) {
            Text = text;
            EndLine = endLine;
        }

        public string? Text { get; }

        public int EndLine { get; }
    }

    private readonly struct HeaderLine {
        public HeaderLine(string text, int line, int column) {
            Text = text;
            Line = line;
            Column = column;
        }

        public string Text { get; }

        public int Line { get; }

        public int Column { get; }
    }

    private readonly struct DiagramDescriptor {
        public DiagramDescriptor(MermaidDiagramKind kind, string headerKind, string direction) {
            Kind = kind;
            HeaderKind = headerKind;
            Direction = direction;
        }

        public MermaidDiagramKind Kind { get; }

        public string HeaderKind { get; }

        public string Direction { get; }
    }
}
