using System;
using System.IO;
using Xunit;

namespace SqlMigrationLint.Tests;

public sealed class MigrationFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "sqlmigrationlint-tests-" + Guid.NewGuid().ToString("N"));

    public MigrationFileTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void TryParse_UpAndDownSections_ExtractsBothBodies()
    {
        string path = WriteMigration("""
            using Microsoft.EntityFrameworkCore.Migrations;

            namespace App.Migrations;

            public partial class AddUsers : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
                    migrationBuilder.CreateTable(name: "Users");
                }

                protected override void Down(MigrationBuilder migrationBuilder)
                {
                    migrationBuilder.DropTable(name: "Users");
                }
            }
            """);

        MigrationFile? migration = MigrationFile.TryParse(path);

        Assert.NotNull(migration);
        Assert.Equal("AddUsers", migration.MigrationName);
        Assert.Equal(path, migration.FilePath);
        Assert.NotNull(migration.UpBody);
        Assert.Contains("CreateTable", migration.UpBody);
        Assert.DoesNotContain("DropTable", migration.UpBody);
        Assert.NotNull(migration.DownBody);
        Assert.Contains("DropTable", migration.DownBody);
        Assert.DoesNotContain("CreateTable", migration.DownBody);
    }

    [Fact]
    public void TryParse_UpOnly_ReturnsNullDownBody()
    {
        string path = WriteMigration("""
            using Microsoft.EntityFrameworkCore.Migrations;

            namespace App.Migrations;

            public partial class AddIndex : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
                    migrationBuilder.CreateIndex(name: "IX_Users_Email", table: "Users", column: "Email");
                }
            }
            """);

        MigrationFile? migration = MigrationFile.TryParse(path);

        Assert.NotNull(migration);
        Assert.Equal("AddIndex", migration.MigrationName);
        Assert.NotNull(migration.UpBody);
        Assert.Contains("CreateIndex", migration.UpBody);
        Assert.Null(migration.DownBody);
    }

    [Fact]
    public void TryParse_EmptyFile_ReturnsNull()
    {
        string path = WriteMigration(string.Empty);

        MigrationFile? migration = MigrationFile.TryParse(path);

        Assert.Null(migration);
    }

    private string WriteMigration(string content)
    {
        string path = Path.Combine(_directory, "20260101000000_Test.cs");
        File.WriteAllText(path, content);
        return path;
    }
}
