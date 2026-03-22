using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet("{domain}/{application}/{versionDoc}/{documentName}/{documentKey}")]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
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

    [HttpPost("{id}/external-process/uploads")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> UploadExternalFile(
        [FromRoute] Guid id,
        [Required] IFormFile file,
        [FromForm] TimeSpan? processTimer,
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
}
