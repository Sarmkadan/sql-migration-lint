namespace SqlMigrationLint;

using System;
using System.Collections.Generic;
using System.Text.Json;
using SqlMigrationLint.JsonSerialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="LintFinding"/>.
/// </summary>
/// <remarks>
/// Superseded by <see cref="LintJsonContext"/> and <see cref="LintJson"/>, which serialize these
/// types through source generation instead of reflection. Kept only to avoid breaking existing
/// call sites.
/// </remarks>
[Obsolete("Use SqlMigrationLint.JsonSerialization.LintJson with LintJsonContext instead.")]
public static class GitHubAnnotationsWriterJsonExtensions
{
    /// <summary>
    /// Serializes a <see cref="LintFinding"/> to a JSON string.
    /// </summary>
    /// <param name="value">The finding to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the finding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this LintFinding value, bool indented = false) =>
        LintJson.ToJson(value, indented);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="LintFinding"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="LintFinding"/> instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static LintFinding? FromJson(string? json) =>
        string.IsNullOrEmpty(json) ? null : LintJson.FromJson<LintFinding>(json);

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="LintFinding"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized finding if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out LintFinding? value)
    {
        value = null;
        return !string.IsNullOrEmpty(json) && LintJson.TryFromJson(json, out value);
    }

    /// <summary>
    /// Serializes a collection of <see cref="LintFinding"/> to a JSON string.
    /// </summary>
    /// <param name="value">The findings to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the findings collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this IReadOnlyList<LintFinding> value, bool indented = false) =>
        LintJson.ToJson(value, indented);

    /// <summary>
    /// Deserializes a JSON string to a collection of <see cref="LintFinding"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="IReadOnlyList{LintFinding}"/> of findings, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static IReadOnlyList<LintFinding>? FromJsonToList(string? json) =>
        string.IsNullOrEmpty(json) ? null : LintJson.FromJson<IReadOnlyList<LintFinding>>(json);

    /// <summary>
    /// Attempts to deserialize a JSON string to a collection of <see cref="LintFinding"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized findings if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out IReadOnlyList<LintFinding>? value)
    {
        value = null;
        return !string.IsNullOrEmpty(json) && LintJson.TryFromJson(json, out value);
    }
}
