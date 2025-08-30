using ReportCenter.Common.Providers.MessageQueues.Dtos;

namespace ReportCenter.Common.Providers.MessageQueues.Interfaces;

public interface IMessagePublisher
{
    public Task PublishProcessesAsync(ReportMessageDto message, CancellationToken cancellationToken = default);
    public Task PublishProgressAsync(
        ReportMessageProgressDto message,
        CancellationToken cancellationToken = default);
}
