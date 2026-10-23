namespace Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;

public class LoadListSampleTubeDto() : LoadListItemBase(aggregateIntoArticle: false)
{
    public SampleTubeType SampleTubeType { get; set; }
}

//TODO: Decide types. 
public enum SampleTubeType
{
    Sample,
    PhadiaQualityControl,
    LabQualityControl,
    Unknown
}