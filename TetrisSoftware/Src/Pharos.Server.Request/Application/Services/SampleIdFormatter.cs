using System.Globalization;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Request.Application.Services;

public class SampleIdFormatter : ISampleIdFormatter
{
    private readonly IOptionsMonitor<RequestSettings> _requestSettings;

    public SampleIdFormatter(IOptionsMonitor<RequestSettings> requestSettings)
    {
        _requestSettings = requestSettings;
    }

    public string Format(string sampleId, DateTimeOffset? created)
    {
        if (!_requestSettings.CurrentValue.ReuseSampleId || !created.HasValue)
        {
            return sampleId;
        }

        const int dateLength = 6; // "yyMMdd" is always 6 chars
        return string.Create(sampleId.Length + 1 + dateLength, (sampleId, Created: created.Value), static (span, state) =>
        {
            state.sampleId.AsSpan().CopyTo(span);
            span[state.sampleId.Length] = '-';
            state.Created.TryFormat(span[(state.sampleId.Length + 1)..], out _, "yyMMdd", CultureInfo.InvariantCulture);
        });
    }

    public string Format(RequestUid requestUid)
    {
        return Format(requestUid.SampleId, requestUid.RequestCreatedDate);
    }
}