using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="MigrationFile"/>.
    /// </summary>
    public static class MigrationFileJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Serializes a <see cref="MigrationFile"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The migration file to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the migration file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this MigrationFile value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="MigrationFile"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="MigrationFile"/> instance if deserialization succeeds, otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static MigrationFile? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                return JsonSerializer.Deserialize<MigrationFile>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="MigrationFile"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized migration file, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out MigrationFile? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            value = null;

            try
            {
                value = JsonSerializer.Deserialize<MigrationFile>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}