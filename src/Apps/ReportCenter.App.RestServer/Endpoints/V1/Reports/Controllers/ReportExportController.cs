using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;
using ReportCenter.App.RestServer.Extensions;
using ReportCenter.Common.Providers.MessageQueues.Enums;
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
public class ReportExportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, Application.ProblemJson)]
    public async Task<IActionResult> Create([FromBody] CreateReportExportCommand request)
    {
        var result = await _mediator.Send(request);
        return Ok(result);
    }

    [HttpGet("{Id}")]
    [ProducesResponseType<ReportCompleteResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromRoute] Guid Id)
    {
        var result = await _mediator.Send(new GetReportByIdQuery(Id));
        return Ok(result);
    }

    [HttpGet("{Id}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType<ReportCompleteResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(
        [FromRoute] Guid Id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DownloadReportQuery(Id), cancellationToken);
        if (result == null)
            return NotFound();

        return File(result, "application/octet-stream", "report-center-file.xlsx", enableRangeProcessing: true);
    }

    [HttpGet("{domain}/{application}/{versionDoc}/{documentName}")]
    [ProducesResponseType<IList<ReportResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<IList<ReportResponse>>(StatusCodes.Status206PartialContent)]
    public async Task<IActionResult> GetFromOrigem(
        [FromRoute] string domain,
        [FromRoute] string application,
        [FromRoute] string documentName,
        [FromRoute] short versionDoc,
        [FromQuery] SearchReportFromOrigemRequestDto request)
    {
        var result = await _mediator.Send(new SearchReportFromOrigemQuery(
            domain,
            application,
            versionDoc,
            documentName,
            ReportType.Export,
            request.DocumentKeyComposition,
            request.SortBy,
            request.Skip,
            request.Take,
            request.IncludeExpiredFiles
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

    [HttpGet("{domain}/{application}/{versionDoc}/{documentName}/{documentKey}")]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFromOrigem(
        [FromRoute] string domain,
        [FromRoute] string application,
        [FromRoute] short versionDoc,
        [FromRoute] string documentName,
        [FromRoute] string documentKey)
    {
        var result = await _mediator.Send(new SearchReportFromOrigemQuery(
            domain,
            application,
            versionDoc,
            documentName,
            ReportType.Export,
            documentKey,
            null,
            0,
            1,
            false
        ));

        var item = await result.Items.FirstOrDefaultAsync();
        return item == null
            ? NotFound()
            : Ok(item);
    }
}
