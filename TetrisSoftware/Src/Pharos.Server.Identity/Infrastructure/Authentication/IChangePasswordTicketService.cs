namespace Pharos.Server.Identity.Infrastructure.Authentication;

public interface IChangePasswordTicketService
{
    string CreateTicket(string username, ChangePasswordTicketType ticketType);

    bool TryConsumeTicket(string username, string ticket, ChangePasswordTicketType ticketType);
}

public enum ChangePasswordTicketType
{
    Initial,
    Retry
}
