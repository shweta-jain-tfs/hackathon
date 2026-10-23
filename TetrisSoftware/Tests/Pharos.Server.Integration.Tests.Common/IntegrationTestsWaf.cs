using System.Net;
using AutoFixture;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure.Settings;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;
using Xunit;
using LisConnectionState = Pharos.Server.Lis.Application.Interfaces.LisConnectionState;

namespace Pharos.Server.Integration.Tests.Common;

public class IntegrationTestsWaf : DatabaseTestsWaf
{
    private readonly Fixture _fixture = new();

    public HttpClient HttpClient { get; private set; } = default!;
    public Mock<ILisConnection>? LisConnectionMock { get; set; }

    protected override string ConnectionStringName => DatabaseSettings.MainConnectionStringName;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        HttpClient = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting($"{SettingsServiceCollectionExtensions.GetConfigSectionName(typeof(LisSettings))}:{nameof(LisSettings.LisChannels)}:0:{nameof(LisChannel.ProtocolType)}", Enum.GetName(LisProtocolType.Astm));

        var lisSettingsPrefix = $"{SettingsServiceCollectionExtensions.GetConfigSectionName(typeof(LisSettings))}:{nameof(LisSettings.LisChannels)}:0:";
        builder.UseSetting($"{lisSettingsPrefix}{nameof(LisChannel.ProtocolType)}", Enum.GetName(LisProtocolType.Astm));
        builder.UseSetting($"{lisSettingsPrefix}{nameof(LisChannel.IsEnabled)}", "true");
        builder.UseSetting($"{lisSettingsPrefix}{nameof(LisChannel.ImportSettings)}:{nameof(LisImportSettings.AskForMissingRequestsInstrumentScan)}", "true");

        // Configure LIS export settings to enable automatic exports for integration tests
        var lisExportSettingsPrefix = $"{lisSettingsPrefix}{nameof(LisChannel.ExportSettings)}:{nameof(LisChannel.ExportSettings.Sample)}:";
        builder.UseSetting($"{lisExportSettingsPrefix}{nameof(LisExportSampleSettings.AutomaticSampleExport)}", Enum.GetName(LisAutoExportTrigger.WhenTestIsApprovedOrRejected));
        builder.UseSetting($"{lisExportSettingsPrefix}{nameof(LisExportSampleSettings.WhatToExport)}", Enum.GetName(WhatToExport.AllStatuses));

        builder.ConfigureServices(services =>
        {
            services.ReplaceSingletonService<ICurrentUserService, FakeCurrentUserService>();
            services.ReplaceSingletonService<IInstrumentConnectionFactory, FakeInstrumentConnectionFactory>();
            services.RemoveService<IAuthorizationHandler, PharosPolicyProvider>();
            services.AddSingleton<IAuthorizationPolicyProvider, FakePolicyProvider>();

            services.ReplaceScopedService<ICurrentInstrumentService, FakeCurrentInstrumentService>();
            services.RemoveService<IAuthorizationHandler, PharosPolicyAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, FakeAuthorizationHandler>();
            services.ReplaceSingletonService<Pharos.Server.Application.Security.IAuthorizationService, FakeAuthorizationService>();

            services
                .AddAuthentication(defaultScheme: FakeAuthenticationHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>(FakeAuthenticationHandler.TestScheme, options => { });

            MockLisConnectionFactory(services);
        });
    }

    public override async Task ResetState()
    {
        //remove instrumentConnections
        var connectionFactory = Services.GetService<IInstrumentConnectionFactory>() as FakeInstrumentConnectionFactory;
        await connectionFactory!.Clear();

        FakeAuthorizationService.Reset();

        var instrumentManager = Services.GetRequiredService<InstrumentManager>();
        instrumentManager.Clear();
        await base.ResetState();
    }

    private void MockLisConnectionFactory(IServiceCollection services)
    {
        LisConnectionMock = new Mock<ILisConnection>();
        LisConnectionMock.Setup(x => x.LisChannelId).Returns(0);
        LisConnectionMock.Setup(x => x.Role).Returns(LisConnectionRole.Both);
        LisConnectionMock.Setup(x => x.State).Returns(LisConnectionState.Open);
        LisConnectionMock.Setup(x => x.SendMessageAsync(It.IsAny<LisMessageType>(), It.IsAny<string>())).ReturnsAsync(true);

        var factoryMock = new Mock<ILisConnectionFactory>();
        factoryMock.Setup(x => x.CreateConnection(It.IsAny<int>(), It.IsAny<LisChannel>(), It.IsAny<LisConnectionRole>())).Returns(LisConnectionMock.Object);

        services.RemoveAllKeyed<ILisConnectionFactory>(LisServiceKeys.AstmConnectionFactory);
        services.AddKeyedSingleton<ILisConnectionFactory>(LisServiceKeys.AstmConnectionFactory, factoryMock.Object);
    }

    /// <summary>
    ///     Creates instrument in database.
    ///     Ensures there is an instrument in the instrument state manager with initialized instrument storage info.
    ///     Sets up an outgoing connection to a mock instruments.
    /// </summary>
    /// <returns>The instrument mock and the instrument state object</returns>
    internal async Task<(Mock<IInstrumentConnection>, Instrument.Domain.Instrument)> AddInstrumentConnection()
    {
        // Add instrument to database
        var instrumentNumber = _fixture.Create<string>()[..Constants.InstrumentSerialNumberLength];
        var instrument = new Instrument.Domain.Instrument
        {
            SerialNumber = instrumentNumber,
            IpAddress = IPAddress.Loopback
        };

        var instrumentManager = Services.GetRequiredService<InstrumentManager>();
        await using (var scope = Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetService<IInstrumentDbContext>()!;

            // important to add the InstrumentCreatedEvent. Other parts of the system listens to this.
            instrument.AddDomainEvent(new InstrumentCreatedEvent(instrument.SerialNumber));
            dbContext.Instruments.Add(instrument);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            instrumentManager.AddInstrument(instrument);
        }

        // Get the instrumentState and reset the storage information
        var backgroundNotificationHandler = Services.GetRequiredService<IBackgroundNotificationHandler>();
        await backgroundNotificationHandler.WaitForNotificationsAsync(TimeSpan.FromSeconds(4)); // if it is not done in 5 seconds, something is wrong

        instrument = (Instrument.Domain.Instrument)instrumentManager.GetInstrument(instrument.Id);

        // Setup the instrument connection
        var instrumentConnectionMock = new Mock<IInstrumentConnection>();

        // Adds the mocked instrument to the mocked connection factory
        var connectionFactory = Services.GetService<IInstrumentConnectionFactory>() as FakeInstrumentConnectionFactory;
        connectionFactory!.AddInstrumentConnection(instrumentConnectionMock.Object);

        // Opens a connection to the mocked connection
        var sessionManager = Services.GetService<ICommunicationSessionManager>()!;
        var session = await sessionManager.StartSessionAsync(IPAddress.Any, 0, Guid.NewGuid(), instrumentNumber);
        session.SetSynchronizationFlag(CommunicationSynchronizationState.Synchronized);

        return (instrumentConnectionMock, instrument);
    }
}
