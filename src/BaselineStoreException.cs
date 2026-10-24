using System;

namespace SqlMigrationLint;

/// <summary>
/// Exception thrown when an error occurs while reading or writing a baseline file.
/// </summary>
public sealed class BaselineStoreException : Exception
{
    /// <summary>
    /// The file path associated with the error.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Creates an exception that reuses the inner exception's message.
    /// </summary>
    /// <param name="filePath">The baseline file the error relates to.</param>
    /// <param name="innerException">The underlying I/O or parsing error.</param>
    public BaselineStoreException(string filePath, Exception innerException)
        : base(innerException.Message, innerException)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <summary>
    /// Creates an exception with an explicit message.
    /// </summary>
    /// <param name="filePath">The baseline file the error relates to.</param>
    /// <param name="message">A description of the failure.</param>
    /// <param name="innerException">The underlying error, if any.</param>
    public BaselineStoreException(string filePath, string message, Exception innerException)
        : base(message, innerException)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }
}
