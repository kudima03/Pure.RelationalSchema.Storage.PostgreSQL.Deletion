using Npgsql;
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

        Touch(Deletion(table, Cells(rows.Take(40))));

        Assert.Equal(StoredRowsCount - 40, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesEveryRowWhenAllAreSupplied()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);

        Touch(Deletion(table, Cells(rows)));

        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletingRowsThatDoNotExistIsNoOp()
    {
        ITable table = _fixture!.Schema.Tables.First();
        _fixture.Seed(table, [.. new RandomRowsForTable(table)]);

        Touch(Deletion(table, Cells([.. new RandomRowsForTable(table)])));

        Assert.Equal(StoredRowsCount, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesDuplicatedInputRows()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);

        Touch(
            Deletion(table, Cells([.. rows.Take(10), .. rows.Take(10), .. rows.Take(10)]))
        );

        Assert.Equal(StoredRowsCount - 10, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesEveryStoredCopyOfMatchingRow()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] copies = [.. new DefaultRowsForTable(table).Take(3)];
        IRow[] others = [.. new RandomRowsForTable(table).Take(2)];
        _fixture.Seed(table, [.. copies, .. others]);

        Touch(Deletion(table, Cells(copies.Take(1))));

        Assert.Equal(others.Length, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesRowsMatchingSuppliedCellsOnly()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        _fixture.Seed(table, NamedRows(table));

        Touch(Deletion(table, [[Cell(Text(table), "a")]]));

        Assert.Equal(2, _fixture.RowCount(table));
        Assert.Equal(0, _fixture.RowCount(table, Text(table), "a"));
    }

    [Fact]
    public void DeletesRowsMatchingEverySuppliedCell()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        _fixture.Seed(table, NamedRows(table));

        Touch(
            Deletion(table, [[Cell(Text(table), "a"), Cell(Number(table), "2")]])
        );

        Assert.Equal(3, _fixture.RowCount(table));
        Assert.Equal(1, _fixture.RowCount(table, Text(table), "a"));
        Assert.Equal(0, _fixture.RowCount(table, Number(table), "2"));
    }

    [Fact]
    public void DeletesRowsMatchingAnyOfRowsWithDifferentCells()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        _fixture.Seed(table, NamedRows(table));

        Touch(
            Deletion(
                table,
                [
                    [Cell(Text(table), "a"), Cell(Number(table), "1")],
                    [Cell(Number(table), "3")],
                    [Cell(Text(table), "absent")],
                ]
            )
        );

        Assert.Equal(2, _fixture.RowCount(table));
        Assert.Equal(1, _fixture.RowCount(table, Text(table), "a"));
        Assert.Equal(1, _fixture.RowCount(table, Text(table), "b"));
        Assert.Equal(0, _fixture.RowCount(table, Text(table), "c"));
    }

    [Fact]
    public void DeletingByCellsThatMatchNothingIsNoOp()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        _fixture.Seed(table, NamedRows(table));

        Touch(
            Deletion(
                table,
                [
                    [Cell(Text(table), "absent")],
                    [Cell(Text(table), "a"), Cell(Number(table), "3")],
                ]
            )
        );

        Assert.Equal(4, _fixture.RowCount(table));
    }

    [Fact]
    public void RowWithoutCellsDeletesEveryRowOfTable()
    {
        ITable table = _fixture!.Schema.Tables.First();
        ITable untouched = _fixture.Schema.Tables.ElementAt(1);
        _fixture.Seed(table, [.. new RandomRowsForTable(table)]);
        _fixture.Seed(untouched, [.. new RandomRowsForTable(untouched)]);

        Touch(Deletion(table, [[]]));

        Assert.Equal(0, _fixture.RowCount(table));
        Assert.Equal(StoredRowsCount, _fixture.RowCount(untouched));
    }

    [Fact]
    public void RowWithoutCellsAmongOtherRowsDeletesEveryRowOfTable()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        _fixture.Seed(table, NamedRows(table));

        Touch(Deletion(table, [[Cell(Text(table), "absent")], []]));

        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void RowWithoutCellsDeletesEveryRowOfTableWithoutColumns()
    {
        ITable table = _fixture!.Schema.Tables.Last();
        IRow[] rows = [.. new DefaultRowsForTable(table).Take(2)];
        _fixture.Seed(table, rows);

        Touch(Deletion(table, Cells(rows.Take(1))));

        Assert.Equal(0, _fixture.RowCount(table));
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
    public void FailsForCellOfColumnOutsideOfTable()
    {
        ITable table = _fixture!.Schema.Tables.First();
        ITable other = _fixture.Schema.Tables.ElementAt(1);
        _fixture.Seed(table, [.. new RandomRowsForTable(table)]);
        IPostgreSqlStoredSchemaDataSet dataSet = Deletion(
            table,
            [[Cell(Text(other), "a")]]
        );

        _ = Assert.Throws<PostgresException>(() => dataSet[table].TableSchema);
        Assert.Equal(StoredRowsCount, _fixture.RowCount(table));
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

        Touch(
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                stored,
                rows.Select(x => new CellsGrouping(x.Key, Cells(x)))
            )
        );

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

        Touch(Deletion(table, Cells(rows)));

        Assert.Contains(table.Columns, x => x.Type is DeterminedHashColumnType);
        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesRowsByByteaCellOnly()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        IRow[] rows = [.. new DefaultRowsForTable(table).Take(1)];
        Touch(
            new PostgreSqlStoredSchemaDataSetWithInsertedRows(
                Stored(),
                [new Grouping(table, rows)]
            )
        );
        IColumn bytea = table.Columns.Single(x => x.Type is DeterminedHashColumnType);

        Touch(
            Deletion(
                table,
                [[new KeyValuePair<IColumn, ICell>(bytea, rows[0].Cells[bytea])]]
            )
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

        Touch(Deletion(table, Cells([quoted])));

        Assert.Equal(1, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesRowsBySingleQuotedCellOnly()
    {
        ITable table = _fixture!.Schema.Tables.ElementAt(1);
        _fixture.Seed(
            table,
            [
                WithStringValue(table, "O'Reilly's ''quoted''"),
                WithStringValue(table, "OReillys"),
            ]
        );

        Touch(Deletion(table, [[Cell(Text(table), "O'Reilly's ''quoted''")]]));

        Assert.Equal(1, _fixture.RowCount(table));
        Assert.Equal(1, _fixture.RowCount(table, Text(table), "OReillys"));
    }

    [Fact]
    public void WrappingTwiceIsIdempotent()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);
        IGrouping<ITable, IEnumerable<KeyValuePair<IColumn, ICell>>>[] deleted =
        [
            new CellsGrouping(table, Cells(rows.Take(40))),
        ];

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

        _ = Deletion(table, Cells(rows));

        Assert.Equal(StoredRowsCount, _fixture.RowCount(table));
    }

    [Fact]
    public void DeletesOnlyOnce()
    {
        ITable table = _fixture!.Schema.Tables.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture.Seed(table, rows);
        IPostgreSqlStoredSchemaDataSet dataSet = Deletion(table, Cells(rows));

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

        Touch(Deletion(included, Cells(includedRows)));

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

    private static IEnumerable<IEnumerable<KeyValuePair<IColumn, ICell>>> Cells(
        IEnumerable<IRow> rows
    )
    {
        return rows.Select(x => x.Cells);
    }

    private static KeyValuePair<IColumn, ICell> Cell(IColumn column, string value)
    {
        return new KeyValuePair<IColumn, ICell>(column, new Cell(new String(value)));
    }

    private static IColumn Text(ITable table)
    {
        return table.Columns.Single(x => x.Type is StringColumnType);
    }

    private static IColumn Number(ITable table)
    {
        return table.Columns.Single(x => x.Type is UIntColumnType);
    }

    private static IEnumerable<IRow> NamedRows(ITable table)
    {
        return
        [
            Named(table, "a", "1"),
            Named(table, "a", "2"),
            Named(table, "b", "1"),
            Named(table, "c", "3"),
        ];
    }

    private static IRow Named(ITable table, string text, string number)
    {
        return new Row(
            new Dictionary<IColumn, IColumn, ICell>(
                table.Columns,
                x => x,
                x =>
                    x.Type switch
                    {
                        StringColumnType => new Cell(new String(text)),
                        UIntColumnType => new Cell(new String(number)),
                        _ => new Cell(new DefaultValueForColumnType(x.Type)),
                    },
                x => new ColumnHash(x)
            )
        );
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
        IEnumerable<IEnumerable<KeyValuePair<IColumn, ICell>>> rows
    )
    {
        return new PostgreSqlStoredSchemaDataSetWithDeletedRows(
            Stored(),
            [new CellsGrouping(table, rows)]
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
