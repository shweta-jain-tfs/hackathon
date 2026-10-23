using System.Data;
using System.Data.SqlClient;
using CustomAction.CustomActionLogic.Interfaces.Database;
using CustomAction.Wrappers;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.CustomActionLogic.Database;

public class DbCommandFactory : IDbCommandFactory
{
    public IDbCommandWrapper CreateCommand(string commandText, IDbConnection connection)
    {
        return new SqlCommandWrapper(new SqlCommand(commandText, (SqlConnection)connection));
    }
}