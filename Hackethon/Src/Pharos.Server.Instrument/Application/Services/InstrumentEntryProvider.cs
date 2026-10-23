using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Pharos.Contracts;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings;

namespace Pharos.Server.Instrument.Application.Services;

/// <summary>
/// Provides instrument entries for the provider-managed PageList by querying
/// all configured instruments from <see cref="IInstrumentManager"/>.
/// Instrument clients are narrowed by request IP; desktop/auto clients see all instruments.
/// If an instrument client has no IP match, no entries are returned.
/// </summary>
internal sealed class InstrumentEntryProvider(
    IInstrumentManager instrumentManager,
    IClientDeviceService clientDeviceService,
    IHttpContextAccessor httpContextAccessor) : IPageListEntryProvider
{
    private const string _unknownInstrumentDisplayName = "Unnamed instrument";

    public Task<IReadOnlyList<PageListEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var allInstruments = instrumentManager.GetInstruments();
        if (ShouldReturnAllInstruments())
        {
            return Task.FromResult<IReadOnlyList<PageListEntry>>(ToEntries(allInstruments));
        }

        var clientIp = GetClientIp();
        var matchingInstruments = clientIp is null
            ? []
            : GetMatchingInstruments(allInstruments, clientIp);

        return Task.FromResult<IReadOnlyList<PageListEntry>>(ToEntries(matchingInstruments));
    }

    private static List<IInstrument> GetMatchingInstruments(ICollection<IInstrument> instruments, IPAddress clientIp)
    {
        return instruments
            .Where(i => InstrumentIpMatches(i.IpAddress, clientIp))
            .ToList();
    }

    private static bool InstrumentIpMatches(IPAddress? instrumentIp, IPAddress clientIp)
    {
        if (instrumentIp is null)
        {
            return false;
        }

        if ((instrumentIp.Equals(IPAddress.Loopback) || instrumentIp.Equals(IPAddress.IPv6Loopback))
            && (clientIp.Equals(IPAddress.Loopback) || clientIp.Equals(IPAddress.IPv6Loopback)))
        {
            return true;
        }

        if (clientIp.IsIPv4MappedToIPv6 && instrumentIp.AddressFamily == AddressFamily.InterNetwork)
        {
            return clientIp.MapToIPv4().Equals(instrumentIp);
        }

        if (clientIp.AddressFamily == AddressFamily.InterNetwork && instrumentIp.IsIPv4MappedToIPv6)
        {
            return clientIp.Equals(instrumentIp.MapToIPv4());
        }

        if (instrumentIp.AddressFamily != clientIp.AddressFamily)
        {
            return false;
        }

        return instrumentIp.Equals(clientIp);
    }

    private static List<PageListEntry> ToEntries(ICollection<IInstrument> instruments)
    {
        return instruments
            .Select(i => new PageListEntry
            {
                Key = i.SerialNumber,
                DisplayName = GetDisplayName(i)
            })
            .ToList();
    }

    private static string GetDisplayName(IInstrument instrument)
    {
        return string.IsNullOrWhiteSpace(instrument.DisplayName)
            ? _unknownInstrumentDisplayName
            : instrument.DisplayName;
    }

    private bool ShouldReturnAllInstruments()
    {
        return clientDeviceService.ClientDeviceType != ClientDeviceType.Instrument;
    }

    private IPAddress? GetClientIp()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (HasForwardedForHeader())
        {
            return null;
        }

        var ip = httpContext.Connection.RemoteIpAddress;
        if (ip is null)
        {
            return null;
        }

        if (IPAddress.IsLoopback(ip))
        {
            return IPAddress.Loopback;
        }

        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
    }

    private bool HasForwardedForHeader()
    {
        return !string.IsNullOrEmpty(httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].ToString());
    }
}
