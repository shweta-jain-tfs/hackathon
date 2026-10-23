using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;

namespace Pharos.Server.Settings.Commands;

public class NotifySettingsChangedCommandTests
{
    [Fact]
    public async Task Should_send_current_user_display_name_with_settings_notification()
    {
        var userId = Guid.NewGuid();
        var fixture = new NotifySettingsChangedFixture(userId);

        fixture.Sender
            .Setup(s => s.Send(
                It.Is<GetApplicationUserByIdQuery>(query => query.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUserDto
            {
                FullName = "Niklas",
                UserName = "niklas"
            });

        await fixture.Sut.Handle(
            new NotifySettingsChangedCommand(
                UpdatedKeys: ["Settings:Updated", "Settings:Updated", "Settings:Other"],
                DeletedKeys: ["Settings:Deleted"]),
            TestContext.Current.CancellationToken);

        fixture.SettingsClient.Verify(client => client.SettingsChanged(
                It.Is<List<string>>(keys => keys.SequenceEqual(new[] { "Settings:Updated", "Settings:Other" })),
                It.Is<List<string>>(keys => keys.SequenceEqual(new[] { "Settings:Deleted" })),
                "Niklas"),
            Times.Once);
    }

    [Fact]
    public async Task Should_send_null_changed_by_when_notification_has_no_current_user()
    {
        var fixture = new NotifySettingsChangedFixture(userId: null);

        await fixture.Sut.Handle(
            new NotifySettingsChangedCommand(UpdatedKeys: ["Settings:Updated"]),
            TestContext.Current.CancellationToken);

        fixture.SettingsClient.Verify(client => client.SettingsChanged(
                It.Is<List<string>>(keys => keys.SequenceEqual(new[] { "Settings:Updated" })),
                It.Is<List<string>>(keys => keys.Count == 0),
                null),
            Times.Once);
        fixture.Sender.Verify(
            s => s.Send(It.IsAny<GetApplicationUserByIdQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class NotifySettingsChangedFixture
    {
        public NotifySettingsChangedFixture(Guid? userId)
        {
            SettingsClient = new Mock<ISettingsHub>();
            Sender = new Mock<ISender>();

            Sut = new NotifySettingsChangedCommandHandler(
                SettingsClient.Object,
                new CurrentUserServiceStub(userId),
                Sender.Object);
        }

        public NotifySettingsChangedCommandHandler Sut { get; }

        public Mock<ISettingsHub> SettingsClient { get; }

        public Mock<ISender> Sender { get; }

    }

    private sealed class CurrentUserServiceStub(Guid? userId) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
    }
}
