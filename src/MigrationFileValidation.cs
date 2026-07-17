using System;
using System.Collections.Generic;
using System.Globalization;

namespace SqlMigrationLint
{
  /// <summary>
  /// Provides validation helpers for <see cref="MigrationFile"/> instances.
  /// </summary>
  public static class MigrationFileValidation
  {
    /// <summary>
    /// Validates a migration file and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The migration file to validate.</param>
    /// <returns>A list of validation problems; empty if the file is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this MigrationFile value)
    {
      ArgumentNullException.ThrowIfNull(value);

      var problems = new List<string>();

      // Validate FilePath
      if (string.IsNullOrWhiteSpace(value.FilePath))
      {
        problems.Add("FilePath cannot be null or whitespace.");
      }

      // Validate MigrationName
      if (string.IsNullOrWhiteSpace(value.MigrationName))
      {
        problems.Add("MigrationName cannot be null or whitespace.");
      }
      else
      {
        // Check for valid migration naming convention
        if (!value.MigrationName.EndsWith("Migration", StringComparison.Ordinal))
        {
          problems.Add("MigrationName should end with 'Migration' (e.g., 'CreateUsersTableMigration').");
        }

        // Check for PascalCase convention
        if (!IsPascalCase(value.MigrationName))
        {
          problems.Add("MigrationName should use PascalCase naming convention.");
        }
      }

      // Validate Lines
      if (value.Lines == null)
      {
        problems.Add("Lines cannot be null.");
      }
      else if (value.Lines.Length == 0)
      {
        problems.Add("Lines cannot be empty.");
      }
      else
      {
        // Check for null elements in Lines array
        foreach (var line in value.Lines)
        {
          if (line == null)
          {
            problems.Add("Lines array cannot contain null elements.");
            break;
          }
        }
      }

      // Validate UpBody
      if (value.UpBody == null)
      {
        problems.Add("UpBody cannot be null.");
      }
      else if (string.IsNullOrWhiteSpace(value.UpBody))
      {
        problems.Add("UpBody cannot be empty or whitespace.");
      }

      // Validate DownBody
      if (value.DownBody == null)
      {
        problems.Add("DownBody cannot be null.");
      }
      else if (string.IsNullOrWhiteSpace(value.DownBody))
      {
        problems.Add("DownBody cannot be empty or whitespace.");
      }

      return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified string uses PascalCase naming convention.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string uses PascalCase; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    private static bool IsPascalCase(string value)
    {
      ArgumentNullException.ThrowIfNull(value);

      if (value.Length < 2)
      {
        return false;
      }

      // First character must be uppercase
      if (!char.IsUpper(value[0]))
      {
        return false;
      }

      // No underscores allowed
      if (value.Contains('_'))
      {
        return false;
      }

      // Check that we have at least one lowercase letter after the first uppercase
      for (int i = 1; i < value.Length; i++)
      {
        if (char.IsLower(value[i]))
        {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Determines whether a migration file is valid.
    /// </summary>
    /// <param name="value">The migration file to check.</param>
    /// <returns>True if the migration file is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this MigrationFile value)
    {
      ArgumentNullException.ThrowIfNull(value);
      return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a migration file is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The migration file to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the migration file is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this MigrationFile value)
    {
      ArgumentNullException.ThrowIfNull(value);

      var problems = Validate(value);
      if (problems.Count > 0)
      {
        throw new ArgumentException(
          $"Migration file is invalid:{Environment.NewLine} - {
            string.Join($"{Environment.NewLine} - ", problems)
          }");
      }
    }
  }
}