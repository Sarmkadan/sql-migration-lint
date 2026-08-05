using System;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// Tests for <see cref="MigrationLinterJsonExtensions"/>.
/// </summary>
public sealed class MigrationLinterJsonExtensionsTests
{
    private static MigrationLinter CreateLinter() =>
        new MigrationLinter(Array.Empty<ILintRule>());

    [Fact]
    public void ToJson_ValidInstance_ProducesNonEmptyCamelCaseJson()
    {
        var linter = CreateLinter();

        var json = linter.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("\"lintReport\"", json);
        Assert.Contains("\"config\"", json);
    }

    [Fact]
    public void ToJson_Indented_ProducesMultiLineJson()
    {
        var linter = CreateLinter();

        var compact = linter.ToJson(indented: false);
        var indented = linter.ToJson(indented: true);

        Assert.DoesNotContain('\n', compact);
        Assert.Contains('\n', indented);
    }

    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        MigrationLinter? linter = null;

        Assert.Throws<ArgumentNullException>(() => linter!.ToJson());
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentNullException()
    {
        string? json = null;

        Assert.Throws<ArgumentNullException>(() => MigrationLinterJsonExtensions.FromJson(json!));
    }

    [Fact]
    public void FromJson_EmptyOrWhitespaceJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MigrationLinterJsonExtensions.FromJson(string.Empty));
        Assert.Throws<ArgumentException>(() => MigrationLinterJsonExtensions.FromJson("   "));
    }

    [Fact]
    public void FromJson_MalformedJsonSyntax_ThrowsJsonException()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => MigrationLinterJsonExtensions.FromJson("not valid json"));
    }

    [Fact]
    public void FromJson_WellFormedJsonThatDoesNotBindConstructorParameters_ThrowsInvalidOperationException()
    {
        // MigrationLinter's constructor parameters do not correspond to its public properties, so even
        // syntactically valid JSON for its own serialized shape cannot be bound back into an instance.
        var linter = CreateLinter();
        var json = linter.ToJson();

        Assert.Throws<InvalidOperationException>(() => MigrationLinterJsonExtensions.FromJson(json));
    }

    [Fact]
    public void TryFromJson_MalformedJsonSyntax_ReturnsFalseAndNullValue()
    {
        var succeeded = MigrationLinterJsonExtensions.TryFromJson("not valid json", out var value);

        Assert.False(succeeded);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_NullJson_ThrowsArgumentNullException()
    {
        string? json = null;

        Assert.Throws<ArgumentNullException>(() => MigrationLinterJsonExtensions.TryFromJson(json!, out _));
    }
}
