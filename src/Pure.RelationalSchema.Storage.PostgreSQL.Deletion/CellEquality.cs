using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Char = Pure.Primitives.Char.Char;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion;

internal sealed record CellEquality : IString
{
    private readonly IString _columnName;

    private readonly IString _value;

    public CellEquality(KeyValuePair<IColumn, ICell> cell)
        : this(new HexString(new ColumnHash(cell.Key)), cell.Value.Value)
    { }

    public CellEquality(IString columnName, IString value)
    {
        _columnName = columnName;
        _value = value;
    }

    public string TextValue =>
        new WhitespaceJoinedString(
            new WrappedString(new DoubleQuoteString(), new TrimmedHash(_columnName)),
            new EqualsSignString(),
            new WrappedString(new SingleQuoteString(), new SingleQuoteEscapedString(_value))
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
