using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Command;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Commands.CopySettings;
using Pharos.Server.Settings.Application.Commands.DeleteSettings;
using Pharos.Server.Settings.Application.Commands.ImportSettings;
using Pharos.Server.Settings.Application.Commands.UpdateSettings;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Queries;

namespace Pharos.Server.Settings.Web.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Permissions.EditSystemSettings)]
public class SettingsController(ISender sender) : ControllerBase
{
    [HttpGet("/subnav")]
    public async Task<Dictionary<string, string>> GetMenuItems(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMenuItemsQuery(), cancellationToken);
    }

    [HttpGet("/pageKeyMap")]
    public async Task<Dictionary<string, string>> GetPageKeyMap(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetPageKeyMapQuery(), cancellationToken);
    }

    [HttpGet("{key}")]
    public async Task<Page> GetPage(string key, string rootPageLabel, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetSettingQuery(key, rootPageLabel), cancellationToken);
    }

    [HttpDelete]
    public Task DeleteSettings(string[] keys, CancellationToken cancellationToken)
    {
        var command = new DeleteSettingsCommand(keys);
        return sender.Send(command, cancellationToken);
    }

    [HttpDelete("pageListEntryKey")]
    public Task<DeleteResponse> DeletePages(string[] pageListEntryKeys, CancellationToken cancellationToken)
    {
        var command = new DeletePagesCommand(pageListEntryKeys);
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("{id}")]
    public Task<SettingsUpdateResult> UpdateSetting([FromBody] UpdateSettingCommand command, CancellationToken cancellationToken)
    {
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("export")]
    public async Task<string> GetSettingsExport(CancellationToken cancellationToken)
    {
        var exportedSettingsString = await sender.Send(new GetSettingsExportQuery(), cancellationToken);
        return exportedSettingsString;
    }

    [HttpPost("import")]
    public async Task<SettingsImportResult> ImportSettings([FromBody] ImportSettingsCommand importSettingsCommand, CancellationToken cancellationToken)
    {
        return await sender.Send(importSettingsCommand, cancellationToken);
    }

    [HttpPost("copy")]
    public async Task<CopySettingsResult> CopySettings([FromBody] CopySettingsCommand copySettingsCommand, CancellationToken cancellationToken)
    {
        return await sender.Send(copySettingsCommand, cancellationToken);
    }

    [HttpGet("checkMaxItems")]
    public async Task<CheckPageListMaxItemsResult> CheckPageListMaxItems(string pageListKey, string rootPageLabel, CancellationToken cancellationToken)
    {
        return await sender.Send(new CheckPageListMaxItemsQuery(pageListKey, rootPageLabel), cancellationToken);
    }
}
