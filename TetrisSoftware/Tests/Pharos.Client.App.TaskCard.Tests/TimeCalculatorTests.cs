using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Localization;
using Xunit;

namespace Pharos.Client.App.TaskCard.Tests;

public class TimeCalculatorTests
{
    private readonly TimeCalculator _sut;
    
    public TimeCalculatorTests()
    {
        var translationMock = new Mock<IStringLocalizer<Translations>>();
        translationMock.Setup(x => x["TaskCard_ShortTimeUnit_Day"]).Returns(new LocalizedString(string.Empty, "d"));
        translationMock.Setup(x => x["TaskCard_ShortTimeUnit_Hour"]).Returns(new LocalizedString(string.Empty, "h"));
        translationMock.Setup(x => x["TaskCard_ShortTimeUnit_Minute"]).Returns(new LocalizedString(string.Empty, "min"));
        _sut = new TimeCalculator(translationMock.Object);
    }

    public static IEnumerable<object[]> TestData()
    {
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), null!, null!]; //no expiration set -> null
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 06, 0, 0, 1, TimeSpan.Zero), null!]; //yesterday -> null
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 07, 0, 0, 1, TimeSpan.Zero), null!]; //1 second differs -> null
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 07, 0, 0, 59, TimeSpan.Zero), null!]; //59 seconds differs -> null
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 07, 0, 1, 0, TimeSpan.Zero), "1 min"]; //1 minute differs -> 1 min
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 07, 0, 1, 59, TimeSpan.Zero), "1 min"]; //1 minute 59 seconds differs -> 1 min
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 07, 1, 0, 0, TimeSpan.Zero), "1 h"]; //1 hour differs -> 1 h
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 07, 23, 59, 0, TimeSpan.Zero), "23 h"]; //23 hours 59 minutes differs -> 23 h
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 11, 08, 0, 0, 0, TimeSpan.Zero), "1 d"]; //1 day differs -> 1 d
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 12, 24, 0, 0, 0, TimeSpan.Zero), "47 d"]; //christmas -> 47 d
        yield return [new DateTimeOffset(2024, 11, 07, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 11, 07, 0, 0, 0, TimeSpan.Zero), "365 d"]; //1 year differs -> 365 d
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public void GetDifference_Should_ReturnCorrectValues(DateTimeOffset now, DateTimeOffset? expiresAt, string? expected)
    {
        _sut.GetDifference(now, expiresAt).Should().BeEquivalentTo(expected);
    }
}
