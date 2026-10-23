using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.TestHelpers;

internal static class LocalizerMockFactory
{
    public static Mock<IStringLocalizer<Translations>> Create()
    {
        var mock = new Mock<IStringLocalizer<Translations>>();

        mock.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        mock.Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) =>
                new LocalizedString(key, $"{key}: {string.Join(", ", args)}"));

        return mock;
    }
}
