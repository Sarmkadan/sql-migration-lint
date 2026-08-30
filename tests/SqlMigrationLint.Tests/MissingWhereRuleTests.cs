using System.Reflection;
using SqlMigrationLint;
using Xunit;

namespace SqlMigrationLint.Tests;

public sealed class MissingWhereRuleTests
{
    private static readonly ILintRule Rule = GetRule();

    [Fact]
    public void Evaluate_UpdateWithoutWhere_ReturnsFinding()
    {
        var operation = CreateSqlOperation("UPDATE Customers SET IsActive = 0;");

        var finding = Rule.Evaluate(operation);

        Assert.NotNull(finding);
        Assert.Equal("ML101", finding.RuleName);
        Assert.Equal(LintSeverity.Warning, finding.Severity);
        Assert.Contains("missing a WHERE clause", finding.Message);
        Assert.Equal(operation.File, finding.File);
        Assert.Equal(operation.Line, finding.Line);
    }

    [Fact]
    public void Evaluate_DeleteWithoutWhere_ReturnsFinding()
    {
        var finding = Rule.Evaluate(CreateSqlOperation("DELETE FROM Customers;"));

        Assert.NotNull(finding);
        Assert.Equal("ML101", finding.RuleName);
    }

    [Fact]
    public void Evaluate_UpdateWithWhere_ReturnsNull()
    {
        var finding = Rule.Evaluate(
            CreateSqlOperation("UPDATE Customers SET IsActive = 0 WHERE LastLogin IS NULL;"));

        Assert.Null(finding);
    }

    [Theory]
    [InlineData("update Customers set IsActive = 0;")]
    [InlineData("delete from Customers;")]
    public void Evaluate_LowercaseStatementWithoutWhere_ReturnsFinding(string sql)
    {
        var finding = Rule.Evaluate(CreateSqlOperation(sql));

        Assert.NotNull(finding);
        Assert.Equal("ML101", finding.RuleName);
    }

    [Fact]
    public void Evaluate_MultiLineStatementWithoutWhere_ReturnsFinding()
    {
        const string sql = """
            UPDATE Customers
            SET IsActive = 0,
                Status = 'Archived';
            """;

        var finding = Rule.Evaluate(CreateSqlOperation(sql));

        Assert.NotNull(finding);
        Assert.Equal("ML101", finding.RuleName);
    }

    [Fact]
    public void Evaluate_NonSqlOperation_ReturnsNull()
    {
        var operation = new AddColumnOperation
        {
            TableName = "Customers",
            ColumnName = "IsActive",
            File = "Migration.cs",
            Line = 12
        };

        var finding = Rule.Evaluate(operation);

        Assert.Null(finding);
    }

    [Fact]
    public void AppliesTo_ReturnsTrueOnlyForSqlOperations()
    {
        var sqlOperation = CreateSqlOperation("SELECT 1;");
        var nonSqlOperation = new AddColumnOperation
        {
            TableName = "Customers",
            ColumnName = "IsActive"
        };

        Assert.True(Rule.AppliesTo(sqlOperation));
        Assert.False(Rule.AppliesTo(nonSqlOperation));
    }

    private static SqlOperation CreateSqlOperation(string sql)
    {
        return new SqlOperation
        {
            Sql = sql,
            File = "Migration.cs",
            Line = 12
        };
    }

    private static ILintRule GetRule()
    {
        var ruleType = typeof(MigrationLinter).Assembly.GetType(
            "SqlMigrationLint.MissingWhereRule",
            throwOnError: true)!;
        var instanceField = ruleType.GetField(
            "Instance",
            BindingFlags.Public | BindingFlags.Static)!;

        return Assert.IsAssignableFrom<ILintRule>(instanceField.GetValue(null));
    }
}
