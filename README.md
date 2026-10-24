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

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the component breakdown, data flow, rule catalog, extension points, and known limitations.
