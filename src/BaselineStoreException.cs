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

    public BaselineStoreException(string filePath, Exception innerException)
        : base(innerException.Message, innerException)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public BaselineStoreException(string filePath, string message, Exception innerException)
        : base(message, innerException)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }
}
