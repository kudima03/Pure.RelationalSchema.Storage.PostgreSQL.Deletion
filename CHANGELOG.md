# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PostgreSQL.Deletion` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- **Breaking:** `PostgreSqlStoredSchemaDataSetWithDeletedRows` now takes the rows to delete as sets of cells, `IEnumerable<IGrouping<ITable, IEnumerable<KeyValuePair<IColumn, ICell>>>>`, instead of `IEnumerable<IGrouping<ITable, IRow>>`. Pass `row.Cells` to keep deleting whole rows.
- A stored row is deleted when every supplied cell of an entry equals its stored value, so a row can be identified by some of its cells (for example its key columns). Entries of one table may name different columns. The statement is now `DELETE … WHERE (c1 = v1 AND c2 = v2) OR (…)` instead of `WHERE (columns) IN (rows)`.
- An entry with no cells matches, and deletes, every row of its table. This also deletes rows of a table without columns, which used to be skipped.
- Supplied entries are no longer de-duplicated; duplicates do not change the result.
- A cell whose column does not belong to the table is no longer reported as `InvalidOperationException`; PostgreSQL rejects the statement with `PostgresException` (`42703`).

### Removed

- The dependency on `Pure.RelationalSchema.Storage.HashCodes`, which was only used to de-duplicate rows.

## [0.1.0-preview.0.4.0] - 2026-10-23

### Changed

- Bumped `Pure.Primitives.String.Operations` from 1.6.0 to 1.7.0.

## [0.1.0-preview.0.3.0] - 2026-10-22

### Changed

- Bumped `Pure.Primitives.String.Operations` from 1.5.1 to 1.6.0.

## [0.1.0-preview.0.2.0] - 2026-10-22

### Added

- Row deletion: `PostgreSqlStoredSchemaDataSetWithDeletedRows` wraps an `IPostgreSqlStoredSchemaDataSet` and lazily deletes rows grouped by table with one `DELETE … WHERE (columns) IN (rows)` statement per table. Rows are matched by all of their cells, duplicates in the input are ignored, and embedded single quotes in values are escaped.

### Changed

- Bumped `Pure.Primitives` from 3.6.5 to 3.7.0.
