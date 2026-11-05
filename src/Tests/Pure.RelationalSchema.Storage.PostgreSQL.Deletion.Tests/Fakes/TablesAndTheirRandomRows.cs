using System.Collections;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

internal sealed record TablesAndTheirRandomRows
    : IEnumerable<IGrouping<ITable, IEnumerable<KeyValuePair<IColumn, ICell>>>>
{
    private readonly IEnumerable<
        IGrouping<ITable, IEnumerable<KeyValuePair<IColumn, ICell>>>
    > _tablesAndRows;

    public TablesAndTheirRandomRows(IEnumerable<ITable> tables)
    {
        _tablesAndRows = tables.Select(x => new CellsGrouping(
            x,
            new RandomRowsForTable(x).Select(row => row.Cells)
        ));
    }

    public IEnumerator<
        IGrouping<ITable, IEnumerable<KeyValuePair<IColumn, ICell>>>
    > GetEnumerator()
    {
        return _tablesAndRows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
