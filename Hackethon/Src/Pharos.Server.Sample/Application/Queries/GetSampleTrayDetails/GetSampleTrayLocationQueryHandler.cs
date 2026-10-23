using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTrayDetails;

internal record GetSampleTrayLocationQuery(string TrayId) : IRequest<string>;

internal sealed class GetSampleTrayLocationQueryHandler(IInstrumentManager instrumentManager, ISampleDbContext dbContext) : IRequestHandler<GetSampleTrayLocationQuery, string>
{
    public async Task<string> Handle(GetSampleTrayLocationQuery request, CancellationToken cancellationToken)
    {
        var sampleTrayInDb = await dbContext.SampleTrays
                                                .Where(tray => tray.Id == request.TrayId)
                                                .FirstOrDefaultAsync(cancellationToken);

        if (sampleTrayInDb == null)
        {
            throw new NotFoundException();
        }

        return GetTrayLocation(sampleTrayInDb);
    }

    private string GetTrayLocation(SampleTray tray)
    {
        if (string.IsNullOrEmpty(tray.InstrumentNumber))
        {
            return Translations.TrayLocation_NotLoaded;
        }

        var instrumentName = instrumentManager.FindInstrument(tray.InstrumentNumber)?.DisplayName ?? tray.InstrumentNumber;
        var drawerName = tray.Drawer is DrawerType.Sample1 ? Translations.Drawer_LeftSampleDrawer : Translations.Drawer_RightSampleDrawer;
        var trayPosition = $"{Translations.TrayPosition} {tray.TrayPosition}";

        var trayLocation = $"{instrumentName}, {drawerName}, {trayPosition}";

        return trayLocation;
    }
}
