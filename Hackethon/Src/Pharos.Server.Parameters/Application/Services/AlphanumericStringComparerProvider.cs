using Pharos.Server.Application.Queries;

namespace Pharos.Server.Parameters.Application.Services;

internal class AlphanumericStringComparerProvider : IAlphanumericStringComparerProvider
{
    public IComparer<string> GetComparer(bool caseSensitive = false, bool descending = false)
    {
        return new AlphanumericStringComparer(caseSensitive, descending);
    }
}