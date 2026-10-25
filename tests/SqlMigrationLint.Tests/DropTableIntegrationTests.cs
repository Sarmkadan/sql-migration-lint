using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SqlMigrationLint;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// End-to-end tests for <see cref="DropTableRule"/> (rule id <c>drop-table</c>): realistic EF Core migration files are
/// written to disk and pushed through <see cref="MigrationLinter.Lint(string)"/> (file discovery -> parsing -> rules -> report).
/// </summary>
public sealed class DropTableIntegrationTests : IDisposable
{
    private const string RuleId = "drop-table";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "sqlmigrationlint-it-" + Guid.NewGuid().ToString("N"));

    public DropTableIntegrationTests()
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

    [Fact]
    public void Lint_DropTableInUpSql_ReportsBlockerForMigrationFile()
    {
        var path = WriteMigration("20261024110000_DropLegacyAudit", new[]
        {
            "DROP TABLE \"LegacyAudit\";"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        var finding = Assert.Single(report.Findings, f => f.RuleName == RuleId);
        Assert.Equal(LintSeverity.Blocker, finding.Severity);
        Assert.Equal(path, finding.File);
        // Per-file rules evaluate the whole Up body, so the finding is anchored to line 1 rather than the statement.
        Assert.Equal(1, finding.Line);
        Assert.StartsWith("DROP TABLE statement permanently removes tables and their data.", finding.Message);
        Assert.True(report.HasBlockers);
        Assert.Equal(1, report.MigrationsScanned);
    }

    [Fact]
    public void Lint_DropTableIfExistsOnStagingTable_StillReportsFinding()
    {
        // The rule has no exemption for IF EXISTS or for temp/staging names, so these are reported like any other drop.
        var path = WriteMigration("20261024110000_DropImportStaging", new[]
        {
            "DROP TABLE IF EXISTS \"Orders_staging\";",
            "DROP TABLE IF EXISTS temp_import;"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        var finding = Assert.Single(report.Findings, f => f.RuleName == RuleId);
        Assert.Equal(LintSeverity.Blocker, finding.Severity);
        Assert.Equal(path, finding.File);
    }

    [Theory]
    [InlineData("-- DROP TABLE \"Orders\";")]
    [InlineData("/* DROP TABLE \"Orders\"; */")]
    public void Lint_DropTableInsideSqlComment_ReportsNoFinding(string statement)
    {
        WriteMigration("20261024110000_NoteAboutDrop", new[]
        {
            statement,
            "CREATE TABLE \"Notes\" (\"Id\" integer PRIMARY KEY, \"Body\" text NOT NULL);"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.DoesNotContain(report.Findings, f => f.RuleName == RuleId);
    }

    // Known gap: string literals are not blanked, so DROP TABLE text inside '...' still matches. Only comments are stripped.
    [Theory(Skip = "Known gap: drop-table does not exclude string-literal contents (only comments are stripped).")]
    [InlineData("INSERT INTO \"Notes\" (\"Body\") VALUES ('DROP TABLE \"Orders\";');")]
    public void Lint_DropTableOnlyInsideCommentOrStringLiteral_ReportsNoFinding(string statement)
    {
        WriteMigration("20261024110000_NoteAboutDrop", new[]
        {
            statement,
            "CREATE TABLE \"Notes\" (\"Id\" integer PRIMARY KEY, \"Body\" text NOT NULL);"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.DoesNotContain(report.Findings, f => f.RuleName == RuleId);
    }

    [Fact]
    public void Lint_SuppressionCommentInMigration_DoesNotSuppressFinding()
    {
        // The project has no inline suppression syntax: a comment naming the rule does not silence it.
        // Suppression is only available through .sqlmigrationlint.json ("off") or the CLI --ignore option.
        WriteMigration("20261024110000_DropWithComment", new[]
        {
            "-- sqlmigrationlint:disable drop-table",
            "DROP TABLE \"LegacyAudit\";"
        });

        var report = MigrationLinter.CreateDefault().Lint(_root);

        Assert.Single(report.Findings, f => f.RuleName == RuleId);
    }

    [Fact]
    public void Lint_DropTableWithRuleSetToOffInConfig_ReportsNoFinding()
    {
        WriteMigration("20261024110000_DropLegacyAudit", new[]
        {
            "DROP TABLE \"LegacyAudit\";"
        });
        var configPath = Path.Combine(_root, ".sqlmigrationlint.json");
        File.WriteAllText(configPath, "{ \"rules\": { \"drop-table\": \"off\" } }");

        var report = MigrationLinter.CreateDefault(configPath).Lint(_root);

        Assert.DoesNotContain(report.Findings, f => f.RuleName == RuleId);
    }

    [Fact]
    public void Lint_DropTableWithRuleDowngradedToWarningInConfig_ReportsWarningSeverity()
    {
        WriteMigration("20261024110000_DropLegacyAudit", new[]
        {
            "DROP TABLE \"LegacyAudit\";"
        });
        var configPath = Path.Combine(_root, ".sqlmigrationlint.json");
        File.WriteAllText(configPath, "{ \"rules\": { \"drop-table\": \"warning\" } }");

        var report = MigrationLinter.CreateDefault(configPath).Lint(_root);

        var finding = Assert.Single(report.Findings, f => f.RuleName == RuleId);
        Assert.Equal(LintSeverity.Warning, finding.Severity);
    }

    /// <summary>
    /// Writes a realistic EF Core migration whose <c>Up</c> runs the given SQL statements through
    /// <c>migrationBuilder.Sql</c>, and whose <c>Down</c> recreates the table so unrelated rules stay quiet.
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
        sb.AppendLine($"    [DbContext(typeof(OrdersDbContext))]");
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
