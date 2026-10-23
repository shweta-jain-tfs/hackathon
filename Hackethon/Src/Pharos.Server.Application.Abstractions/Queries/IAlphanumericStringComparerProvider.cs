namespace Pharos.Server.Application.Queries;

public interface IAlphanumericStringComparerProvider
{
    IComparer<string> GetComparer(bool caseSensitive = false, bool descending = false);
}

