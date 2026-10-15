using System.Collections;
using Pure.Collections.Generic;
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
    public void ProducesDeleteWithTupleInForEveryRow()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [First, Second],
            [Row(("a", "1")), Row(("b", "2"))]
        );

        Assert.Equal(
            $"DELETE FROM \"{Trimmed(SchemaName)}\".\"{Trimmed(TableName)}\" WHERE ("
                + $"\"{Hash(First)}\", \"{Hash(Second)}\""
                + ") IN (('a', '1'), ('b', '2'));",
            statement.TextValue
        );
    }

    [Fact]
    public void ProducesSingleTupleForSingleRow()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [First, Second],
            [Row(("a", "1"))]
        );

        Assert.EndsWith(") IN (('a', '1'));", statement.TextValue);
    }

    [Fact]
    public void OrdersValuesByColumnsRegardlessOfCellOrder()
    {
        IRow row = new Row(
            new Dictionary<KeyValuePair<IColumn, string>, IColumn, ICell>(
                [
                    new KeyValuePair<IColumn, string>(Second, "1"),
                    new KeyValuePair<IColumn, string>(First, "a"),
                ],
                x => x.Key,
                x => new Cell(new String(x.Value)),
                x => new ColumnHash(x)
            )
        );

        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [First, Second],
            [row]
        );

        Assert.EndsWith("(('a', '1'));", statement.TextValue);
    }

    [Fact]
    public void ThrowsWhenRowMissesColumn()
    {
        IRow row = new Row(
            new Dictionary<KeyValuePair<IColumn, string>, IColumn, ICell>(
                [new KeyValuePair<IColumn, string>(First, "a")],
                x => x.Key,
                x => new Cell(new String(x.Value)),
                x => new ColumnHash(x)
            )
        );

        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [First, Second],
            [row]
        );

        _ = Assert.Throws<InvalidOperationException>(() => statement.TextValue);
    }

    [Fact]
    public void EscapesSingleQuotesInValues()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [First, Second],
            [Row(("it's", "'")), Row(("''", "a'b'c"))]
        );

        Assert.EndsWith(
            "(('it''s', ''''), ('''''', 'a''b''c'));",
            statement.TextValue
        );
    }

    [Fact]
    public void TrimsIdentifiersToPostgreSqlLimit()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(new string('t', 100)),
            new String(new string('s', 100)),
            [First, Second],
            [Row(("a", "1"))]
        );

        Assert.StartsWith(
            $"DELETE FROM \"{new string('s', 63)}\".\"{new string('t', 63)}\" WHERE",
            statement.TextValue
        );
    }

    [Fact]
    public void UsesHashOfTableAndItsColumns()
    {
        ITable table = new Table(new String("Test"), [First, Second], []);

        DeleteStatement statement = new DeleteStatement(
            table,
            new String(SchemaName),
            [Row(("a", "1"))]
        );

        Assert.Equal(
            $"DELETE FROM \"{Trimmed(SchemaName)}\"."
                + $"\"{Hash(new TableHash(table))}\" WHERE ("
                + $"\"{Hash(First)}\", \"{Hash(Second)}\""
                + ") IN (('a', '1'));",
            statement.TextValue
        );
    }

    [Fact]
    public void EnumeratesCharactersOfText()
    {
        DeleteStatement statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [First, Second],
            [Row(("a", "1"))]
        );

        Assert.Equal(statement.TextValue, Text(statement));
    }

    [Fact]
    public void EnumeratesCharactersOfTextNonGenerically()
    {
        IEnumerable statement = new DeleteStatement(
            new String(TableName),
            new String(SchemaName),
            [First, Second],
            [Row(("a", "1"))]
        );

        Assert.Equal(
            ((DeleteStatement)statement).TextValue,
            Text(new NonGenericItems(statement).Cast<IChar>())
        );
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

    private static IRow Row((string First, string Second) values)
    {
        return new Row(
            new Dictionary<KeyValuePair<IColumn, string>, IColumn, ICell>(
                [
                    new KeyValuePair<IColumn, string>(First, values.First),
                    new KeyValuePair<IColumn, string>(Second, values.Second),
                ],
                x => x.Key,
                x => new Cell(new String(x.Value)),
                x => new ColumnHash(x)
            )
        );
    }
}
