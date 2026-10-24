# sql-migration-lint

Lints EF Core migrations for dangerous operations before they hit production. It uses text heuristics only: no database connection and no Roslyn.

## Getting Started

Requires the .NET 10 SDK (the project targets `net10.0`).

### 1. Build

From the repository root:

```bash
dotnet build sql-migration-lint.csproj
```

### 2. Create a migration that builds an index without CONCURRENTLY

The linter reads the `Up` body of each EF Core migration file under a `Migrations/` folder. This sample creates an index on PostgreSQL without `CONCURRENTLY`, which locks the table while the index builds:

```bash
mkdir -p /tmp/sml-sample/Migrations
cat > /tmp/sml-sample/Migrations/20260101000000_AddOrderStatusIndex.cs <<'EOF'
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Migrations
{
    [DbContext(typeof(ShopDbContext))]
    [Migration("20260101000000_AddOrderStatusIndex")]
    public partial class AddOrderStatusIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_Orders_Status"" ON ""Orders"" (""Status"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX ""IX_Orders_Status"";");
        }
    }
}
EOF
```

### 3. Lint it

Pass the folder that contains `Migrations/`:

```bash
dotnet run --project sql-migration-lint.csproj -- /tmp/sml-sample
```

Output:

```text
[Warning] non-concurrent-index: Statement 'CREATE INDEX' lacks CONCURRENTLY/ONLINE option and will lock the table. Consider adding 'CONCURRENTLY' for PostgreSQL operations. (/tmp/sml-sample/Migrations/20260101000000_AddOrderStatusIndex.cs:1)
[Warning] non-concurrent-index: Statement 'CREATE INDEX' lacks CONCURRENTLY/ONLINE option and will lock the table. Consider adding 'CONCURRENTLY' for PostgreSQL operations. (/tmp/sml-sample/Migrations/20260101000000_AddOrderStatusIndex.cs:1)
```

- `non-concurrent-index` is the rule id. Use it with `--ignore`, or as a key in `.sqlmigrationlint.json`.
- The `:1` is the line number. SQL-level findings always report line 1 today.
- The finding appears twice because the rule is currently registered twice. This is a known bug.
- The exit code is `0`, because the default `--fail-on Error` only fails on errors. Add `--fail-on Warning` to fail on this finding (exit code `1`).

### 4. Fix it

PostgreSQL's `CREATE INDEX CONCURRENTLY` builds the index without blocking writes. It cannot run inside a transaction block, and EF Core wraps each migration in one, so the fixed `Sql` calls pass `suppressTransaction: true`:

```bash
cat > /tmp/sml-sample/Migrations/20260101000000_AddOrderStatusIndex.cs <<'EOF'
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Migrations
{
    [DbContext(typeof(ShopDbContext))]
    [Migration("20260101000000_AddOrderStatusIndex")]
    public partial class AddOrderStatusIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY ""IX_Orders_Status"" ON ""Orders"" (""Status"");
            ", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX CONCURRENTLY IF EXISTS ""IX_Orders_Status"";", suppressTransaction: true);
        }
    }
}
EOF
```

### 5. Lint it again

```bash
dotnet run --project sql-migration-lint.csproj -- /tmp/sml-sample --ignore ML102
```

Output:

```text
No issues found. All migrations passed linting.
```

The `--ignore ML102` flag is needed because the naming check reports a false positive on `CONCURRENTLY`. It reads the word after `CREATE INDEX` as the index name, so it flags the fixed migration with:

```text
[Warning] ML102: Index 'CONCURRENTLY' does not start with prefix 'IX_'. (/tmp/sml-sample/Migrations/20260101000000_AddOrderStatusIndex.cs:1)
```

To disable the check for the whole project, put this in `.sqlmigrationlint.json` in the directory you run the linter from, and pass it with `--config .sqlmigrationlint.json`:

```json
{
  "rules": {
    "ML102": "off"
  }
}
```

The configuration is checked when it loads. An unknown rule id, or a severity other than `off`, `warning` or `error`, stops the run with exit code `1`. The message lists the valid rule ids and severity values.

Some rules accept options, set under an `options` object keyed by rule id:

```json
{
  "options": {
    "add-column-with-default": {
      "exemptTablePatterns": ["^__EFMigrationsHistory$", "^Audit_"]
    }
  }
}
```

- `exemptTablePatterns` (`add-column-with-default`): regular expressions matched against the table name as written in the migration. Matching is case-insensitive and unanchored, so `Audit` also matches `OrderAuditTrail`; use `^Audit$` to match one table exactly. A table name that includes a schema, such as `dbo.Users`, is matched with the schema included.

Option values are checked with the rule ids and severities, and every problem is reported in one message: an unknown option name, a non-array value, or a pattern that is empty or is not a valid regular expression. The message names the bad pattern and the regex error.

## Command-line options

```text
dotnet run --project sql-migration-lint.csproj -- <path> [options]
```

| Option | Description |
| --- | --- |
| `--fail-on <Info\|Warning\|Error>` | Lowest severity that makes the exit code non-zero (default: `Error`). |
| `--format <text\|json\|github>` | Output format (default: `text`). `--json` is a shortcut for `--format json`. |
| `--ignore <ids...>` | Rule ids to skip. |
| `--only-latest <n>` | Lint only the last `n` migrations. |
| `--config <path>` | Path to a `.sqlmigrationlint.json` file. |

