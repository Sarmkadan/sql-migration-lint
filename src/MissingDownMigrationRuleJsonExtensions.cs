using System;
using System.Text.Json;
using SqlMigrationLint.JsonSerialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization helpers for <see cref="MissingDownMigrationRule"/>.
    /// </summary>
    /// <remarks>
    /// Superseded by <see cref="LintJsonContext"/> and <see cref="LintJson"/>, which serialize this
    /// type through source generation instead of reflection. Kept only to avoid breaking existing
    /// call sites.
    /// </remarks>
    [Obsolete("Use SqlMigrationLint.JsonSerialization.LintJson with LintJsonContext instead.")]
    public static class MissingDownMigrationRuleJsonExtensions
    {
        /// <summary>
        /// Serializes the <see cref="MissingDownMigrationRule"/> to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="MissingDownMigrationRule"/> to serialize.</param>
        /// <param name="indented">If true, formats the JSON with indentation.</param>
        /// <returns>A JSON string representing the <see cref="MissingDownMigrationRule"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this MissingDownMigrationRule value, bool indented = false) =>
            LintJson.ToJson(value, indented);

        /// <summary>
        /// Deserializes a JSON string to a <see cref="MissingDownMigrationRule"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="MissingDownMigrationRule"/> instance, or null if input is null.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when JSON deserialization fails.</exception>
        public static MissingDownMigrationRule? FromJson(string json) =>
            LintJson.FromJson<MissingDownMigrationRule>(json);

        /// <summary>
        /// Tries to deserialize a JSON string to a <see cref="MissingDownMigrationRule"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized <see cref="MissingDownMigrationRule"/>, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeded; otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out MissingDownMigrationRule? value) =>
            LintJson.TryFromJson(json, out value);
    }
}
