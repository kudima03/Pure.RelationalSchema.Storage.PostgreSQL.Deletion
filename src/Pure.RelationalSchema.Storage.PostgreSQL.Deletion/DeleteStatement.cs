using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion;

internal sealed record DeleteStatement : IString
{
    private readonly IString _tableName;

    private readonly IString _schemaName;

    private readonly IEnumerable<IEnumerable<KeyValuePair<IColumn, ICell>>> _rows;

    public DeleteStatement(
        ITable table,
        IString schemaName,
        IEnumerable<IEnumerable<KeyValuePair<IColumn, ICell>>> rows
    )
        : this(new HexString(new TableHash(table)), schemaName, rows)
    { }

    public DeleteStatement(
        IString tableName,
        IString schemaName,
        IEnumerable<IEnumerable<KeyValuePair<IColumn, ICell>>> rows
    )
    {
        _tableName = tableName;
        _schemaName = schemaName;
        _rows = rows;
    }

    public string TextValue =>
        new WhitespaceJoinedString(
            new String("DELETE FROM"),
            new JoinedString(
                new DotString(),
                [
                    new WrappedString(
                        new DoubleQuoteString(),
                        new TrimmedHash(_schemaName)
                    ),
                    new WrappedString(
                        new DoubleQuoteString(),
                        new TrimmedHash(_tableName)
                    ),
                ]
            ),
            new String("WHERE"),
            new ConcatenatedString(
                new JoinedString(
                    new ConcatenatedString(
                        new WhitespaceString(),
                        new String("OR"),
                        new WhitespaceString()
                    ),
                    _rows.Select(x => new CellsConjunction(x))
                ),
                new SemicolonString()
            )
        ).TextValue;

    public IEnumerator<IChar> GetEnumerator()
    {
        return TextValue.Select(x => new Char(x)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
