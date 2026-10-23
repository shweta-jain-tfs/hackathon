namespace Pharos.Server.Application.Command;

public class DeleteResponse
{
    public int Deleted { get; set; }
    public int Skipped { get; set; }
    public List<int> SkippedIds { get; set; } = [];
    public List<string> SkippedPageListEntryKeys { get; set; } = [];

    public List<DeleteSkippedTubeTypeReason> SkippedTubeTypeReasons { get; set; } = [];
}

public class DeleteSkippedTubeTypeReason
{
    public string PageListEntryKey { get; set; } = string.Empty;

    public bool IsDefaultTubeType { get; set; }

    public bool UsedByRequests { get; set; }

    public bool UsedByQualityControls { get; set; }
}
