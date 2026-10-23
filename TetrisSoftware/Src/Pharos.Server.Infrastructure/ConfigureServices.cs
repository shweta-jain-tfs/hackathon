using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Compression;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.DataProtection;
using Pharos.Server.Infrastructure.Events;
using Pharos.Server.Infrastructure.Hubs;
using Pharos.Server.Infrastructure.Jobs;
using Pharos.Server.Infrastructure.Process;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Infrastructure.Settings;
using Quartz;
using FileReader = Pharos.Server.Infrastructure.Services.FileReader;
using FileWriter = Pharos.Server.Infrastructure.Services.FileWriter;

namespace Pharos.Server.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TempFileStorageSettings>(options => configuration.GetSection(TempFileStorageSettings.Section).Bind(options));

        // configure other services
        services.AddScoped<IJobHub, JobHub>();
        services.AddScoped<IFileWriter, FileWriter>();
        services.AddScoped<IFileReader, FileReader>();

        services.AddTransient<IDateTime, DateTimeService>();
        services.AddTransient<IStreamReaderFactory, StreamReaderFactory>();
        services.AddTransient<IAuditableEntitySaveChangesInterceptor, AuditableEntitySaveChangesInterceptor>();

        services.AddOptions<CertificateSettings>().BindConfiguration(CertificateSettings.CertificateSettingsSectionName);
        services.AddSingleton<ICertificateStore, CertificateStore>();

        services.AddQuartz(quartz =>
        {
            var jobKey = ColumnMasterKeyRotationJob.Key();
            quartz.AddJob<ColumnMasterKeyRotationJob>(options => options.WithIdentity(jobKey));
            quartz.AddTrigger(options => options
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule => schedule
                    .WithInterval(TimeSpan.FromDays(7))
                    .RepeatForever()));
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        services.AddSingleton<IDbEncryptionService, DbEncryptionService>();
        services.AddSingleton<IVolumeChangeEventSource, WmiVolumeChangeEventSource>();
        services.AddSingleton<IZipFile, ZipFileWrapper>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<ILogFilePathProvider, LogFilePathProvider>();

        services.AddHostedServiceAsSingleton<ITempFileStorage, TempFileStorage>();
        services.AddHostedService<UsbDriveMonitor>();

        return services;
    }
}
