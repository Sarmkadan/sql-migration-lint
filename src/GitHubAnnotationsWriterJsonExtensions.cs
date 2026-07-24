namespace SqlMigrationLint
{
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
        /// <exception cref="SqlMigrationLintJsonException">Thrown when serialization fails.</exception>
        public static string ToJson(this LintFinding value, bool indented = false)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value);
                return LintJson.ToJson(value, indented);
            }
            catch (JsonException ex)
            {
                throw new SqlMigrationLintJsonException("Failed to serialize LintFinding to JSON.", ex);
            }
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="LintFinding"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="LintFinding"/> instance, or null if the JSON is null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="SqlMigrationLintJsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static LintFinding? FromJson(string? json)
        {
            if (json is null)
            {
                return null;
            }

            if (json.Length == 0)
            {
                return null;
            }

            try
            {
                return LintJson.FromJson<LintFinding>(json);
            }
            catch (JsonException ex)
            {
                throw new SqlMigrationLintJsonException("Failed to deserialize JSON to LintFinding.", ex);
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="LintFinding"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized finding if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty.</exception>
        public static bool TryFromJson(string json, out LintFinding? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            value = null;
            return LintJson.TryFromJson(json, out value);
        }

        /// <summary>
        /// Serializes a collection of <see cref="LintFinding"/> to a JSON string.
        /// </summary>
        /// <param name="value">The findings to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the findings collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="SqlMigrationLintJsonException">Thrown when serialization fails.</exception>
        public static string ToJson(this IReadOnlyList<LintFinding> value, bool indented = false)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value);
                return LintJson.ToJson(value, indented);
            }
            catch (JsonException ex)
            {
                throw new SqlMigrationLintJsonException("Failed to serialize LintFinding collection to JSON.", ex);
            }
        }

        /// <summary>
        /// Deserializes a JSON string to a collection of <see cref="LintFinding"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An <see cref="IReadOnlyList{LintFinding}"/> of findings, or null if the JSON is null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="SqlMigrationLintJsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static IReadOnlyList<LintFinding>? FromJsonToList(string? json)
        {
            if (json is null)
            {
                return null;
            }

            if (json.Length == 0)
            {
                return null;
            }

            try
            {
                return LintJson.FromJson<IReadOnlyList<LintFinding>>(json);
            }
            catch (JsonException ex)
            {
                throw new SqlMigrationLintJsonException("Failed to deserialize JSON to LintFinding collection.", ex);
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a collection of <see cref="LintFinding"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized findings if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty.</exception>
        public static bool TryFromJson(string json, out IReadOnlyList<LintFinding>? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            value = null;
            return LintJson.TryFromJson(json, out value);
        }
    }
}
