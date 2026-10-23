using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Commands.SendCmrTestOrder;
using ValidationException = FluentValidation.ValidationException;

namespace Pharos.Server.Instrument.Web.Controllers;

[ApiController]
[Route("api/custommaderun")]
[Authorize(Permissions.RunServiceTools)]
public class CustomMadeRunsController(ISender sender, ICurrentInstrumentService currentInstrumentService) : ControllerBase
{
    [HttpPost]
    [Route("sendtoinstrument")]
    [Authorize(Permissions.RunServiceTools)]
    public async Task<CmrTestOrderResultDto> UploadFile(IFormFile file)
    {
        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Only CSV files are accepted.");
        }

        var bytes = await GetBytes(file);
        return await sender.Send(new SendCmrTestOrderCommand(CurrentInstrumentNumber, bytes));
    }

    private static async Task<byte[]> GetBytes(IFormFile file)
    {
        await using var memStream = new MemoryStream();
        await file.CopyToAsync(memStream);
        return memStream.ToArray();
    }

    private string CurrentInstrumentNumber => currentInstrumentService.InstrumentNumber ?? throw new NotFoundException();
}