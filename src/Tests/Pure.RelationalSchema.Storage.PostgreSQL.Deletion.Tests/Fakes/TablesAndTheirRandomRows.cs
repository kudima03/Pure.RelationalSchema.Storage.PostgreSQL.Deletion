using System.Collections;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

internal sealed record TablesAndTheirRandomRows : IEnumerable<IGrouping<ITable, IRow>>
{
    private readonly IEnumerable<IGrouping<ITable, IRow>> _tablesAndRows;

    public TablesAndTheirRandomRows(IEnumerable<ITable> tables)
    {
        _tablesAndRows = tables.Select(x => new Grouping(x, new RandomRowsForTable(x)));
    }

    public IEnumerator<IGrouping<ITable, IRow>> GetEnumerator()
    {
        return _tablesAndRows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
