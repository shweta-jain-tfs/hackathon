namespace Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;

/// <summary>
/// A base36 decoder
/// </summary>
internal static class Base36
{
    /// <summary>
    /// Decode the Base36 Encoded string into an integer number.
    /// </summary>
    /// <param name="base36">The string containing base36 number.</param>
    /// <returns>The decoded integer number.</returns>
    public static int DecodeInt(string base36)
    {
        var result = 0;
        for (int i = 0, pos = base36.Length - 1; pos >= 0; i++, pos--)
        {
            var digit = base36[i];
            if (char.IsAsciiDigit(digit))
            {
                result += (digit - '0') * (int)Math.Pow(36, pos);
            }
            else if (char.IsAsciiLetterUpper(digit))
            {
                result += (digit - 'A' + 10) * (int)Math.Pow(36, pos);
            }
            else
            {
                // ReSharper disable once StringLiteralTypo
                throw new ArgumentException($"Invalid Base36 string format. Valid characters are 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            }
        }
        return result;
    }

    public static int DecodeInt(char base36Digit)
    {
        if (char.IsAsciiDigit(base36Digit))
        {
            return base36Digit - '0';
        }

        if (char.IsAsciiLetterUpper(base36Digit))
        {
            return base36Digit - 'A' + 10;
        }

        // ReSharper disable once StringLiteralTypo
        throw new ArgumentException($"Invalid Base36 digit. Valid characters are 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
    }

    public static string EncodeInt(int value)
    {
        var buffer = "";

        if (value > 1295)
        {
            value = 1295;
        }

        if (value < 0)
        {
            value = 0;
        }

        var hb = value / 36;
        var lb = value % 36;

        if (hb > 9)
        {
            buffer += (char)(hb + 'A' - 10);
        }
        else if (hb > 0)
        {
            buffer += (char)(hb + '0');
        }

        if (lb > 9)
        {
            buffer += (char)(lb + 'A' - 10);
        }
        else
        {
            buffer += (char)(lb + '0');
        }

        return buffer;
    }
}