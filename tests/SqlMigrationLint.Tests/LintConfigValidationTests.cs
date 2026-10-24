using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace SqlMigrationLint.Tests;

public class LintConfigValidationTests
{
    [Fact]
    public void Validate_KnownRuleIds_ReturnsNoProblems()
    {
        var config = new LintConfig
        {
            Rules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["drop-table"] = RuleSeverityConfig.Warning,
                ["ML103"] = RuleSeverityConfig.Off
            }
        };

        Assert.Empty(config.Validate());
    }

    [Fact]
    public void Validate_RuleIdDifferingOnlyByCase_ReturnsNoProblems()
    {
        var config = new LintConfig
        {
            Rules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["DROP-TABLE"] = RuleSeverityConfig.Error
            }
        };

        Assert.Empty(config.Validate());
    }

    [Fact]
    public void Validate_UnknownRuleId_ReportsIdAndListsValidRuleIds()
    {
        var config = new LintConfig
        {
            Rules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["create-index-without-concurent"] = RuleSeverityConfig.Warning
            }
        };

        var problems = config.Validate();

        var problem = Assert.Single(problems);
        Assert.Contains("'create-index-without-concurent'", problem);
        Assert.Contains("create-index-without-concurrent", problem);
        Assert.Contains("drop-table", problem);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_MaxDegreeOfParallelismBelowOne_ReturnsProblem(int value)
    {
        var config = new LintConfig { MaxDegreeOfParallelism = value };

        var problem = Assert.Single(config.Validate());
        Assert.Contains("MaxDegreeOfParallelism must be at least 1", problem);
    }

    [Fact]
    public void Validate_MaxDegreeOfParallelismUnset_ReturnsNoProblems()
    {
        Assert.Empty(new LintConfig().Validate());
    }

    [Fact]
    public void Validate_NullConfig_ThrowsArgumentNullException()
    {
        LintConfig? config = null;

        Assert.Throws<ArgumentNullException>(() => config!.Validate());
    }

    [Fact]
    public void ValidRuleIds_ContainsEveryBuiltInRule()
    {
        var ids = LintConfigValidation.ValidRuleIds;

        Assert.Equal(ids.Distinct(StringComparer.OrdinalIgnoreCase).Count(), ids.Count);
        Assert.Contains("create-index-without-concurrent", ids);
        Assert.Contains("drop-table", ids);
        Assert.Contains("non-concurrent-index", ids);
        Assert.Contains("ML100", ids);
        Assert.Contains("ML101", ids);
        Assert.Contains("ML102", ids);
        Assert.Contains("ML103", ids);
    }

    [Theory]
    [InlineData("warning")]
    [InlineData("Warning")]
    [InlineData("error")]
    [InlineData("off")]
    public void ValidateRules_AllowedValue_ReturnsNoProblems(string severity)
    {
        var root = Parse($"{{\"rules\":{{\"drop-table\":\"{severity}\"}}}}");

        Assert.Empty(LintConfigValidation.ValidateRules(root));
    }

    [Fact]
    public void ValidateRules_UnknownSeverityString_ReportsValueAndAllowedSet()
    {
        var root = Parse("{\"rules\":{\"rename-column\":\"danger\"}}");

        var problem = Assert.Single(LintConfigValidation.ValidateRules(root));
        Assert.Contains("'rename-column'", problem);
        Assert.Contains("invalid severity 'danger'", problem);
        Assert.Contains("Allowed values: off, warning, error", problem);
    }

    [Fact]
    public void ValidateRules_NonStringSeverity_ReportsRawValue()
    {
        var root = Parse("{\"rules\":{\"drop-table\":1}}");

        var problem = Assert.Single(LintConfigValidation.ValidateRules(root));
        Assert.Contains("invalid severity 1", problem);
    }

    [Fact]
    public void ValidateRules_UnknownRuleId_ReportsIdAndListsValidRuleIds()
    {
        var root = Parse("{\"rules\":{\"drop-tabel\":\"warning\"}}");

        var problem = Assert.Single(LintConfigValidation.ValidateRules(root));
        Assert.Contains("Unknown rule id 'drop-tabel'", problem);
        Assert.Contains("drop-table", problem);
    }

    [Fact]
    public void ValidateRules_NonObjectRoot_ReturnsNoProblems()
    {
        // Structural errors are reported by deserialization, not by this check.
        Assert.Empty(LintConfigValidation.ValidateRules(Parse("[]")));
    }

    [Fact]
    public void Validate_InvalidExemptTablePattern_ReturnsProblem()
    {
        var config = new LintConfig
        {
            Options = new LintRuleOptions
            {
                AddColumnWithDefault = new AddColumnWithDefaultOptions { ExemptTablePatterns = ["Audit(" ] }
            }
        };

        var problem = Assert.Single(config.Validate());
        Assert.Contains("pattern 'Audit(' is not a valid regular expression", problem);
    }

    [Fact]
    public void ValidateOptions_ValidOptions_ReturnsNoProblems()
    {
        var root = Parse("{\"options\":{\"add-column-with-default\":{\"exemptTablePatterns\":[\"^Audit_\"]}}}");

        Assert.Empty(LintConfigValidation.ValidateOptions(root));
    }

    [Fact]
    public void ValidateOptions_NoOptionsSection_ReturnsNoProblems()
    {
        Assert.Empty(LintConfigValidation.ValidateOptions(Parse("{\"rules\":{}}")));
    }

    [Fact]
    public void ValidateOptions_NonObjectRoot_ReturnsNoProblems()
    {
        // Structural errors are reported by deserialization, not by this check.
        Assert.Empty(LintConfigValidation.ValidateOptions(Parse("[]")));
    }

    [Fact]
    public void ValidateOptions_UnknownOptionName_ReportsNameAndAllowedOptions()
    {
        var root = Parse("{\"options\":{\"add-column-with-default\":{\"exemptTablePattern\":[]}}}");

        var problem = Assert.Single(LintConfigValidation.ValidateOptions(root));
        Assert.Contains("Unknown option 'exemptTablePattern'", problem);
        Assert.Contains("Allowed options: exemptTablePatterns", problem);
    }

    [Fact]
    public void ValidateOptions_EveryBadPatternReported()
    {
        var root = Parse("{\"options\":{\"add-column-with-default\":{\"exemptTablePatterns\":[\"(\",\"^ok$\",\"[\",\"\"]}}}");

        var problems = LintConfigValidation.ValidateOptions(root);

        Assert.Equal(3, problems.Count);
        Assert.Contains(problems, p => p.Contains("index 0") && p.Contains("pattern '('"));
        Assert.Contains(problems, p => p.Contains("index 2") && p.Contains("pattern '['"));
        Assert.Contains(problems, p => p.Contains("index 3") && p.Contains("empty or whitespace"));
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
