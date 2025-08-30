using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;
using ReportCenter.App.RestServer.Extensions;
using ReportCenter.Core.Reports.Queries;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.App.RestServer.Endpoints.V1.Me.Controllers;

[ApiController]
[ApiVersion(1)]
[Authorize]
[Produces("application/json")]
[Route("v{version:apiVersion}/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet()]
    [ProducesResponseType<IList<ReportResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<IList<ReportResponse>>(StatusCodes.Status206PartialContent)]
    public async Task<IActionResult> Get(
        [FromQuery] SearchReportFromOrigemRequestDto request)
    {
        var result = await _mediator.Send(new SearchReportFromOrigemQuery(
            request.Domain,
            request.Application,
            request.Version,
            request.DocumentName,
            request.ReportType,
            request.DocumentKeyComposition,
            request.IncludeExpiredFiles,
            request.SortBy,
            request.Skip,
            request.Take
        ));

        var list = await result.Items.ToListAsync();

        Response.Headers.AddContentRangeHeaders(request.Skip, request.Take, result.TotalCount);

        return StatusCode(
            result.TotalCount == list.Count
                ? StatusCodes.Status200OK
                : StatusCodes.Status206PartialContent,
            list
        );
    }
}
