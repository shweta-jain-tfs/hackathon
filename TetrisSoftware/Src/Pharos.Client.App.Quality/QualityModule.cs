using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Quality.Models;
using Pharos.Client.App.Quality.Services;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Quality;

public class QualityModule : IClientAppModule
{
    private QualityModule()
    {

    }

    public static IClientAppModule Instance { get; } = new QualityModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //DataServices
        services.AddScoped<IMethodQualityDataSource, MethodQualityDataSource>();
        services.AddScoped<IMethodQualityControlDetailsDataSource, MethodQualityControlDetailsDataSource>();
        services.AddScoped<IMethodQualityControlsOverviewDataSource, MethodQualityControlsOverviewDataSource>();
        services.AddScoped<ICurvesDataSource, CurvesDataSource>();
        services.AddScoped<ICurveControlResultDataSource, CurveControlDataSource>();
        services.AddScoped<IQualityControlDataSource, QualityControlDataSource>();
        services.AddScoped<IMethodDataSource, MethodDataSource>();
        services.AddScoped<IQualityDataSource, QualityDataSource>();
        services.AddKeyedScoped<IScannedBarcodeService, QcScannedBarcodeService>(ScannedBarcodeKeys.QcScannedBarcodeService);

        //Notifications
        services.AddScoped<IQualityControlTubeNotificationProvider, QualityControlTubeNotificationProvider>();

        //ViewModels
        services.AddTransient<MethodListViewModel>();
        services.AddTransient<MethodViewModel>();
        services.AddTransient<MethodQualityControlDetailsPageViewModel>();
        services.AddTransient<MethodQualityControlOverviewViewModel>();
        services.AddTransient<CalibratorCurveViewModel>();
        services.AddTransient<QualityControlViewModel>();
        services.AddTransient<CurveControlViewModel>();
        services.AddTransient<CurvesViewModel>();
        services.AddTransient<QcCartridgeDetailsViewModel>();
    }
}