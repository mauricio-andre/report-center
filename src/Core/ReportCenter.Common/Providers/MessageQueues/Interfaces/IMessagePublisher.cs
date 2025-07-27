using ReportCenter.Common.Providers.MessageQueues.Dtos;

namespace ReportCenter.Common.Providers.MessageQueues.Interfaces;

public interface IMessagePublisher
{
    public Task PublishAsync(ReportMessageDto message, CancellationToken cancellationToken = default);
}
