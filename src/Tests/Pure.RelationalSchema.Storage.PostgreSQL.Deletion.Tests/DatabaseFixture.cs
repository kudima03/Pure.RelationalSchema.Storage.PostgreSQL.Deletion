using System.Data;
using Npgsql;
using Pure.HashCodes.Abstractions;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.ColumnType;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Testcontainers.PostgreSql;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion.Tests;

using Column = Column.Column;
using Schema = Schema.Schema;
using Table = Table.Table;

public sealed record DatabaseFixture : IDisposable
{
    private const int MaxIdentifierLength = 63;

    private readonly PostgreSqlContainer _postgres;

    public IDbConnection Connection { get; }

    public ISchema Schema { get; }

    public DatabaseFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _postgres.StartAsync().GetAwaiter().GetResult();

        Connection = new NpgsqlConnection(_postgres.GetConnectionString());

        Connection.Open();

        IReadOnlyCollection<IColumn> columns1 =
        [
            new Column(new String("Column1"), new BoolColumnType()),
            new Column(new String("Column3"), new DateTimeColumnType()),
            new Column(new String("Column4"), new IntColumnType()),
            new Column(new String("Column5"), new LongColumnType()),
        ];

        IReadOnlyCollection<IColumn> columns2 =
        [
            new Column(new String("Column6"), new StringColumnType()),
            new Column(new String("Column8"), new UIntColumnType()),
            new Column(new String("Column9"), new ULongColumnType()),
            new Column(new String("Column10"), new UShortColumnType()),
            new Column(new String("Column11"), new DeterminedHashColumnType()),
        ];

        ITable table1 = new Table(new String("Test"), columns1, []);

        ITable table2 = new Table(new String("Test"), columns2, []);

        ITable table3 = new Table(new String("Empty"), [], []);

        Schema = new Schema(new String("Test"), [table1, table2, table3], []);

        Execute(
            $"CREATE SCHEMA IF NOT EXISTS \"{Trimmed(new SchemaHash(Schema))}\";"
        );

        foreach (ITable table in Schema.Tables)
        {
            Execute(
                $"CREATE TABLE IF NOT EXISTS {QualifiedName(table)} ("
                    + string.Join(
                        ", ",
                        table.Columns.Select(x =>
                            $"\"{Trimmed(new ColumnHash(x))}\" {TypeName(x.Type)} NOT NULL"
                        )
                    )
                    + ");"
            );
        }
    }

    public void Seed(ITable table, IEnumerable<IRow> rows)
    {
        foreach (IRow row in rows)
        {
            Execute(
                table.Columns.Any()
                    ? $"INSERT INTO {QualifiedName(table)} VALUES ("
                        + string.Join(
                            ", ",
                            table.Columns.Select(x =>
                                $"'{row.Cells[x].Value.TextValue.Replace("'", "''")}'"
                            )
                        )
                        + ");"
                    : $"INSERT INTO {QualifiedName(table)} DEFAULT VALUES;"
            );
        }
    }

    public long RowCount(ITable table)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM {QualifiedName(table)};";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    public void Dispose()
    {
        Connection.Dispose();
        _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static string Trimmed(IDeterminedHash hash)
    {
        string name = new HexString(hash).TextValue;
        return name[..Math.Min(MaxIdentifierLength, name.Length)];
    }

    private static string TypeName(IColumnType type)
    {
        return type switch
        {
            BoolColumnType => "boolean",
            DateTimeColumnType => "timestamp",
            IntColumnType => "integer",
            LongColumnType => "bigint",
            StringColumnType => "text",
            UIntColumnType => "bigint",
            ULongColumnType => "numeric",
            UShortColumnType => "integer",
            DeterminedHashColumnType => "bytea",
            _ => throw new NotSupportedException(),
        };
    }

    private string QualifiedName(ITable table)
    {
        return $"\"{Trimmed(new SchemaHash(Schema))}\".\"{Trimmed(new TableHash(table))}\"";
    }

    private void Execute(string sql)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText = sql;
        _ = command.ExecuteNonQuery();
    }
}
