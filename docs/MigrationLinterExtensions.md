# MigrationLinterExtensions

Static utility class providing extension methods for analyzing and categorizing lint findings from SQL migration analysis.

This class offers a comprehensive set of methods to query, filter, and aggregate `LintFinding` collections returned by migration linter analyzers. The methods support common reporting scenarios such as severity-based filtering, file grouping, counting, and risk assessment.

## API

### `GetFindingsBySeverity(IEnumerable<LintFinding> findings, LintSeverity severity)`

Filters a collection of lint findings to return only those with the specified severity level.

- **findings**: The collection of lint findings to filter
- **severity**: The severity level to match against
- **Returns**: An enumerable containing only findings with the matching severity
- **Throws**: `ArgumentNullException` if `findings` is null

### `GroupFindingsByFile(IEnumerable<LintFinding> findings)`

Groups a collection of lint findings by the file path where each finding occurred.

- **findings**: The collection of lint findings to group
- **Returns**: A read-only dictionary mapping file paths to lists of findings for that file
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetTotalFindingsCount(IEnumerable<LintFinding> findings)`

Returns the total number of lint findings in the collection.

- **findings**: The collection of lint findings to count
- **Returns**: The total count of findings
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetFindingsCountBySeverity(IEnumerable<LintFinding> findings)`

Counts the number of findings for each severity level in the collection.

- **findings**: The collection of lint findings to analyze
- **Returns**: A read-only dictionary mapping each severity level to its count of findings
- **Throws**: `ArgumentNullException` if `findings` is null

### `HasFindingsOfSeverity(IEnumerable<LintFinding> findings, LintSeverity severity)`

Determines whether the collection contains any findings of the specified severity level.

- **findings**: The collection of lint findings to check
- **severity**: The severity level to search for
- **Returns**: `true` if at least one finding has the specified severity; otherwise `false`
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetFindingsPercentage(IEnumerable<LintFinding> findings)`

Calculates the percentage of findings relative to the total possible findings across all rules.

- **findings**: The collection of lint findings to measure
- **Returns**: A double representing the percentage (0.0 to 100.0) of findings found
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetMaxFindingSeverity(IEnumerable<LintFinding> findings)`

Determines the highest severity level present in the collection of findings.

- **findings**: The collection of lint findings to analyze
- **Returns**: The highest `RiskLevel` severity found, or `RiskLevel.None` if the collection is empty
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetRuleNamesWithFindings(IEnumerable<LintFinding> findings)`

Extracts the unique rule names that have at least one finding in the collection.

- **findings**: The collection of lint findings to process
- **Returns**: An enumerable of rule names that have findings
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetBlockerFindings(IEnumerable<LintFinding> findings)`

Filters findings to return only those marked as blockers (highest severity).

- **findings**: The collection of lint findings to filter
- **Returns**: An enumerable containing only blocker-level findings
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetDangerFindings(IEnumerable<LintFinding> findings)`

Filters findings to return only those marked as danger level (high severity).

- **findings**: The collection of lint findings to filter
- **Returns**: An enumerable containing only danger-level findings
- **Throws**: `ArgumentNullException` if `findings` is null

### `GetWarningFindings(IEnumerable<LintFinding> findings)`

Filters findings to return only those marked as warning level (medium severity).

- **findings**: The collection of lint findings to filter
- **Returns**: An enumerable containing only warning-level findings
- **Throws**: `ArgumentNullException` if `findings` is null

## Usage

### Example 1: Basic reporting by severity

```csharp
var findings = migrationAnalyzer.AnalyzeMigrations(migrationFiles);

// Get counts by severity
var countsBySeverity = MigrationLinterExtensions.GetFindingsCountBySeverity(findings);
Console.WriteLine($"Total findings: {MigrationLinterExtensions.GetTotalFindingsCount(findings)}");
Console.WriteLine($"Blocker findings: {countsBySeverity[LintSeverity.Blocker]}");
Console.WriteLine($"Danger findings: {countsBySeverity[LintSeverity.Danger]}");
Console.WriteLine($"Warning findings: {countsBySeverity[LintSeverity.Warning]}");

// Check if any critical issues exist
if (MigrationLinterExtensions.HasFindingsOfSeverity(findings, LintSeverity.Blocker))
{
    Console.WriteLine("CRITICAL: Blocker findings detected!");
}
```

### Example 2: File-based reporting

```csharp
var findings = migrationAnalyzer.AnalyzeMigrations(migrationFiles);

// Group findings by file
var findingsByFile = MigrationLinterExtensions.GroupFindingsByFile(findings);

foreach (var fileGroup in findingsByFile)
{
    Console.WriteLine($"\nFile: {fileGroup.Key}");
    Console.WriteLine($"Total issues: {fileGroup.Value.Count}");
    
    var maxSeverity = MigrationLinterExtensions.GetMaxFindingSeverity(fileGroup.Value);
    Console.WriteLine($"Max severity: {maxSeverity}");
    
    // List all findings for this file
    foreach (var finding in fileGroup.Value)
    {
        Console.WriteLine($"  - [{finding.Severity}] {finding.RuleName}: {finding.Message}");
    }
}
```

## Notes

- All methods are thread-safe and can be called concurrently from multiple threads
- Methods that accept `IEnumerable<LintFinding>` will enumerate the collection exactly once; subsequent calls will re-enumerate
- `GetMaxFindingSeverity` returns `RiskLevel.None` for empty collections rather than throwing
- The percentage calculation in `GetFindingsPercentage` uses the total number of possible findings across all registered rules as the denominator
- Methods do not modify the input collections; all operations are read-only
- For performance-critical scenarios, consider caching results when the same findings collection is analyzed multiple times
- The `GroupFindingsByFile` method preserves the original finding order within each file group
- All methods validate their input parameters and throw `ArgumentNullException` for null collections