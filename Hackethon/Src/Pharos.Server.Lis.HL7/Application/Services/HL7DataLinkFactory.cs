using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Frames;
using Pharos.Server.Lis.HL7.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Services;

internal sealed class HL7DataLinkFactory(ILoggerFactory loggerFactory) : IHL7DataLinkFactory
{
    public IHL7DataLink CreateHL7DataLink(HL7Settings settings)
    {
        return new HL7DataLink(new HL7FrameBuilder(), new HL7MessageParser(), loggerFactory.CreateLogger<HL7DataLink>());
    }
}
