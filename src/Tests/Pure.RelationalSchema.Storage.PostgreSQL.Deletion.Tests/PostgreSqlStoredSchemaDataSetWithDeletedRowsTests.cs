using Pure.Collections.Generic;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;
using Pure.RelationalSchema.Storage.PostgreSQL.Insertion;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests;

public sealed record PostgreSqlStoredSchemaDataSetWithDeletedRowsTests
    : IAsyncLifetime,
        IDisposable
{
    private const int StoredRowsCount = 100;

    private DatabaseFixture? _fixture;

    [Fact]
    public void DeletesSuppliedSubsetAndKeepsTheRest()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);

        Touch(Deletion(table, rows.Take(40)));

        Assert.Equal(StoredRowsCount - 40, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesEveryRowWhenAllAreSupplied()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);

        Touch(Deletion(table, rows));

        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletingRowsThatDoNotExistIsNoOp()
    {
        ITable table = _fixture!.Schema.Tables.First();
        _fixture.Seed(table, [.. new RandomRowsForTable(table)]);

        Touch(Deletion(table, [.. new RandomRowsForTable(table)]));

        Assert.Equal(StoredRowsCount, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesDuplicatedInputRows()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);

        Touch(Deletion(table, [.. rows.Take(10), .. rows.Take(10), .. rows.Take(10)]));

        Assert.Equal(StoredRowsCount - 10, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesEveryStoredCopyOfMatchingRow()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] copies = [.. new DefaultRowsForTable(table).Take(3)];
        IRow[] others = [.. new RandomRowsForTable(table).Take(2)];
        _fixture.Seed(table, [.. copies, .. others]);

        Touch(Deletion(table, copies.Take(1)));

        Assert.Equal(others.Length, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesNothingWhenNoRowsAreSupplied()
    {
        ITable table = _fixture!.Schema.Tables.First();
        _fixture.Seed(table, [.. new RandomRowsForTable(table)]);

        Touch(Deletion(table, []));

        Assert.Equal(StoredRowsCount, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesNothingFromTableWithoutColumns()
    {
        ITable table = _fixture!.Schema.Tables.Last();
        IRow[] rows = [.. new DefaultRowsForTable(table).Take(2)];
        _fixture.Seed(table, rows);

        Touch(Deletion(table, rows));

        Assert.Equal(rows.Length, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesRowsWrittenByInsertion()
    {
        IPostgreSqlStoredSchemaDataSet stored = Stored();
        ITable[] tables = [.. stored.Keys.Take(2)];
        IGrouping<ITable, IRow>[] rows =
        [
            .. tables.Select(x => new Grouping(x, [.. new RandomRowsForTable(x)])),
        ];
        Touch(new PostgreSqlStoredSchemaDataSetWithInsertedRows(stored, rows));

        Touch(new PostgreSqlStoredSchemaDataSetWithDeletedRows(stored, rows));

        Assert.All(tables, table => Assert.Equal(0, _fixture!.RowCount(table)));
    }

    [Fact]
    public void DeletesRowsWithByteaColumn()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        IRow[] rows = [.. new DefaultRowsForTable(table).Take(1)];
        Touch(
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                Stored(),
                [new Grouping(table, rows)]
            )
        );

        Touch(Deletion(table, rows));

        Assert.Contains(
            table.Columns,
            x => x.Type is DeterminedHashColumnType
        );
        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesRowsWithSingleQuotesInValues()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        IRow quoted = WithStringValue(table, "O'Reilly's ''quoted''");
        IRow plain = WithStringValue(table, "OReillys");
        _fixture.Seed(table, [quoted, plain]);

        Touch(Deletion(table, [quoted]));

        Assert.Equal(1, _fixture.RowCount(table));
    }

    [Fact]
    public void WrappingTwiceIsIdempotent()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);
        IGrouping<ITable, IRow>[] deleted = [new Grouping(table, rows.Take(40))];

        Touch(
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                new PostgreSqlStoredSchemaDataSetWithDeletedRows(Stored(), deleted),
                deleted
            )
        );

        Assert.Equal(StoredRowsCount - 40, _fixture.RowCount(table));
    }

    [Fact]
    public void DoesNotDeleteBeforeFirstAccess()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);

        _ = Deletion(table, rows);

        Assert.Equal(StoredRowsCount, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesOnlyOnce()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);
        IPostgreSqlStoredSchemaDataSet dataSet = Deletion(table, rows);

        _ = dataSet[table].TableSchema;
        _fixture.Seed(table, rows);
        _ = dataSet[table].ElementType;
        _ = dataSet[table].AsEnumerable().Count();

        Assert.Equal(StoredRowsCount, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesNothingFromTablesOutsideOfGroupings()
    {
        ITable included = _fixture!.Schema.Tables.First();
        ITable untouched = _fixture.Schema.Tables.ElementAt(1);
        IRow[] includedRows = [.. new RandomRowsForTable(included)];
        _fixture.Seed(included, includedRows);
        _fixture.Seed(untouched, [.. new RandomRowsForTable(untouched)]);

        Touch(Deletion(included, includedRows));

        Assert.Equal(0, _fixture.RowCount(included));
        Assert.Equal(StoredRowsCount, _fixture.RowCount(untouched));
    }

    public void Dispose()
    {
        _fixture?.Dispose();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        _fixture = new DatabaseFixture();
        return Task.CompletedTask;
    }

    private static void Touch(IPostgreSqlStoredSchemaDataSet dataSet)
    {
        foreach (IStoredTableDataSet tableDataSet in dataSet.Values)
        {
            _ = tableDataSet.TableSchema;
        }
    }

    private static IRow WithStringValue(ITable table, string value)
    {
        return new Row(
            new Dictionary<IColumn, IColumn, ICell>(
                table.Columns,
                x => x,
                x =>
                    x.Type is StringColumnType
                        ? new Cell(new String(value))
                        : new Cell(new DefaultValueForColumnType(x.Type)),
                x => new ColumnHash(x)
            )
        );
    }

    private PostgreSqlStoredSchemaDataSetWithDeletedRows Deletion(
        ITable table,
        IEnumerable<IRow> rows
    )
    {
        return new PostgreSqlStoredSchemaDataSetWithDeletedRows(
            Stored(),
            [new Grouping(table, rows)]
        );
    }

    private FakeStoredSchemaDataSet Stored()
    {
        return new FakeStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection,
            _ => []
        );
    }
}
