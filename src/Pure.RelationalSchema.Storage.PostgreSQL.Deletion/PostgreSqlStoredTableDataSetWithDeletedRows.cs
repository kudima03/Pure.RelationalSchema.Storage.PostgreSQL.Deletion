using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Pure.Primitives.Abstractions.Bool;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Bool;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.HashCodes;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Deletion;

internal sealed record PostgreSqlStoredTableDataSetWithDeletedRows : IStoredTableDataSet
{
    private readonly IStoredTableDataSet _dataSet;

    private readonly Lazy<IBool> _rowsDeleted;

    public PostgreSqlStoredTableDataSetWithDeletedRows(
        IStoredTableDataSet dataSet,
        IDbConnection connection,
        IString schemaName,
        IEnumerable<IRow> rows
    )
    {
        _dataSet = dataSet;
        _rowsDeleted = new Lazy<IBool>(() =>
        {
            IReadOnlyCollection<IRow> rowsToDelete =
            [
                .. rows.DistinctBy(x => new HexString(new RowHash(x)).TextValue),
            ];

            if (rowsToDelete.Count == 0 || !dataSet.TableSchema.Columns.Any())
            {
                return new True();
            }

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = new DeleteStatement(
                dataSet.TableSchema,
                schemaName,
                rowsToDelete
            ).TextValue;
            _ = command.ExecuteNonQuery();
            return new True();
        });
    }

    public ITable TableSchema =>
        _rowsDeleted.Value.BoolValue
            ? _dataSet.TableSchema
            : throw new ArgumentException("Value was not deleted.");

    public Type ElementType =>
        _rowsDeleted.Value.BoolValue
            ? _dataSet.ElementType
            : throw new ArgumentException("Value was not deleted.");

    public Expression Expression =>
        _rowsDeleted.Value.BoolValue
            ? _dataSet.Expression
            : throw new ArgumentException("Value was not deleted.");

    public IQueryProvider Provider =>
        _rowsDeleted.Value.BoolValue
            ? _dataSet.Provider
            : throw new ArgumentException("Value was not deleted.");

    public IEnumerator<IRow> GetEnumerator()
    {
        return _rowsDeleted.Value.BoolValue
            ? _dataSet.GetEnumerator()
            : throw new ArgumentException("Value was not deleted.");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IAsyncEnumerator<IRow> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        return _rowsDeleted.Value.BoolValue
            ? _dataSet.GetAsyncEnumerator(cancellationToken)
            : throw new ArgumentException("Value was not deleted.");
    }
}
