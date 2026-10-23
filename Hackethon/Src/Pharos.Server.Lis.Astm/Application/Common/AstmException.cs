namespace Pharos.Server.Lis.Astm.Application.Common;

internal sealed class AstmException : Exception
{
    public AstmException()
        : base(string.Empty)
    {
    }

    public AstmException(string? message)
        : base(message)
    {
    }

    public AstmException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

}