namespace F360.TaskProcessor.Application.Services;

/// <summary>
/// Contrato do orquestrador de processamento executado pelos Workers em background (Requisito 2).
/// </summary>
public interface ITaskProcessingOrchestrator
{
    Task ProcessTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}
