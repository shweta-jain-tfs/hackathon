namespace Pharos.Server.Application.Parameters;
public class PhadiaQualityControlDto
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
