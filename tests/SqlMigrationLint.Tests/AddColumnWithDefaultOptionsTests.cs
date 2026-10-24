using System;
using Xunit;

namespace SqlMigrationLint.Tests;

public sealed class AddColumnWithDefaultOptionsTests
{
    [Fact]
    public void Validate_DefaultOptions_ReturnsNoProblems()
    {
        Assert.Empty(new AddColumnWithDefaultOptions().Validate());
    }

    [Fact]
    public void Validate_ValidPatterns_ReturnsNoProblems()
    {
        var options = new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["^__EFMigrationsHistory$", "^Audit_.*", "Log"]
        };

        Assert.Empty(options.Validate());
    }

    [Fact]
    public void Validate_InvalidRegex_ReportsPatternAndRegexError()
    {
        var options = new AddColumnWithDefaultOptions { ExemptTablePatterns = ["("] };

        var problem = Assert.Single(options.Validate());
        Assert.Contains("index 0", problem);
        Assert.Contains("pattern '('", problem);
        Assert.Contains("is not a valid regular expression", problem);
        Assert.Contains("Not enough )'s", problem);
    }

    [Fact]
    public void Validate_SeveralInvalidPatterns_ReportsEveryProblemTogether()
    {
        var options = new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["(", "^Users$", "[a-", "^Orders$", ""]
        };

        var problems = options.Validate();

        Assert.Equal(3, problems.Count);
        Assert.Contains(problems, p => p.Contains("index 0") && p.Contains("pattern '('"));
        Assert.Contains(problems, p => p.Contains("index 2") && p.Contains("pattern '[a-'"));
        Assert.Contains(problems, p => p.Contains("index 4") && p.Contains("empty or whitespace"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespacePattern_ReturnsProblem(string pattern)
    {
        var options = new AddColumnWithDefaultOptions { ExemptTablePatterns = [pattern] };

        var problem = Assert.Single(options.Validate());
        Assert.Contains("index 0", problem);
        Assert.Contains("would exempt every table", problem);
    }

    [Fact]
    public void Validate_NullPatternEntry_ReturnsProblem()
    {
        var options = new AddColumnWithDefaultOptions { ExemptTablePatterns = [null!] };

        var problem = Assert.Single(options.Validate());
        Assert.Contains("must be a string, not null", problem);
    }

    [Fact]
    public void Validate_NullPatternList_ReturnsProblem()
    {
        var options = new AddColumnWithDefaultOptions { ExemptTablePatterns = null! };

        var problem = Assert.Single(options.Validate());
        Assert.Contains("must not be null", problem);
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        AddColumnWithDefaultOptions? options = null;

        Assert.Throws<ArgumentNullException>(() => options!.Validate());
    }

    [Fact]
    public void Constructor_InvalidPatterns_ThrowsWithEveryProblem()
    {
        var options = new AddColumnWithDefaultOptions { ExemptTablePatterns = ["(", "[a-"] };

        var ex = Assert.Throws<ArgumentException>(() => new AddColumnWithDefaultRule(options));

        Assert.Contains("pattern '('", ex.Message);
        Assert.Contains("pattern '[a-'", ex.Message);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AddColumnWithDefaultRule(null!));
    }

    [Fact]
    public void AppliesTo_ExemptTable_ReturnsFalse()
    {
        var rule = new AddColumnWithDefaultRule(new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["^__EFMigrationsHistory$"]
        });

        Assert.False(rule.AppliesTo(CreateAddColumn("__EFMigrationsHistory")));
    }

    [Fact]
    public void AppliesTo_TableNotMatchingAnyPattern_ReturnsTrue()
    {
        var rule = new AddColumnWithDefaultRule(new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["^__EFMigrationsHistory$", "^Audit_"]
        });

        Assert.True(rule.AppliesTo(CreateAddColumn("Orders")));
    }

    [Fact]
    public void AppliesTo_PatternMatchesRegardlessOfCase()
    {
        var rule = new AddColumnWithDefaultRule(new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["^audit_log$"]
        });

        Assert.False(rule.AppliesTo(CreateAddColumn("AUDIT_LOG")));
    }

    [Fact]
    public void AppliesTo_UnanchoredPattern_MatchesSubstring()
    {
        var rule = new AddColumnWithDefaultRule(new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["Audit"]
        });

        Assert.False(rule.AppliesTo(CreateAddColumn("OrderAuditTrail")));
    }

    [Fact]
    public void AppliesTo_AnchoredPattern_DoesNotMatchLongerName()
    {
        var rule = new AddColumnWithDefaultRule(new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["^Audit$"]
        });

        Assert.True(rule.AppliesTo(CreateAddColumn("AuditLog")));
    }

    [Fact]
    public void AppliesTo_DefaultInstance_ExemptsNothing()
    {
        Assert.True(AddColumnWithDefaultRule.Instance.AppliesTo(CreateAddColumn("__EFMigrationsHistory")));
    }

    [Fact]
    public void Evaluate_ExemptTable_ReturnsNull()
    {
        var rule = new AddColumnWithDefaultRule(new AddColumnWithDefaultOptions
        {
            ExemptTablePatterns = ["^Audit_"]
        });

        var operation = CreateAddColumn("Audit_Events");

        Assert.False(rule.AppliesTo(operation));
        Assert.Null(rule.Evaluate(operation));
    }

    private static AddColumnOperation CreateAddColumn(string tableName)
    {
        return new AddColumnOperation
        {
            TableName = tableName,
            ColumnName = "Status",
            IsNullable = false,
            DefaultValue = "0",
            TableExists = true,
            File = "Migrations/001_AddStatus.cs",
            Line = 1
        };
    }
}
