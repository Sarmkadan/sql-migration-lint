using System;
using System.Text.Json;
using SqlMigrationLint.JsonSerialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization utilities for configuration
    /// and metadata related to migration file operations.
    /// </summary>
    /// <remarks>
    /// Obsolete: superseded by the source-generated <see cref="LintJsonContext"/>, exposed through
    /// <see cref="LintJson"/>. That single context replaces the reflection-based
    /// <see cref="JsonSerializerOptions"/> construction previously duplicated in this class.
    /// </remarks>
    [Obsolete("Use SqlMigrationLint.JsonSerialization.LintJson instead, which is backed by the source-generated LintJsonContext.")]
    public static class MigrationFileExtensionsJsonExtensions
    {
        /// <summary>
        /// Serializes an object to a JSON string with camelCase property names.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="SqlMigrationLintJsonException">Thrown when serialization fails.</exception>
        public static string ToJson<T>(this T value, bool indented = false)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value);
                return LintJson.ToJson(value, indented);
            }
            catch (JsonException ex)
            {
                throw new SqlMigrationLintJsonException("Failed to serialize object to JSON.", ex);
            }
        }

        /// <summary>
        /// Deserializes a JSON string to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An instance of type <typeparamref name="T"/> if deserialization succeeds, otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="SqlMigrationLintJsonException">Thrown when JSON deserialization fails.</exception>
        public static T? FromJson<T>(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            if (json.Length == 0)
            {
                return default;
            }

            try
            {
                return LintJson.FromJson<T>(json);
            }
            catch (JsonException ex)
            {
                throw new SqlMigrationLintJsonException($"Failed to deserialize JSON to {typeof(T).Name}.", ex);
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized object if successful; otherwise, null.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson<T>(string json, out T? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            value = default;

            if (json.Length == 0)
            {
                return false;
            }

            try
            {
                return LintJson.TryFromJson(json, out value);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
