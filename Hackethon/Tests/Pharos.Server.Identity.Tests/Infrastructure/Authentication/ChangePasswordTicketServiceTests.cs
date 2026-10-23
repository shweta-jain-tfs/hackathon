using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authentication;

public sealed class ChangePasswordTicketServiceTests
{
    [Fact]
    public void CreateTicket_WhenConsumedWithMatchingUserAndType_ReturnsTrueOnlyOnce()
    {
        var now = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.SetupGet(x => x.Now).Returns(() => now);
        var sut = new ChangePasswordTicketService(mockDateTime.Object);

        var ticket = sut.CreateTicket("test-user", ChangePasswordTicketType.Initial);

        var firstConsume = sut.TryConsumeTicket("test-user", ticket, ChangePasswordTicketType.Initial);
        var secondConsume = sut.TryConsumeTicket("test-user", ticket, ChangePasswordTicketType.Initial);

        firstConsume.Should().BeTrue();
        secondConsume.Should().BeFalse();
    }

    [Fact]
    public void TryConsumeTicket_WhenUsernameHasDifferentCasingAndWhitespace_ReturnsTrue()
    {
        var now = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.SetupGet(x => x.Now).Returns(() => now);
        var sut = new ChangePasswordTicketService(mockDateTime.Object);

        var ticket = sut.CreateTicket("  TeSt-UsEr  ", ChangePasswordTicketType.Initial);

        var consumed = sut.TryConsumeTicket("test-user", ticket, ChangePasswordTicketType.Initial);

        consumed.Should().BeTrue();
    }

    [Fact]
    public void TryConsumeTicket_WhenUsernameDoesNotMatch_ReturnsFalse()
    {
        var now = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.SetupGet(x => x.Now).Returns(() => now);
        var sut = new ChangePasswordTicketService(mockDateTime.Object);

        var ticket = sut.CreateTicket("test-user", ChangePasswordTicketType.Initial);

        var consumed = sut.TryConsumeTicket("other-user", ticket, ChangePasswordTicketType.Initial);

        consumed.Should().BeFalse();
    }

    [Fact]
    public void TryConsumeTicket_WhenTicketTypeDoesNotMatch_ReturnsFalse()
    {
        var now = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.SetupGet(x => x.Now).Returns(() => now);
        var sut = new ChangePasswordTicketService(mockDateTime.Object);

        var ticket = sut.CreateTicket("test-user", ChangePasswordTicketType.Initial);

        var consumed = sut.TryConsumeTicket("test-user", ticket, ChangePasswordTicketType.Retry);

        consumed.Should().BeFalse();
    }

    [Fact]
    public void TryConsumeTicket_WhenTicketIsExpired_ReturnsFalse()
    {
        var now = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.SetupGet(x => x.Now).Returns(() => now);
        var sut = new ChangePasswordTicketService(mockDateTime.Object);

        var ticket = sut.CreateTicket("test-user", ChangePasswordTicketType.Initial);
        now = now.AddMinutes(6);

        var consumed = sut.TryConsumeTicket("test-user", ticket, ChangePasswordTicketType.Initial);

        consumed.Should().BeFalse();
    }
}