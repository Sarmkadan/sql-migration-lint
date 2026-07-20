# Implementation Summary: .sqlmigrationlint.json Configuration Support

## Overview
Added support for loading optional `.sqlmigrationlint.json` configuration files to allow users to:
- Disable specific lint rules (set to "off")
- Override rule severities (set to "warning" or "error")
- Apply custom configurations per project

## Files Created/Modified

### New Files
1. **src/LintConfig.cs** - Configuration loading and management
   - Defines `RuleSeverityConfig` enum (Off, Warning, Error)
   - Implements `LintConfig` class with `Load()` method
   - Provides `GetEffectiveSeverity()` and `ShouldEvaluateRule()` methods

2. **.sqlmigrationlint.example.json** - Example configuration file
   - Demonstrates all available rules with default configurations
   - Serves as documentation for users

3. **.sqlmigrationlint.json** - Default configuration file
   - Example configuration that can be customized per project

### Modified Files
1. **src/MigrationLinter.cs**
   - Added `LintConfig? _config` field
   - Updated constructors to accept optional `LintConfig? config` parameter
   - Modified `CreateDefault()` and `CreateDefaultWithGlobalRules()` to accept optional `configPath` parameter
   - Updated `Lint()` method to check `_config?.ShouldEvaluateRule(rule.Name)` before evaluating rules
   - Added `public LintConfig? Config => _config;` property

2. **src/Program.cs**
   - Added `--config` command-line option
   - Updated linter initialization to use configuration file
   - Modified to pass configPath to `MigrationLinter.CreateDefaultWithGlobalRules()`

## Rule Configuration

### Rule Severity Levels
- **off** - Rule is disabled and will not run
- **warning** - Rule runs but only generates warnings
- **error** - Rule runs with error severity (default behavior)

### Available Rule IDs

#### Destructive Operation Rules (from DestructiveOperationRules)
- `drop-table` - Detects DROP TABLE statements
- `drop-column` - Detects DROP COLUMN statements  
- `drop-index` - Detects DROP INDEX statements
- `rename-column` - Detects column renaming operations
- `rename-table` - Detects table renaming operations
- `delete-data` - Detects DELETE and TRUNCATE statements
- `drop-database-object` - Detects DROP DATABASE/SCHEMA statements


#### Lock/Heavy Operation Rules (from LockHeavyOperationRules)
- `add-column-with-default` - Detects adding columns with default values
- `alter-column-type-change` - Detects column type changes
- `create-index-without-concurrent` - Detects index creation without CONCURRENTLY/ONLINE
- `add-foreign-key-without-index` - Detects foreign keys without indexes
- `nullable-false-without-default` - Detects NOT NULL columns without defaults
- `add-not-null-without-default` - Detects adding NOT NULL columns without defaults
- `non-concurrent-index` - Detects non-concurrent index operations

#### Global Rules
- `ML103` - Duplicate migration version detection

#### Other Rules
- `empty-down` - Detects empty Down() methods
- `missing-where` - Detects UPDATE/DELETE without WHERE clauses
- `non-concurrent-index` - Duplicate of the lock rule above

## Usage

### Command Line
```bash
# Use default configuration (no config file)
dotnet run -- path/to/migrations

# Use custom configuration file
dotnet run -- path/to/migrations --config path/to/.sqlmigrationlint.json

# View help
sql-migration-lint --help
```

### Configuration File Format
```json
{
  "rules": {
    "rule-id": "off|warning|error"
  }
}
```

### Example Configuration
```json
{
  "$schema": "https://raw.githubusercontent.com/sarmkadan/sql-migration-lint/main/.sqlmigrationlint.json",
  "rules": {
    "drop-table": "warning",
    "delete-data": "off",
    "add-column-with-default": "warning",
    "ML103": "error"
  }
}
```

## Behavior

### Missing Configuration File
- If `.sqlmigrationlint.json` doesn't exist, all rules run with their default severities
- This maintains backward compatibility with existing projects

### With Configuration File
- Rules not specified in the config file use their default severities
- Rules set to "off" are not evaluated (skipped entirely)
- Rules set to "warning" or "error" override the default severity

### Backward Compatibility
- All existing code continues to work without any configuration file
- No breaking changes to the API
- Existing command-line options still work

## Testing
- Build verified with `dotnet build` - SUCCESS
- Build verification script `aider_buildcmd.py` - PASSED
- Configuration loading tested with valid JSON files
- Enum parsing tested with case-insensitive values

## Implementation Details

### Key Design Decisions
1. **Optional Configuration**: Missing config file = all defaults (no breaking changes)
2. **Case-Insensitive Enum Parsing**: Uses `JsonStringEnumConverter` for user-friendly config
3. **Simple Rule Filtering**: Uses `ShouldEvaluateRule()` method for clean rule skipping
4. **Minimal Changes**: Only modified necessary files, no changes to .csproj or dependencies

### Error Handling
- Gracefully handles missing configuration files (returns null)
- Provides clear error messages for invalid JSON
- Validates configuration during load

## Future Enhancements
Possible future improvements:
- Schema validation URL in config files
- Multiple configuration file locations (project-level, team-level, global)
- Configuration validation warnings
- Rule-specific configuration options
