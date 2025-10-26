using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Core.Reports.Commands;
using ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;
using ReportCenter.Core.Reports.Queries;
using ReportCenter.Core.Reports.Responses;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ReportCenter.Common.Exceptions;
using Microsoft.Extensions.Localization;
using ReportCenter.Common.Localization;

namespace ReportCenter.App.RestServer.Endpoints.V1.Me.Controllers;

[ApiController]
[ApiVersion(1)]
[Authorize]
[Produces("application/json")]
[Route("v{version:apiVersion}/[controller]")]
public class ReportImportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStringLocalizer<ReportCenterResource> _stringLocalizer;

    public ReportImportController(
        IMediator mediator,
        IStringLocalizer<ReportCenterResource> stringLocalizer)
    {
        _mediator = mediator;
        _stringLocalizer = stringLocalizer;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ReportCompleteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, Application.ProblemJson)]
    public async Task<IActionResult> Create(
        [FromForm] CreateReportImportDto request,
        [Required] IFormFile file)
    {
        var errors = request.CatchJsonFormatExceptions();
        if (errors.Keys.Any())
            throw new BadFormattedJsonException(_stringLocalizer, errors);

        var result = await _mediator.Send(new CreateReportImportCommand(
            file.OpenReadStream(),
            Path.GetExtension(file.FileName),
            request.Domain,
            request.Application,
            request.DocumentName,
            request.DocumentKey,
            request.Version,
            request.ExpirationDate,
            request.FiltersDictionary,
            request.ExtraPropertiesDictionary,
            request.ExternalProcess
        ));
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
            ReportType.Import,
            documentKey
        ));

        return Ok(result);
    }
}
