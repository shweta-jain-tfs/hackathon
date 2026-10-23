using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Instrument.Application.Commands;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Queries.GetSampleTubeImage;

internal class GetSampleTubeImageQueryHandler(
    ICommunicationSessionManager sessionManager,
    IOptionsSnapshot<SpecialSampleHandlingSettings> specialSampleHandling) : InstrumentCommandHandlerBase<GetSampleTubeImageQuery, SampleTubeImageDto>(sessionManager)
{
    protected override bool ThrowIfNotAllowed => false;

    protected override SampleTubeImageDto GetDefaultResponse(GetSampleTubeImageQuery request)
    {
        return new SampleTubeImageDto
        {
            ImageFound = false
        };
    }

    protected override async Task<SampleTubeImageDto> Handle(GetSampleTubeImageQuery request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var response = await session.Connection.GetImageAsync(new ImageRequest
        {
            ImageIdentity = request.ImageId
        }, cancellationToken);

        if (response.Images.Count > 0)
        {
            var image = response.Images[0];

            var sampleTube = new SampleTubeImageDto
            {
                ImageFound = true,
                ImageData = image.Image.ToByteArray(),
                TubeId = image.TubeId,
                Time = image.Time.ToDateTimeOffset().ToLocalTime()
            };

            //if we are not using tubeId setting, set the tubeId to null since the tubeId will not be a display friendly value
            if (!specialSampleHandling.Value.ShowTubeId())
            {
                sampleTube.TubeId = string.Empty;
            }

            return sampleTube;
        }

        return new SampleTubeImageDto
        {
            ImageFound = false
        };
    }
}