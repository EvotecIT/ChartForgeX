# Fluent migration

The Roslyn tool that moved ChartForgeX 5.0 onto generated fluent methods. It loads the solution with
`MSBuildWorkspace`, resolves every call and member reference to its symbol, and rewrites it from the maps in `maps/`.
It isn't part of any package; remove it once 5.0 has shipped and no branch still needs rewriting.

## Maps

Each line of a `.map` file is one entry; `#` starts a comment and `  # reason` after an entry is its reason.

| Entry | Effect |
| --- | --- |
| `move Type.Member -> Path [delete]` | References become `receiver.Path`. A one-segment path renames in place; `Group.Name` moves the declaration (and private fields only it uses) into the type of the existing property `Group`. `delete` deletes a forwarder and still rewrites its references. |
| `call Type.Method[(T1,T2)] -> template` | Call sites follow the template; the method is deleted. |
| `keep Type.Method[(T1,T2)] -> NewName  # reason` | The hand-written method stays under a new name. If a property already uses that name, the method becomes internal `SetNewName` and a public extension forwarder goes into `<Type>.Fluent.cs`. |
| `stay Type.Method  # reason` | Unchanged; used for `With*` methods on immutable values. |
| `verb Type.Property -> NameBy` | Names a delegate-valued property's method and adds `[Fluent("NameBy")]`. |
| `inline Type.Property` | The owner's fluent surface includes the property's methods (matches `[FluentInline]`). |

A template is a list of fluent steps separated by ` ; `. `{param}` is the argument text, or the parameter's default when
it was omitted; `{param:value}` unwraps a nullable argument; `[ ... ]` is dropped when a parameter inside it wasn't
passed. `Group.Sub.Method(args)` renders as nested configure callbacks, and consecutive steps on one group merge into one
callback. A step that starts with `!` is appended as written.

Plain setters (`{ A.B = value; return this; }`) and copy-then-commit `Configure*` methods need no entry: the planner
derives them from their bodies and the moves.

## Running

```bash
dotnet run --project tools/fluent-migration -c Release -- plan ChartForgeX.sln tools/fluent-migration/maps
dotnet run --project tools/fluent-migration -c Release -- apply ChartForgeX.sln tools/fluent-migration/maps <extra.csproj ...>
```

`plan` writes `out/` next to the maps: unmapped methods, derived templates, kept methods with their reasons, and setters
that are not public. `apply` rewrites the files in place. Set `FLUENT_DOCS` to a file listing Markdown paths to rewrite
their C# code blocks instead, and `FLUENT_PROPS` (`Name=Value;...`) for MSBuild properties such as the `ProductDll` the
benchmark fixtures compile against.
