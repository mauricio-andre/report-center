using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;
using ReportCenter.App.RestServer.Extensions;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.Core.Reports.Queries;
using ReportCenter.Core.Reports.Responses;
using static System.Net.Mime.MediaTypeNames;

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

    [HttpGet("{id}")]
    [ProducesResponseType<ReportCompleteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> Get(
        [FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetReportByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("{id}/downloads")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> Download(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DownloadReportQuery(id), cancellationToken);
        if (result == null)
            return NotFound();

        return File(result.Stream, "application/octet-stream", result.FileName, enableRangeProcessing: true);
    }

    [HttpPatch("{id}/external-process")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> UpdateExternalState(
        [FromRoute] Guid id,
        [FromBody] UpdateReportExternalProcessStateDto request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateReportExternalProcessStateCommand(
                id,
                request.ProcessState,
                request.ProcessTimer,
                request.ProcessMessage),
            cancellationToken);

        return NoContent();
    }
}
