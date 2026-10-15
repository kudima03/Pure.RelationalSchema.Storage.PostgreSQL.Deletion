using System.Collections;
using System.Globalization;
using Pure.HashCodes;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Number;
using Pure.Primitives.Random.Bool;
using Pure.Primitives.Random.Number;
using Pure.Primitives.Random.String;
using Pure.Primitives.String.Operations;
using Pure.Primitives.Switches.String;
using Pure.RelationalSchema.Abstractions.ColumnType;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

internal sealed record RandomValueForColumnType : IString
{
    private readonly IColumnType _columnType;

    public RandomValueForColumnType(IColumnType columnType)
    {
        _columnType = columnType;
    }

    public string TextValue =>
        new StringSwitch<IColumnType>(
            _columnType,
            [
                new KeyValuePair<IColumnType, IString>(
                    new BoolColumnType(),
                    new String(new RandomBool())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new IntColumnType(),
                    new String(new RandomInt())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new LongColumnType(),
                    new String(new RandomUInt())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UShortColumnType(),
                    new String(new RandomUShort())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UIntColumnType(),
                    new String(new RandomUInt())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new ULongColumnType(),
                    new String(new RandomUInt())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new StringColumnType(),
                    new RandomString(
                        new UShort(20),
                        new Char((char)65),
                        new Char((char)91)
                    )
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateColumnType(),
                    new String(DateTime.Now.ToString(CultureInfo.InvariantCulture))
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateTimeColumnType(),
                    new String(DateTime.Now.ToString(CultureInfo.InvariantCulture))
                ),
                new KeyValuePair<IColumnType, IString>(
                    new TimeColumnType(),
                    new String(TimeOnly.FromDateTime(DateTime.Now).ToShortTimeString())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DeterminedHashColumnType(),
                    new HexString(new DeterminedHash(new RandomString()))
                ),
            ],
            x => new ColumnTypeHash(x)
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
