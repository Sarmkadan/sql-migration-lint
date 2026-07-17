using System;
using System.Text.Json;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization helpers for <see cref="MissingDownMigrationRule"/>.
    /// </summary>
    public static class MissingDownMigrationRuleJsonExtensions
    {
        /// <summary>
        /// JSON serializer options with camelCase property naming.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="MissingDownMigrationRule"/> to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="MissingDownMigrationRule"/> to serialize.</param>
        /// <param name="indented">If true, formats the JSON with indentation.</param>
        /// <returns>A JSON string representing the <see cref="MissingDownMigrationRule"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this MissingDownMigrationRule value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = new JsonSerializerOptions(SerializerOptions)
            {
                WriteIndented = indented
            };
            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="MissingDownMigrationRule"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="MissingDownMigrationRule"/> instance, or null if input is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when JSON deserialization fails.</exception>
        public static MissingDownMigrationRule? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            return JsonSerializer.Deserialize<MissingDownMigrationRule>(json, SerializerOptions);
        }

        /// <summary>
        /// Tries to deserialize a JSON string to a <see cref="MissingDownMigrationRule"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized <see cref="MissingDownMigrationRule"/>, or null if input is null.</param>
        /// <returns>True if deserialization succeeded; otherwise false.</returns>
        public static bool TryFromJson(string json, out MissingDownMigrationRule? value)
        {
            try
            {
                value = FromJson(json);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}