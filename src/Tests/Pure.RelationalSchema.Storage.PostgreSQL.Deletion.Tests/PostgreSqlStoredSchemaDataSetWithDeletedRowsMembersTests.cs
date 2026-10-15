using System.Collections;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests;

public sealed record PostgreSqlStoredSchemaDataSetWithDeletedRowsMembersTests
    : IAsyncLifetime,
        IDisposable
{
    private const int ExistingRowsCount = 3;

    private DatabaseFixture? _fixture;

    [Fact]
    public void CountEqualsNumberOfSuppliedGroupings()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Assert.Equal(schema.Count, dataSet.Count);
    }

    [Fact]
    public void ExposesOnlyTablesFromSuppliedGroupings()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable included = schema.Keys.First();
        ITable excluded = schema.Keys.Last();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([included])
            );

        _ = Assert.Single(dataSet);
        Assert.Equal([included], dataSet.Keys);
        _ = Assert.Single(dataSet.Values);
        Assert.True(dataSet.ContainsKey(included));
        Assert.False(dataSet.ContainsKey(excluded));
        _ = Assert.ThrowsAny<Exception>(() => dataSet[excluded]);
    }

    [Fact]
    public void KeysAndValuesFollowSuppliedOrder()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys.Reverse())
            );

        Assert.Equal(schema.Keys.Reverse(), dataSet.Keys);
        Assert.Equal(dataSet.Count, dataSet.Values.Count());
        Assert.All(
            dataSet.Values.Zip(dataSet.Select(x => x.Value)),
            pair => Assert.Same(pair.First, pair.Second)
        );
    }

    [Fact]
    public void TryGetValueFindsSuppliedTable()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.True(dataSet.TryGetValue(table, out IStoredTableDataSet? value));
        Assert.Same(dataSet[table], value);
    }

    [Fact]
    public void TryGetValueDoesNotFindMissingTable()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([schema.Keys.First()])
            );

        Assert.False(dataSet.TryGetValue(schema.Keys.Last(), out IStoredTableDataSet? _));
    }

    [Fact]
    public void ExposesConnectionAndSchemaOfWrappedDataSet()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows(schema.Keys)
            );

        Assert.Same(schema.Connection, dataSet.Connection);
        Assert.Same(schema.Schema, dataSet.Schema);
    }

    [Fact]
    public void NonGenericEnumerationYieldsAllPairs()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();

        IEnumerable dataSet = new PostgreSqlStoredSchemaDataSetWithDeletedRows(
            schema,
            new TablesAndTheirRandomRows(schema.Keys)
        );

        Assert.Equal(schema.Count, new NonGenericItems(dataSet).Count());
    }

    [Fact]
    public void TableDataSetExposesWrappedTableSchema()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.Same(schema[table].TableSchema, dataSet[table].TableSchema);
    }

    [Fact]
    public void TableDataSetExposesWrappedElementType()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.Equal(schema[table].ElementType, dataSet[table].ElementType);
    }

    [Fact]
    public void TableDataSetExposesWrappedQueryableMembers()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        IStoredTableDataSet tableDataSet = dataSet[table];

        Assert.Same(schema[table].Expression, tableDataSet.Expression);
        Assert.Same(schema[table].Provider, tableDataSet.Provider);
        Assert.Equal(ExistingRowsCount, tableDataSet.AsQueryable().Count());
    }

    [Fact]
    public void TableDataSetEnumeratesWrappedRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        Assert.Equal(ExistingRowsCount, dataSet[table].AsEnumerable().Count());
    }

    [Fact]
    public void TableDataSetNonGenericEnumerationYieldsWrappedRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        IEnumerable tableDataSet = dataSet[table];

        Assert.Equal(ExistingRowsCount, new NonGenericItems(tableDataSet).Count());
    }

    [Fact]
    public async Task TableDataSetEnumeratesWrappedRowsAsynchronously()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                new TablesAndTheirRandomRows([table])
            );

        IRow[] rows = await dataSet[table].ToArrayAsync();

        Assert.Equal(ExistingRowsCount, rows.Length);
    }

    [Fact]
    public async Task AsynchronousEnumerationDeletesRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture!.Seed(table, rows);

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                [new Grouping(table, rows)]
            );

        _ = await dataSet[table].ToArrayAsync();

        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void SynchronousEnumerationDeletesRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture!.Seed(table, rows);

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                [new Grouping(table, rows)]
            );

        Assert.Equal(ExistingRowsCount, dataSet[table].AsEnumerable().Count());
        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void QueryableMembersDeleteRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture!.Seed(table, rows);

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                [new Grouping(table, rows)]
            );

        _ = dataSet[table].Provider;

        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void ExpressionDeletesRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture!.Seed(table, rows);

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                [new Grouping(table, rows)]
            );

        _ = dataSet[table].Expression;

        Assert.Equal(0, _fixture.RowCount(table));
    }

    [Fact]
    public void ElementTypeDeletesRows()
    {
        IPostgreSqlStoredSchemaDataSet schema = Schema();
        ITable table = schema.Keys.First();
        IRow[] rows = [.. new RandomRowsForTable(table)];
        _fixture!.Seed(table, rows);

        IPostgreSqlStoredSchemaDataSet dataSet =
            new PostgreSqlStoredSchemaDataSetWithDeletedRows(
                schema,
                [new Grouping(table, rows)]
            );

        _ = dataSet[table].ElementType;

        Assert.Equal(0, _fixture.RowCount(table));
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

    private FakeStoredSchemaDataSet Schema()
    {
        return new FakeStoredSchemaDataSet(
            _fixture!.Schema,
            _fixture.Connection,
            table => [.. new DefaultRowsForTable(table).Take(ExistingRowsCount)]
        );
    }
}
