using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Services;

internal sealed class SampleQcIdProvider : ISampleQcIdProvider
{
    private readonly IDateTime _dateTime;
    private readonly SpecialSampleHandlingSettings _settings;
    private readonly ISender _sender;

    public SampleQcIdProvider(IOptionsSnapshot<SpecialSampleHandlingSettings> settings, IDateTime dateTime, ISender sender)
    {
        _dateTime = dateTime;
        _settings = settings.Value;
        _sender = sender;
    }

    public async Task<SampleQcTubeKey> GetSampleQcTubeKey(string? tubeBarcode)
    {
        return await GetSampleQcTubeKey(tubeBarcode, string.Empty);
    }

    public async Task<SampleQcTubeKey> GetSampleQcTubeKey(string? tubeBarcode, string instrumentNumber)
    {
        var sampleQcId = await GetSampleQcId(tubeBarcode, instrumentNumber);

        //no barcode found
        if (sampleQcId.UnreadableBarcode)
        {
            return new SampleQcTubeKey(tubeBarcode, sampleQcId.sampleType, sampleQcId.Id, $"{_dateTime.Now.Ticks:X}", true, sampleQcId.TubeNeeded, string.Empty, -1);
        }

        if (sampleQcId.sampleType == SampleType.QualityControl)
        {
            return new SampleQcTubeKey(tubeBarcode, sampleQcId.sampleType, sampleQcId.Id, $"{_dateTime.Now.Ticks:X}", false, sampleQcId.TubeNeeded, sampleQcId.QcLotNumber, sampleQcId.TubeTypeId);
        }

        var unreadableBarcode = false;
        var tubeIdPart = string.Empty;

        if (_settings.SampleTubesWithSameSampleId)
        {
            if (_settings is { ExtractSampleIdSuffixFromBarcode: true, ExtractSampleIdFromBarcode: true }) //should not look at sample id suffix if sample id is not extracted
            {
                // User enters position count starting from 1, but Substring uses 0-based index so subtract 1
                var startIndex = _settings.SampleIdSuffixStartPosition - 1;
                var length = Math.Max(0, Math.Min(_settings.SampleIdSuffixLength, tubeBarcode!.Length - startIndex));

                if (length > 0)
                {
                    tubeIdPart = tubeBarcode.Substring(startIndex, length);
                }
                else
                {
                    unreadableBarcode = true;
                    tubeIdPart = $"{_dateTime.Now.Ticks:X}";
                }
            }
            else
            {
                tubeIdPart = $"{_dateTime.Now.Ticks:X}";
            }
        }

        return new SampleQcTubeKey(tubeBarcode, sampleQcId.sampleType, sampleQcId.Id, tubeIdPart, unreadableBarcode, true, string.Empty, -1);
    }

    public SampleQcTubeKey GetSampleTubeKeyNoBarcode(string sampleId)
    {
        if (string.IsNullOrWhiteSpace(sampleId))
        {
            throw new ArgumentException(null, nameof(sampleId));
        }

        var tubeIdPart = string.Empty;
        if (!_settings.SampleTubesWithSameSampleId)
        {
            tubeIdPart = $"{_dateTime.Now.Ticks:X}";
        }

        return new SampleQcTubeKey(string.Empty, SampleType.Sample, sampleId, tubeIdPart, false, true, string.Empty, -1);
    }

    private async Task<(string Id, bool UnreadableBarcode, SampleType sampleType, bool TubeNeeded, string QcLotNumber, int TubeTypeId)> GetSampleQcId(string? tubeBarcode, string instrumentNumber)
    {
        var unreadableBarcode = string.IsNullOrWhiteSpace(tubeBarcode);
        if (unreadableBarcode)
        {
            return (string.Empty, true, SampleType.Sample, false, string.Empty, -1);
        }

        var qcResult = await _sender.Send(new IsQualityControlQuery(tubeBarcode!, instrumentNumber));
        if (qcResult.IsQualityControl)
        {
            return (qcResult.QualityControlIdentity, false, SampleType.QualityControl, qcResult.IsNeeded, qcResult.LotNumber, qcResult.TubeTypeId);
        }

        var id = tubeBarcode!;
        if (_settings.ExtractSampleIdFromBarcode)
        {
            // User enters position count starting from 1, but Substring uses 0-based index so subtract 1
            var startIndex = _settings.SampleIdStartPosition - 1;
            var length = Math.Max(0, Math.Min(_settings.SampleIdLength, tubeBarcode!.Length - startIndex));

            if (length > 0)
            {
                id = tubeBarcode.Substring(startIndex, length);
            }
            else
            {
                id = string.Empty;
                unreadableBarcode = true;
            }
        }

        return (id, unreadableBarcode, SampleType.Sample, true, string.Empty, -1);
    }
}