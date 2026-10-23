namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

public static class SampleRequestIdValidator
{
    public static bool IsValidSampleId(string? sampleId)
    {
        if (string.IsNullOrEmpty(sampleId))
        {
            return false;
        }

        if (!AllowedCharacters(sampleId))
        {
            return false;
        }

        return true;
    }

    public static bool IsValidRequestId(string? requestId)
    {
        if (string.IsNullOrEmpty(requestId))
        {
            return true;
        }

        if (!AllowedCharacters(requestId))
        {
            return false;
        }

        return true;
    }

    public static bool AllowedCharacters(string sampleId)
    {
        foreach (var character in sampleId)
        {
            var isAllowed =
                (character >= 32 && character <= 125 &&
                 character != '&' // ASCII 38
                 && character != '\\' // ASCII 92
                 && character != '^' // ASCII 94
                 && character != '|') // ASCII 124
                || (character >= 128 && character <= 254);

            if (!isAllowed)
            {
                return false;
            }
        }

        return true;
    }
}