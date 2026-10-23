using Pharos.Client.App.Identity.Helpers;
using Pharos.Client.Common.Models;

namespace Pharos.Client.Tests.Helpers;

public class PasswordHelperTests
{
    [Theory]
    [MemberData(nameof(InvalidPasswordCases))]
    public void IsPasswordValid_WhenInvalidCase_ReturnsFalse(string? password, UiSettings? uiSettings, string userName)
    {
        var result = PasswordHelper.IsPasswordValid(password!, uiSettings, userName);

        Assert.False(result);
    }

    public static IEnumerable<object?[]> InvalidPasswordCases()
    {
        yield return new object?[] { "Valid1!", null, "user" }; // ui settings null
        yield return new object?[] { null, CreateUiSettings(1, false, false, false, false), "user" }; // password null
        yield return new object?[] { string.Empty, CreateUiSettings(1, false, false, false, false), "user" }; // password empty
        yield return new object?[] { "Password!", CreateUiSettings(1, true, false, false, false), "user" }; // missing digit
        yield return new object?[] { "1", CreateUiSettings(1, false, true, false, false), "user" }; // missing upper/lower
        yield return new object?[] { "password1!", CreateUiSettings(1, false, true, false, false), "user" }; // missing uppercase
        yield return new object?[] { "PASSWORD1!", CreateUiSettings(1, false, true, false, false), "user" }; // missing lowercase
        yield return new object?[] { "Password1", CreateUiSettings(1, false, false, true, false), "user" }; // missing non-alphanumeric
        yield return new object?[] { "Pass1!", CreateUiSettings(8, false, false, false, false), "user" }; // too short
        yield return new object?[] { "MyUser123!", CreateUiSettings(1, false, false, false, true), "user" }; // contains username
        yield return new object?[] { "MyUsEr123!", CreateUiSettings(1, false, false, false, true), "user" }; // contains username case-insensitive
    }

    [Fact]
    public void IsPasswordValid_WhenOnlyDigitsAndMinimumLengthIsOne_ReturnsTrue()
    {
        var uiSettings = CreateUiSettings(requiredLength: 1, requireDigit: false, requireBothUpperAndLowerCase: false, requireNonAlphanumeric: false, requireNoUsername: false);

        var result = PasswordHelper.IsPasswordValid("1", uiSettings, "user");

        Assert.True(result);
    }

    [Fact]
    public void IsPasswordValid_WhenAllRequirementsAreMet_ReturnsTrue()
    {
        var uiSettings = CreateUiSettings(requiredLength: 8, requireDigit: true, requireBothUpperAndLowerCase: true, requireNonAlphanumeric: true, requireNoUsername: true);

        var result = PasswordHelper.IsPasswordValid("Valid1!A", uiSettings, "user");

        Assert.True(result);
    }

    private static UiSettings CreateUiSettings(
        int requiredLength,
        bool requireDigit,
        bool requireBothUpperAndLowerCase,
        bool requireNonAlphanumeric,
        bool requireNoUsername)
    {
        return new UiSettings
        {
            SecuritySettings = new SecuritySettings
            {
                PasswordRequiredLength = requiredLength,
                PasswordRequireDigit = requireDigit,
                PasswordRequiresBothUpperAndLowerCase = requireBothUpperAndLowerCase,
                PasswordRequireNonAlphanumeric = requireNonAlphanumeric,
                PasswordRequireNoUsername = requireNoUsername,
            }
        };
    }

    [Theory]
    [MemberData(nameof(ValidPasswordCases))]
    public void IsPasswordValid_WhenValidCase_ReturnsTrue(string password, UiSettings uiSettings, string userName)
    {
        var result = PasswordHelper.IsPasswordValid(password, uiSettings, userName);

        Assert.True(result);
    }

    public static IEnumerable<object[]> ValidPasswordCases()
    {
        yield return new object[] { "1", CreateUiSettings(1, false, false, false, false), "user" }; // minimum length only
        yield return new object[] { "abc123", CreateUiSettings(1, false, false, false, false), "user" }; // no complexity requirements
        yield return new object[] { "abc123", CreateUiSettings(1, true, false, false, false), "user" }; // digit required and present
        yield return new object[] { "Abcdef", CreateUiSettings(1, false, true, false, false), "user" }; // upper/lower required and present
        yield return new object[] { "Abc1!", CreateUiSettings(1, false, false, true, false), "user" }; // non-alphanumeric required and present
        yield return new object[] { "Pass1!", CreateUiSettings(6, false, false, false, false), "user" }; // exact required length
        yield return new object[] { "password-user-1", CreateUiSettings(1, false, false, false, true), "admin" }; // username restriction enabled but username not present
        yield return new object[] { "Valid1!A", CreateUiSettings(8, true, true, true, true), "user" }; // all requirements enabled
    }
}
