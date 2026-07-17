using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization utilities for configuration
    /// and metadata related to migration file operations.
    /// </summary>
    public static class MigrationFileExtensionsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Serializes an object to a JSON string with camelCase property names.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this object value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An object if deserialization succeeds, otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static object? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                return JsonSerializer.Deserialize<object>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to an object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized object if successful; otherwise, null.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out object? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            value = null;

            try
            {
                value = JsonSerializer.Deserialize<object>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
