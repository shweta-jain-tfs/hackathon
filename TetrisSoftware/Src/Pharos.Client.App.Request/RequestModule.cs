using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Request.Models;
using Pharos.Client.App.Request.Services;
using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.Request;

public class RequestModule : IClientAppModule
{
    private RequestModule()
    {
    }

    public static IClientAppModule Instance { get; } = new RequestModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //DataServices
        services.AddScoped<IRequestSummaryDataService, RequestSummaryDataService>();
        services.AddKeyedScoped<IScannedBarcodeService, SampleScannedBarcodeService>(ScannedBarcodeKeys.SampleScannedBarcodeService);

        services.AddScoped<IMethodTestDataSource, MethodTestDataSource>();
        services.AddScoped<IEntityDataSource<Panel>, PanelDataSource>();
        services.AddScoped<ISearchPatientDataSource, PatientDataSource>();
        services.AddScoped<IReflexDefinitionDataSource, ReflexDefinitionDataSource>();
        services.AddScoped<IRequestorDataSource, RequestorDataSource>();
        services.AddScoped<IEntityDataSource<TubeDefinition>, TubeDefinitionDataSource>();

        services.AddScoped<RequestDetailsDataService>();
        services.AddScoped<IRequestDetailsDataService>(x => x.GetRequiredService<RequestDetailsDataService>());
        services.AddScoped<ISampleTubeDataSource>(x => x.GetRequiredService<RequestDetailsDataService>());
        services.AddScoped<IVolumeCalculationService>(x => x.GetRequiredService<RequestDetailsDataService>());

        //Notifications
        services.AddScoped<IRequestSummaryNotificationProvider, RequestSummaryNotificationProvider>();
        services.AddScoped<IRequestDetailsNotificationProvider, RequestDetailsNotificationProvider>();
        services.AddScoped<ISampleTubeNotificationProvider, SampleTubeNotificationProvider>();


        //ViewModels
        services.AddTransient<RequestListViewModel>();
        services.AddTransient<RequestDetailPageViewModel>();
        services.AddTransient<MethodListViewModel>();
        services.AddTransient<PanelListViewModel>();
    }
}