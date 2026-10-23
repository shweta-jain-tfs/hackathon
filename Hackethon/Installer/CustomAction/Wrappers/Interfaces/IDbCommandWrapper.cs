using System;

namespace CustomAction.Wrappers.Interfaces;

public interface IDbCommandWrapper : IDisposable
{
    void SetCommandTimeout(int timeout);
    void AddParameter(string parameterName, string value);
    bool ExecuteScalar();
    void ExecuteNonQuery();
}