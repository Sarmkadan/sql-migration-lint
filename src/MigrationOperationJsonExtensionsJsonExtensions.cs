using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization helpers for <see cref="MigrationOperation"/>.
    /// </summary>
    public static class MigrationOperationJsonExtensionsJsonExtensions
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
        /// Serializes a <see cref="MigrationOperation"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The migration operation to serialize.</param>
        /// <param name="indented">If true, formats the JSON with indentation.</param>
        /// <returns>A JSON string representing the migration operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this MigrationOperation value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = new JsonSerializerOptions(SerializerOptions)
            {
                WriteIndented = indented
            };
            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="MigrationOperation"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="MigrationOperation"/> instance if deserialization succeeds, otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when JSON deserialization fails.</exception>
        public static MigrationOperation? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            return JsonSerializer.Deserialize<MigrationOperation>(json, SerializerOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="MigrationOperation"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized migration operation, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out MigrationOperation? value)
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