using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace SqlMigrationLint.Tests;

public class SarifReportWriterTests
{
    private static JsonDocument Write(params LintFinding[] findings)
    {
        var report = new LintReport(findings, migrationsScanned: 1, hasBlockers: false, maxRisk: RiskLevel.Warning);
        using var writer = new StringWriter();

        new SarifReportWriter().WriteReport(report, writer);

        return JsonDocument.Parse(writer.ToString());
    }

    [Fact]
    public void WriteReport_AnyFindings_EmitsSarif210Header()
    {
        using var doc = Write();

        Assert.Equal("2.1.0", doc.RootElement.GetProperty("version").GetString());
        Assert.Equal("https://json-schema.org/sarif-2.1.0.json", doc.RootElement.GetProperty("$schema").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("runs").GetArrayLength());
    }

    [Fact]
    public void WriteReport_NoFindings_EmitsEmptyRulesAndResults()
    {
        using var doc = Write();

        var run = doc.RootElement.GetProperty("runs")[0];
        Assert.Equal("sql-migration-lint", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());
        Assert.Equal(0, run.GetProperty("tool").GetProperty("driver").GetProperty("rules").GetArrayLength());
        Assert.Equal(0, run.GetProperty("results").GetArrayLength());
    }

    [Fact]
    public void WriteReport_RepeatedRules_DeclaresEachRuleOnceAndIndexesResults()
    {
        var findings = new List<LintFinding>
        {
            new("drop-table", LintSeverity.Danger, "a", "V1.cs", 1),
            new("missing-where", LintSeverity.Warning, "b", "V2.cs", 1),
            new("drop-table", LintSeverity.Blocker, "c", "V3.cs", 1)
        };

        using var doc = Write(findings.ToArray());

        var run = doc.RootElement.GetProperty("runs")[0];
        var rules = run.GetProperty("tool").GetProperty("driver").GetProperty("rules");
        Assert.Equal(2, rules.GetArrayLength());
        Assert.Equal("drop-table", rules[0].GetProperty("id").GetString());
        Assert.Equal("missing-where", rules[1].GetProperty("id").GetString());

        var results = run.GetProperty("results");
        Assert.Equal(3, results.GetArrayLength());
        Assert.Equal(0, results[0].GetProperty("ruleIndex").GetInt32());
        Assert.Equal(1, results[1].GetProperty("ruleIndex").GetInt32());
        Assert.Equal(0, results[2].GetProperty("ruleIndex").GetInt32());
    }

    [Theory]
    [InlineData(LintSeverity.Blocker, "error")]
    [InlineData(LintSeverity.Danger, "error")]
    [InlineData(LintSeverity.Warning, "warning")]
    public void WriteReport_Severity_MapsToSarifLevel(LintSeverity severity, string expectedLevel)
    {
        using var doc = Write(new LintFinding("drop-table", severity, "msg", "V1.cs", 1));

        var result = doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0];
        Assert.Equal(expectedLevel, result.GetProperty("level").GetString());
        Assert.Equal("msg", result.GetProperty("message").GetProperty("text").GetString());
        Assert.Equal("drop-table", result.GetProperty("ruleId").GetString());
    }

    [Fact]
    public void WriteReport_RelativeFileWithBackslashes_EmitsForwardSlashUri()
    {
        using var doc = Write(new LintFinding("drop-table", LintSeverity.Warning, "msg", "Migrations\\V1.cs", 7));

        var physical = doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("locations")[0].GetProperty("physicalLocation");
        Assert.Equal("Migrations/V1.cs", physical.GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.Equal(7, physical.GetProperty("region").GetProperty("startLine").GetInt32());
    }

    [Fact]
    public void WriteReport_LineUnknown_OmitsRegion()
    {
        using var doc = Write(new LintFinding("drop-table", LintSeverity.Warning, "msg", "V1.cs", null));

        var physical = doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("locations")[0].GetProperty("physicalLocation");
        Assert.False(physical.TryGetProperty("region", out _));
        Assert.Equal("V1.cs", physical.GetProperty("artifactLocation").GetProperty("uri").GetString());
    }
}
