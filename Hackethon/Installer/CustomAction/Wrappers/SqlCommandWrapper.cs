using System.Data;
using System.Data.SqlClient;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.Wrappers;

public class SqlCommandWrapper(SqlCommand sqlCommand) : IDbCommandWrapper
{
    public void SetCommandTimeout(int timeout)
    {
        sqlCommand.CommandTimeout = timeout;
    }

    public void AddParameter(string parameterName, string value)
    {
        sqlCommand.Parameters.Add(parameterName, SqlDbType.NVarChar).Value = value;
    }

    public bool ExecuteScalar()
    {
        return (int)sqlCommand.ExecuteScalar() > 0;
    }

    public void ExecuteNonQuery()
    {
        sqlCommand.ExecuteNonQuery();
    }

    public void Dispose()
    {
        sqlCommand?.Dispose();
    }
}