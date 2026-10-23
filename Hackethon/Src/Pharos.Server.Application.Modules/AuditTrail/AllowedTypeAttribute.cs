namespace Pharos.Server.Application.AuditTrail
{

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AllowedTypeAttribute(Type allowedType) : Attribute
    {
        public Type AllowedType { get; } = allowedType;
    }
}
