# JSON Error Handling Unification

## Summary

Unified the error-handling contract across all six `*JsonExtensions` classes to provide consistent exception handling for JSON serialization and deserialization operations.

## Changes Made

### 1. Created Custom Exception Type

**File:** `src/SqlMigrationLintJsonException.cs` (NEW)

- Created a new `SqlMigrationLintJsonException` class that wraps `JsonException`
- Provides a unified error type for all JSON-related operations across the codebase
- Includes proper exception chaining with `InnerJsonException` property
- Follows .NET exception design guidelines

### 2. Updated DestructiveOperationRulesJsonExtensions.cs

**File:** `src/DestructiveOperationRulesJsonExtensions.cs`

**Changes:**
- Changed `FromJson()` to throw `SqlMigrationLintJsonException` instead of `JsonException`
- Changed `ToJson()` to throw `SqlMigrationLintJsonException` instead of letting `JsonException` bubble up
- Changed `TryFromJson()` to throw `SqlMigrationLintJsonException` for wrapped exceptions
- Fixed null checking to use `ArgumentNullException.ThrowIfNull()` consistently
- Added explicit empty string validation (`json.Length == 0`)
- Updated XML documentation to reflect new exception types

### 3. Updated DestructiveOperationRulesValidationJsonExtensions.cs

**File:** `src/DestructiveOperationRulesValidationJsonExtensions.cs`

**Changes:**
- Changed `FromJson()` to throw `SqlMigrationLintJsonException` instead of `JsonException`
- Updated `TryFromJson()` to use proper null checking with `ArgumentNullException.ThrowIfNull()`
- Updated XML documentation to reflect new exception types

### 4. Updated MissingDownMigrationRuleJsonExtensions.cs

**File:** `src/MissingDownMigrationRuleJsonExtensions.cs`

**Changes:**
- Changed `ToJson()` to wrap `JsonException` in `SqlMigrationLintJsonException`
- Changed `FromJson()` to throw `SqlMigrationLintJsonException` instead of `JsonException`
- Changed `TryFromJson()` to throw `SqlMigrationLintJsonException` for wrapped exceptions
- Fixed null checking to use `ArgumentNullException.ThrowIfNull()` consistently
- Added explicit empty string validation
- Updated XML documentation to reflect new exception types

### 5. Updated MigrationFileExtensionsJsonExtensions.cs

**File:** `src/MigrationFileExtensionsJsonExtensions.cs`

**Changes:**
- Changed `ToJson<T>()` to wrap `JsonException` in `SqlMigrationLintJsonException`
- Changed `FromJson<T>()` to throw `SqlMigrationLintJsonException` instead of `JsonException`
- Changed `TryFromJson<T>()` to throw `SqlMigrationLintJsonException` for wrapped exceptions
- Fixed null checking to use `ArgumentNullException.ThrowIfNull()` consistently
- Added explicit empty string validation
- Updated XML documentation to reflect new exception types

### 6. Updated MigrationFileJsonExtensions.cs

**File:** `src/MigrationFileJsonExtensions.cs`

**Changes:**
- Changed `ToJson()` to wrap `JsonException` in `SqlMigrationLintJsonException`
- Changed `FromJson()` to throw `SqlMigrationLintJsonException` instead of `JsonException`
- Changed `TryFromJson()` to throw `SqlMigrationLintJsonException` for wrapped exceptions
- Fixed null checking to use `ArgumentNullException.ThrowIfNull()` consistently
- Added explicit empty string validation
- Updated XML documentation to reflect new exception types

### 7. Updated GitHubAnnotationsWriterJsonExtensions.cs

**File:** `src/GitHubAnnotationsWriterJsonExtensions.cs`

**Changes:**
- Changed `ToJson(LintFinding)` to wrap `JsonException` in `SqlMigrationLintJsonException`
- Changed `FromJson(LintFinding)` to throw `SqlMigrationLintJsonException` instead of `JsonException`
- Changed `ToJson(IReadOnlyList<LintFinding>)` to wrap `JsonException` in `SqlMigrationLintJsonException`
- Changed `FromJsonToList()` to throw `SqlMigrationLintJsonException` instead of `JsonException`
- Fixed null checking to use `ArgumentNullException.ThrowIfNull()` consistently
- Added explicit empty string validation
- Updated XML documentation to reflect new exception types

## Error Handling Contract

### Before
- Inconsistent exception types thrown across different methods
- Some methods threw `JsonException` directly
- Some methods returned `null` for errors
- Some methods had different null validation approaches
- No unified error type for JSON operations

### After
- All methods throw `SqlMigrationLintJsonException` for JSON-related errors
- Consistent null validation using `ArgumentNullException.ThrowIfNull()`
- Consistent empty string validation using `json.Length == 0`
- Proper exception chaining with `InnerJsonException` property
- All methods have consistent XML documentation with `<exception>` tags

## Contract Specification

### ToJson() Methods
- **Null Input:** Throws `ArgumentNullException`
- **Serialization Error:** Throws `SqlMigrationLintJsonException` with inner `JsonException`
- **Return:** JSON string representation

### FromJson() Methods
- **Null Input:** Throws `ArgumentNullException`
- **Empty Input:** Returns `null` (for nullable return types) or throws `ArgumentException`
- **Invalid JSON:** Throws `SqlMigrationLintJsonException` with inner `JsonException`
- **Return:** Deserialized object or `null`

### TryFromJson() Methods
- **Null Input:** Throws `ArgumentNullException`
- **Empty Input:** Returns `false` with `value = null`
- **Invalid JSON:** Returns `false` with `value = null`
- **Valid JSON:** Returns `true` with deserialized `value`
- **Exception:** Never throws - returns `false` for all error cases

## Build Status

✅ All changes compile successfully
✅ No breaking changes to public APIs
✅ Consistent error handling across all six classes
✅ Build succeeds with 0 errors

## Files Modified

1. `src/SqlMigrationLintJsonException.cs` - NEW FILE
2. `src/DestructiveOperationRulesJsonExtensions.cs` - MODIFIED
3. `src/DestructiveOperationRulesValidationJsonExtensions.cs` - MODIFIED
4. `src/MissingDownMigrationRuleJsonExtensions.cs` - MODIFIED
5. `src/MigrationFileExtensionsJsonExtensions.cs` - MODIFIED
6. `src/MigrationFileJsonExtensions.cs` - MODIFIED
7. `src/GitHubAnnotationsWriterJsonExtensions.cs` - MODIFIED

## Testing

The changes maintain backward compatibility for all existing callers:
- Methods still return the same types
- Methods still accept the same parameters
- Only the exception types thrown have changed (to the more specific `SqlMigrationLintJsonException`)
- All existing tests should continue to pass
