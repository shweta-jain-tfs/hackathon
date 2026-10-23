using Pharos.Client.App.Search.Models;
using Pharos.Client.Common.Localization;
using Xunit;

namespace Pharos.Client.App.Search.Tests;

public class ClientSearchResultTypeTests
{
    [Fact]
    public void AllValuesExceptUnknown_ShouldHaveResourceStrings()
    {
        foreach (var resultType in Enum.GetValues<ClientSearchResultType>())
        {
            if (resultType == ClientSearchResultType.Unknown)
            {
                continue;
            }

            resultType.ToLocalizedString(
                "SearchListPage",
                throwIfNotFound: true);
        }
    }
}