using System;
using System.Text.Json;
using SqlMigrationLint.JsonSerialization;

namespace SqlMigrationLint;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="NamingConventionRule"/>.
/// </summary>
/// <remarks>
/// Superseded by <see cref="LintJsonContext"/> and <see cref="LintJson"/>, which serialize this type
/// through source generation instead of reflection. Kept only to avoid breaking existing call sites.
/// </remarks>
[Obsolete("Use SqlMigrationLint.JsonSerialization.LintJson with LintJsonContext instead.")]
public static class NamingConventionRuleJsonExtensions
{
    /// <summary>
    /// Serializes a <see cref="NamingConventionRule"/> value to a JSON string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this NamingConventionRule value, bool indented = false) =>
        LintJson.ToJson(value, indented);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="NamingConventionRule"/> value.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="NamingConventionRule"/> value, or <see langword="null"/> if the JSON is empty.</returns>
    /// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="JsonException">The JSON is invalid or cannot be deserialized.</exception>
    public static NamingConventionRule? FromJson(string json) =>
        LintJson.FromJson<NamingConventionRule>(json);

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="NamingConventionRule"/> value.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized value if successful.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/> or empty.</exception>
    public static bool TryFromJson(string json, out NamingConventionRule? value) =>
        LintJson.TryFromJson(json, out value);
}
