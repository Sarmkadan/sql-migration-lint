using System;
using System.Collections.Generic;
using System.Text.Json;
using SqlMigrationLint.JsonSerialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides System.Text.Json serialization extensions for <see cref="IReadOnlyList{T}"/> where T is <see cref="string"/>.
    /// </summary>
    /// <remarks>
    /// Obsolete: superseded by the source-generated <see cref="LintJsonContext"/>, exposed through
    /// <see cref="LintJson"/>. That single context replaces the reflection-based
    /// <see cref="JsonSerializerOptions"/> construction previously duplicated in this class.
    /// </remarks>
    [Obsolete("Use SqlMigrationLint.JsonSerialization.LintJson instead, which is backed by the source-generated LintJsonContext.")]
    public static class MigrationFileValidationJsonExtensions
    {
        /// <summary>
        /// Serializes a collection of migration file validation problems to a JSON string.
        /// </summary>
        /// <param name="value">The collection of validation problems to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representing the validation problems.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this IReadOnlyList<string> value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            return LintJson.ToJson(value, indented);
        }

        /// <summary>
        /// Deserializes a JSON string to a collection of migration file validation problems.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A collection of validation problems, or null if the JSON represents a null value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static IReadOnlyList<string>? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            return LintJson.FromJson<IReadOnlyList<string>>(json.Trim());
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a collection of migration file validation problems.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized collection of validation problems if successful; otherwise, null.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        public static bool TryFromJson(string json, out IReadOnlyList<string>? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            return LintJson.TryFromJson(json.Trim(), out value);
        }
    }
}
