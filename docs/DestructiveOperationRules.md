# DestructiveOperationRules

The `DestructiveOperationRules` class provides a centralized collection of linting rules designed to identify potentially hazardous SQL migration operations, such as `DROP TABLE` or `ALTER COLUMN`, which may result in irreversible data loss. By incorporating these rules into the migration validation workflow, developers can proactively detect and prevent unsafe structural changes before they are executed in target environments.

## API

### `public static IReadOnlyList<ILintRule> All`
A static, read-only collection containing all `ILintRule` instances implemented for detecting destructive operations. This member allows developers to retrieve the full set of rules for batch validation of migration scripts.

### `public bool AppliesTo`
Determines whether a specific migration operation is subject to the conditions defined by the rule. This method assesses the operation metadata to decide if further evaluation is required.

### `public LintFinding? Evaluate`
Evaluates a given migration operation against the logic of the rule. If a violation is identified, it returns a `LintFinding` object detailing the issue; otherwise, it returns `null` to indicate no violation was detected.

## Usage

### Example 1: Validating a migration against all rules
This example demonstrates how to iterate through the complete set of destructive operation rules to validate a pending migration operation.

```csharp
using SqlMigrationLint;

var operation = GetPendingMigrationOperation();
foreach (var rule in DestructiveOperationRules.All)
{
    if (rule.AppliesTo(operation))
    {
        var finding = rule.Evaluate(operation);
        if (finding != null)
        {
            Console.WriteLine($"Violation found: {finding.Message}");
        }
    }
}
```

### Example 2: Checking a specific operation type
This example shows how a rule can be applied directly to a target operation in a controlled manner.

```csharp
using SqlMigrationLint;

// Assume 'dropTableRule' is an instance found within DestructiveOperationRules
ILintRule dropTableRule = DestructiveOperationRules.All.First(r => r.Name == "DropTableRule");
var operation = new DropTableOperation("Users");

if (dropTableRule.AppliesTo(operation))
{
    var result = dropTableRule.Evaluate(operation);
    if (result != null)
    {
        // Handle the detected destructive operation finding
        Logger.LogWarning(result.Message);
    }
}
```

## Notes

*   **Rule Coverage:** While `DestructiveOperationRules` covers common destructive SQL patterns, it does not guarantee absolute safety. Validation should be viewed as one layer of a multi-tiered safety strategy, which should also include database backups and pre-deployment analysis.
*   **Thread Safety:** The `All` collection is immutable and safe for concurrent read access. Individual `ILintRule` implementations are designed to be stateless, ensuring that `AppliesTo` and `Evaluate` can be called concurrently from multiple threads without side effects.
*   **Performance:** `Evaluate` calls are intended to be lightweight operations. For large migration scripts, consider implementing batch processing to minimize overhead when checking thousands of operations.
