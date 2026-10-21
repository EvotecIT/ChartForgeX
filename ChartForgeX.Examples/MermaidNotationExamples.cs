/// <summary>Shows reusable diagram notes, synchronization nodes, and typed entity captions.</summary>
internal static class MermaidNotationExamples {
    internal const string Class = """
        classDiagram
        class Repository~Item~ {
          +List~Item~ items
          +find() Item
        }
        Repository "1" <|--|> Store : contracts
        note for Repository "One stable identity\nTyped generic members"
        """;

    internal const string State = """
        stateDiagram-v2
        state Fork <<fork>>
        state Join <<join>>
        state Choice <<choice>>
        [*] --> Fork
        Fork --> Ready
        Fork --> Waiting
        Ready --> Join
        Waiting --> Join
        Join --> Choice
        Choice --> Complete : approved
        note right of Ready : Two independent paths
        """;

    internal const string EntityRelationship = """
        erDiagram
        C["Customer account"] {
          string id PK, FK "External key"
          string display_name
        }
        O[Orders] {
          int id PK
        }
        C ||--o{ O : places
        """;
}
