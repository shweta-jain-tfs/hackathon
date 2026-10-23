using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Validation;

internal interface ISettingsCrossValidator
{
    IEnumerable<FailedSettingImportError> Validate(IReadOnlyDictionary<string, Option> options);
}
