using System.Collections;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

internal sealed record CellsGrouping
    : IGrouping<ITable, IEnumerable<KeyValuePair<IColumn, ICell>>>
{
    private readonly IEnumerable<IEnumerable<KeyValuePair<IColumn, ICell>>> _rows;

    public CellsGrouping(
        ITable key,
        IEnumerable<IEnumerable<KeyValuePair<IColumn, ICell>>> rows
    )
    {
        Key = key;
        _rows = rows;
    }

    public ITable Key { get; }

    public IEnumerator<IEnumerable<KeyValuePair<IColumn, ICell>>> GetEnumerator()
    {
        return _rows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
