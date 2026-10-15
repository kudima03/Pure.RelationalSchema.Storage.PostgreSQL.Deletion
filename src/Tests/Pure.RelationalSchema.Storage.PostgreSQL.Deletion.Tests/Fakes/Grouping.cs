using System.Collections;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

internal sealed record Grouping : IGrouping<ITable, IRow>
{
    private readonly IEnumerable<IRow> _rows;

    public Grouping(ITable key, IEnumerable<IRow> rows)
    {
        Key = key;
        _rows = rows;
    }

    public ITable Key { get; }

    public IEnumerator<IRow> GetEnumerator()
    {
        return _rows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
