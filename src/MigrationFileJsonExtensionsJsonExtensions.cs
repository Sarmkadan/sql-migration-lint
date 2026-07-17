using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Represents migration file JSON serialization configuration.
    /// </summary>
    public sealed class MigrationFileJsonConfig
    {
        /// <summary>
        /// Gets the collection of JSON property names used for migration file serialization.
        /// </summary>
        public static IReadOnlyList<string> PropertyNames { get; } = new[]
        {
            "fileName",
            "content",
            "upOperations",
            "downOperations",
            "applied"
        };

        /// <summary>
        /// Gets whether to use camelCase naming policy for JSON serialization.
        /// </summary>
        public static bool UseCamelCase { get; } = true;

        /// <summary>
        /// Gets whether to ignore null values during JSON serialization.
        /// </summary>
        public static bool IgnoreNullValues { get; } = true;
    }

    /// <summary>
    /// Provides System.Text.Json serialization extensions for <see cref="MigrationFileJsonConfig"/>.
    /// </summary>
    public static class MigrationFileJsonConfigJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private static string ToJson(MigrationFileJsonConfig value, bool indented, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(value);

            var localOptions = indented
                ? new JsonSerializerOptions(options) { WriteIndented = true }
                : options;

            return JsonSerializer.Serialize(value, localOptions);
        }

        /// <summary>
        /// Serializes a <see cref="MigrationFileJsonConfig"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The migration file JSON configuration to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the migration file JSON configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this MigrationFileJsonConfig value, bool indented = false) =>
            ToJson(value, indented, _jsonOptions);

        /// <summary>
        /// Deserializes a JSON string to a <see cref="MigrationFileJsonConfig"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="MigrationFileJsonConfig"/> instance if deserialization succeeds, otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static MigrationFileJsonConfig? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(json);

            try
            {
                return JsonSerializer.Deserialize<MigrationFileJsonConfig>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="MigrationFileJsonConfig"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized migration file JSON configuration, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out MigrationFileJsonConfig? value)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(json);

            value = null;

            try
            {
                value = JsonSerializer.Deserialize<MigrationFileJsonConfig>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
