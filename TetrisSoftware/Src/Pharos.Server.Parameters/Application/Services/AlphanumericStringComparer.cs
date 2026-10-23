using System.Globalization;

namespace Pharos.Server.Parameters.Application.Services;

internal class AlphanumericStringComparer(bool caseSensitive = false, bool descending = false) : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null)
        {
            return y == null ? 0 : -1;
        }

        if (y == null)
        {
            return 1;
        }

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            var chX = x[i];
            var chY = y[j];

            if ((char.IsDigit(chX) || chX == '.') && (char.IsDigit(chY) || chY == '.'))
            {
                var numX = ParseDecimalToken(x, ref i);
                var numY = ParseDecimalToken(y, ref j);

                var numResult = numX.CompareTo(numY);
                if (numResult != 0)
                {
                    return descending ? -numResult : numResult;
                }
            }
            else
            {
                var cx = caseSensitive ? chX : char.ToLowerInvariant(chX);
                var cy = caseSensitive ? chY : char.ToLowerInvariant(chY);

                var charResult = cx.CompareTo(cy);
                if (charResult != 0)
                {
                    return descending ? -charResult : charResult;
                }

                i++;
                j++;
            }
        }

        var lengthResult = (x.Length - i).CompareTo(y.Length - j);
        return descending ? -lengthResult : lengthResult;
    }

    private static decimal ParseDecimalToken(string s, ref int index)
    {
        var start = index;
        var hasDot = false;

        while (index < s.Length && (char.IsDigit(s[index]) || (s[index] == '.' && !hasDot)))
        {
            if (s[index] == '.')
            {
                hasDot = true;
            }

            index++;
        }

        var token = s.Substring(start, index - start);

        return decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