## Baselines

When you adopt the linter on a codebase that already has findings, record them in a baseline once. After that, fail the build only on findings that are not in the baseline.

The baseline is only available through the library API (`BaselineStore`). The `sql-migration-lint` CLI does not read or write baseline files. The examples below use a small console project in your repository that references the linter.

### Setup

Create `tools/BaselineCheck/BaselineCheck.csproj`, and point the `ProjectReference` at your copy of the linter:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="path/to/sql-migration-lint/sql-migration-lint.csproj" />
  </ItemGroup>
</Project>
```

Create `tools/BaselineCheck/Program.cs`:

```csharp
using SqlMigrationLint;

// usage: baseline-check <write|check> <rootPath> <baselinePath>
var mode = args[0];
var root = args[1];
var baselinePath = args[2];

var report = MigrationLinter.CreateDefault().Lint(root);

if (mode == "write")
{
    BaselineStore.WriteBaseline(baselinePath, report, indented: true);
    Console.WriteLine($"Wrote {report.Findings.Count} fingerprint(s) to {baselinePath}");
    return 0;
}

var fresh = BaselineStore.FilterFindings(baselinePath, report).ToArray();
foreach (var f in fresh)
{
    Console.WriteLine($"[{f.Severity}] {f.RuleName}: {f.Message} ({f.File}:{f.Line})");
}

Console.WriteLine(fresh.Length == 0 ? "No new findings." : $"{fresh.Length} new finding(s).");
return fresh.Length == 0 ? 0 : 1;
```

`<rootPath>` is the directory that contains the `Migrations/` folder, the same value you pass to `MigrationLinter.Lint`. In the examples it is `src/Shop`.

### Where the baseline is stored

The baseline is a JSON array of fingerprints, one per finding, in a file you choose. This repository's tests use `.sqlmigrationlint-baseline.json` at the root. Commit it to the repository. Each fingerprint is a SHA-256 hash of the rule name, file path and line number, so the file contains no SQL or file names:

```json
[
  "vvnkoFte530FPASTrD8SUeTObuDIrlmzlf6OKkzrfos="
]
```

### Generate a baseline

From the repository root:

```bash
dotnet run --project tools/BaselineCheck -- write src/Shop .sqlmigrationlint-baseline.json
```

Output:

```text
Wrote 1 fingerprint(s) to .sqlmigrationlint-baseline.json
```

The same thing through the API:

```csharp
var report = MigrationLinter.CreateDefault().Lint("src/Shop");
BaselineStore.WriteBaseline(".sqlmigrationlint-baseline.json", report, indented: true);
```

`WriteBaseline` records every finding in the report, including new ones. Only generate a baseline on purpose, in a reviewed commit.

### Lint against the baseline

```bash
dotnet run --project tools/BaselineCheck -- check src/Shop .sqlmigrationlint-baseline.json
```

With no new findings, the output is `No new findings.` and the exit code is `0`. With new findings, each one is printed, followed by a count, and the exit code is `1`:

```text
[Blocker] drop-table: DROP TABLE statement permanently removes tables and their data. Consider backing up or using a soft delete approach. (src/Shop/Migrations/20261024140000_Drop.cs:1)
[Blocker] drop-database-object: DROP statement permanently removes database objects and their data. Consider backing up or using a soft delete approach. (src/Shop/Migrations/20261024140000_Drop.cs:1)
2 new finding(s).
```

The same thing through the API:

```csharp
var report = MigrationLinter.CreateDefault().Lint("src/Shop");
var fresh = BaselineStore.FilterFindings(".sqlmigrationlint-baseline.json", report).ToArray();
```

`FilterFindings` throws `BaselineStoreException` if the baseline file is missing, unreadable, or malformed. Commit the baseline before you enable the CI step.

### Update the baseline after fixing findings

1. Fix the findings, or remove the migrations that contain them.
2. Run `check`. Fixed findings are no longer reported, so the check passes. Their fingerprints stay in the file until you regenerate it.
3. To drop the fixed fingerprints, run `write` again and commit the new baseline with the fix.

Fingerprints include the line number. If you edit lines above a baselined finding in the same file, the finding is reported as new. Regenerate the baseline in the same commit as the edit.

Fingerprints also include the file path, as spelled by the root you pass in. Write and check must use the same spelling. A baseline written with `.` does not match a check run with an absolute path. Use the same relative root, such as `src/Shop`, everywhere.

### CI: fail on new findings only

This step runs from the repository root with the same root and baseline paths that were used to generate the baseline:

```yaml
name: migration-lint

on: [pull_request]

jobs:
  lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Fail on new migration findings
        run: dotnet run --project tools/BaselineCheck -- check src/Shop .sqlmigrationlint-baseline.json
```

The step fails on any new finding, whatever its severity. To fail only on some severities, filter `fresh` by `Severity` in `Program.cs` before printing.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the component breakdown, data flow, rule catalog, extension points, and known limitations.
