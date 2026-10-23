namespace Pharos.Server.Lis.HL7.Application.Messages;

internal static class HL7MessageType
{
    // Message codes
    public const string Ack = "ACK";
    public const string Qbp = "QBP";
    public const string Oru = "ORU";
    public const string Orm = "ORM";
    public const string Oul = "OUL";

    // Trigger events
    public const string O01 = "O01";
    public const string R01 = "R01";
    public const string R23 = "R23";
    public const string Zos = "ZOS";

    public static bool IsAckMessage(string messageCode)
    {
        return string.Equals(messageCode, Ack, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOrderMessage(string messageCode, string triggerEvent)
    {
        return IsEqual(messageCode, triggerEvent, Orm, O01)
            || IsEqual(messageCode, triggerEvent, Oru, R01)
            || IsEqual(messageCode, triggerEvent, Oul, R23);
    }

    private static bool IsEqual(string messageCode, string triggerEvent, string expectedCode, string expectedEvent)
    {
        return string.Equals(messageCode, expectedCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(triggerEvent, expectedEvent, StringComparison.OrdinalIgnoreCase);
    }
}