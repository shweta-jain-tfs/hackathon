using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.Common.App.Extensions;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.App.State.CurrentInstrument.Models;
using Pharos.Client.Common.App.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.Common.App;

public static class ConfigureServices
{
    public static IServiceCollection AddCommonAppServices(this IServiceCollection services)
    {
        // Register navigation history manager as a service.
        services.AddScoped<NavigationHistoryManager>();
        services.AddScoped<IExceptionHandlerService, ExceptionHandlerService>();

        // Register toaster service.
        services.AddScoped<ToasterViewModel>();
        services.AddScoped<IToasterService>(sp => sp.GetRequiredService<ToasterViewModel>());
        services.AddScoped<ScannedBarcodeService>();

        // Register Instrument Dilution Service
        services.AddScoped<IInstrumentDilutionService, InstrumentDilutionService>();

        //DataServices
        services.AddScoped<ICurrentInstrumentDataService, CurrentInstrumentDataService>();

        //ViewModels
        services.AddScoped<ICurrentInstrumentViewModel, CurrentInstrumentViewModel>();
        services.AddScoped<InstrumentStateViewModel>();
        services.AddScoped<ScannedBarcodeViewModel>();

        //Help Content Service
        services.AddScoped<IHelpContentService, HelpContentService>();
        services.AddScoped<IUiComponentStringsLocalizer, UiComponentStringsLocalizer>();

        return services;
    }
}
