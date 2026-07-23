using System;
using System.Text.Json;
using SqlMigrationLint.JsonSerialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="MigrationFile"/>.
    /// </summary>
    /// <remarks>
    /// Superseded by <see cref="LintJsonContext"/> and <see cref="LintJson"/>, which serialize this
    /// type through source generation instead of reflection. Kept only to avoid breaking existing
    /// call sites.
    /// </remarks>
    [Obsolete("Use SqlMigrationLint.JsonSerialization.LintJson with LintJsonContext instead.")]
    public static class MigrationFileJsonExtensions
    {
        /// <summary>
        /// Serializes a <see cref="MigrationFile"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The migration file to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the migration file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when serialization fails.</exception>
        public static string ToJson(this MigrationFile value, bool indented = false) =>
            LintJson.ToJson(value, indented);

        /// <summary>
        /// Deserializes a JSON string to a <see cref="MigrationFile"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="MigrationFile"/> instance if deserialization succeeds, otherwise null.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static MigrationFile? FromJson(string json) =>
            LintJson.TryFromJson<MigrationFile>(json, out var value) ? value : null;

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="MigrationFile"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized migration file, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds, otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out MigrationFile? value) =>
            LintJson.TryFromJson(json, out value);
    }
}
