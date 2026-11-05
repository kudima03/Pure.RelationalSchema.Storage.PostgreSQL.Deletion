using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Storage.Abstractions;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion;

internal sealed record CellsConjunction : IString
{
    private readonly IEnumerable<KeyValuePair<IColumn, ICell>> _cells;

    public CellsConjunction(IEnumerable<KeyValuePair<IColumn, ICell>> cells)
    {
        _cells = cells;
    }

    public string TextValue =>
        new WrappedString(
            new LeftRoundBracketString(),
            _cells.Any()
                ? new JoinedString(
                    new ConcatenatedString(
                        new WhitespaceString(),
                        new String("AND"),
                        new WhitespaceString()
                    ),
                    _cells.Select(x => new CellEquality(x))
                )
                : new String("TRUE"),
            new RightRoundBracketString()
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
