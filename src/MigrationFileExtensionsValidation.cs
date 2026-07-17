using System;
using System.Collections.Generic;
using System.Globalization;

namespace SqlMigrationLint
{
    /// <summary>
    /// Provides validation extension methods for <see cref="MigrationFile"/> to validate migration file state.
    /// </summary>
    public static class MigrationFileExtensionsValidation
    {
        /// <summary>
        /// Validates the migration file and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The migration file.</param>
        /// <returns>A list of validation problems, or empty list if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this MigrationFile value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Validate FilePath is not null or empty
            if (string.IsNullOrEmpty(value.FilePath))
            {
                problems.Add("FilePath is null or empty");
            }
            else
            {
                // Validate file exists if path is provided
                if (!System.IO.File.Exists(value.FilePath))
                {
                    problems.Add($"File does not exist at path: {value.FilePath}");
                }
            }

            // Validate MigrationName is not null or empty
            if (string.IsNullOrEmpty(value.MigrationName))
            {
                problems.Add("MigrationName is null or empty");
            }

            // Validate Lines array is not null
            if (value.Lines is null)
            {
                problems.Add("Lines array is null");
            }
            else
            {
                // Validate Lines is not empty
                if (value.Lines.Length == 0)
                {
                    problems.Add("Lines array is empty");
                }
            }

            // Validate UpBody and DownBody are consistent with file structure
            if (value.Lines is not null && value.Lines.Length > 0)
            {
                // Check if UpBody should exist based on file content
                bool hasUpMethod = false;
                bool hasDownMethod = false;

                for (int i = 0; i < value.Lines.Length; i++)
                {
                    if (value.Lines[i].Contains("Up(") && value.Lines[i].Contains("MigrationBuilder"))
                    {
                        hasUpMethod = true;
                    }
                    else if (value.Lines[i].Contains("Down(") && value.Lines[i].Contains("MigrationBuilder"))
                    {
                        hasDownMethod = true;
                    }
                }

                // If Up method exists in file but UpBody is null, that's a problem
                if (hasUpMethod && value.UpBody is null)
                {
                    problems.Add("Migration declares Up method but UpBody is null");
                }

                // If Down method exists in file but DownBody is null, that's a problem
                if (hasDownMethod && value.DownBody is null)
                {
                    problems.Add("Migration declares Down method but DownBody is null");
                }
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Checks if the migration file is in a valid state.
        /// </summary>
        /// <param name="value">The migration file.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static bool IsValid(this MigrationFile value)
        {
            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures the migration file is in a valid state.
        /// </summary>
        /// <param name="value">The migration file.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when validation fails, containing a list of problems.</exception>
        public static void EnsureValid(this MigrationFile value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);

            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"Migration file validation failed:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", problems)}");
            }
        }
    }
}
