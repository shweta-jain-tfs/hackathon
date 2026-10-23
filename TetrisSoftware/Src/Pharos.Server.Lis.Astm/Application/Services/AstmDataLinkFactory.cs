using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Astm.Application.Frames;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Application.Services;

internal sealed class AstmDataLinkFactory(ILoggerFactory loggerFactory) : IAstmDataLinkFactory
{
    public IAstmDataLink CreateAstmDataLink(AstmSettings settings)
    {
        var messageEncoding = settings.Other.Encoding;
        IFrameChecksumCalculator checksumCalculator = settings.CommunicationSettings.UseLegacyChecksumCalculation
                                                     ? new IdmChecksumCalculator(messageEncoding)
                                                     : new StandardChecksumCalculator();
        return new AstmDataLink(
            settings,
            new AstmFrameBuilder(settings.CommunicationSettings.UseOnlyEtx, settings.CommunicationSettings.EtxOnlySplitLargeRecords, settings.CommunicationSettings.EtxOnlyNormalFrameBlockSize, checksumCalculator),
            new AstmMessageParser(),
            loggerFactory.CreateLogger<AstmDataLink>());
    }
}