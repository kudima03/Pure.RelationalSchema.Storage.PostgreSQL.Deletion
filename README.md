# Pure.RelationalSchema.Storage.PostgreSQL.Deletion

PostgreSQL row deletion for the **Pure** relational schema ecosystem — `DELETE` decorators over `Pure.RelationalSchema.Storage.PostgreSQL.Querying` datasets.

[![.NET build & test](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Deletion/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Deletion/actions/workflows/build-and-test.yml)
[![publish NuGet](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Deletion/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Deletion/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Pure.RelationalSchema.Storage.PostgreSQL.Deletion)](https://www.nuget.org/packages/Pure.RelationalSchema.Storage.PostgreSQL.Deletion)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

`Pure.RelationalSchema.Storage.PostgreSQL.Deletion` deletes rows from a live PostgreSQL database through the storage abstractions defined in `Pure.RelationalSchema.Storage`. `PostgreSqlStoredSchemaDataSetWithDeletedRows` wraps any `IPostgreSqlStoredSchemaDataSet` (the usual implementation is `PostgreSqlStoredSchemaDataSet` from `Pure.RelationalSchema.Storage.PostgreSQL.Querying`) and a set of rows grouped by table. One `DELETE` statement per table is executed lazily, the first time a table dataset of the result is accessed. After that, the result exposes the wrapped dataset, so the remaining rows can be read back through the same `IStoredTableDataSet`.

Rows have no primary key, so **a row is matched by all of its cells**:

```sql
DELETE FROM "<schema>"."<table>" WHERE ("<c1>", "<c2>", …) IN (('v1', 'v2', …), …);
```

Every column is created `NOT NULL`, so the tuple comparison is exact. Matching works as follows:

- Duplicate rows in the supplied batch are de-duplicated by `RowHash`.
- Every stored copy of a matching row is deleted, not just one.
- Supplying rows that are not stored is a no-op.
- Nothing is executed when a table's grouping is empty or the table has no columns.
- Wrapping the result again with the same rows deletes nothing more.

The result exposes **only the tables present in the supplied groupings**, not every table of the wrapped dataset. This matches `PostgreSqlStoredSchemaDataSetWithInsertedRows` from the Insertion package.

This package covers deletes only. Creating the tables, reading rows and inserting rows are handled by the related packages listed below. The tables must already exist with the hash-based names produced by `Pure.RelationalSchema.Storage.PostgreSQL.Creation`. Schema, table and column identifiers are the first 63 hexadecimal characters of the hashes from `Pure.RelationalSchema.HashCodes`, not the model names. The caller owns the `IDbConnection`: it must be opened before use, and this package never disposes it.

## Public API

| Type | Kind | Description |
|------|------|-------------|
| `PostgreSqlStoredSchemaDataSetWithDeletedRows` | record | Wraps an `IPostgreSqlStoredSchemaDataSet` and deletes grouped `IRow` data from the corresponding tables, matching each row by all of its cells; implements `IPostgreSqlStoredSchemaDataSet` |

The constructor takes any `IPostgreSqlStoredSchemaDataSet`. The table-level decorator, `DeleteStatement` and the hashing and escaping helpers are internal. `IPostgreSqlStoredSchemaDataSet` comes from `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`; this package does not depend on Querying, Creation or Insertion.

## Dependencies

- [`Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Abstractions/tree/0.1.0-preview.0.1.0) — provides `IPostgreSqlStoredSchemaDataSet`
- [`Pure.RelationalSchema.Storage`](https://github.com/kudima03/Pure.RelationalSchema.Storage/tree/0.1.0-preview.8.0.0) — storage abstractions (`IStoredSchemaDataSet`, `IStoredTableDataSet`, `IRow`) that this package implements
- [`Pure.RelationalSchema.Storage.HashCodes`](https://github.com/kudima03/Pure.RelationalSchema.Storage.HashCodes/tree/0.1.0-preview.2.2.0) — `RowHash`, used to de-duplicate the supplied rows
- [`Pure.RelationalSchema.HashCodes`](https://github.com/kudima03/Pure.RelationalSchema.HashCodes/tree/3.3.0) — hash codes used to derive PostgreSQL object names
- [`Pure.Primitives.Abstractions`](https://github.com/kudima03/Pure.Primitives.Abstractions/tree/4.3.0) — base interfaces for primitive types (`IString`, `IBool`, etc.)
- [`Pure.Primitives`](https://github.com/kudima03/Pure.Primitives/tree/3.7.0) — concrete primitive type implementations
- [`Pure.Primitives.String.Operations`](https://github.com/kudima03/Pure.Primitives.String.Operations/tree/1.7.0) — string composition utilities used to build SQL statements
- [`Pure.Collections.Generic`](https://github.com/kudima03/Pure.Collections.Generic/tree/0.1.0-preview.3.0.0) — generic collection implementations (e.g. `OrderedDictionary` and `Dictionary<TKey1, TKey2, TValue>` with dual-key lookup)

## Target Frameworks

- .NET 8
- .NET 9
- .NET 10

## Installation

```
dotnet add package Pure.RelationalSchema.Storage.PostgreSQL.Deletion
```

This package does not create tables, read rows or insert rows itself. The example below also uses `Pure.RelationalSchema.Storage.PostgreSQL.Creation` to create the tables, `Pure.RelationalSchema.Storage.PostgreSQL.Querying` for the wrapped dataset and `Pure.RelationalSchema.Storage.PostgreSQL.Insertion` to store the rows first; none of them is a dependency of this package.

## Usage

```csharp
using System.Data;
using Npgsql;
using Pure.Collections.Generic;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Creation;
using Pure.RelationalSchema.Storage.PostgreSQL.Deletion;
using Pure.RelationalSchema.Storage.PostgreSQL.Insertion;
using Pure.RelationalSchema.Storage.PostgreSQL.Querying;
using Column = Pure.RelationalSchema.Column.Column;
using Schema = Pure.RelationalSchema.Schema.Schema;
using String = Pure.Primitives.String.String;
using Table = Pure.RelationalSchema.Table.Table;

IDbConnection connection = new NpgsqlConnection(connectionString);
connection.Open();

IColumn idColumn = new Column(new String("Id"), new IntColumnType());
IColumn nameColumn = new Column(new String("Name"), new StringColumnType());
ITable usersTable = new Table(new String("Users"), [idColumn, nameColumn], []);
ISchema schema = new Schema(new String("Shop"), [usersTable], []);

ISchema createdSchema = new PostgreSqlCreatedSchema(schema, connection);
_ = createdSchema.Tables.ToArray();

IPostgreSqlStoredSchemaDataSet dataSet =
    new PostgreSqlStoredSchemaDataSet(createdSchema, connection);

IRow alice = User("1", "Alice");
IRow bob = User("2", "Bob");
IRow carol = User("3", "Carol O'Brien");

IPostgreSqlStoredSchemaDataSet withInserted =
    new PostgreSqlStoredSchemaDataSetWithInsertedRows(
        dataSet,
        [new TableRows(usersTable, [alice, bob])]
    );

// Both decorators are lazy: the INSERT runs on first access to the table dataset.
_ = withInserted[usersTable].TableSchema;

// Rows are matched by all of their cells. Carol is not stored, so she is a no-op.
IPostgreSqlStoredSchemaDataSet withDeleted =
    new PostgreSqlStoredSchemaDataSetWithDeletedRows(
        dataSet,
        [new TableRows(usersTable, [alice, carol])]
    );

foreach (IRow row in withDeleted[usersTable])
{
    Console.WriteLine(row.Cells[nameColumn].Value.TextValue); // Bob
}

IRow User(string id, string name)
{
    return new Row(
        new Dictionary<IColumn, IColumn, ICell>(
            [idColumn, nameColumn],
            column => column,
            column =>
                column == idColumn
                    ? new Cell(new String(id))
                    : new Cell(new String(name)),
            column => new ColumnHash(column)
        )
    );
}

// Rows are supplied grouped by table. LINQ's GroupBy cannot be used here: it hashes
// the key, and ITable implementations do not support GetHashCode.
internal sealed class TableRows : IGrouping<ITable, IRow>
{
    private readonly IEnumerable<IRow> _rows;

    public TableRows(ITable key, IEnumerable<IRow> rows)
    {
        Key = key;
        _rows = rows;
    }

    public ITable Key { get; }

    public IEnumerator<IRow> GetEnumerator()
    {
        return _rows.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
```

`GroupBy` cannot be used to group the rows: it hashes the key, and `ITable` implementations do not support `GetHashCode`, so supply your own `IGrouping<ITable, IRow>` as above. Every cell value is an `IString`, compared with the stored value after PostgreSQL coerces the quoted literal to the column type.

## Known limitations

- **Quote escaping differs from Insertion.** Values are written as quoted string literals, and embedded single quotes are escaped by doubling them. `Pure.RelationalSchema.Storage.PostgreSQL.Insertion` does **not** escape them yet, so a value containing `'` can be deleted by this package but cannot be inserted by Insertion. Values are inlined rather than sent as parameters, so do not pass untrusted input.
- **Rows are matched by value.** Every supplied row must have a cell for every column of its table; a row missing a column throws `InvalidOperationException` on first access. There is no way to delete only one of several identical stored rows.
- **All rows go into one statement.** Each table gets a single `DELETE … IN (…)` statement holding every supplied row. Very large batches produce very large SQL text.
- **Only supplied tables are exposed.** Tables of the wrapped dataset that have no grouping are not exposed by the result.

## Related packages

- `Pure.RelationalSchema.Storage.PostgreSQL.Creation` — creates the schema and tables this package deletes from
- `Pure.RelationalSchema.Storage.PostgreSQL.Querying` — reads rows from those tables; `PostgreSqlStoredSchemaDataSet` is the usual dataset to wrap
- `Pure.RelationalSchema.Storage.PostgreSQL.Insertion` — inserts rows into those tables
- `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions` — provides `IPostgreSqlStoredSchemaDataSet`
