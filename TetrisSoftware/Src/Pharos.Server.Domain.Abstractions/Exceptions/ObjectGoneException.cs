namespace Pharos.Server.Domain.Exceptions;

public class ObjectGoneException : Exception
{
    public ObjectGoneException()
        : base()
    {
    }

    public ObjectGoneException(string message)
        : base(message)
    {
    }

    public ObjectGoneException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
