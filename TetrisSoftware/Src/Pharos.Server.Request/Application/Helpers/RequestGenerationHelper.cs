using Pharos.Server.Domain;
using Pharos.Server.Request.Application.Commands.RequestGenerator;
using Pharos.Server.Request.Application.Queries.RequestGenerator;

namespace Pharos.Server.Request.Application.Helpers;

internal static class RequestGenerationHelper
{
    public static List<string> GenerateSampleIds(GenerateRequestsDto options)
    {
        var sampleIds = new List<string>(options.AmountToGenerate);
        var formatString = $"{{0}}{{1:D{options.SampleIdNumberOfDigits}}}{{2}}";

        for (var i = 0; i < options.AmountToGenerate; i++)
        {
            var number = options.SampleIdStartOffset + i;
            sampleIds.Add(string.Format(formatString, options.SampleIdHeadingCharacters, number, options.SampleIdTrailingCharacters));
        }

        return sampleIds;
    }

    public static List<RequestGenerationTrayPlacement> GenerateTrayPlacements(GenerateRequestsDto options)
    {
        var placements = new List<RequestGenerationTrayPlacement>(options.AmountToGenerate);
        for (var sampleIndex = 0; sampleIndex < options.AmountToGenerate; sampleIndex++)
        {
            var trayIndex = sampleIndex / options.SamplesPerTray;
            var tubePosition = (sampleIndex % options.SamplesPerTray) + 1;
            placements.Add(new RequestGenerationTrayPlacement(
                (options.TraysStartOffset + trayIndex).ToString($"D{Constants.TrayIdLength}"),
                tubePosition));
        }

        return placements;
    }

    public static List<RequestPreviewDto> BuildPreview(
        List<string> sampleIds,
        GenerateRequestsDto options,
        List<RequestGenerationTrayPlacement> trayPlacements)
    {
        var previewItems = new List<RequestPreviewDto>(sampleIds.Count);
        var formattedTests = string.Join(", ", options.Tests.Select(t => t.TestName));

        for (var i = 0; i < sampleIds.Count; i++)
        {
            string? tray = null;
            string? trayPosition = null;

            if (options.PlaceSamplesInTrays)
            {
                var trayPlacement = trayPlacements[i];
                tray = trayPlacement.TrayId;
                trayPosition = ConvertPositionToGrid(trayPlacement.TubePosition);
            }

            previewItems.Add(new RequestPreviewDto
            {
                Id = i + 1,
                SampleId = sampleIds[i],
                Tests = formattedTests,
                Tray = tray,
                TrayPosition = trayPosition
            });
        }

        return previewItems;
    }

    private static string ConvertPositionToGrid(int position)
    {
        var row = (position - 1) / 10;
        var col = ((position - 1) % 10) + 1;
        var rowLetter = (char)('A' + row);
        return $"{rowLetter}{col}";
    }
}
