using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Request.Application.Commands.ProcessLisExport;

internal static class LisSettingsExtensions
{
    public static LisAutoExportTrigger GetAutomaticSampleExportMode(this IOptionsSnapshot<LisSettings> lisSettings, int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;

        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return LisAutoExportTrigger.NotUsed;
        }

        var channel = channels[lisChannelId];
        var sampleSettings = channel.ExportSettings.Sample;

        return sampleSettings?.AutomaticSampleExport ?? LisAutoExportTrigger.NotUsed;
    }

    public static IncludeOtherTestsInAutomaticExports GetIncludeOtherTests(this IOptionsSnapshot<LisSettings> lisSettings, int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;

        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return IncludeOtherTestsInAutomaticExports.NotUsed;
        }

        var channel = channels[lisChannelId];
        var sampleSettings = channel.ExportSettings?.Sample;

        return sampleSettings?.IncludeOtherTestsInAutomaticExports ?? IncludeOtherTestsInAutomaticExports.NotUsed;
    }

    public static WhatToExport GetWhatToExport(this IOptionsSnapshot<LisSettings> lisSettings, int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;

        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return WhatToExport.AllStatuses;
        }

        var channel = channels[lisChannelId];
        var sampleSettings = channel.ExportSettings.Sample;

        return sampleSettings?.WhatToExport ?? WhatToExport.AllStatuses;
    }


    public static bool IncludeLocallyEnteredSamples(this IOptionsSnapshot<LisSettings> lisSettings, int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;

        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return false;
        }

        var channel = channels[lisChannelId];
        var sampleSettings = channel.ExportSettings?.Sample;

        return sampleSettings?.IncludeLocallyEnteredSamples ?? false;
    }

    public static bool ShouldExportTestsInAlphabeticalOrder(this IOptionsSnapshot<LisSettings> lisSettings, int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;
        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return false;
        }

        var sampleSettings = channels[lisChannelId].ExportSettings?.Sample;
        return sampleSettings?.ExportTestsInAlphabeticalOrder ?? false;
    }

    public static bool IncludeRejectedTestsForRetestedSamples(this IOptionsSnapshot<LisSettings> lisSettings, int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;
        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return false;
        }
        var sampleSettings = channels[lisChannelId].ExportSettings?.Sample;
        return sampleSettings?.DuringRetestIncludeRejectedResults ?? false;
    }

    public static bool IncludeNewTestsForRetestedSamples(this IOptionsSnapshot<LisSettings> lisSettings, int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;
        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return false;
        }
        var sampleSettings = channels[lisChannelId].ExportSettings?.Sample;
        return sampleSettings?.DuringRetestIncludeNewResults ?? false;
    }
}
