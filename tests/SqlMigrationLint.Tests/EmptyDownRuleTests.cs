using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// Exercises <c>EmptyDownRule</c> (ML100) through the public <see cref="MigrationLinter"/> facade,
/// since the rule itself is internal and is registered as a per-operation rule by <c>LintRuleRegistry</c>.
/// </summary>
public sealed class EmptyDownRuleTests : IDisposable
{
    private const string RuleId = "ML100";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "sqlmigrationlint-tests-" + Guid.NewGuid().ToString("N"));

    public EmptyDownRuleTests()
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
    public void Lint_DownBodyWithStatements_DoesNotReportEmptyDown()
    {
        WriteMigration("AddUsers", upBody: "migrationBuilder.CreateTable(name: \"Users\");", downBody: "migrationBuilder.DropTable(name: \"Users\");");

        Assert.Empty(FindingsFor(RuleId));
    }

    [Fact]
    public void Lint_EmptyDownBodyWithNonEmptyUp_ReportsEmptyDown()
    {
        WriteMigration("AddUsers", upBody: "migrationBuilder.CreateTable(name: \"Users\");", downBody: string.Empty);

        var finding = Assert.Single(FindingsFor(RuleId));
        Assert.Contains("AddUsers", finding.Message);
        Assert.Equal(RuleId, finding.RuleName);
    }

    [Fact]
    public void Lint_DownBodyOnlyThrows_ReportsEmptyDown()
    {
        WriteMigration("AddUsers", upBody: "migrationBuilder.CreateTable(name: \"Users\");", downBody: "throw new NotSupportedException();");

        Assert.Single(FindingsFor(RuleId));
    }

    private void WriteMigration(string className, string upBody, string downBody)
    {
        string content = $$"""
            using Microsoft.EntityFrameworkCore.Migrations;

            namespace App.Migrations;

            public partial class {{className}} : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
                    {{upBody}}
                }

                protected override void Down(MigrationBuilder migrationBuilder)
                {
                    {{downBody}}
                }
            }
            """;

        File.WriteAllText(Path.Combine(_root, "Migrations", "20260101000000_" + className + ".cs"), content);
    }

    private List<LintFinding> FindingsFor(string ruleId)
    {
        var report = MigrationLinter.CreateDefault().Lint(_root);
        return report.Findings.Where(f => f.RuleName == ruleId).ToList();
    }
}
