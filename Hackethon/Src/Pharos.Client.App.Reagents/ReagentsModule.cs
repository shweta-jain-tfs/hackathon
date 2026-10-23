using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Reagents.Models;
using Pharos.Client.App.Reagents.Models.BottleDrawer;
using Pharos.Client.App.Reagents.Models.Inventory;
using Pharos.Client.App.Reagents.Models.Inventory.Bottles;
using Pharos.Client.App.Reagents.Models.Inventory.Carriers;
using Pharos.Client.App.Reagents.Models.Inventory.Cartridges;
using Pharos.Client.App.Reagents.Models.Inventory.DilutionPlates;
using Pharos.Client.App.Reagents.Models.LiquidDrawer;
using Pharos.Client.App.Reagents.Models.SolidWasteDrawer;
using Pharos.Client.App.Reagents.Services;
using Pharos.Client.App.Reagents.ViewModels;
using Pharos.Client.App.Reagents.ViewModels.BarcodeDetails;
using Pharos.Client.App.Reagents.ViewModels.BottleDrawer;
using Pharos.Client.App.Reagents.ViewModels.Inventory;
using Pharos.Client.App.Reagents.ViewModels.LiquidDrawer;
using Pharos.Client.App.Reagents.ViewModels.SolidWasteDrawer;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Contracts;

namespace Pharos.Client.App.Reagents;

public class ReagentsModule : IClientAppModule
{
    private ReagentsModule()
    {

    }

    public static IClientAppModule Instance { get; } = new ReagentsModule();
    public bool IsDeviceSpecific => true;
    public ClientDeviceType? DeviceType => ClientDeviceType.Instrument;
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //Reagents
        services.AddScoped<IReagentDrawerDataSource, ReagentDrawerDataSource>();
        services.AddKeyedScoped<IScannedBarcodeService, ReagentScannedBarcodeService>(ScannedBarcodeKeys.ReagentScannedBarcodeService);

        services.AddTransient<ReagentLegendViewModel>();
        services.AddTransient<ReagentLoaderPanelViewModel>();
        services.AddTransient<ReagentDrawerViewModel>();
        services.AddTransient<ReagentPageViewModel>();

        //Barcode Details
        services.AddTransient<ReagentDetailsViewModel>();

        //Inventory
        services.AddTransient<InventoryPageViewModel>();
        services.AddTransient<CarriersListViewModel>();
        services.AddTransient<CartridgesListViewModel>();
        services.AddTransient<BottlesListViewModel>();
        services.AddTransient<DilutionPlatesViewModel>();
        services.AddTransient<DrawerDilutionPlatesViewModel>();
        services.AddTransient<InternalDilutionPlatesViewModel>();

        services.AddScoped<IReagentDataSource, ReagentDataSource>();
        services.AddScoped<CarriersListDataSource>();
        services.AddScoped<CartridgesListDataSource>();
        services.AddScoped<BottlesListDataSource>();
        services.AddScoped<DrawerDilutionPlatesDataSource>();
        services.AddScoped<InternalDilutionPlatesDataSource>();

        services.AddScoped<IEntityDataSource<CarrierListResult>>(x => x.GetRequiredService<CarriersListDataSource>());
        services.AddScoped<IEntityDataSource<CartridgeListResult>>(x => x.GetRequiredService<CartridgesListDataSource>());
        services.AddScoped<IEntityDataSource<BottleListResult>>(x => x.GetRequiredService<BottlesListDataSource>());
        services.AddScoped<IEntityDataSource<DrawerDilutionPlatesResult>>(x => x.GetRequiredService<DrawerDilutionPlatesDataSource>());
        services.AddScoped<IEntityDataSource<InternalDilutionPlatesResult>>(x => x.GetRequiredService<InternalDilutionPlatesDataSource>());

        services.AddScoped<ICarriersNotificationProvider, CarriersNotificationProvider>();
        services.AddScoped<ICartridgesNotificationProvider, CartridgesNotificationProvider>();
        services.AddScoped<IBottlesNotificationProvider, BottlesNotificationPovider>();
        services.AddScoped<IDilutionPlatesNotificationProvider, DilutionPlatesNotificationProvider>();

        //Bottle Drawer
        services.AddTransient<BottlePageViewModel>();
        services.AddTransient<BottleDrawerViewModel>();
        services.AddTransient<BottleDrawerPanelViewModel>();
        services.AddScoped<IBottleDrawerDataSource, BottleDrawerDataSource>();

        //Liquids
        services.AddTransient<LiquidPageViewModel>();
        services.AddTransient<LiquidDrawerViewModel>();
        services.AddTransient<LiquidLegendViewModel>();
        services.AddScoped<ILiquidDrawerDataSource, LiquidDrawerDataSource>();

        //Waste
        services.AddTransient<SolidWastePageViewModel>();
        services.AddTransient<SolidWasteDrawerViewModel>();
        services.AddTransient<SolidWasteLegendViewModel>();
        services.AddScoped<ISolidWasteDrawerDataSource, SolidWasteDrawerDataSource>();
    }
}