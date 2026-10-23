namespace Pharos.Client.Common.Models;

public partial class SystemErrorSummary
{
    public override bool Equals(object? obj)
    {
        return Equals(obj as SystemErrorSummary);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
