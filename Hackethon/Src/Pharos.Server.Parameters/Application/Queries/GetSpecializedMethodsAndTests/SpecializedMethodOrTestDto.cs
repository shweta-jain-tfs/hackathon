namespace Pharos.Server.Parameters.Application.Queries.GetSpecializedMethodsAndTests;

public class SpecializedMethodOrTestDto
{
    public int Id { get; set; }
    public int MethodId { get; set; }
    public int? MethodTestId { get; set; }
    public SpecializedMethodOrTestType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSpecializedMethodOrTestActivated { get; set; }
}

public enum SpecializedMethodOrTestType
{
    OptionalMethod = 1,
    OptionalTest = 2,
    RUOMethod = 3,
    RUOTest = 4
}
