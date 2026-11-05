using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests;

using Column = Column.Column;
using Table = Table.Table;

public sealed record DeleteStatementTests
{
    private static readonly string TableName = new string('t', 64);

    private static readonly string SchemaName = new string('s', 64);

    private static readonly IColumn First = new Column(
        new String("First"),
        new StringColumnType()
    );

    private static readonly IColumn Second = new Column(
        new String("Second"),
        new IntColumnType()
    );

    [Fact]
    public void ProducesDeleteWithConjunctionForEveryRow()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "a"), (Second, "1")), Cells((First, "b"), (Second, "2"))]
        );

        Assert.Equal(
            $"DELETE FROM \"{Trimmed(SchemaName)}\".\"{Trimmed(TableName)}\" WHERE "
                + $"(\"{Hash(First)}\" = 'a' AND \"{Hash(Second)}\" = '1')"
                + $" OR (\"{Hash(First)}\" = 'b' AND \"{Hash(Second)}\" = '2');",
            statement.TextValue
        );
    }

    [Fact]
    public void ProducesSingleConjunctionForSingleRow()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "a"), (Second, "1"))]
        );

        Assert.EndsWith(
            $" WHERE (\"{Hash(First)}\" = 'a' AND \"{Hash(Second)}\" = '1');",
            statement.TextValue
        );
    }

    [Fact]
    public void MatchesOnlySuppliedCells()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((Second, "1"))]
        );

        Assert.EndsWith($" WHERE (\"{Hash(Second)}\" = '1');", statement.TextValue);
    }

    [Fact]
    public void CombinesRowsWithDifferentColumns()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "a")), Cells((First, "b"), (Second, "2")), Cells((Second, "3"))]
        );

        Assert.EndsWith(
            $" WHERE (\"{Hash(First)}\" = 'a')"
                + $" OR (\"{Hash(First)}\" = 'b' AND \"{Hash(Second)}\" = '2')"
                + $" OR (\"{Hash(Second)}\" = '3');",
            statement.TextValue
        );
    }

    [Fact]
    public void MatchesEveryRowForRowWithoutCells()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells()]
        );

        Assert.Equal(
            $"DELETE FROM \"{Trimmed(SchemaName)}\".\"{Trimmed(TableName)}\""
                + " WHERE (TRUE);",
            statement.TextValue
        );
    }

    [Fact]
    public void CombinesRowWithoutCellsWithOtherRows()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "a")), Cells()]
        );

        Assert.EndsWith(
            $" WHERE (\"{Hash(First)}\" = 'a') OR (TRUE);",
            statement.TextValue
        );
    }

    [Fact]
    public void KeepsSuppliedOrderOfCells()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((Second, "1"), (First, "a"))]
        );

        Assert.EndsWith(
            $" WHERE (\"{Hash(Second)}\" = '1' AND \"{Hash(First)}\" = 'a');",
            statement.TextValue
        );
    }

    [Fact]
    public void KeepsSuppliedOrderOfRows()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "b")), Cells((First, "a"))]
        );

        Assert.EndsWith(
            $" WHERE (\"{Hash(First)}\" = 'b') OR (\"{Hash(First)}\" = 'a');",
            statement.TextValue
        );
    }

    [Fact]
    public void EscapesSingleQuotesInValues()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "it's"), (Second, "'")), Cells((First, "''"), (Second, "a'b'c"))]
        );

        Assert.EndsWith(
            $" WHERE (\"{Hash(First)}\" = 'it''s' AND \"{Hash(Second)}\" = '''')"
                + $" OR (\"{Hash(First)}\" = '''''' AND \"{Hash(Second)}\" = 'a''b''c');",
            statement.TextValue
        );
    }

    [Fact]
    public void TrimsIdentifiersToPostgreSqlLimit()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(new string('t', 100)),
            new String(new string('s', 100)),
            [Cells((First, "a"))]
        );

        Assert.StartsWith(
            $"DELETE FROM \"{new string('s', 63)}\".\"{new string('t', 63)}\" WHERE",
            statement.TextValue
        );
    }

    [Fact]
    public void UsesHashOfTableAndColumnsOfCells()
    {
        ITable table = new Table(new String("Test"), [First, Second], []);

        DeleteStatement statement = new DeleteStatement(
            table,
            new String(SchemaName),
            [Cells((First, "a"), (Second, "1"))]
        );

        Assert.Equal(
            $"DELETE FROM \"{Trimmed(SchemaName)}\"."
                + $"\"{Hash(new TableHash(table))}\" WHERE "
                + $"(\"{Hash(First)}\" = 'a' AND \"{Hash(Second)}\" = '1');",
            statement.TextValue
        );
    }

    [Fact]
    public void EnumeratesCharactersOfText()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "a"))]
        );

        Assert.Equal(statement.TextValue, Text(statement));
    }

    [Fact]
    public void EnumeratesCharactersOfTextNonGenerically()
    {
        IEnumerable statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [Cells((First, "a"))]
        );

        Assert.Equal(
            ((DeleteStatement)statement).TextValue,
            Text(new NonGenericItems(statement).Cast<IChar>())
        );
    }

    [Fact]
    public void CellEqualityComparesTrimmedColumnWithQuotedValue()
    {
        CellEquality equality = new CellEquality(
            new String(new string('c', 64)),
            new String("it's")
        );

        Assert.Equal($"\"{new string('c', 63)}\" = 'it''s'", equality.TextValue);
    }

    [Fact]
    public void CellEqualityEnumeratesCharactersOfText()
    {
        CellEquality equality = new CellEquality(Cells((First, "a")).Single());

        Assert.Equal($"\"{Hash(First)}\" = 'a'", Text(equality));
    }

    [Fact]
    public void CellEqualityEnumeratesCharactersOfTextNonGenerically()
    {
        IEnumerable equality = new CellEquality(Cells((Second, "1")).Single());

        Assert.Equal(
            $"\"{Hash(Second)}\" = '1'",
            Text(new NonGenericItems(equality).Cast<IChar>())
        );
    }

    [Fact]
    public void CellsConjunctionEnumeratesCharactersOfText()
    {
        CellsConjunction conjunction = new CellsConjunction(
            Cells((First, "a"), (Second, "1"))
        );

        Assert.Equal(
            $"(\"{Hash(First)}\" = 'a' AND \"{Hash(Second)}\" = '1')",
            Text(conjunction)
        );
    }

    [Fact]
    public void CellsConjunctionEnumeratesCharactersOfTextNonGenerically()
    {
        IEnumerable conjunction = new CellsConjunction(Cells());

        Assert.Equal("(TRUE)", Text(new NonGenericItems(conjunction).Cast<IChar>()));
    }

    [Fact]
    public void EscapedStringEnumeratesCharactersOfText()
    {
        IEnumerable escaped = new SingleQuoteEscapedString(new String("it's"));

        Assert.Equal("it''s", Text(new NonGenericItems(escaped).Cast<IChar>()));
    }

    [Fact]
    public void EscapedStringEnumeratesCharactersOfTextGenerically()
    {
        SingleQuoteEscapedString escaped = new SingleQuoteEscapedString(new String("'"));

        Assert.Equal("''", Text(escaped));
    }

    [Fact]
    public void TrimmedHashEnumeratesCharactersOfText()
    {
        IEnumerable trimmed = new TrimmedHash(new String(new string('x', 70)));

        Assert.Equal(
            new string('x', 63),
            Text(new NonGenericItems(trimmed).Cast<IChar>())
        );
    }

    [Fact]
    public void TrimmedHashEnumeratesCharactersOfTextGenerically()
    {
        TrimmedHash trimmed = new TrimmedHash(new String(new string('y', 63)));

        Assert.Equal(new string('y', 63), Text(trimmed));
    }

    private static string Text(IEnumerable<IChar> chars)
    {
        return new string([.. chars.Select(x => x.CharValue)]);
    }

    private static string Trimmed(string name)
    {
        return name[..63];
    }

    private static string Hash(IColumn column)
    {
        return Hash(new ColumnHash(column));
    }

    private static string Hash(Pure.HashCodes.Abstractions.IDeterminedHash hash)
    {
        string hex = new HexString(hash).TextValue;
        return hex[..Math.Min(63, hex.Length)];
    }

    private static IEnumerable<KeyValuePair<IColumn, ICell>> Cells(
        params (IColumn Column, string Value)[] cells
    )
    {
        return
        [
            .. cells.Select(x => new KeyValuePair<IColumn, ICell>(
                x.Column,
                new Cell(new String(x.Value))
            )),
        ];
    }
}
