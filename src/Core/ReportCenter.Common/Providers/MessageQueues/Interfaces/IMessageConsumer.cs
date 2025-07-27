using ReportCenter.Common.Providers.MessageQueues.Dtos;

namespace ReportCenter.Common.Providers.MessageQueues.Interfaces;

public interface IMessageConsumer : IDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task RegistryConsumer(Func<object, CancellationToken, Task> ReceivedAsync, CancellationToken cancellationToken = default);
    ReportMessageDto DeserializeMessage(object args);
    ValueTask AbortProcessingAsync(object args, Exception ex, CancellationToken cancellationToken = default);
    ValueTask CompleteMessage(object args, CancellationToken cancellationToken = default);
    ValueTask<string?> GetParentTransactionId(object args);
    Task StopAsync(CancellationToken cancellationToken = default);
}
