# CLAUDE.md

sql-migration-lint: a .NET 10 console tool + library (`SqlMigrationLint`) that lints EF Core migration `.cs` files for dangerous operations (destructive SQL, lock-heavy schema changes, missing/empty `Down`) using text heuristics - no DB connection, no Roslyn.

## Build

```bash
dotnet build sql-migration-lint.csproj
dotnet run --project sql-migration-lint.csproj -- <path> [--fail-on Error] [--format text|json|github] [--config .sqlmigrationlint.json] [--ignore <ids>] [--only-latest N]
```

Single project at repo root (`sql-migration-lint.csproj`, no `.sln`). `Directory.Build.targets` excludes `tests/**/*.cs` from the root project - do not remove it.

## Test

```bash
dotnet test tests/SqlMigrationLint.Tests/SqlMigrationLint.Tests.csproj
dotnet test tests/SqlMigrationLint.Tests/SqlMigrationLint.Tests.csproj --filter "FullyQualifiedName~MissingWhereRuleTests"
```

xUnit 2.9, coverlet collector. Tests reference the root project directly.

## Lint / format

No analyzers or `.editorconfig`. `Nullable` and `ImplicitUsings` enabled, `LangVersion latest`, `GenerateDocumentationFile` on (public members should have XML docs). Use `dotnet format` if formatting is needed.

## Key directories and entry points

- `src/Program.cs` - CLI entry (`System.CommandLine` 2.0 beta4). Builds the rule set inline; it duplicates `MigrationLinter` scanning logic rather than calling it - keep both in sync.
- `src/MigrationLinter.cs` - library facade: `new MigrationLinter(rules)` / `CreateDefault()`, `Lint(path)` -> `LintReport`.
- `src/MigrationFile.cs` - regex + brace-depth parser extracting `Up`/`Down` bodies.
- `src/MigrationOperation.cs` - operation records (`SqlOperation` is the only one the CLI produces).
- `src/ILintRule.cs`, `IPerFileLintRule.cs`, `IGlobalLintRule.cs` - rule contracts (per operation / per parsed file / whole migration set).
- `src/DestructiveOperationRules.cs`, `LockHeavyOperationRules.cs`, `*Rule.cs` - rule implementations.
- `src/LintConfig.cs` - `.sqlmigrationlint.json` model (`rules: { "<id>": "off|warning|error" }`).
- `src/BaselineStore.cs` - baseline of accepted findings.
- `src/ConsoleReportWriter.cs`, `JsonReportWriter.cs`, `GitHubAnnotationsWriter.cs`, `IReportWriter.cs` - output formats.
- `src/JsonSerialization/` - `System.Text.Json` source-gen context (`LintJsonContext`) and `LintJson` helpers; `SqlMigrationLintJsonException` is the unified JSON error type.
- `docs/ARCHITECTURE.md` - data flow, design decisions, known limitations. Read it before touching parsing or rule registration.
- `tests/SqlMigrationLint.Tests/` - one test class per rule/extension.

## Conventions

- Namespace is flat `SqlMigrationLint` for everything (including `JsonSerialization/`). One public type per file, file name = type name.
- Rules are stateless singletons exposed via `public static readonly Instance`; grouped rule sets expose `All`.
- Rule ids: descriptive kebab-case for SQL/operation rules (`drop-table`, `add-column-with-default`), `ML1xx` codes for migration-structure rules (`ML100` empty Down, `ML101` missing Down, `ML102` naming / large update). Ids are the config keys and `--ignore` values.
- `LintFinding` is a positional record (`File, Line, Message, Severity, RuleName, ...`); `Line` is always 1 today.
- Three severity enums exist: `LintSeverity` (on findings), `RiskLevel` (ordered threshold), `LintFindingSeverity` (CLI `--fail-on`). Check which one a call site uses before mixing.
- Per-type companion files follow a fixed pattern: `<Type>Extensions.cs`, `<Type>JsonExtensions.cs` (`ToJson`/`FromJson`/`TryFromJson`), `<Type>Validation.cs`. Add new helpers to the matching file rather than a new grab-bag.
- Tests: xUnit `[Fact]`/`[Theory]`, class named `<Subject>Tests`, method names `Method_Scenario_Expected`.
- Top-level `*.md` files other than README/docs (`IMPLEMENTATION_SUMMARY.md`, etc.) are historical notes, not living docs.
