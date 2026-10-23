using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace MediatR.Extensions.Microsoft.DependencyInjection.Tests;

public class NotificationPublisherTests
{
    public class MockPublisher : INotificationHandlingStrategy
    {
        public int CallCount { get; set; }

        public async Task HandleNotification(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
        {
            foreach (var handlerExecutor in handlerExecutors)
            {
                await handlerExecutor.HandlerCallback(notification, cancellationToken);
                CallCount++;
            }
        }
    }

    [Fact]
    public void ShouldResolveDefaultPublisher()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Logger());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();

        mediator.ShouldNotBeNull();

        var publisher = provider.GetService<INotificationHandlingStrategy>();

        publisher.ShouldNotBeNull();
    }

    [Fact]
    public async Task ShouldSubstitutePublisherInstance()
    {
        var publisher = new MockPublisher();
        var services = new ServiceCollection();
        services.AddSingleton(new Logger());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
            cfg.NotificationHandlingStrategy = publisher;
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();

        mediator.ShouldNotBeNull();

        await mediator.Publish(new Pinged(), TestContext.Current.CancellationToken);

        publisher.CallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ShouldSubstitutePublisherServiceType()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Logger());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
            cfg.NotificationHandlingStrategyType = typeof(MockPublisher);
            cfg.Lifetime = ServiceLifetime.Singleton;
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();
        var publisher = provider.GetService<INotificationHandlingStrategy>();

        mediator.ShouldNotBeNull();
        publisher.ShouldNotBeNull();

        await mediator.Publish(new Pinged(), TestContext.Current.CancellationToken);

        var mock = publisher.ShouldBeOfType<MockPublisher>();

        mock.CallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ShouldSubstitutePublisherServiceTypeWithWhenAll()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Logger());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
            cfg.NotificationHandlingStrategyType = typeof(TaskWhenAllHandlingStrategy);
            cfg.Lifetime = ServiceLifetime.Singleton;
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();
        var publisher = provider.GetService<INotificationHandlingStrategy>();

        mediator.ShouldNotBeNull();
        publisher.ShouldNotBeNull();

        await Should.NotThrowAsync(mediator.Publish(new Pinged(), TestContext.Current.CancellationToken));

        publisher.ShouldBeOfType<TaskWhenAllHandlingStrategy>();
    }
}