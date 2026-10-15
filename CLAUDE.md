# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory.

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes             # check code style (CI enforces this)
dotnet format                                 # auto-fix code style
dotnet test --no-build --verbosity normal     # run integration tests (requires Docker for Testcontainers)
dotnet tool restore && dotnet stryker --mutation-level Complete --break-at 86
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`. Stryker leaves a mutated assembly in `bin/`, so rebuild before running `dotnet test --no-build` after it.

## Architecture

This is the **deletion (write)** part of the PostgreSQL storage implementation for the Pure relational schema ecosystem. It has no public abstractions of its own — it provides one public decorator that implements `IPostgreSqlStoredSchemaDataSet` (from `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions`). Creating tables, reading rows and inserting rows live in the sibling packages `Pure.RelationalSchema.Storage.PostgreSQL.Creation`, `Pure.RelationalSchema.Storage.PostgreSQL.Querying` and `Pure.RelationalSchema.Storage.PostgreSQL.Insertion`. The design deliberately mirrors Insertion so the two write packages stay symmetric.

**Dependencies:** `Pure.RelationalSchema.Storage.PostgreSQL.Abstractions` is the only package from this family that the library references. Tests additionally reference the published `Pure.RelationalSchema.Storage.PostgreSQL.Insertion` package to prove that rows written by Insertion (including `bytea` values) can be deleted. There is no dependency on `Querying` or `Creation`.

**Key relationships:**

- `PostgreSqlStoredSchemaDataSetWithDeletedRows` (public) wraps an `IPostgreSqlStoredSchemaDataSet` and rows grouped by `ITable`. Like the Insertion decorator, it exposes **only the tables present in the supplied groupings**, and passes `Connection` and `Schema` through from the wrapped dataset.
- `PostgreSqlStoredTableDataSetWithDeletedRows` (internal) is the per-table decorator. On first access of any member it lazily runs one `DELETE` after de-duplicating the supplied rows by `RowHash`, then delegates every member to the wrapped `IStoredTableDataSet`. It executes nothing when there are no rows or the table has no columns (an empty tuple is invalid SQL). The `throw new ArgumentException("Value was not deleted.")` arms are unreachable and mirror Insertion; they account for the uncovered branches and the NoCoverage mutants.
- `DeleteStatement` (internal) builds `DELETE FROM "<schema>"."<table>" WHERE ("c1", …) IN (('v1', …), …);`. Rows have no primary key, so a row is matched by **all** of its cells; every column is `NOT NULL`, so tuple `IN` is exact. Columns come from the table, in table order, and each value is looked up in the row by `ColumnHash` (a missing column throws `InvalidOperationException`). Values are single-quoted text literals that PostgreSQL coerces to the column type; for `bytea` this matches how Insertion writes hex strings.
- `SingleQuoteEscapedString` (internal) doubles embedded `'`. Insertion's `InsertStatement` does **not** escape quotes; this asymmetry is pre-existing and documented in the README. Do not remove the escaping to "match" Insertion.
- All SQL is composed as an `IString` value using `Pure.Primitives.String.Operations` combinators. Do not use string interpolation in library code.
- The caller owns the `IDbConnection`. It is shared with every other dataset using it, so no type here may dispose it (the `IDbCommand` is disposed).

**Naming convention:** the tables must already exist with hash-based names. Schema, table and column identifiers are the first 63 characters (PostgreSQL's identifier limit) of `HexString` over `SchemaHash` / `TableHash` / `ColumnHash` from `Pure.RelationalSchema.HashCodes`. This convention must match the Creation, Querying and Insertion packages, since it is the only contract between them and this one. `TrimmedHash` is deliberately duplicated in each repo rather than shared; it expects input of at least 63 characters (a 64-character SHA-256 hex string in practice).

**Testing:** Integration tests use `Testcontainers.PostgreSql` to spin up a real PostgreSQL instance per test class instance (`IAsyncLifetime`). `DatabaseFixture` creates the hash-named schema and three tables with raw SQL — one with bool/timestamp/int/bigint columns, one with text/numeric/`bytea` columns, and one with **no columns** — and seeds rows with `DatabaseFixture.Seed` (raw `INSERT`, quotes escaped; `DEFAULT VALUES` for the zero-column table). The wrapped dataset is the in-memory `Fakes/FakeStoredSchemaDataSet`, keyed by `TableHash` because `Table.GetHashCode()` throws. Deletes are observed with `SELECT count(*)` (`DatabaseFixture.RowCount`), not by enumerating the decorator, whose enumeration passes through to the fake. The decorator is lazy, so touch a member first to trigger the delete.

- `Fakes/RandomRowsForTable` snapshots each random value into a plain `String`; `RandomValueForColumnType` re-rolls on every `TextValue` read, so without the snapshot a seeded row and the row passed for deletion would differ. Materialize random rows (`[.. new RandomRowsForTable(table)]`) before seeding, because each enumeration produces new rows.
- `DeleteStatementTests` asserts the exact SQL text (mutation-testing targets) and uses 64-character schema/table names because `TrimmedHash` takes a fixed 63-character prefix.
- Non-generic enumeration is exercised through `Fakes/NonGenericItems`; `Cast<T>()` short-circuits when the source already implements `IEnumerable<T>` and would not call the non-generic `GetEnumerator`.
- On stored datasets use `.AsEnumerable()` with `Assert.Empty` / `Assert.Single`, because the bare overloads are ambiguous between `IEnumerable` and `IAsyncEnumerable`.

CI runs mutation testing via `dotnet-stryker` at `Complete` level with an 86% break threshold and requires >=99% line coverage (warning at 99%).

**CI style check:** the `Code style check by csharpier` step in `.github/workflows/build-and-test.yml` is commented out on purpose. csharpier wants `) { }` on one line for chained constructors with an empty body, while `dotnet format` (IDE0055 with `csharp_new_line_before_open_brace = all`) wants `{ }` on its own line, so both cannot pass on `PostgreSqlStoredSchemaDataSetWithDeletedRows.cs`. `dotnet format` is the authoritative check.

**Multi-targeting:** net8.0, net9.0, net10.0. `IsAotCompatible` is `true` and the build must stay free of IL2026/IL3050 warnings. Do not use `AsQueryable()` in library code (annotated as trim/AOT-unsafe).

**Package validation:** `EnablePackageValidation = true`. `PackageValidationBaselineVersion` is set to the last published version (currently `0.1.0-preview.0.1.0`); update it after each release so breaking API changes fail the build.

**Publishing:** triggered by pushing a semver tag matching `*.*.*`. The tag name becomes the `PackageVersion`. Packages are published to both GitHub Packages and NuGet.org.

## Code Style

Enforced via `.editorconfig` and `dotnet format --verify-no-changes` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — properties, indexers, accessors, and lambdas may be expression-bodied
- File-scoped namespace declarations
- Private fields prefixed with `_` (underscore camelCase)
- No non-private instance fields
- `using` directives outside the namespace, System directives sorted first
- Max line length: 90 characters
- Braces on new lines (`csharp_new_line_before_open_brace = all`)
- No implicit object creation (`new()` without type) when the type is apparent

## Commit Messages

Do not mention Claude or AI assistance in commit messages.
