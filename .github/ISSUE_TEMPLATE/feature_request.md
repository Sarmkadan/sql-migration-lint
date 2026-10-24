---
name: Feature request
about: Suggest a new lint rule, a new option, or an improvement to an existing one
labels: enhancement
---

## Problem

<!-- What migration risk or workflow gap are you running into? Describe the situation, not only the solution. -->

## Proposed solution

<!-- What you would like the tool to do. Mention the rule id (kebab-case for operation rules, ML1xx for structure rules) if it applies. -->

## Alternatives considered

<!-- Other approaches you considered, including workarounds such as `--ignore`, `.sqlmigrationlint.json` severity overrides, or baselines. -->

## Example rule

<!-- Optional. Sketch the rule you have in mind: which SQL or EF Core operation it should match, the severity, and a sample migration that should trigger it and one that should not. -->

```csharp
// Example migration that should be flagged:
migrationBuilder.Sql("...");

// Example migration that should not be flagged:
migrationBuilder.Sql("...");
```

**Suggested severity:** <!-- warning / error -->
