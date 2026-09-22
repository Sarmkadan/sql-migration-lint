using SqlMigrationLint;
using Xunit;

namespace SqlMigrationLint.Tests;

public sealed class AddColumnWithDefaultRuleTests
{
    private readonly AddColumnWithDefaultRule _rule = AddColumnWithDefaultRule.Instance;

    [Fact]
    public void AppliesTo_NullOperation_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _rule.AppliesTo(null!));
    }

    [Fact]
    public void Evaluate_NullOperation_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _rule.Evaluate(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AppliesTo_EmptyOrWhitespaceSql_ThrowsArgumentException(string sql)
    {
        var operation = new SqlOperation { Sql = sql };

        Assert.Throws<ArgumentException>(() => _rule.AppliesTo(operation));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_EmptyOrWhitespaceSql_ThrowsArgumentException(string sql)
    {
        var operation = new SqlOperation { Sql = sql };

        Assert.Throws<ArgumentException>(() => _rule.Evaluate(operation));
    }

    [Fact]
    public void AppliesTo_AddColumnWithDefaultOnExistingTable_ReturnsTrue()
    {
        var operation = CreateAddColumnOperation();

        Assert.True(_rule.AppliesTo(operation));
    }

    [Fact]
    public void Evaluate_AddColumnWithDefaultOnExistingTable_ReturnsFinding()
    {
        var operation = CreateAddColumnOperation();

        var finding = _rule.Evaluate(operation);

        Assert.NotNull(finding);
        Assert.Equal("add-column-with-default", finding.RuleName);
        Assert.Equal(LintSeverity.Warning, finding.Severity);
        Assert.Equal(operation.File, finding.File);
        Assert.Equal(operation.Line, finding.Line);
    }

    private static AddColumnOperation CreateAddColumnOperation()
    {
        return new AddColumnOperation
        {
            TableName = "Users",
            ColumnName = "Email",
            IsNullable = false,
            DefaultValue = "'default@example.com'",
            TableExists = true,
            File = "Migrations/001_CreateUsers.cs",
            Line = 10
        };
    }
}
