using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.App.Sample.Services;
using Pharos.Client.App.Sample.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Contracts;

namespace Pharos.Client.App.Sample;

public class SampleModule : IClientAppModule
{
    private SampleModule()
    {

    }

    public static IClientAppModule Instance { get; } = new SampleModule();
    public bool IsDeviceSpecific => true;
    public ClientDeviceType? DeviceType => ClientDeviceType.Instrument;
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //DataServices
        services.AddScoped<SampleTrayDataService>();
        services.AddKeyedScoped<IScannedBarcodeService, TrayScannedBarcodeService>(ScannedBarcodeKeys.TrayScannedBarcodeService);
        services.AddScoped<IEntityDataService<SampleTray, string>>(x => x.GetRequiredService<SampleTrayDataService>());
        services.AddScoped<IGetByIdDataSource<SampleTray, string>>(x => x.GetRequiredService<SampleTrayDataService>());
        services.AddScoped<ISampleTrayDataService>(x => x.GetRequiredService<SampleTrayDataService>());

        services.AddScoped<SampleTrayDetailsDataService>();
        services.AddScoped<IEntityDataService<SampleTrayDetails, string>>(x => x.GetRequiredService<SampleTrayDetailsDataService>());
        services.AddScoped<IGetByIdDataSource<SampleTrayDetails, string>>(x => x.GetRequiredService<SampleTrayDetailsDataService>());
        services.AddScoped<ISampleTrayDetailsDataService>(x => x.GetRequiredService<SampleTrayDetailsDataService>());

        services.AddScoped<ILeftSampleDrawerDataSource, LeftSampleDrawerDataSource>();
        services.AddScoped<IRightSampleDrawerDataSource, RightSampleDrawerDataSource>();
        services.AddScoped<IAllSampleDrawerDataSource, SampleDrawersDataSource>();
        services.AddScoped<IRequestDataSource, RequestDataSource>();

        //Notifications
        services.AddScoped<IEntityNotificationProvider<SampleTray, string>, SampleTrayNotificationProvider>();
        services.AddScoped<ISampleTrayPositionNotificationProvider, SampleTrayPositionNotificationProvider>();

        //ViewModels
        services.AddTransient<SampleDrawerPageViewModel>();
        services.AddTransient<LeftSampleDrawerViewModel>();
        services.AddTransient<RightSampleDrawerViewModel>();
        services.AddTransient<TraysViewModel>();
        services.AddTransient<TrayPanelViewModel>();
        services.AddTransient<SampleLegendViewModel>();
        services.AddTransient<AddTrayViewModel>();
    }
}