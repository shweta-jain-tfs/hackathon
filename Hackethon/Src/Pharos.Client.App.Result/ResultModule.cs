using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Result.Models;

namespace Pharos.Client.App.Result;

public class ResultModule : IClientAppModule
{
    private ResultModule()
    {
    }

    public static IClientAppModule Instance { get; } = new ResultModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //DataServices
        services.AddScoped<TestResultDataService>();
        services.AddScoped<ISampleTestResultDataService>(x => x.GetRequiredService<TestResultDataService>());
        services.AddScoped<ITestResultReplicateDataService>(x => x.GetRequiredService<TestResultDataService>());
        services.AddScoped<IAnalyticalRunResultDataSource>(x => x.GetRequiredService<TestResultDataService>());
        services.AddScoped<IQualityControlDataSource>(x => x.GetRequiredService<TestResultDataService>());

        services.AddScoped<CurveControlDataSource>();
        services.AddScoped<IEntityDataSource<CurveControlTestResult>>(x => x.GetRequiredService<CurveControlDataSource>());
        services.AddScoped<ICurveControlResultDataSource>(x => x.GetRequiredService<CurveControlDataSource>());

        services.AddScoped<IAnalyticalRunDataSource, AnalyticalRunDataSource>();
        services.AddScoped<ICurveDataSource, CurveDataSource>();
        services.AddScoped<IFileSource<GetResultExportQuery>, ResultExportFileSource>();
        services.AddScoped<IMethodNameDataSource, MethodNameDataSource>();
        services.AddScoped<ChartStateService>();

        //Notifications
        services.AddScoped<IEntityNotificationProvider<AnalyticalRunTestResult>, TestResultNotificationProvider>();
        services.AddScoped<ITestResultChartNotificationProvider, TestResultChartNotificationProvider>();
        services.AddScoped<IEntityNotificationProvider<AnalyticalRun>, AnalyticalRunNotificationProvider>();

        //ViewModels
        services.AddTransient<TestResultViewModel>();
        services.AddTransient<AnalyticalRunListViewModel>();
        services.AddTransient<CurveViewModel>();
        services.AddTransient<CurveControlChartViewModel>();
        services.AddTransient<QualityControlViewModel>();
        services.AddTransient<MethodNameViewModel>();
        services.AddTransient<PendingReflexViewModel>();
    }
}