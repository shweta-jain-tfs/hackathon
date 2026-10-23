namespace Pharos.Client.App.Identity.Helpers;

public static class ApplicationUserHelper
{
    public static bool AreAllUsernameCharactersValid(string userName)
    {
        // A HashSet provides very fast lookups (O(1) time complexity) to check if a character is allowed
        var allowedChars = new HashSet<char>(UserContract.AllowedNameCharacters);
        return userName.All(allowedChars.Contains);
    }

    private static bool IsUserNameLengthValid(string userName)
    {
        return !string.IsNullOrEmpty(userName) && userName.Length <= UserContract.MaximumNameLength;
    }

    public static bool IsUserNameValid(string userName, List<string> existingUserNames)
    {
        return IsUserNameLengthValid(userName) && !existingUserNames.Contains(userName, StringComparer.OrdinalIgnoreCase) && AreAllUsernameCharactersValid(userName);
    }
}
