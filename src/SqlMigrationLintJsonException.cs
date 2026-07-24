using System;
using System.Text.Json;

namespace SqlMigrationLint;

/// <summary>
/// Represents errors that occur during JSON serialization or deserialization operations.
/// </summary>
/// <remarks>
/// This exception provides a unified error type for all JSON-related operations across
/// the various *JsonExtensions classes, wrapping the underlying <see cref="JsonException"/>.
/// </remarks>
public sealed class SqlMigrationLintJsonException : Exception
{
    /// <summary>
    /// Gets the original <see cref="JsonException"/> that caused this exception.
    /// </summary>
    public JsonException? InnerJsonException { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlMigrationLintJsonException"/> class.
    /// </summary>
    public SqlMigrationLintJsonException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlMigrationLintJsonException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SqlMigrationLintJsonException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlMigrationLintJsonException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SqlMigrationLintJsonException(string message, JsonException innerException)
        : base(message, innerException)
    {
        InnerJsonException = innerException;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlMigrationLintJsonException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SqlMigrationLintJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
        if (innerException is JsonException jsonException)
        {
            InnerJsonException = jsonException;
        }
    }
}