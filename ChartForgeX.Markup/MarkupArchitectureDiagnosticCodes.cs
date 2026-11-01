namespace ChartForgeX.Markup;

/// <summary>
/// Diagnostic codes reported by <see cref="MarkupArchitectureParser"/>. Codes stay stable so editors, CLIs and agents
/// can match on them.
/// </summary>
public static class MarkupArchitectureDiagnosticCodes {
    /// <summary>A statement the architecture grammar does not know (warning).</summary>
    public const string UnknownCommand = "CFXA001";
    /// <summary>A statement with missing or misplaced parts, or unclosed quotes (error).</summary>
    public const string InvalidStatement = "CFXA002";
    /// <summary>A node, group, connection, or use case id declared twice (error).</summary>
    public const string DuplicateId = "CFXA003";
    /// <summary>A use case step or connection that refers to something that does not exist (error).</summary>
    public const string UnknownReference = "CFXA004";
    /// <summary>A connection to an undeclared node, added as a generic node (warning).</summary>
    public const string ImplicitNode = "CFXA005";
    /// <summary>A use case step between nodes that no connection joins; a dashed connection is added (warning).</summary>
    public const string ImplicitConnection = "CFXA006";
    /// <summary>A group or use case without its closing <c>end</c> (error).</summary>
    public const string UnclosedBlock = "CFXA007";
    /// <summary>An <c>end</c> with nothing open to close (error).</summary>
    public const string UnexpectedEnd = "CFXA008";
    /// <summary>A payload that cannot be read, such as JSON whose brackets do not balance (error).</summary>
    public const string InvalidPayload = "CFXA009";
    /// <summary>A response that answers no earlier request in the use case (warning).</summary>
    public const string UnansweredResponse = "CFXA010";
    /// <summary>An attribute the statement does not use (warning).</summary>
    public const string UnknownAttribute = "CFXA011";
    /// <summary>A diagram without nodes (error).</summary>
    public const string EmptyDiagram = "CFXA012";
    /// <summary>A use case without steps (warning).</summary>
    public const string EmptyUseCase = "CFXA013";
    /// <summary>An icon id the built-in catalog does not know (warning).</summary>
    public const string UnknownIcon = "CFXA014";
    /// <summary>An attribute or option value that cannot be read, such as an unknown status (error).</summary>
    public const string InvalidValue = "CFXA015";
}
