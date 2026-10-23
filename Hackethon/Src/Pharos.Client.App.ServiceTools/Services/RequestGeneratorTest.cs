namespace Pharos.Client.App.ServiceTools.Services;

public class RequestGeneratorTest : ViewModelBase, IEquatable<RequestGeneratorTest>
{
    public AssayTechnology AssayTechnology
    {
        get;
        set => SetField(ref field, value);
    }

    public string MethodName
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public string TestName
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public int Replicates
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public int InstrumentDilutionFactor
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public bool IsActive
    {
        get => field;
        set => SetField(ref field, value);
    } = true;

    public bool Equals(RequestGeneratorTest? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(MethodName, other.MethodName, StringComparison.Ordinal) &&
               string.Equals(TestName, other.TestName, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is RequestGeneratorTest other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MethodName, TestName);
    }
}
