namespace Pharos.Server.Parameters.Application.Queries.GetMethods;

public class MethodDto
{
    public int Id { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string? MethodNameLis { get; set; }
    public string FullMethodName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}