using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Pure.Collections.Generic;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Abstractions;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion;

public sealed record PostgreSqlStoredSchemaDataSetWithDeletedRows
    : IPostgreSqlStoredSchemaDataSet
{
    private readonly IReadOnlyDictionary<ITable, IStoredTableDataSet> _tablesDatasets;

    public PostgreSqlStoredSchemaDataSetWithDeletedRows(
        IPostgreSqlStoredSchemaDataSet dataset,
        IEnumerable<IGrouping<ITable, IRow>> rows
    )
        : this(
            dataset.Schema,
            new OrderedDictionary<
                KeyValuePair<ITable, IStoredTableDataSet>,
                ITable,
                IStoredTableDataSet
            >(
                [
                    .. rows.Select(group => new KeyValuePair<ITable, IStoredTableDataSet>(
                        group.Key,
                        new PostgreSqlStoredTableDataSetWithDeletedRows(
                            dataset[group.Key],
                            dataset.Connection,
                            new TrimmedHash(
                                new HexString(new SchemaHash(dataset.Schema))
                            ),
                            group
                        )
                    )),
                ],
                pair => pair.Key,
                pair => pair.Value,
                table => new TableHash(table)
            ),
            dataset.Connection
        )
    { }

    private PostgreSqlStoredSchemaDataSetWithDeletedRows(
        ISchema schema,
        IReadOnlyDictionary<ITable, IStoredTableDataSet> tablesDatasets,
        IDbConnection connection
    )
    {
        _tablesDatasets = tablesDatasets;
        Connection = connection;
        Schema = schema;
    }

    public IDbConnection Connection { get; }

    public IStoredTableDataSet this[ITable key] => _tablesDatasets[key];

    public IEnumerable<ITable> Keys => _tablesDatasets.Keys;

    public IEnumerable<IStoredTableDataSet> Values => _tablesDatasets.Values;

    public ISchema Schema { get; }

    public int Count => _tablesDatasets.Count;

    public IEnumerator<KeyValuePair<ITable, IStoredTableDataSet>> GetEnumerator()
    {
        return _tablesDatasets.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool ContainsKey(ITable key)
    {
        return _tablesDatasets.ContainsKey(key);
    }

    public bool TryGetValue(
        ITable key,
        [MaybeNullWhen(false)] out IStoredTableDataSet value
    )
    {
        return _tablesDatasets.TryGetValue(key, out value);
    }
}
