namespace Pharos.Common.Web.Extensions;

public static class StringExtensions
{
    public static string ToLowerWithCapitalize(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var firstChar = char.ToUpper(input[0]);
        var rest = input[1..].ToLower();

        return new string($"{firstChar}{rest}");
    }

    public static (string firstPart, string secondPart) SplitOnMiddleSpace(this string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (string.Empty, string.Empty);
        }

        title = title.Trim();

        if (!title.Contains(' '))
        {
            return (title, string.Empty);
        }

        var center = title.Length / 2;

        var leftSpace = title.LastIndexOf(' ', center);
        var rightSpace = title.IndexOf(' ', center);

        var splitIndex = leftSpace == -1 ? rightSpace :
                        rightSpace == -1 ? leftSpace :
                        (center - leftSpace <= rightSpace - center) ? leftSpace : rightSpace;

        return (title[..splitIndex], title[(splitIndex + 1)..]);
    }

}