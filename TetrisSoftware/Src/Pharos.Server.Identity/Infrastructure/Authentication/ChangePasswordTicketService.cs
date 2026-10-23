using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authentication;

internal sealed class ChangePasswordTicketService(IDateTime dateTime) : IChangePasswordTicketService
{
    private static readonly TimeSpan _ticketLifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, TicketRecord> _tickets = new(StringComparer.Ordinal);

    public string CreateTicket(string username, ChangePasswordTicketType ticketType)
    {
        var ticket = GenerateTicket();
        var expiresAt = dateTime.Now.Add(_ticketLifetime);
        var normalizedUsername = NormalizeUsername(username);

        _tickets[ticket] = new TicketRecord(normalizedUsername, ticketType, expiresAt);
        CleanupExpired();

        return ticket;
    }

    public bool TryConsumeTicket(string username, string ticket, ChangePasswordTicketType ticketType)
    {
        CleanupExpired();
        var normalizedUsername = NormalizeUsername(username);

        if (!_tickets.TryRemove(ticket, out var record))
        {
            return false;
        }

        return record.ExpiresAt > dateTime.Now
            && string.Equals(record.NormalizedUsername, normalizedUsername, StringComparison.Ordinal)
            && record.TicketType == ticketType;
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToUpperInvariant();
    }

    private void CleanupExpired()
    {
        var now = dateTime.Now;

        foreach (var kvp in _tickets)
        {
            if (kvp.Value.ExpiresAt <= now)
            {
                _tickets.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string GenerateTicket()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private sealed record TicketRecord(string NormalizedUsername, ChangePasswordTicketType TicketType, DateTimeOffset ExpiresAt);
}
