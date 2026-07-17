using System;
using System.Text.Json;

namespace SqlMigrationLint;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="DestructiveOperationRulesValidation"/>.
/// </summary>
public static class DestructiveOperationRulesValidationJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a collection of validation problems to a JSON string.
    /// </summary>
    /// <param name="value">The collection of validation problems to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the validation problems.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this IReadOnlyList<string> value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a collection of validation problems.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A collection of validation problems, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static IReadOnlyList<string>? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IReadOnlyList<string>>(json.Trim(), _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a collection of validation problems.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized collection of validation problems if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
/// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    public static bool TryFromJson(string json, out IReadOnlyList<string>? value)
    {
        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<IReadOnlyList<string>>(json.Trim(), _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
