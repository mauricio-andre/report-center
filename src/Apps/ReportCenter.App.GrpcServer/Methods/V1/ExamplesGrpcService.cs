using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ReportCenter.App.GrpcServer.Methods.V1.Examples;

namespace CqrsProject.App.GrpcServer.Methods.V1.Examples;

[Authorize]
public class ExamplesGrpcService : ExamplesService.ExamplesServiceBase
{
    public ExamplesGrpcService()
    {
    }

    public override async Task ExportList(ExampleExportRequest request, IServerStreamWriter<ExampleReply> responseStream, ServerCallContext context)
    {
        for (int i = 0; i < 100; i++)
        {
            await responseStream.WriteAsync(new ExampleReply
            {
                Texto = "Mauricio",
                Inteiro = 28,
                Decimal = 15.32,
                Data = DateTimeOffset.Now.ToTimestamp()
            });
        }
    }
}
