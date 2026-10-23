using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.TaskCards;

[RootConfigSection(sortOrder: 60)]
[DisplayNameLocal("TaskCardSettings")]
public class TaskCardSettings
{
    /// <summary>
    /// Seconds until missing test order event is raised after a sample tube has been identified and sent to LIS
    /// </summary>
    [DisplayNameLocal("TaskCardSettings_Sound")]
    public bool Sound { get; set; } = true;

}
