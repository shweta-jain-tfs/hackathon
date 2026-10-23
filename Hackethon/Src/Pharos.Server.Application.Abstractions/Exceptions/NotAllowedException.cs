namespace Pharos.Server.Application.Exceptions;

public class NotAllowedException : Exception
{
    public NotAllowedException()
    {
    }

    public NotAllowedException(string message) : base(message)
    {
    }
}