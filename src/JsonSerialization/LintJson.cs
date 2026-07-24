using System;
using System.Text.Json;

namespace SqlMigrationLint.JsonSerialization;

/// <summary>
/// Provides a single, source-generation-backed entry point for serializing and deserializing the
/// types registered on <see cref="LintJsonContext"/>. This replaces the ad hoc
/// <see cref="JsonSerializerOptions"/> construction that used to be duplicated across the
/// individual per-type <c>*JsonExtensions</c> classes.
/// </summary>
public static class LintJson
{
    /// <summary>
    /// Maximum allowed JSON input size in bytes (10 MB).
    /// </summary>
    public const int MaxJsonSizeBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// JSON serializer options with MaxDepth protection and size validation.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultOptions = new(LintJsonContext.Default.Options)
    {
        MaxDepth = 1024 // Explicitly set MaxDepth to prevent stack overflow attacks
    };

    /// <summary>
    /// Options that mirror <see cref="LintJsonContext.Default"/> but request indented output.
    /// </summary>
    private static readonly JsonSerializerOptions IndentedOptions = new(DefaultOptions)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Validates that the JSON input does not exceed the maximum allowed size.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the JSON exceeds <see cref="MaxJsonSizeBytes"/>.</exception>
    private static void ValidateJsonSize(string json)
    {
        if (json.Length > MaxJsonSizeBytes)
        {
            throw new ArgumentException(
                $"JSON input exceeds maximum allowed size of {MaxJsonSizeBytes} bytes. " +
                $"Actual size: {json.Length} bytes.");
        }
    }

    /// <summary>
    /// Serializes <paramref name="value"/> to a JSON string using the source-generated
    /// <see cref="LintJsonContext"/>.
    /// </summary>
    /// <typeparam name="T">The type to serialize. Must be registered on <see cref="LintJsonContext"/>.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson<T>(T value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? IndentedOptions : DefaultOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an instance of <typeparamref name="T"/> using the
    /// source-generated <see cref="LintJsonContext"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into. Must be registered on <see cref="LintJsonContext"/>.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An instance of <typeparamref name="T"/>, or null if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> exceeds maximum allowed size.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static T? FromJson<T>(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        ValidateJsonSize(json);

        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an instance of <typeparamref name="T"/> using the
    /// source-generated <see cref="LintJsonContext"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into. Must be registered on <see cref="LintJsonContext"/>.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized value if successful; otherwise, default.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> exceeds maximum allowed size.</exception>
    public static bool TryFromJson<T>(string json, out T? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            ValidateJsonSize(json);
            value = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            return true;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}
