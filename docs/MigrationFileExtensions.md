# MigrationFileExtensions

A static class providing extension methods for analyzing SQL migration files, including line counts, SQL statement extraction, and metadata retrieval for Up/Down migration bodies. Designed for use with Entity Framework Core-style migrations to support linting and validation workflows.

## API

### GetLineCount
Gets the total number of lines in the migration file.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `int`: The total line count.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetUpBodyLineCount
Gets the line count of the Up migration body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `int`: The Up body line count. Returns 0 if no Up body exists.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetDownBodyLineCount
Gets the line count of the Down migration body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `int`: The Down body line count. Returns 0 if no Down body exists.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetUpSqlStatements
Extracts SQL statements from the Up migration body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `IEnumerable<string>`: SQL statements in the Up body. Empty if no Up body exists.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetDownSqlStatements
Extracts SQL statements from the Down migration body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `IEnumerable<string>`: SQL statements in the Down body. Empty if no Down body exists.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetAllSqlStatements
Combines SQL statements from both Up and Down migration bodies.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `IEnumerable<string>`: All SQL statements in the migration file.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetFileNameWithoutExtension
Retrieves the filename without its extension.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `string`: The filename without extension.

**Exceptions**
- `ArgumentException`: If the path is null or invalid.

---

### GetDirectoryName
Retrieves the directory path of the migration file.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `string`: The directory path.

**Exceptions**
- `ArgumentException`: If the path is null or invalid.

---

### HasUpBody
Determines whether the migration contains an Up body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `bool`: `true` if an Up body exists; otherwise `false`.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### HasDownBody
Determines whether the migration contains a Down body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `bool`: `true` if a Down body exists; otherwise `false`.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetTotalSqlStatementCount
Gets the total number of SQL statements in the migration.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `int`: The combined count of Up and Down SQL statements.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetFileSize
Gets the size of the migration file in bytes.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `long`: The file size in bytes.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetUpBodyCommentCount
Counts single-line comments (`--`) in the Up migration body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `int`: The number of comments in the Up body.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetDownBodyCommentCount
Counts single-line comments (`--`) in the Down migration body.

**Parameters**
- `path` (string): The file path to the migration.

**Returns**
- `int`: The number of comments in the Down body.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path is null or invalid.

---

### GetUpBodyKeywordCount
Counts occurrences of specified SQL keywords in the Up migration body.

**Parameters**
- `path` (string): The file path to the migration.
- `keywords` (IEnumerable<string>): Keywords to search for (case-insensitive).

**Returns**
- `int`: Total keyword occurrences in the Up body.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path or keywords are null.

---

### GetDownBodyKeywordCount
Counts occurrences of specified SQL keywords in the Down migration body.

**Parameters**
- `path` (string): The file path to the migration.
- `keywords` (IEnumerable<string>): Keywords to search for (case-insensitive).

**Returns**
- `int`: Total keyword occurrences in the Down body.

**Exceptions**
- `FileNotFoundException`: If the file does not exist.
- `ArgumentException`: If the path or keywords are null.

---

## Usage

```csharp
// Example 1: Analyze migration structure and validate presence of Up/Down bodies
string migrationPath = "migrations/20231010120000_AddUsersTable.cs";

int totalLines = migrationPath.GetLineCount();
int upLines = migrationPath.GetUpBodyLineCount();
int downLines = migrationPath.GetDownBodyLineCount();

Console.WriteLine($"Total lines: {totalLines}");
Console.WriteLine($"Up body lines: {upLines}");
Console.WriteLine($"Down body lines: {downLines}");

if (!migrationPath.HasUpBody() || !migrationPath.HasDownBody())
{
    Console.WriteLine("Warning: Missing Up or Down migration body.");
}
```

```csharp
// Example 2: Extract and analyze SQL statements for destructive operations
string migrationPath = "migrations/20231010120000_AddUsersTable.cs";

var allStatements = migrationPath.GetAllSqlStatements();
int totalStatements = migrationPath.GetTotalSqlStatementCount();

var destructiveKeywords = new[] { "DROP", "DELETE", "TRUNCATE" };
int upDestructiveCount = migrationPath.GetUpBodyKeywordCount(destructiveKeywords);
int downDestructiveCount = migrationPath.GetDownBodyKeywordCount(destructiveKeywords);

Console.WriteLine($"Total SQL statements: {totalStatements}");
Console.WriteLine($"Destructive keywords in Up body: {upDestructiveCount}");
Console.WriteLine($"Destructive keywords in Down body: {downDestructiveCount}");
```

---

## Notes

- All methods throw `ArgumentException` for null or invalid paths. File-related methods throw `FileNotFoundException` if the file does not exist.
- Methods returning line counts or statement counts return 0 for missing Up/Down bodies rather than throwing exceptions.
- `GetUpBodyKeywordCount` and `GetDownBodyKeywordCount` perform case-insensitive matching on SQL keywords.
- Thread safety is not guaranteed for concurrent access to the same file path. File system operations may introduce race conditions if files are modified during analysis.
- Up/Down body extraction logic accounts for EF Core migration formatting, including C# method structure and embedded SQL strings.
- Comment counting only considers single-line SQL comments (`--`). Multi-line comments (`/* */`) are not currently detected.
