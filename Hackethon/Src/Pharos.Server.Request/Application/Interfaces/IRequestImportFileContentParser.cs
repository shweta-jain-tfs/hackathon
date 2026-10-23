using Pharos.Server.Request.Application.Models;

namespace Pharos.Server.Request.Application.Interfaces;

internal interface IRequestImportFileContentParser
{
    ImportedRequests Parse(string fileContentAsString, string fileName);
}
