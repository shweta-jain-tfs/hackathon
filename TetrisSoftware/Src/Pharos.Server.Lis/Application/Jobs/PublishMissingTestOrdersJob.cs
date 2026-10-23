using MediatR;
using Pharos.Server.Application.Lis;
using Quartz;

namespace Pharos.Server.Lis.Application.Jobs;

internal class PublishMissingTestOrdersJob : IJob
{
    private readonly IPublisher _publisher;

    public PublishMissingTestOrdersJob(IPublisher publisher)
    {
        _publisher = publisher;
    }

    // Use const field instead of property to avoid Quartz.NET property injection conflicts
    // Quartz tries to automatically inject JobDataMap values into properties with matching names
    public const string SampleId = "SampleId";

    public async Task Execute(IJobExecutionContext context)
    {
        var sampleId = context.MergedJobDataMap.GetString(SampleId);

        if (sampleId != null)
        {
            await _publisher.Publish(new MissingTestOrdersEvent(sampleId));
        }
    }

    public static JobKey Key(string sampleId) { return new JobKey($"publish-missing-test-orders-key-{sampleId}"); }
}