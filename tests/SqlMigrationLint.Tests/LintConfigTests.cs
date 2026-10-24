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

    [Fact]
    public void Load_OptionsWithExemptTablePatterns_BindsPatterns()
    {
        var path = WriteConfig("""
            {
              "rules": { "add-column-with-default": "warning" },
              "options": {
                "add-column-with-default": {
                  "exemptTablePatterns": ["^__EFMigrationsHistory$", "^Audit_"]
                }
              }
            }
            """);

        var config = LintConfig.Load(path);

        Assert.NotNull(config);
        Assert.Equal(
            new[] { "^__EFMigrationsHistory$", "^Audit_" },
            config.Options.AddColumnWithDefault.ExemptTablePatterns);
    }

    [Fact]
    public void Load_NoOptionsSection_LeavesOptionsAtDefaults()
    {
        var path = WriteConfig("{ \"rules\": { \"drop-table\": \"warning\" } }");

        var config = LintConfig.Load(path);

        Assert.NotNull(config);
        Assert.Empty(config.Options.AddColumnWithDefault.ExemptTablePatterns);
    }

    [Fact]
    public void Load_InvalidExemptPattern_ThrowsWithPatternAndRegexError()
    {
        var path = WriteConfig("""
            {
              "options": { "add-column-with-default": { "exemptTablePatterns": ["Audit_(" ] } }
            }
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("Option 'exemptTablePatterns' of rule 'add-column-with-default' has an invalid pattern at index 0", ex.Message);
        Assert.Contains("pattern 'Audit_(' is not a valid regular expression", ex.Message);
    }

    [Fact]
    public void Load_SeveralProblemsAcrossRulesAndOptions_ReportsEveryProblemInOneMessage()
    {
        var path = WriteConfig("""
            {
              "rules": {
                "drop-tabel": "warning",
                "drop-column": "danger"
              },
              "options": {
                "add-column-with-default": {
                  "exemptTablePatterns": ["[unclosed", "^ok$", "(also-bad"],
                  "exemptTabelPatterns": ["^typo$"]
                },
                "drop-table": { "exemptTablePatterns": ["^x$"] },
                "ML999": {}
              }
            }
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("Unknown rule id 'drop-tabel'", ex.Message);
        Assert.Contains("invalid severity 'danger'", ex.Message);
        Assert.Contains("pattern '[unclosed' is not a valid regular expression", ex.Message);
        Assert.Contains("pattern '(also-bad' is not a valid regular expression", ex.Message);
        Assert.Contains("Unknown option 'exemptTabelPatterns' for rule 'add-column-with-default'", ex.Message);
        Assert.Contains("Rule 'drop-table' does not accept options", ex.Message);
        Assert.Contains("Unknown rule id 'ML999'", ex.Message);
    }

    [Fact]
    public void Load_OptionsNotAnObject_ThrowsWithProblem()
    {
        var path = WriteConfig("{ \"options\": [] }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("'options' must be an object keyed by rule id, but was []", ex.Message);
    }

    [Fact]
    public void Load_RuleOptionsNotAnObject_ThrowsWithProblem()
    {
        var path = WriteConfig("{ \"options\": { \"add-column-with-default\": \"yes\" } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("Options for rule 'add-column-with-default' must be an object, but was \"yes\"", ex.Message);
    }

    [Fact]
    public void Load_ExemptTablePatternsNotAnArray_ThrowsWithProblem()
    {
        var path = WriteConfig("{ \"options\": { \"add-column-with-default\": { \"exemptTablePatterns\": \"^Audit$\" } } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("Option 'exemptTablePatterns' of rule 'add-column-with-default' must be an array of strings, but was \"^Audit$\"", ex.Message);
    }

    [Fact]
    public void Load_ExemptTablePatternNotAString_ThrowsWithProblem()
    {
        var path = WriteConfig("{ \"options\": { \"add-column-with-default\": { \"exemptTablePatterns\": [42] } } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("at index 0: the pattern must be a string, but was 42", ex.Message);
    }

    [Fact]
    public void Load_EmptyExemptTablePattern_ThrowsWithProblem()
    {
        var path = WriteConfig("{ \"options\": { \"add-column-with-default\": { \"exemptTablePatterns\": [\"\"] } } }");

        var ex = Assert.Throws<InvalidOperationException>(() => LintConfig.Load(path));

        Assert.Contains("would exempt every table", ex.Message);
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
