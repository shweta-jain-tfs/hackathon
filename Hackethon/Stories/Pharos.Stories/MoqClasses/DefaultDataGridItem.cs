namespace Pharos.Stories.MoqClasses;

public class DefaultDataGridItem : IEquatable<DefaultDataGridItem>
{
    public int Id { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public DateTime? DateAndTimeOfBirth { get; set; }
    public bool HasLongFirstName { get; set; }
    public string? Description { get; set; }

    public bool Equals(DefaultDataGridItem? other)
    {
        if (other == null)
        {
            return false;
        }

        return other.Id == Id;
    }
}