# Architecture

## Overview

sql-migration-lint is a single-project .NET 10 console tool (`SqlMigrationLint`, entry point `src/Program.cs`) that scans EF Core migration `.cs` files for dangerous operations - destructive SQL, lock-heavy schema changes, irreversible migrations - and reports findings to the console, as JSON, or as GitHub Actions annotations. It exits non-zero when findings reach a configurable severity threshold (`--fail-on`), making it usable as a CI gate.

There is no database connection and no Roslyn parsing: the tool works purely on the text of migration files. `Microsoft.EntityFrameworkCore.Relational` is referenced but the linter defines its own lightweight operation model (see below); the CLI is built on `System.CommandLine` (2.0 beta4).

## Data flow

```
CLI args (System.CommandLine)
        |
        v
Program.Main
  - resolves <path>/Migrations
  - enumerates *.cs, skipping *.Designer.cs and *Snapshot*
  - optional --only-latest N (by file-name order)
        |
        v
MigrationFile.TryParse(file)          <- text heuristics, no compiler
  - IsMigration: looks for "partial class X : Migration"
  - extracts Up/Down method bodies via regex signature match
    + brace-depth tracking
        |
        v
SqlOperation { File, Line = 1, Sql = UpBody }
        |
        v
for each ILintRule: AppliesTo() -> Evaluate() -> LintFinding?
        |
        v
findings -> max RiskLevel -> console/JSON output -> exit code
```

Each migration file is wrapped in exactly one `SqlOperation` whose `Sql` is the whole `Up` body; `Line` is always `1` (per-statement line numbers are not computed - see limitations).

## Components

### CLI - `src/Program.cs`

Defines the root command: positional `path` (defaults to `.`), `--fail-on <None|Warning|Danger|Blocker>` (default `Danger`), `--json`, `--ignore <rule-ids>`, `--only-latest <n>`. Builds the rule set inline (`DestructiveOperationRules.All` + `LockHeavyOperationRules.All` + `EmptyDownRule.Instance`), lints, prints color-coded findings or JSON, and calls `Environment.Exit(1)` when `maxRisk >= failOn`.

### Library facade - `src/MigrationLinter.cs`

`MigrationLinter` is the programmatic equivalent of the CLI loop: constructed with any `IEnumerable<ILintRule>` (or `CreateDefault()` for the built-ins), `Lint(rootPath)` returns a `LintReport` (findings, migrations scanned, `HasBlockers`, `MaxRisk`). Note: `Program.cs` does **not** use `MigrationLinter`; the file-enumeration and rule loop are duplicated in both places, so changes to scanning behavior must be made twice. The `RiskLevel` enum (None < Warning < Danger < Blocker) lives in this file and drives the exit-code decision.

### Parsing - `src/MigrationFile.cs`

`MigrationFile.TryParse` reads all lines and:

1. Confirms the file is a migration (`partial class <Name>` on a line that also mentions `Migration`), capturing the class name as `MigrationName`.
2. Locates `Up(MigrationBuilder ...)` / `Down(MigrationBuilder ...)` signatures by regex (any parameter name - EF scaffolds `migrationBuilder`).
3. Finds each method's end via brace-depth counting (`FindMethodEnd`), so nested blocks such as `table => new { ... }` lambdas inside `CreateTable` don't truncate the body.
4. Extracts the body text (`ExtractBody`), excluding a standalone `{` line so an empty method yields an empty body.

Trade-off: this is deliberately a text heuristic, not a syntax tree. It is fast and dependency-free but can be fooled by braces inside string literals or unconventional formatting. That is accepted because rules are themselves regex heuristics over the body text.

Supporting helpers (not used by the CLI path, available to library consumers):

- `MigrationFileExtensions` - line/statement/comment/keyword counting over Up/Down bodies.
- `MigrationFileJsonExtensions`, `MigrationOperationJsonExtensions`, `DestructiveOperationRulesJsonExtensions` - System.Text.Json round-trip helpers.
- `MigrationFileValidation`, `DestructiveOperationRulesValidation` - human-readable validation of instances.

### Operation model - `src/MigrationOperation.cs`

`MigrationOperation` is an abstract record carrying `File`/`Line`. Concrete records:

