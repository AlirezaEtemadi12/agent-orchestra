# API naming

> **AUTHORITATIVE** for names in `api/`. Trimmed from Project B's `Web/.ai/coding/naming.md`.

| Thing | Shape | Example |
|---|---|---|
| Module folder | `Modules/<Module>/` | `Modules/Import/` |
| Feature | `Modules/<Module>/Features/<Action>/` | `Features/ListRuns/` |
| Query | `<Action>Query.cs` | `ListRunsQuery.cs` |
| Handler | `<Action>Handler.cs` | `ListRunsHandler.cs` |
| Entity | `<Name>Entity.cs` | `RunEntity.cs` |
| EF configuration | `Data/Configurations/<Name>Configuration.cs` | `RunConfiguration.cs` |
| DTO | `<Name>Dto.cs`, beside the feature | `RunListItemDto.cs` |
| Installer | `<Module>ModuleInstaller.cs` | `ImportModuleInstaller.cs` |
| Dependency field | `_` + the type name | `_importService` |
| Error code | `<Module>.<Thing>.<Reason>` | `Runs.Run.NotFound` |
| Test | states the guarantee, no `Should` | `Importing_twice_keeps_the_row_counts` |

Classes are `sealed` unless something inherits them. Interfaces exist only where there is a second
implementation or a test double needs one — never one interface per class.
