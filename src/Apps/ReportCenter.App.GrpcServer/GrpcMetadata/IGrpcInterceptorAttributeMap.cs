using ReportCenter.App.GrpcServer.Attributes;

namespace ReportCenter.App.GrpcServer.GrpcMetadata;

public interface IGrpcInterceptorAttributeMap
{
    public bool TryGetInterceptorsByMethod(
        string method,
        out IEnumerable<InterceptorAttribute>? interceptorAttributes);
}
