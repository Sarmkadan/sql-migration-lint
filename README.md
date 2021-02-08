# sql-migration-lint

Lints EF Core migrations for dangerous operations before they hit production.

## MigrationFile

The `MigrationFile` class represents a parsed EF Core migration file. It provides access to the file's metadata, such as its file path, migration name, and contents. You can use it to inspect and lint migration files before applying them to your database.

