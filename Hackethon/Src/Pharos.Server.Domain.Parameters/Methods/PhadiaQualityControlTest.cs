#pragma warning disable CS8618

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Method parameters for a Phadia Quality Control
/// </summary>
public class PhadiaQualityControlTest : BaseQualityControlTest
{
    public PhadiaQualityControlTest()
    {
        Enabled = true;
        Replicates = 1;
        MaxCv = 1;
        NumberOfSd = 1;
        NumberOfTests = 5;
    }

    //SortOrder only exists to be able to export the tests in the same order that they were imported
    public int SortOrder { get; set; }
}