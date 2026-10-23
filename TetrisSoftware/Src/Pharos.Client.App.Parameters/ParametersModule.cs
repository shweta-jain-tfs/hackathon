using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.Common.ViewModels;
using Pharos.Client.App.Parameters.LoadListConfiguration.Models;
using Pharos.Client.App.Parameters.LoadListConfiguration.ViewModels;
using Pharos.Client.App.Parameters.Methods.Models;
using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.App.Parameters.Packages.Models;
using Pharos.Client.App.Parameters.Packages.ViewModels;
using Pharos.Client.App.Parameters.Panels.Models;
using Pharos.Client.App.Parameters.Panels.ViewModels;
using Pharos.Client.App.Parameters.QualityControl.Models;
using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.App.Parameters.Reflex.Models;
using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Client.App.Parameters.Requestors.Models;
using Pharos.Client.App.Parameters.Requestors.ViewModels;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Parameters;

public class ParametersModule : IClientAppModule
{
    private ParametersModule()
    {
    }

    public static IClientAppModule Instance { get; } = new ParametersModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {

        //Common
        services.AddScoped<ParameterUpdatePackageVersionDataSource>();
        services.AddScoped<ImportParametersDataService>();
        services.AddScoped<IParametersNotificationProvider, ParameterNotificationProvider>();
        services.AddScoped<IMethodTestDataSource, MethodTestDataSource>();
        services.AddTransient<ParameterUpdatePackageVersionViewModel>();
        services.AddTransient<MethodListViewModel>();
        services.AddTransient<ExportParameterPackageViewModel>();

        //Load list configuration
        services.AddScoped<LoadListConfigurationsDataService>();
        services.AddScoped<IEntityDataService<Client.Common.Models.LoadListConfiguration>>(x => x.GetRequiredService<LoadListConfigurationsDataService>());
        services.AddTransient<LoadListConfigurationsViewModel>();

        //Methods
        services.AddTransient<MethodTestDetailsPageViewModel>();
        services.AddScoped<IMethodTestDetailsPageDataService, MethodTestDetailsPageDataService>();
        services.AddScoped<MethodsPageViewModel>();
        services.AddTransient<MethodDetailsPageViewModel>();
        services.AddTransient<MethodTestsGridViewModel>();
        services.AddScoped<IMethodDataSource, MethodDataSource>();
        services.AddScoped<ILisChannelDataSource, LisChannelDataSource>();
        services.AddScoped<IMethodTestPagedDataSource, MethodTestPagedDataSource>();
        services.AddScoped<ISpecializedMethodsAndTestsDataSource, SpecializedMethodsAndTestsDataSource>();
        services.AddTransient<SpecializedMethodsAndTestsViewModel>();

        //Packages
        services.AddScoped<IPackagesDataSource, PackagesDataSource>();
        services.AddScoped<IEntityDataService<PackageDetails>, PackageDetailsDataService>();
        services.AddTransient<PackagesViewModel>();
        services.AddTransient<PackageDetailsViewModel>();

        //Panels
        services.AddScoped<IMethodPanelsDataSource, MethodPanelsDataSource>();
        services.AddScoped<IDetailsPageDataService<MethodPanelDetails>, MethodPanelDetailsDataService>();
        services.AddTransient<PanelsListViewModel>();
        services.AddTransient<PanelsDetailsPageViewModel>();

        //Quality controls
        services.AddScoped<IPhadiaQualityControlItemSource, PhadiaQualityControlItemSource>();
        services.AddScoped<ILabQualityControlItemSource, LabQualityControlItemSource>();
        services.AddScoped<IEntityDataSource<TubeDefinition>, TubeDefinitionDataSource>();
        services.AddScoped<IQualityControlBarcodeFormatItemSource, QualityControlBarcodeFormatItemSource>();
        services.AddTransient<LabQualityControlDetailsPageViewModel>();
        services.AddTransient<PhadiaQualityControlDetailsPageViewModel>();
        services.AddTransient<PhadiaQualityControlListViewModel>();
        services.AddTransient<LabQualityControlListViewModel>();
        services.AddSingleton<IBarcodeFormatBuilder, BarcodeFormatBuilder>();
        services.AddScoped<IQualityControlBarcodeFormatNotificationProvider, QualityControlBarcodeFormatNotificationProvider>();
        services.AddScoped<IQualityControlScheduleItemSource, QualityControlScheduleItemSource>();
        services.AddTransient<QualityControlScheduleListViewModel>();

        //Reflex
        services.AddScoped<IReflexDetailDataService, ReflexDetailDataService>();
        services.AddScoped<IRequestorDataSource, RequestorDataSource>();
        services.AddScoped<IReflexDetailNotificationProvider, ReflexDetailNotificationProvider>();
        services.AddScoped<ReflexDefinitionSummaryDataService>();
        services.AddScoped<IReflexDataService>(x => x.GetRequiredService<ReflexDefinitionSummaryDataService>());
        services.AddScoped<IGetByIdDataSource<ReflexDefinitionSummary>>(x => x.GetRequiredService<ReflexDefinitionSummaryDataService>());
        services.AddTransient<ReflexDefinitionDetailsPageViewModel>();
        services.AddTransient<ReflexDefinitionListPageViewModel>();

        //Requestors
        services.AddScoped<RequestorSummaryDataService>();
        services.AddScoped<IEntityDataService<RequestorSummary>>(x => x.GetRequiredService<RequestorSummaryDataService>());
        services.AddScoped<IRequestorDetailsDataService, RequestorDetailsDataService>();
        services.AddScoped<IRequestorDetailsNotificationProvider, RequestorDetailsNotificationProvider>();
        services.AddScoped<IGetByIdDataSource<RequestorSummary>>(x => x.GetRequiredService<RequestorSummaryDataService>());
        services.AddScoped<IEntityNotificationProvider<RequestorSummary>, RequestorSummaryNotificationProvider>();
        services.AddTransient<RequestorListViewModel>();
        services.AddTransient<RequestorDetailPageViewModel>();
    }
}
