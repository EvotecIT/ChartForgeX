namespace ChartForgeX.Topology;

/// <summary>
/// Describes what happens when a scenario step travels over an edge.
/// </summary>
public enum TopologyScenarioInteraction {
    /// <summary>The step marks a route segment without request semantics.</summary>
    None,
    /// <summary>A synchronous request: the sender waits for the answer.</summary>
    Request,
    /// <summary>An asynchronous message that the sender does not wait for.</summary>
    Message,
    /// <summary>The answer to an earlier request.</summary>
    Response,
    /// <summary>A call that fails and never receives an answer, such as a timeout or a refused connection.</summary>
    Failure
}

/// <summary>
/// Selects which payloads a scenario text transcript includes.
/// </summary>
public enum TopologyScenarioTextPayloads {
    /// <summary>Every step's payload.</summary>
    All,
    /// <summary>Only the active step's payload.</summary>
    Active,
    /// <summary>No payloads.</summary>
    None
}

/// <summary>
/// Describes how a scenario step payload should be presented.
/// </summary>
public enum TopologyScenarioPayloadFormat {
    /// <summary>Plain text.</summary>
    Text,
    /// <summary>A JSON document.</summary>
    Json,
    /// <summary>An XML document.</summary>
    Xml
}
