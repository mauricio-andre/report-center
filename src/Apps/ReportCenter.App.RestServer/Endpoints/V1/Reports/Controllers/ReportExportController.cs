using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;
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
    [ProducesResponseType<ReportCompleteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, Application.ProblemJson)]
    public async Task<IActionResult> Create([FromBody] CreateReportExportCommand request)
    {
        var result = await _mediator.Send(request);
        return Ok(result);
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

    [HttpGet("{domain}/{application}/{versionDoc}/{documentName}/{documentKey}")]
    [ProducesResponseType<ReportCompleteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> GetFromOrigem(
        [FromRoute] string domain,
        [FromRoute] string application,
        [FromRoute] short versionDoc,
        [FromRoute] string documentName,
        [FromRoute] string documentKey)
    {
        var result = await _mediator.Send(new GetReportByKeysQuery(
            domain,
            application,
            versionDoc,
            documentName,
            ReportType.Export,
            documentKey
        ));

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

    [HttpPost("{id}/external-process/uploads")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> UploadExternalFile(
        [FromRoute] Guid id,
        IFormFile file,
        [FromHeader(Name = "Process-Timer")] TimeSpan? processTimer,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UploadReportExportExternalCommand(
                id,
                file.OpenReadStream(),
                Path.GetExtension(file.FileName),
                processTimer),
            cancellationToken);

        return Created();
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
            new UpdateReportStateCommand(
                id,
                request.ProcessState,
                request.ProcessTimer,
                request.ProcessMessage),
            cancellationToken);

        return NoContent();
    }
}
