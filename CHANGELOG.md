# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PostgreSQL.Deletion` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Row deletion: `PostgreSqlStoredSchemaDataSetWithDeletedRows` wraps an `IPostgreSqlStoredSchemaDataSet` and lazily deletes rows grouped by table with one `DELETE … WHERE (columns) IN (rows)` statement per table. Rows are matched by all of their cells, duplicates in the input are ignored, and embedded single quotes in values are escaped.