- `SqlOperation` (raw SQL text) - the only type the CLI currently produces.
- `AddColumnOperation`, `AlterColumnOperation`, `CreateIndexOperation`, `AddForeignKeyOperation` (+ `ForeignKeyColumn`) - structured operations with the properties the lock-heavy rules need. Nothing in the repo constructs these from migration files yet; they exist for callers that already have structured operation data (e.g. from EF's own model diff).

### Rules

All rules implement `ILintRule` (`src/ILintRule.cs`): `Name`, `Description`, `Severity`, `AppliesTo(MigrationOperation)`, `Evaluate(MigrationOperation) -> LintFinding?`. `AppliesTo` is a cheap type/shape filter; `Evaluate` does the actual matching. Findings are `LintFinding` records with `LintSeverity` (Blocker/Danger/Warning).

**Registered by default** (CLI and `MigrationLinter.CreateDefault()`):

| Group | Rules | Operates on |
|---|---|---|
| `DestructiveOperationRules` | `drop-table`, `drop-column`, `drop-index`, `rename-column`, `rename-table`, `delete-data`, `drop-database-object` | `SqlOperation` (regex on SQL text) |
| `LockHeavyOperationRules` | `add-column-with-default`, `alter-column-type-change`, `data-loss-alter-column`, `create-index-without-concurrent`, `add-foreign-key-without-index`, `nullable-false-without-default`, `add-not-null-without-default` | structured operations only - never fire from the CLI today, since the CLI produces only `SqlOperation` |
| `EmptyDownRule` | `ML100` - Up non-empty but Down is empty / only `throw` / only comments | `SqlOperation` (re-parses the file to inspect `DownBody`) |

**Implemented but not registered anywhere:** `MissingDownMigrationRule` (`ML101` - matches `CreateTable`/`CreateIndex`/`AddColumn` calls in Up against corresponding Drops in Down) and `NamingConventionRule` (`ML102` - `IX_`/`FK_`/`PK_`/`UQ_` prefixes, configurable via constructor). To use them, pass them to `new MigrationLinter([...])`; they are not in `CreateDefault()` or the CLI rule list.

### Output - `src/GitHubAnnotationsWriter.cs`

Formats findings as GitHub Actions workflow commands (`::error file=...,line=...::message`); Blocker and Danger both map to `error`, Warning to `warning`. Not wired into the CLI - it is a library helper for CI integrations (`WriteAll`/`WriteToConsole`).

## Key design decisions

- **Text heuristics over Roslyn/EF model**: no compilation of the target project is needed, so the tool runs on any checkout in seconds. The cost is false negatives/positives on unusual formatting; severity levels and `--ignore` mitigate this.
- **One `SqlOperation` per migration**: rules see the whole Up body at once. Simple, but a migration triggers each rule at most once and `Line` is always 1.
- **Singleton stateless rules** (`Instance` fields): rules hold no per-run state, so a single instance is safe across files and runs.
- **Severity split into two enums**: `LintSeverity` (on findings) and `RiskLevel` (ordered, for threshold comparison). The mapping lives in `Program.cs`/`MigrationLinter`. A third enum, `LintFindingSeverity` (`src/LintFindingSeverity.cs`), is currently unused by any code path.

## Extension points

- **Custom rules**: implement `ILintRule` and construct `new MigrationLinter(rules)`. The CLI has no plugin mechanism; adding a rule to the CLI means editing the rule list in `Program.cs` (and `CreateDefault()` to keep them in sync).
- **Structured operations**: build `AddColumnOperation`/`AlterColumnOperation`/etc. yourself to activate the lock-heavy rules with real metadata (`TableExists`, `IsNullable`, `Options`, ...).
- **Output formats**: `--json` from the CLI, or `GitHubAnnotationsWriter` from library code.

## Known limitations

- `Line` is always 1; findings point at the file, not the offending statement.
- The lock-heavy rule group is dead weight in the CLI path (needs structured operations the CLI never creates).
- `ML101` and `ML102` exist but are not registered by default.
- Overlapping regexes produce duplicate findings: a `DROP TABLE x;` fires both `drop-table` and `drop-database-object`; `ALTER TABLE ... RENAME COLUMN` fires both `rename-column` and `rename-table`.
- Destructive-SQL regexes require a terminating `;` - statements without one are missed. Names in brackets/quotes (`[IX_Foo]`) are not matched by `ML102`.
- Brace counting in `MigrationFile` does not understand string literals; a `}` inside a SQL string could end a body early.
- `EmptyDownRule` and `MissingDownMigrationRule` re-read and re-parse the file inside `Evaluate` (the parsed `MigrationFile` is not passed through the operation), costing an extra file read per rule per migration.
- `Program.cs` duplicates `MigrationLinter`'s scanning logic instead of calling it.
