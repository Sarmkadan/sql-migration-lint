using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint;

/// <summary>
/// Provides JSON serialization and deserialization helpers for collections of <see cref="ILintRule"/>
/// that implement destructive operation detection rules.
/// </summary>
/// <remarks>
/// Obsolete: kept only because <see cref="ILintRule"/> is a polymorphic interface that is not
/// registered on the source-generated <c>SqlMigrationLint.JsonSerialization.LintJsonContext</c>.
/// Prefer registering concrete rule types on that context and using
/// <c>SqlMigrationLint.JsonSerialization.LintJson</c> instead of adding new callers here.
/// </remarks>
[Obsolete("Prefer SqlMigrationLint.JsonSerialization.LintJson backed by the source-generated LintJsonContext; this reflection-based helper remains only for polymorphic ILintRule collections.")]
public static class DestructiveOperationRulesJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 1024 // Explicitly set MaxDepth to prevent stack overflow attacks
    };

    /// <summary>
    /// Serializes a collection of destructive operation lint rules to a JSON string.
    /// </summary>
    /// <param name="value">The collection of lint rules to serialize. Must not be null.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing the collection of lint rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="SqlMigrationLintJsonException">Thrown when serialization fails.</exception>
    public static string ToJson(this IReadOnlyList<ILintRule> value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }
        catch (JsonException ex)
        {
            throw new SqlMigrationLintJsonException("Failed to serialize destructive operation rules to JSON.", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string into a collection of lint rules.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Must not be null or empty.</param>
    /// <returns>A collection of lint rules, or null if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> exceeds maximum allowed size.</exception>
    /// <exception cref="SqlMigrationLintJsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
    public static IReadOnlyList<ILintRule>? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (json.Length == 0)
        {
            throw new ArgumentException("JSON input cannot be empty.");
        }

        if (json.Length > SqlMigrationLint.JsonSerialization.LintJson.MaxJsonSizeBytes)
        {
            throw new ArgumentException(
                $"JSON input exceeds maximum allowed size of {SqlMigrationLint.JsonSerialization.LintJson.MaxJsonSizeBytes} bytes. " +
                $"Actual size: {json.Length} bytes.");
        }

        try
        {
            return JsonSerializer.Deserialize<ILintRule[]>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new SqlMigrationLintJsonException("Failed to deserialize JSON to destructive operation rules collection.", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a collection of lint rules.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Must not be null or empty.</param>
    /// <param name="value">Receives the deserialized collection of lint rules, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> exceeds maximum allowed size.</exception>
    public static bool TryFromJson(string json, out IReadOnlyList<ILintRule>? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            if (json.Length == 0)
            {
                throw new ArgumentException("JSON input cannot be empty.");
            }

            if (json.Length > SqlMigrationLint.JsonSerialization.LintJson.MaxJsonSizeBytes)
            {
                throw new ArgumentException(
                    $"JSON input exceeds maximum allowed size of {SqlMigrationLint.JsonSerialization.LintJson.MaxJsonSizeBytes} bytes. " +
                    $"Actual size: {json.Length} bytes.");
            }

            value = JsonSerializer.Deserialize<ILintRule[]>(json, _jsonOptions);
            return true;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}