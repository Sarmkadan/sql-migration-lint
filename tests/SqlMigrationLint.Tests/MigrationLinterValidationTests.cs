using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// Tests for <see cref="MigrationLinterValidation"/>.
/// </summary>
public sealed class MigrationLinterValidationTests
{
    private static MigrationLinter CreateLinter() =>
        new MigrationLinter(Array.Empty<ILintRule>());

    [Fact]
    public void Validate_NullValue_ThrowsArgumentNullException()
    {
        MigrationLinter? linter = null;

        Assert.Throws<ArgumentNullException>(() => linter!.Validate());
    }

    [Fact]
    public void Validate_NoLintReportGenerated_ReturnsEmptyList()
    {
        var linter = CreateLinter();

        var problems = linter.Validate();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ValidLintReport_ReturnsEmptyList()
    {
        var linter = CreateLinter();
        var rootPath = Directory.CreateTempSubdirectory().FullName;
        Directory.CreateDirectory(Path.Combine(rootPath, "Migrations"));
        try
        {
            linter.Lint(rootPath);

            var problems = linter.Validate();

            Assert.Empty(problems);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Validate_NegativeMigrationsScanned_ReportsProblem()
    {
        var linter = CreateLinter();
        var report = new LintReport(new List<LintFinding>(), migrationsScanned: -1, hasBlockers: false, maxRisk: RiskLevel.None);
        InjectLintReport(linter, report);

        var problems = linter.Validate();

        Assert.Contains(problems, p => p.Contains("MigrationsScanned"));
    }

    [Fact]
    public void Validate_InvalidMaxRiskValue_ReportsProblem()
    {
        var linter = CreateLinter();
        var report = new LintReport(new List<LintFinding>(), migrationsScanned: 0, hasBlockers: false, maxRisk: (RiskLevel)99);
        InjectLintReport(linter, report);

        var problems = linter.Validate();

        Assert.Contains(problems, p => p.Contains("MaxRisk"));
    }

    /// <summary>
    /// Sets the private lint-report backing field on a <see cref="MigrationLinter"/> so tests can
    /// exercise <see cref="MigrationLinterValidation.Validate"/> against crafted, otherwise
    /// unreachable-via-public-API report states (negative counts, out-of-range enum values).
    /// </summary>
    private static void InjectLintReport(MigrationLinter linter, LintReport report)
    {
        var field = typeof(MigrationLinter).GetField("_lintReport", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate MigrationLinter._lintReport field for test setup.");
        field.SetValue(linter, report);
    }

    [Fact]
    public void IsValid_NullValue_ReturnsFalse()
    {
        MigrationLinter? linter = null;

        Assert.False(linter!.IsValid());
    }

    [Fact]
    public void IsValid_NoLintReportGenerated_ReturnsTrue()
    {
        var linter = CreateLinter();

        Assert.True(linter.IsValid());
    }

    [Fact]
    public void EnsureValid_NullValue_ThrowsArgumentNullException()
    {
        MigrationLinter? linter = null;

        Assert.Throws<ArgumentNullException>(() => linter!.EnsureValid());
    }

    [Fact]
    public void EnsureValid_ValidLinter_DoesNotThrow()
    {
        var linter = CreateLinter();

        linter.EnsureValid();
    }
}
