namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines keys for registering keyed services (for specific LIS protocol implementations).
/// See https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services
/// </summary>
public static class LisServiceKeys
{
    public const string AstmConnectionFactory = "AstmConnectionFactory";
    public const string DataCapConnectionFactory = "DataCapConnectionFactory";
    public const string HL7ConnectionFactory = "HL7ConnectionFactory";

    public const string AstmMessageBuilder = "AstmMessageBuilder";
    public const string DataCapMessageBuilder = "DataCapMessageBuilder";
    public const string HL7MessageBuilder = "HL7MessageBuilder";
}
