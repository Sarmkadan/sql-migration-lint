using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SqlMigrationLint;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="MigrationLinter"/>.
/// </summary>
/// <remarks>
/// This static class offers extension methods for serializing and deserializing <see cref="MigrationLinter"/> instances
/// to and from JSON format, supporting both compact and indented output.
/// Obsolete: kept only because <see cref="MigrationLinter"/> is a service class relying on the
/// reflection-based <see cref="DefaultJsonTypeInfoResolver"/> and is not a DTO registered on the
/// source-generated <c>SqlMigrationLint.JsonSerialization.LintJsonContext</c>. New code that needs
/// to serialize the linter's DTOs (<c>LintReport</c>, <c>LintFinding</c>, <c>LintConfig</c>, ...)
/// should use <c>SqlMigrationLint.JsonSerialization.LintJson</c> directly.
/// </remarks>
[Obsolete("Prefer SqlMigrationLint.JsonSerialization.LintJson backed by the source-generated LintJsonContext for the linter's DTOs; this reflection-based helper remains only for serializing the MigrationLinter service instance itself.")]
public static class MigrationLinterJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a <see cref="MigrationLinter"/> instance to a JSON string using camelCase property naming.
    /// </summary>
    /// <param name="value">The <see cref="MigrationLinter"/> instance to serialize. Must not be <see langword="null"/>.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the <see cref="MigrationLinter"/> using camelCase property names.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this MigrationLinter value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="MigrationLinter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Must not be <see langword="null"/>, empty, or whitespace.</param>
    /// <returns>A <see cref="MigrationLinter"/> instance populated from the JSON data, or <see langword="null"/> if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized into a <see cref="MigrationLinter"/> instance.</exception>
    public static MigrationLinter? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrEmpty(json.Trim());

        return JsonSerializer.Deserialize<MigrationLinter>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="MigrationLinter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Must not be <see langword="null"/>, empty, or whitespace.</param>
    /// <param name="value">Receives the deserialized <see cref="MigrationLinter"/> instance if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or consists only of whitespace.</exception>
    public static bool TryFromJson(string json, out MigrationLinter? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrEmpty(json.Trim());

        try
        {
            value = JsonSerializer.Deserialize<MigrationLinter>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}