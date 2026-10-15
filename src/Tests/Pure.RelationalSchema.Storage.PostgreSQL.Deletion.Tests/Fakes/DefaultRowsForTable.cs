using System.Collections;
using Pure.Collections.Generic;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

internal sealed record DefaultRowsForTable : IEnumerable<IRow>
{
    private readonly IEnumerable<IRow> _rows;

    public DefaultRowsForTable(ITable table)
    {
        _rows = Enumerable
            .Range(0, 100)
            .Select(_ => new Row(
                new Dictionary<IColumn, IColumn, ICell>(
                    table.Columns,
                    x => x,
                    x => new Cell(new DefaultValueForColumnType(x.Type)),
                    x => new ColumnHash(x)
                )
            ));
    }

    public IEnumerator<IRow> GetEnumerator()
    {
        return _rows.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
