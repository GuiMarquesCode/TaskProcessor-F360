using F360.TaskProcessor.Application.DTOs;

namespace F360.TaskProcessor.Application.Interfaces;

/// <summary>
/// Interface segregada (ISP + DIP) responsável exclusivamente por publicar mensagens na fila.
/// A camada Application não conhece detalhes de conexão do RabbitMQ.
/// </summary>
public interface IMessageQueuePublisher
{
    Task PublishAsync(TaskQueueMessage message, CancellationToken cancellationToken = default);
}
