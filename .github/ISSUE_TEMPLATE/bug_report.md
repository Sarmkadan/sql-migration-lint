---
name: Bug report
about: Report a crash, a wrong finding, or unexpected CLI behavior
labels: bug
---

## Description

<!-- A short description of the problem. -->

## Steps to reproduce

1. 
2. 
3. 

**Command run:**

```bash
sql-migration-lint <path> [options]
```

## Example SQL migration

<!-- A minimal EF Core migration (.cs) that triggers the problem. Remove anything not needed to reproduce it. -->

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class Example : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
```

## Expected behavior

<!-- What you expected the linter to report (or not report), and why. -->

## Actual behavior

<!-- What happened instead. Paste the full CLI output, including any stack trace, in a code block. -->

```text

```

## Environment

- Tool version (`<Version>` in `sql-migration-lint.csproj`, or commit SHA if built from source):
- .NET SDK version (`dotnet --version`):
- OS:
