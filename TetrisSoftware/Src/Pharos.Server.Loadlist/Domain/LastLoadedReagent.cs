using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.LoadList.Domain;

#pragma warning disable CS8618
public class LastLoadedReagent : BaseEntity
{
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentNumber { get; set; }

    [MaxLength(100)]
    public string ReagentKey { get; set; }

    [MaxLength(Constants.ArticleNumberLength)]
    public string ArticleNumber { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string LotNumber { get; set; }
}