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

    private readonly IEnumerable<IColumn> _columns;

    private readonly IEnumerable<IRow> _rows;

    public DeleteStatement(ITable table, IString schemaName, IEnumerable<IRow> rows)
        : this(new HexString(new TableHash(table)), schemaName, table.Columns, rows)
    { }

    public DeleteStatement(
        IString tableName,
        IString schemaName,
        IEnumerable<IColumn> columns,
        IEnumerable<IRow> rows
    )
    {
        _tableName = tableName;
        _schemaName = schemaName;
        _columns = columns;
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
            new WrappedString(
                new LeftRoundBracketString(),
                new JoinedString(
                    new ConcatenatedString(new CommaString(), new WhitespaceString()),
                    _columns.Select(x => new WrappedString(
                        new DoubleQuoteString(),
                        new TrimmedHash(new HexString(new ColumnHash(x)))
                    ))
                ),
                new RightRoundBracketString()
            ),
            new String("IN"),
            new ConcatenatedString(
                new WrappedString(
                    new LeftRoundBracketString(),
                    new JoinedString(
                        new ConcatenatedString(new CommaString(), new WhitespaceString()),
                        _rows.Select(row => new WrappedString(
                            new LeftRoundBracketString(),
                            new JoinedString(
                                new ConcatenatedString(
                                    new CommaString(),
                                    new WhitespaceString()
                                ),
                                _columns.Select(column => new WrappedString(
                                    new SingleQuoteString(),
                                    new SingleQuoteEscapedString(
                                        row.Cells.First(cell =>
                                            new HexString(
                                                new ColumnHash(cell.Key)
                                            ).TextValue
                                            == new HexString(
                                                new ColumnHash(column)
                                            ).TextValue
                                        ).Value.Value
                                    )
                                ))
                            ),
                            new RightRoundBracketString()
                        ))
                    ),
                    new RightRoundBracketString()
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
