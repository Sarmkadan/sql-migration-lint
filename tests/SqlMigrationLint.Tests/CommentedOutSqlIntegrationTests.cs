using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SqlMigrationLint;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// End-to-end tests showing that SQL statements which are only commented out are not reported by the rules
/// run through <see cref="MigrationLinter.Lint(string)"/>, while live statements still are.
/// </summary>
public sealed class CommentedOutSqlIntegrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sqlmigrationlint-comments-" + Guid.NewGuid().ToString("N"));

    public CommentedOutSqlIntegrationTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Migrations"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Theory]
    [InlineData("-- DROP TABLE \"Orders\";")]
    [InlineData("/* DROP TABLE \"Orders\"; */")]
    public void Lint_CommentedOutDropTable_ReportsNoDropTableFinding(string statement)
    {
        WriteMigration("20261024110000_CommentedDropTable", new[] { statement, "SELECT 1;" });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.DoesNotContain(report.Findings, f => f.RuleName == "drop-table");
    }

    [Theory]
    [InlineData("-- DROP COLUMN \"Total\" ON TABLE \"Orders\";")]
    [InlineData("/* ALTER TABLE \"Orders\" DROP COLUMN \"Total\"; */")]
    public void Lint_CommentedOutDropColumn_ReportsNoDropColumnFinding(string statement)
    {
        WriteMigration("20261024110000_CommentedDropColumn", new[] { statement, "SELECT 1;" });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.DoesNotContain(report.Findings, f => f.RuleName == "drop-column");
    }

    [Fact]
    public void Lint_CommentedOutDeleteWithoutWhere_ReportsNoMissingWhereFinding()
    {
        WriteMigration("20261024110000_CommentedDelete", new[]
        {
            "-- DELETE FROM \"Orders\";",
            "/* UPDATE \"Orders\" SET \"Status\" = 'x'; */",
            "SELECT 1;"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.DoesNotContain(report.Findings, f => f.RuleName == "ML101");
        Assert.DoesNotContain(report.Findings, f => f.RuleName == "delete-data");
    }

    [Fact]
    public void Lint_CommentedOutAddColumnWithDefault_ReportsNoAddColumnFinding()
    {
        WriteMigration("20261024110000_CommentedAddColumn", new[]
        {
            "-- ALTER TABLE \"Orders\" ADD COLUMN \"Priority\" integer NOT NULL DEFAULT 0;",
            "SELECT 1;"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.DoesNotContain(report.Findings, f => f.RuleName == "add-column-with-default");
    }

    [Fact]
    public void Lint_LiveDropTableNextToCommentedOutDelete_ReportsOnlyTheLiveDrop()
    {
        WriteMigration("20261024110000_MixedLiveAndCommented", new[]
        {
            "-- DELETE FROM \"Orders\";",
            "DROP TABLE \"LegacyAudit\";"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.Single(report.Findings, f => f.RuleName == "drop-table");
        Assert.DoesNotContain(report.Findings, f => f.RuleName == "delete-data");
    }

    [Fact]
    public void Lint_CommentMarkerInsideStringLiteral_StillReportsLiveDrop()
    {
        WriteMigration("20261024110000_MarkerInLiteral", new[]
        {
            "INSERT INTO \"Notes\" (\"Body\") VALUES ('-- keep');",
            "DROP TABLE \"LegacyAudit\";"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.Single(report.Findings, f => f.RuleName == "drop-table");
    }

    /// <summary>
    /// Writes a migration whose <c>Up</c> passes the given SQL lines through <c>migrationBuilder.Sql</c> and whose
    /// <c>Down</c> recreates the table so that unrelated rules stay quiet.
    /// </summary>
    private string WriteMigration(string fileName, IEnumerable<string> upStatements)
    {
        var className = fileName.Substring(fileName.IndexOf('_') + 1);
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;");
        sb.AppendLine();
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("namespace Sarmkadan.Orders.Migrations");
        sb.AppendLine("{");
        sb.AppendLine($"    [Migration(\"{fileName}\")]");
        sb.AppendLine($"    public partial class {className} : Migration");
        sb.AppendLine("    {");
        sb.AppendLine("        protected override void Up(MigrationBuilder migrationBuilder)");
        sb.AppendLine("        {");
        sb.AppendLine("            migrationBuilder.Sql(@\"");
        foreach (var statement in upStatements)
        {
            sb.AppendLine("                " + statement);
        }
        sb.AppendLine("            \");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        protected override void Down(MigrationBuilder migrationBuilder)");
        sb.AppendLine("        {");
        sb.AppendLine("            migrationBuilder.Sql(\"CREATE TABLE \\\"LegacyAudit\\\" (\\\"Id\\\" integer PRIMARY KEY);\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var path = Path.Combine(_root, "Migrations", fileName + ".cs");
        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
