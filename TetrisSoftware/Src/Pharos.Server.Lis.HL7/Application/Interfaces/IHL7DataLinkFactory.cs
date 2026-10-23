using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.HL7.Application.Interfaces;

internal interface IHL7DataLinkFactory
{
    IHL7DataLink CreateHL7DataLink(HL7Settings settings);
}
