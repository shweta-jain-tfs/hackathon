using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Instrument.Application.Commands.UnlockCompartment;
using Tetris.Protocol.Common;


namespace Pharos.Server.Instrument.Application.Services;

public interface IDrawerAuthorizationService
{
    void Add(string instrumentNumber, EnumInstrumentCompartmentWithLock drawer);
    void Remove(string instrumentNumber);
    List<EnumInstrumentCompartmentWithLock> GetDrawers(string instrumentNumber);
    Task SendUnlockIfWaiting(Guid userId, string instrumentNumber);
}

internal sealed class DrawerAuthorizationService : IDrawerAuthorizationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ISender _sender;
    private readonly ILogger<DrawerAuthorizationService> _logger;

    public DrawerAuthorizationService(IMemoryCache memoryCache, ISender sender, ILogger<DrawerAuthorizationService> logger)
    {
        _memoryCache = memoryCache;
        _sender = sender;
        _logger = logger;
    }

    public void Add(string instrumentNumber, EnumInstrumentCompartmentWithLock drawer)
    {
        var compartments = GetDrawers(instrumentNumber);
        if (!compartments.Contains(drawer))
        {
            compartments.Add(drawer);
        }

        _memoryCache.Set(instrumentNumber, compartments, DateTimeOffset.Now.AddMinutes(5));
    }

    public void Remove(string instrumentNumber)
    {
        _memoryCache.Remove(instrumentNumber);
    }

    public List<EnumInstrumentCompartmentWithLock> GetDrawers(string instrumentNumber)
    {
        if (_memoryCache.TryGetValue(instrumentNumber, out var compartments))
        {
            return compartments as List<EnumInstrumentCompartmentWithLock> ?? new List<EnumInstrumentCompartmentWithLock>();
        }
        return new List<EnumInstrumentCompartmentWithLock>();
    }
    public async Task SendUnlockIfWaiting(Guid userId, string instrumentNumber)
    {
        var drawers = GetDrawers(instrumentNumber);
        foreach (var drawer in drawers)
        {
            try
            {
                await _sender.Send(new UnlockCompartmentCommand(instrumentNumber, drawer, userId));
            }
            catch (NotAllowedException e)
            {
                _logger.LogWarning(e, "User {UserId} is not authorized to unlock drawer {Drawer} on instrument {Instrument}", userId, drawer, instrumentNumber);
            }
        }
        Remove(instrumentNumber);
    }
}
