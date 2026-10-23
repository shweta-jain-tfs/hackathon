using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Lis.Domain;


public class LisStatistics : BaseEntity
{
    public int ChannelId { get; set; }
    public LisStatisticsCategory LisStatisticsCategory  { get; set; }
    [MaxLength(Constants.SampleIdMaxLength)]
    public string? LatestSampleId { get; set; }
    public int SamplesCount { get; set; }
    public int? TestsCount { get; set; }
    public DateTimeOffset? LatestTime { get; set; }
    public DateTime CreatedDate { get; set; }

}
