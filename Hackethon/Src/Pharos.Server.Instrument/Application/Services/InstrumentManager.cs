using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Services;

internal class InstrumentManager(
    ICommunicationSessionManager sessionManager,
    ILogger<InstrumentManager> logger,
    IServiceScopeFactory scopeFactory) : IHostedService, IInstrumentManager
{
    private readonly ConcurrentDictionary<string, IInstrument> _instruments = new();

    public ICollection<IInstrument> GetInstruments()
    {
        return _instruments.Values;
    }

    public IInstrument GetInstrument(int instrumentId)
    {
        var instrument = _instruments.Values.FirstOrDefault(x => x.Id == instrumentId);
        if (instrument != null)
        {
            return instrument;
        }

        throw new InvalidOperationException($"An instrument with ID: {instrumentId} is not found.");
    }

    public IInstrument GetInstrument(string instrumentNumber)
    {
        var instrument = FindInstrument(instrumentNumber);
        if (instrument != null)
        {
            return instrument;
        }

        throw new InvalidOperationException($"An instrument with Serial Number: {instrumentNumber} is not found.");
    }

    public void AddInstrument(Domain.Instrument instrument)
    {
        if (!_instruments.TryAdd(instrument.SerialNumber, instrument))
        {
            // should never happen
            throw new InvalidOperationException($"Instrument with Serial Number {instrument.SerialNumber} already exists.");
        }
    }

    public void RemoveInstrument(string instrumentNumber)
    {
        _instruments.TryRemove(instrumentNumber, out _);
    }

    internal void Clear()
    {
        _instruments.Clear();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
#if NSWAG
        
#else
        sessionManager.SessionClosed += OnSessionClosed;
        await LoadInstruments(cancellationToken);
#endif
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _instruments.Clear();
        sessionManager.SessionClosed -= OnSessionClosed;
        return Task.CompletedTask;
    }


    public IInstrument? FindInstrument(string? instrumentNumber)
    {
        if (!string.IsNullOrWhiteSpace(instrumentNumber) && _instruments.TryGetValue(instrumentNumber, out var instrument))
        {
            return instrument;
        }
        return null;
    }

    public IInstrument? FindInstrument(IPAddress ipAddress)
    {
        var addressFamilyMismatch = false;

        var result = _instruments.Values.OrderBy(i => i.Id).FirstOrDefault(i =>
        {
            // treat both IPv4 and IPv6 loopback addresses as equivalent
            if ((i.IpAddress.Equals(IPAddress.Loopback) || i.IpAddress.Equals(IPAddress.IPv6Loopback))
                && (ipAddress.Equals(IPAddress.Loopback) || ipAddress.Equals(IPAddress.IPv6Loopback)))
            {
                return true;
            }

            if (ipAddress.IsIPv4MappedToIPv6 && i.IpAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                return ipAddress.MapToIPv4().Equals(i.IpAddress);
            }

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork && i.IpAddress.IsIPv4MappedToIPv6)
            {
                return ipAddress.Equals(i.IpAddress.MapToIPv4());
            }

            if (i.IpAddress.AddressFamily != ipAddress.AddressFamily)
            {
                addressFamilyMismatch = true;
                return false;
            }

            return i.IpAddress.Equals(ipAddress);
        });

        if (result == null && addressFamilyMismatch)
        {
            // Most probably we cannot find the instrument connection
            // because we have mixed IPv4 and IPv6 addresses.
            logger.LogWarning("Address Family mismatch when trying to find instrument connection by the IP address. " +
                "Check your network configuration.");
        }

        return result;
    }

    private async Task LoadInstruments(CancellationToken cancellationToken)
    {
        logger.LogInformation("Loading the instruments into memory.");
        try
        {
            using var scope = scopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<IInstrumentDbContext>();

            var instruments = await dbContext.Instruments
                                             .ToListAsync(cancellationToken)
                                             .ConfigureAwait(false);

            logger.LogInformation("Found {Count} instruments in the database", instruments.Count);
            foreach (var instrument in instruments)
            {
                logger.LogInformation("Adding instrument Id: {InstrumentId} + {SerialNumber} to instrument list", instrument.Id, instrument.SerialNumber);
                AddInstrument(instrument);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to load instruments into memory.");
        }
    }

    private void OnSessionClosed(object? sender, (Guid SessionId, string InstrumentNumber) args)
    {
        ResetInstrumentState(args.InstrumentNumber).Wait();
    }

    private async Task ResetInstrumentState(string instrumentNumber)
    {
        var instrument = _instruments.FirstOrDefault(x => x.Value.SerialNumber == instrumentNumber).Value;
        if (instrument == null)
        {
            return;
        }
        ((Domain.Instrument)instrument).ResetState();

        using var scope = scopeFactory.CreateScope();

        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(new InstrumentStateChangedEvent(instrument.Id, instrument.State), CancellationToken.None);
        await publisher.Publish(new SignalTowerStateChangedEvent(instrument.SerialNumber, new SignalTowerState
        {
            LightState = EnumSignalTowerLightState.Off,
            SoundState = EnumSignalTowerSoundState.SoundOff
        }), CancellationToken.None);
    }
}
