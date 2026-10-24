using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SqlMigrationLint.Tests;

public class LintConfigTests : IDisposable
{
    private readonly List<string> _files = new();

    [Fact]
    public void Load_ValidConfig_ReturnsRulesWithSeverities()
    {
        var path = WriteConfig("""
            {
              // comments are allowed
              "rules": {
                "drop-table": "warning",
                "DROP-COLUMN": "error",
                "delete-data": "off",
                "ML103": "warning"
              },
              "maxDegreeOfParallelism": 2
            }
            """);

        var config = LintConfig.Load(path);

        Assert.NotNull(config);
        Assert.Equal(4, config.Rules.Count);
        Assert.Equal(RuleSeverityConfig.Warning, config.Rules["drop-table"]);
        Assert.Equal(RuleSeverityConfig.Error, config.Rules["drop-column"]);
        Assert.Equal(RuleSeverityConfig.Off, config.Rules["delete-data"]);
        Assert.Equal(2, config.MaxDegreeOfParallelism);
    }

    [Fact]
    public void Load_MissingFile_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        Assert.Null(LintConfig.Load(path));
    }

    [Fact]
    public void Load_UnknownRuleId_ThrowsListingValidRuleIds()
    {
        var path = WriteConfig("{ \"rules\": { \"create-index-without-concurent\": \"warning\" } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains(path, ex.Message);
        Assert.Contains("Unknown rule id 'create-index-without-concurent'", ex.Message);
        Assert.Contains("Valid rule ids:", ex.Message);
        Assert.Contains("create-index-without-concurrent", ex.Message);
    }

    [Fact]
    public void Load_SeverityOutsideAllowedSet_ThrowsListingAllowedValues()
    {
        var path = WriteConfig("{ \"rules\": { \"rename-column\": \"danger\" } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("Rule 'rename-column' has invalid severity 'danger'", ex.Message);
        Assert.Contains("Allowed values: off, warning, error", ex.Message);
    }

    [Fact]
    public void Load_NumericSeverity_ThrowsListingAllowedValues()
    {
        var path = WriteConfig("{ \"rules\": { \"drop-table\": 1 } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("invalid severity 1", ex.Message);
        Assert.Contains("Allowed values: off, warning, error", ex.Message);
    }

    [Fact]
    public void Load_MultipleProblems_ReportsEveryProblemInOneMessage()
    {
        var path = WriteConfig("{ \"rules\": { \"drop-tabel\": \"warning\", \"drop-column\": \"loud\" } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("Unknown rule id 'drop-tabel'", ex.Message);
        Assert.Contains("invalid severity 'loud'", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Load_MaxDegreeOfParallelismBelowOne_ThrowsWithProblem(int value)
    {
        var path = WriteConfig($"{{ \"rules\": {{}}, \"maxDegreeOfParallelism\": {value} }}");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("MaxDegreeOfParallelism must be at least 1", ex.Message);
    }

    [Fact]
    public void Load_MalformedJson_ThrowsInvalidJsonMessage()
    {
        var path = WriteConfig("{ \"rules\": ");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("Please ensure it's valid JSON", ex.Message);
    }

    private string WriteConfig(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        _files.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _files)
        {
            File.Delete(file);
        }
    }
}
