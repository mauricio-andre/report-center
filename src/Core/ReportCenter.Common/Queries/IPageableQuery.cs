namespace ReportCenter.Common.Queries;

public interface IPageableQuery
{
    public int? Take { get; init; }
    public int? Skip { get; init; }
}
