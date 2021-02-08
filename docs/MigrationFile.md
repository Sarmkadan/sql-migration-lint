# MigrationFile

Represents an individual SQL migration file, providing parsed access to its file path, name, raw lines, and extracted up/down SQL bodies.

## API

### `public string FilePath`
Gets the full file-system path from which this migration was parsed.

### `public string MigrationName`
Gets the logical name of the migration, derived from the file name according to the project’s naming convention.

### `public string[] Lines`
Gets the complete set of raw text lines read from the migration file.

### `public string UpBody`
Gets the extracted SQL body for the forward (up) migration, or an empty string if no body could be determined.

### `public string DownBody`
Gets the extracted SQL body for the rollback (down) migration, or an empty string if no body could be determined.

### `public static MigrationFile? TryParse(string filePath)`
Attempts to parse a migration file at the specified file-system path.

- **filePath**: Absolute or relative path to the migration file.
- **Return value**: A populated `MigrationFile` instance if parsing succeeds; otherwise `null`.
- **Exceptions**: Throws `ArgumentNullException` if `filePath` is `null`. Throws `FileNotFoundException` if no file exists at `filePath`. Throws `UnauthorizedAccessException` if the caller lacks read permissions.

## Usage

```csharp
// Example 1: Parsing a migration file
string filePath = "/migrations/202406151200_CreateUsersTable.sql";
MigrationFile? migration = MigrationFile.TryParse(filePath);
if (migration != null)
{
    Console.WriteLine($"Migration '{migration.MigrationName}' parsed from '{migration.FilePath}'");
    Console.WriteLine($"Up body length: {migration.UpBody.Length}");
    Console.WriteLine($"Down body length: {migration.DownBody.Length}");
}

// Example 2: Skipping invalid files
string invalidPath = "/migrations/InvalidFile.sql";
MigrationFile? invalidMigration = MigrationFile.TryParse(invalidPath);
if (invalidMigration == null)
{
    Console.WriteLine($"Skipped invalid migration file at '{invalidPath}'");
}
```

## Notes

- `TryParse` is the only supported construction path; there is no public constructor.
- All properties are read-only; instances are immutable once created.
- Thread-safe for read-only access; concurrent calls to `TryParse` or property access do not require synchronization.
- If the file’s content does not conform to the expected migration format, `UpBody` and `DownBody` may be empty strings rather than throwing.
