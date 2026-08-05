using System;
using System.Collections.Generic;
using Xunit;

namespace SqlMigrationLint.Tests;

/// <summary>
/// Tests for <see cref="MigrationFileValidationJsonExtensions"/>.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
public sealed class MigrationFileValidationJsonExtensionsTests
{
    [Fact]
    public void ToJson_NonEmptyList_ProducesJsonArray()
    {
        IReadOnlyList<string> problems = new List<string> { "missing down migration", "unsafe drop column" };

        var json = MigrationFileValidationJsonExtensions.ToJson(problems);

        Assert.Equal("[\"missing down migration\",\"unsafe drop column\"]", json);
    }

    [Fact]
    public void ToJson_EmptyList_ProducesEmptyJsonArray()
    {
        IReadOnlyList<string> problems = Array.Empty<string>();

        var json = MigrationFileValidationJsonExtensions.ToJson(problems);

        Assert.Equal("[]", json);
    }

    [Fact]
    public void ToJson_Indented_ProducesMultiLineJson()
    {
        IReadOnlyList<string> problems = new List<string> { "one", "two" };

        var compact = MigrationFileValidationJsonExtensions.ToJson(problems, indented: false);
        var indented = MigrationFileValidationJsonExtensions.ToJson(problems, indented: true);

        Assert.DoesNotContain('\n', compact);
        Assert.Contains('\n', indented);
    }

    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        IReadOnlyList<string>? problems = null;

        Assert.Throws<ArgumentNullException>(() => MigrationFileValidationJsonExtensions.ToJson(problems!));
    }

    [Fact]
    public void FromJson_ValidJsonArray_RoundTripsValues()
    {
        var result = MigrationFileValidationJsonExtensions.FromJson("[\"a\",\"b\",\"c\"]");

        Assert.NotNull(result);
        Assert.Equal(new[] { "a", "b", "c" }, result);
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentNullException()
    {
        string? json = null;

        Assert.Throws<ArgumentNullException>(() => MigrationFileValidationJsonExtensions.FromJson(json!));
    }

    [Fact]
    public void FromJson_EmptyOrWhitespaceJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MigrationFileValidationJsonExtensions.FromJson(string.Empty));
        Assert.Throws<ArgumentException>(() => MigrationFileValidationJsonExtensions.FromJson("   "));
    }

    [Fact]
    public void FromJson_MalformedJsonSyntax_ThrowsJsonException()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => MigrationFileValidationJsonExtensions.FromJson("not valid json"));
    }

    [Fact]
    public void TryFromJson_ValidJsonArray_ReturnsTrueAndPopulatesValue()
    {
        var succeeded = MigrationFileValidationJsonExtensions.TryFromJson("[\"x\",\"y\"]", out var value);

        Assert.True(succeeded);
        Assert.Equal(new[] { "x", "y" }, value);
    }

    [Fact]
    public void TryFromJson_MalformedJsonSyntax_ReturnsFalseAndNullValue()
    {
        var succeeded = MigrationFileValidationJsonExtensions.TryFromJson("not valid json", out var value);

        Assert.False(succeeded);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_NullOrWhitespaceJson_ReturnsFalseAndNullValue()
    {
        Assert.False(MigrationFileValidationJsonExtensions.TryFromJson(null!, out var valueForNull));
        Assert.Null(valueForNull);

        Assert.False(MigrationFileValidationJsonExtensions.TryFromJson("   ", out var valueForWhitespace));
        Assert.Null(valueForWhitespace);
    }
}
#pragma warning restore CS0618
