using System.Net;
using FluentValidation;
using Grpc.Core;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Infrastructure.Grpc;

internal static class ServerCallContextExtensions
{
    /// <summary>
    /// Gets the communication session ID from the gRPC request headers.
    /// </summary>
    /// <param name="context">Context of the current call.</param>
    /// <returns>The session ID in Guid format.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Guid GetSessionId(this ServerCallContext context)
    {
        var header = context.RequestHeaders.Get(CommunicationSession.SessionIdHeader);
        if (header == null)
        {
            throw new ValidationException("The 'sessionId' header is missing from the gRPC request.");
        }

        if (!Guid.TryParse(header.Value, out var sessionId))
        {
            throw new ValidationException($"The 'sessionId' header must be a GUID. Provided value: '{header.Value}'.");
        }

        return sessionId;
    }

    public static IPAddress GetInstrumentIp(this ServerCallContext context)
    {
        return GetInstrumentIp(context.Peer);
    }

    public static IPAddress GetInstrumentIp(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return IPAddress.None;
        }

        if (uri.StartsWith("ipv4"))
        {
            return ParseIPv4(uri);
        }

        if (uri.StartsWith("ipv6"))
        {
            return ParseIPv6(uri);
        }

        return IPAddress.None;
    }

    public static string GetInstrumentSerial(this ServerCallContext context)
    {
        var serial = context.RequestHeaders.Get(CommunicationSession.InstrumentSerialHeader);
        if (serial == null)
        {
            throw new InvalidOperationException("Client ID is missing from the gRPC request headers.");
        }

        return serial.Value;
    }

    private static IPAddress ParseIPv4(string uri)
    {
        // ipv4:127.0.0.1:4328
        return IPAddress.Parse(uri.Split(":")[1]);
    }

    private static IPAddress ParseIPv6(string uri)
    {
        // Examples:
        // ipv6:[::1]:63280
        // ipv6:[::ffff:127.0.0.1]:63280 (IPv4-mapped IPv6)
        // the parsable part is between '[' and ']'
        var start = uri.IndexOf('[') + 1;
        var end = uri.IndexOf(']');
        if (start == 0 || end == -1 || end < start)
        {
            return IPAddress.None;
        }

        var addressText = uri.Substring(start, end - start);

        // First, try to parse as an IPAddress directly
        if (!IPAddress.TryParse(addressText, out var ip))
        {
            return IPAddress.None;
        }

        // If it's an IPv6 address that maps to IPv4, convert to IPv4
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var mapped = ip.MapToIPv4();
            // MapToIPv4 returns the original if not mapped; check IsIPv4MappedToIPv6 to decide
            if (ip.IsIPv4MappedToIPv6)
            {
                return mapped;
            }
        }

        // Otherwise return the parsed address as-is
        return ip;
    }
}