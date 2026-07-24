using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides JSON serialization and deserialization helpers for <see cref="MigrationOperation"/>.
    /// </summary>
    /// <remarks>
    /// Obsolete: kept only because <see cref="MigrationOperation"/> is an abstract record with
    /// polymorphic derived types that are not registered on the source-generated
    /// <c>SqlMigrationLint.JsonSerialization.LintJsonContext</c>. Prefer
    /// <c>SqlMigrationLint.JsonSerialization.LintJson</c> for the DTOs already registered there.
    /// </remarks>
    [Obsolete("Prefer SqlMigrationLint.JsonSerialization.LintJson backed by the source-generated LintJsonContext; this reflection-based helper remains only for the polymorphic MigrationOperation hierarchy.")]
    public static class MigrationOperationJsonExtensions
    {
        /// <summary>
        /// JSON serializer options with camelCase property naming and security constraints.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            MaxDepth = 1024 // Explicitly set MaxDepth to prevent stack overflow attacks
        };

        /// <summary>
        /// Serializes the <see cref="MigrationOperation"/> to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="MigrationOperation"/> to serialize.</param>
        /// <param name="indented">If true, formats the JSON with indentation.</param>
        /// <returns>A JSON string representing the <see cref="MigrationOperation"/>.</returns>
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
        /// Deserializes a JSON string to a <see cref="MigrationOperation"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="MigrationOperation"/> instance, or null if input is null or deserialization fails.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> exceeds maximum allowed size.</exception>
        /// <exception cref="JsonException">Thrown when JSON deserialization fails.</exception>
        public static MigrationOperation? FromJson(string? json)
        {
            if (json is null)
            {
                return null;
            }

            if (json.Length > SqlMigrationLint.JsonSerialization.LintJson.MaxJsonSizeBytes)
            {
                throw new ArgumentException(
                    $"JSON input exceeds maximum allowed size of {SqlMigrationLint.JsonSerialization.LintJson.MaxJsonSizeBytes} bytes. " +
                    $"Actual size: {json.Length} bytes.");
            }

            return JsonSerializer.Deserialize<MigrationOperation>(json, SerializerOptions);
        }

        /// <summary>
        /// Tries to deserialize a JSON string to a <see cref="MigrationOperation"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized <see cref="MigrationOperation"/>, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeded; otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> exceeds maximum allowed size.</exception>
        public static bool TryFromJson(string? json, out MigrationOperation? value)
        {
            try
            {
                value = FromJson(json);
                return value is not null;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}
