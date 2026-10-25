using System;
using System.IO;
using System.Linq;
using SqlMigrationLint;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// Tests for the <see cref="MigrationFileExtensions"/> helpers that build on the parsed Up/Down bodies and operations.
/// </summary>
public sealed class MigrationFileExtensionsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sqlmigrationlint-extensions-" + Guid.NewGuid().ToString("N"));

    public MigrationFileExtensionsTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void HasDownSection_EmptyDownMethod_ReturnsTrue()
    {
        var file = WriteMigration("20261025100000_EmptyDown", up: new[] { "SELECT 1;" }, down: Array.Empty<string>());

        Assert.True(file.HasDownSection());
    }

    [Fact]
    public void HasDownSection_DownMethodWithSql_ReturnsTrue()
    {
        var file = WriteMigration("20261025100000_FilledDown", up: new[] { "SELECT 1;" }, down: new[] { "DELETE FROM \"Orders\" WHERE \"Id\" = 1;" });

        Assert.True(file.HasDownSection());
    }

    [Fact]
    public void HasDownSection_NoDownMethod_ReturnsFalse()
    {
        var file = WriteMigration("20261025100000_NoDown", up: new[] { "SELECT 1;" }, down: null);

        Assert.False(file.HasDownSection());
    }

    [Fact]
    public void HasDownSection_NullFile_ThrowsArgumentNullException()
    {
        MigrationFile? file = null;

        Assert.Throws<ArgumentNullException>(() => file!.HasDownSection());
    }

    [Fact]
    public void StatementCount_UpAndDownStatements_SumsBothBodies()
    {
        var file = WriteMigration(
            "20261025100000_Counted",
            up: new[] { "SELECT 1;", "SELECT 2;" },
            down: new[] { "DELETE FROM \"Orders\" WHERE \"Id\" = 1;" });

        Assert.Equal(3, file.StatementCount());
    }

    [Fact]
    public void StatementCount_CommentedOutStatement_IsNotCounted()
    {
        var file = WriteMigration(
            "20261025100000_CommentedCount",
            up: new[] { "-- DROP TABLE \"Orders\";", "SELECT 1;" },
            down: null);

        Assert.Equal(1, file.StatementCount());
    }

    [Fact]
    public void StatementCount_NoBodies_ReturnsZero()
    {
        var file = WriteMigration("20261025100000_Empty", up: null, down: null);

        Assert.Equal(0, file.StatementCount());
    }

    [Fact]
    public void ContainsOperation_SqlOperationWithoutPredicate_ReturnsTrueWhenUpBodyExists()
    {
        var file = WriteMigration("20261025100000_AnySql", up: new[] { "SELECT 1;" }, down: null);

        Assert.True(file.ContainsOperation<SqlOperation>());
    }

    [Fact]
    public void ContainsOperation_SqlOperationPredicateMatches_ReturnsTrue()
    {
        var file = WriteMigration("20261025100000_DropSql", up: new[] { "DROP TABLE \"Orders\";" }, down: null);

        Assert.True(file.ContainsOperation<SqlOperation>(op => op.Sql.Contains("DROP TABLE", StringComparison.Ordinal)));
    }

    [Fact]
    public void ContainsOperation_SqlOperationPredicateDoesNotMatch_ReturnsFalse()
    {
        var file = WriteMigration("20261025100000_SelectSql", up: new[] { "SELECT 1;" }, down: null);

        Assert.False(file.ContainsOperation<SqlOperation>(op => op.Sql.Contains("DROP TABLE", StringComparison.Ordinal)));
    }

    [Fact]
    public void ContainsOperation_CommentedOutSql_ReturnsFalse()
    {
        var file = WriteMigration("20261025100000_CommentedDrop", up: new[] { "-- DROP TABLE \"Orders\";" }, down: null);

        Assert.False(file.ContainsOperation<SqlOperation>(op => op.Sql.Contains("DROP TABLE", StringComparison.Ordinal)));
    }

    [Fact]
    public void ContainsOperation_AddColumnWithDefault_ReturnsTrueForMatchingColumn()
    {
        var file = WriteMigration(
            "20261025100000_AddColumn",
            up: new[] { "ALTER TABLE \"Orders\" ADD COLUMN \"Priority\" integer NOT NULL DEFAULT 0;" },
            down: null);

        Assert.True(file.ContainsOperation<AddColumnOperation>(op => op.ColumnName == "Priority"));
        Assert.False(file.ContainsOperation<AddColumnOperation>(op => op.ColumnName == "Other"));
    }

    [Fact]
    public void ContainsOperation_NoUpMethod_ReturnsFalse()
    {
        var file = WriteMigration("20261025100000_NoUp", up: null, down: new[] { "SELECT 1;" });

        Assert.False(file.ContainsOperation<SqlOperation>());
    }

    private MigrationFile WriteMigration(string name, string[]? up, string[]? down)
    {
        var lines = new System.Collections.Generic.List<string>
        {
            "using Microsoft.EntityFrameworkCore.Migrations;",
            "",
            "namespace Sample.Migrations",
            "{",
            $"    public partial class {name} : Migration",
            "    {",
        };

        // SQL lines go into the bodies verbatim, the same way the other integration tests lay them out.
        if (up is not null)
        {
            lines.Add("        protected override void Up(MigrationBuilder migrationBuilder)");
            lines.Add("        {");
            lines.AddRange(up.Select(s => "            " + s));
            lines.Add("        }");
        }

        if (down is not null)
        {
            lines.Add("        protected override void Down(MigrationBuilder migrationBuilder)");
            lines.Add("        {");
            lines.AddRange(down.Select(s => "            " + s));
            lines.Add("        }");
        }

        lines.Add("    }");
        lines.Add("}");

        string path = Path.Combine(_root, name + ".cs");
        File.WriteAllLines(path, lines);
        return MigrationFile.Parse(path);
    }
}
