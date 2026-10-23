namespace Pharos.Contracts.Parameters;

public static class InstrumentDilutionFactors
{
    public static readonly List<int> ImmunoCap = [1, 2, 5, 10, 20, 50, 100, 200, 1000];
    public static readonly List<int> ImmunoCapIgG = [1, 2, 5, 10, 20, 50, 100, 200, 1000];
    public static readonly List<int> Elia = [1, 2, 5, 10, 20, 50, 100, 200, 1000];
    public static readonly List<int> ViewAllergy = [1, 2, 5, 10, 20, 50, 100, 200, 1000];
}