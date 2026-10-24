using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SqlMigrationLint;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// End-to-end tests for <see cref="BaselineStore"/>: real migration files and baseline files are written to a
/// temporary directory, linted through <see cref="MigrationLinter.Lint(string)"/>, and filtered against the
/// baseline on disk. No mocks: every step goes through the file system.
/// </summary>
public sealed class BaselineStoreIntegrationTests : IDisposable
{
    private const string MigrationName = "20261024120000_OrdersChanges";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "sqlmigrationlint-baseline-it-" + Guid.NewGuid().ToString("N"));
    private readonly string _baselinePath;

    public BaselineStoreIntegrationTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Migrations"));
        _baselinePath = Path.Combine(_root, ".sqlmigrationlint-baseline.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Baseline_SavedFromKnownFindings_SuppressesAllFindingsOnRerun()
    {
        WriteMigration(new[]
        {
            "DROP TABLE \"LegacyAudit\";",
            "ALTER TABLE \"Orders\" ADD COLUMN \"Status\" integer NOT NULL DEFAULT 0;"
        });

        var initial = MigrationLinter.CreateDefault().Lint(_root);
        Assert.Contains(initial.Findings, f => f.RuleName == "drop-table");
        Assert.Contains(initial.Findings, f => f.RuleName == "add-column-with-default");

        BaselineStore.WriteBaseline(_baselinePath, initial, indented: true);

        Assert.True(File.Exists(_baselinePath));
        Assert.Equal(initial.Findings.Count, BaselineStore.LoadBaseline(_baselinePath).Count);

        var rerun = MigrationLinter.CreateDefault().Lint(_root);
        var remaining = BaselineStore.FilterFindings(_baselinePath, rerun).ToArray();

        Assert.Empty(remaining);
    }

    [Fact]
    public void Baseline_AfterAddingNewViolation_ReportsOnlyTheNewFinding()
    {
        WriteMigration(new[]
        {
            "DROP TABLE \"LegacyAudit\";",
            "ALTER TABLE \"Orders\" ADD COLUMN \"Status\" integer NOT NULL DEFAULT 0;"
        });
        BaselineStore.WriteBaseline(_baselinePath, MigrationLinter.CreateDefault().Lint(_root));

        // Append a new violation below the existing statements so the earlier findings keep their line numbers.
        var migrationPath = WriteMigration(new[]
        {
            "DROP TABLE \"LegacyAudit\";",
            "ALTER TABLE \"Orders\" ADD COLUMN \"Status\" integer NOT NULL DEFAULT 0;",
            "CREATE INDEX \"IX_Orders_Status\" ON \"Orders\" (\"Status\");"
        });

        var rerun = MigrationLinter.CreateDefault().Lint(_root);
        var remaining = BaselineStore.FilterFindings(_baselinePath, rerun).ToArray();

        // Only the new statement may survive the filter. NonConcurrentIndexRule is currently registered twice
        // (LintRuleRegistry and LockHeavyOperationRules.All), so the same finding can appear more than once; the
        // assertion therefore checks the set of distinct findings rather than the count.
        Assert.NotEmpty(remaining);
        Assert.All(remaining, f => Assert.Equal("non-concurrent-index", f.RuleName));
        Assert.All(remaining, f => Assert.Equal(migrationPath, f.File));
        Assert.Single(remaining.Select(f => (f.RuleName, f.File, f.Line)).Distinct());
    }

    [Fact]
    public void LoadBaseline_FileDoesNotExist_ThrowsBaselineStoreExceptionWithFileNotFoundInner()
    {
        // Current contract: a missing baseline is an error, not an empty set. See BaselineStoreTests for the unit-level
        // assertion; this confirms the same behaviour through the real file system.
        Assert.False(File.Exists(_baselinePath));

        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(_baselinePath));

        Assert.Equal(_baselinePath, ex.FilePath);
        Assert.IsType<FileNotFoundException>(ex.InnerException);
        Assert.Contains("Baseline file not found", ex.InnerException!.Message);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("[\"abc\", ")]
    [InlineData("{\"fingerprint\": \"abc\"}")]
    [InlineData("[1, 2, 3]")]
    [InlineData("nul")]
    public void LoadBaseline_CorruptedFile_ThrowsBaselineStoreExceptionWithJsonInner(string contents)
    {
        File.WriteAllText(_baselinePath, contents);

        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(_baselinePath));

        Assert.Equal(_baselinePath, ex.FilePath);
        Assert.IsType<System.Text.Json.JsonException>(ex.InnerException);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void LoadBaseline_EmptyFile_ThrowsBaselineStoreExceptionWithProjectMessage()
    {
        File.WriteAllText(_baselinePath, "   \n\t ");

        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.LoadBaseline(_baselinePath));

        Assert.Equal(_baselinePath, ex.FilePath);
        Assert.Equal("Baseline file is empty or contains only whitespace.", ex.InnerException!.Message);
    }

    [Fact]
    public void FilterFindings_CorruptedBaseline_ThrowsBaselineStoreExceptionAndDoesNotSuppress()
    {
        WriteMigration(new[] { "DROP TABLE \"LegacyAudit\";" });
        File.WriteAllText(_baselinePath, "{ corrupted");

        var report = MigrationLinter.CreateDefault().Lint(_root);

        var ex = Assert.Throws<BaselineStoreException>(() => BaselineStore.FilterFindings(_baselinePath, report).ToArray());
        Assert.Equal(_baselinePath, ex.FilePath);
        Assert.IsType<System.Text.Json.JsonException>(ex.InnerException);
    }

    /// <summary>
    /// Writes a realistic EF Core migration whose <c>Up</c> runs the given SQL statements through
    /// <c>migrationBuilder.Sql</c>, and whose <c>Down</c> is non-empty so unrelated rules stay quiet.
    /// Overwrites any existing file, so calling it again simulates editing the migration script.
    /// </summary>
    private string WriteMigration(IEnumerable<string> upStatements)
    {
        var className = MigrationName.Substring(MigrationName.IndexOf('_') + 1);
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;");
        sb.AppendLine();
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("namespace Sarmkadan.Orders.Migrations");
        sb.AppendLine("{");
        sb.AppendLine($"    [DbContext(typeof(OrdersDbContext))]");
        sb.AppendLine($"    [Migration(\"{MigrationName}\")]");
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
        sb.AppendLine("            migrationBuilder.Sql(\"ALTER TABLE \\\"Orders\\\" DROP COLUMN IF EXISTS \\\"Placeholder\\\";\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var path = Path.Combine(_root, "Migrations", MigrationName + ".cs");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    /// <summary>
    /// Returns the 1-based line number of the first line in <paramref name="path"/> containing <paramref name="marker"/>.
    /// </summary>
    private static int ExpectedLineOf(string path, string marker)
    {
        var lines = File.ReadAllLines(path);
        var index = Array.FindIndex(lines, l => l.Contains(marker, StringComparison.Ordinal));
        Assert.True(index >= 0, $"Marker '{marker}' not found in {path}");
        return index + 1;
    }
}
