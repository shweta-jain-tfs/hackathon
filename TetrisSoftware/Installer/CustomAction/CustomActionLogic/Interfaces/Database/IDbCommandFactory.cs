using System.Data;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.CustomActionLogic.Interfaces.Database;

public interface IDbCommandFactory
{
    IDbCommandWrapper CreateCommand(string commandText, IDbConnection connection);
}