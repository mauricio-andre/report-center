using MediatR;
using ReportCenter.Core.Reports.Responses;

namespace ReportCenter.Core.Reports.Queries;

public record GetReportByIdQuery(Guid Id) : IRequest<ReportCompleteResponse>;
