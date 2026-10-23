using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Sample.Application.Queries;
using Pharos.Server.Sample.Application.Queries.GetSampleDrawerDetails;

namespace Pharos.Server.Sample.Web.Controllers;

[ApiController]
[Route("api/sampledrawer")]
public class SampleDrawerController : ControllerBase
{
    private readonly ICurrentInstrumentService _currentInstrumentService;
    private readonly ISender _sender;

    public SampleDrawerController(ICurrentInstrumentService currentInstrumentService, ISender sender)
    {
        _currentInstrumentService = currentInstrumentService;
        _sender = sender;
    }

    [HttpGet("leftSampleDrawer")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<SampleDrawerDto> GetLeftSampleDrawerDetails(CancellationToken cancellationToken)
    {
        var query = new GetSampleDrawerDetailsQuery(CurrentInstrumentNumber, DrawerType.Sample1);

        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("rightSampleDrawer")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<SampleDrawerDto> GetRightSampleDrawerDetails(CancellationToken cancellationToken)
    {
        var query = new GetSampleDrawerDetailsQuery(CurrentInstrumentNumber, DrawerType.Sample2);

        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("sampleDrawers")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<IEnumerable<SampleDrawerDto>> GetAllSampleDrawersDetails(CancellationToken cancellationToken)
    {
        var sampleDrawerDtos = new List<SampleDrawerDto>();

        var leftDrawerInfoQuery = new GetSampleDrawerDetailsQuery(CurrentInstrumentNumber, DrawerType.Sample1);
        var rightDrawerInfoQuery = new GetSampleDrawerDetailsQuery(CurrentInstrumentNumber, DrawerType.Sample2);

        var leftSampleDrawerDto = await _sender.Send(leftDrawerInfoQuery, cancellationToken);
        var rightSampleDrawerDto = await _sender.Send(rightDrawerInfoQuery, cancellationToken);

        sampleDrawerDtos.Add(leftSampleDrawerDto);
        sampleDrawerDtos.Add(rightSampleDrawerDto);

        return sampleDrawerDtos;
    }

    private string CurrentInstrumentNumber => _currentInstrumentService.InstrumentNumber ?? throw new NotFoundException();
}