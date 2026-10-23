namespace Pharos.Stories.MoqClasses;

public class DefaultTableItem : IEquatable<DefaultTableItem>
{
    public int Id { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public DateTime? DateAndTimeOfBirth { get; set; }

    public bool Equals(DefaultTableItem? other)
    {
        if (other == null)
        {
            return false;
        }

        return other.Id == Id;
    }
}