namespace Pharos.Server.Request.Application.Interfaces;

internal interface ITestDisplayNameProvider
{
    Task<string> GetTestDisplayName(string methodName, string testName);
}