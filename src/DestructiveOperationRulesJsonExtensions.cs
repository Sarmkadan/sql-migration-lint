using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint;

/// <summary>
/// Provides JSON serialization and deserialization helpers for collections of <see cref="ILintRule"/>
/// that implement destructive operation detection rules.
/// </summary>
public static class DestructiveOperationRulesJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a collection of destructive operation lint rules to a JSON string.
    /// </summary>
    /// <param name="value">The collection of lint rules to serialize. Must not be null.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing the collection of lint rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this IReadOnlyList<ILintRule> value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a collection of lint rules.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Must not be null or empty.</param>
    /// <returns>A collection of lint rules, or null if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
    public static IReadOnlyList<ILintRule>? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<ILintRule[]>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a collection of lint rules.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Must not be null or empty.</param>
    /// <param name="value">Receives the deserialized collection of lint rules, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out IReadOnlyList<ILintRule>? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<ILintRule[]>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}