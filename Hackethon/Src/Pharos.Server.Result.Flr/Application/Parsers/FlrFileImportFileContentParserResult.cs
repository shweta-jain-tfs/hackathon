namespace Pharos.Server.Result.Flr.Application.Parsers
{
    internal class FlrFileImportFileContentParserResult(FlrHeaderInformation headerInformation, List<FlrEntry> entityList)
    {
        public FlrHeaderInformation HeaderInformation { get; init; } = headerInformation;
        public List<FlrEntry> EntityList { get; init; } = entityList;
    }
}
