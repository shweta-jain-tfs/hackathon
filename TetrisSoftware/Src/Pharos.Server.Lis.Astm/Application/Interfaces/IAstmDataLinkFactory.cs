using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.Astm.Application.Interfaces;

/// <summary>
/// Defines a factory for creating ASTM data links.
/// </summary>
internal interface IAstmDataLinkFactory
{
    IAstmDataLink CreateAstmDataLink(AstmSettings settings);
}
