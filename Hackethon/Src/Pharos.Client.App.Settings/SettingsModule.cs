using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Settings.Actions;
using Pharos.Client.App.Settings.Services;
using Pharos.Client.App.Settings.ViewModels.Converters;
using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.Settings;

public class SettingsModule : IClientAppModule
{
    private SettingsModule()
    {

    }

    public static IClientAppModule Instance { get; } = new SettingsModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //Notifications
        services.AddScoped<ISettingsNotificationProvider, SettingsNotificationProvider>();

        //ViewModels
        services.AddTransient<IValidator<IntegerViewModel>, IntegerViewModelValidator>();
        services.AddTransient<IValidator<DoubleViewModel>, DoubleViewModelValidator>();

        // Settings
        services.AddScoped<ISettingsDataService<SettingItemBase>, SettingsDataService>();
        services.AddScoped<ISettingsMenuProvider, SettingsMenuProvider>();
        services.AddScoped<SettingsMainViewModel>();
        services.AddScoped<IConverter, Converter>();
        services.AddScoped<IComponentConverter, OptionConverter>();
        services.AddScoped<IComponentConverter, GroupConverter>();
        services.AddScoped<IComponentConverter, PageConverter>();
        services.AddScoped<IComponentConverter, PageListConverter>();
        services.AddScoped<IComponentConverter, PagePlaceHolderConverter>();
        services.AddScoped<IComponentConverter, ActionButtonConverter>();

        services.AddScoped<SettingsActionOperationState>();
        services.AddScoped<ISettingsAction, ExportSettingsAction>();
        services.AddScoped<ISettingsAction, ImportSettingsAction>();
    }
}
