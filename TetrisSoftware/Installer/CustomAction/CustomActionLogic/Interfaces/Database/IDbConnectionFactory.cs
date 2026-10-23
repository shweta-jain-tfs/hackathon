using System.Data;

namespace CustomAction.CustomActionLogic.Interfaces.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection(string connectionString);
}