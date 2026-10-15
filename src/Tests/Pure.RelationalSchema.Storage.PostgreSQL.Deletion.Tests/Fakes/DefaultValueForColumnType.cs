using System.Collections;
using System.Globalization;
using Pure.HashCodes;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Bool;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Pure.Primitives.Switches.String;
using Pure.RelationalSchema.Abstractions.ColumnType;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests.Fakes;

internal sealed record DefaultValueForColumnType : IString
{
    private readonly IColumnType _columnType;

    public DefaultValueForColumnType(IColumnType columnType)
    {
        _columnType = columnType;
    }

    public string TextValue =>
        new StringSwitch<IColumnType>(
            _columnType,
            [
                new KeyValuePair<IColumnType, IString>(
                    new BoolColumnType(),
                    new String(new True())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new IntColumnType(),
                    new String(int.MaxValue.ToString())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new LongColumnType(),
                    new String(uint.MaxValue.ToString())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UShortColumnType(),
                    new String(ushort.MaxValue.ToString())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UIntColumnType(),
                    new String(uint.MaxValue.ToString())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new ULongColumnType(),
                    new String(ulong.MaxValue.ToString())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new StringColumnType(),
                    new String("Hello, world!")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateColumnType(),
                    new String(DateTime.MaxValue.ToString(CultureInfo.InvariantCulture))
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateTimeColumnType(),
                    new String(DateTime.MaxValue.ToString(CultureInfo.InvariantCulture))
                ),
                new KeyValuePair<IColumnType, IString>(
                    new TimeColumnType(),
                    new String(TimeOnly.MaxValue.ToString())
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DeterminedHashColumnType(),
                    new HexString(new DeterminedHash(new EmptyString()))
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
