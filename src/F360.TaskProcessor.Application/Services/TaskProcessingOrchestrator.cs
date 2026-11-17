using F360.TaskProcessor.Application.DTOs;
using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Domain.Enums;
using F360.TaskProcessor.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace F360.TaskProcessor.Application.Services;

/// <summary>
/// Orquestra o processamento de uma tarefa em background:
/// 1. Controle de concorrência atômico (impede que múltiplos workers processem a mesma tarefa).
/// 2. Seleção polimórfica da estratégia via Factory Pattern.
/// 3. Tratamento de falhas e sistema de re-tentativas respeitando o limite máximo (Requisito 2.b).
/// </summary>
public class TaskProcessingOrchestrator : ITaskProcessingOrchestrator
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskStrategyFactory _strategyFactory;
    private readonly IMessageQueuePublisher _queuePublisher;
    private readonly ILogger<TaskProcessingOrchestrator> _logger;

    public TaskProcessingOrchestrator(
        ITaskRepository taskRepository,
        ITaskStrategyFactory strategyFactory,
        IMessageQueuePublisher queuePublisher,
        ILogger<TaskProcessingOrchestrator> logger)
    {
        _taskRepository = taskRepository;
        _strategyFactory = strategyFactory;
        _queuePublisher = queuePublisher;
        _logger = logger;
    }

    public async Task ProcessTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        // 1. Controle de Concorrência (Requisito 2.c):
        // Tenta adquirir a tarefa atomicamente apenas se ela estiver com status "Pendente".
        var task = await _taskRepository.TryAcquireForProcessingAsync(taskId, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning(
                "Tarefa {TaskId} ignorada por este worker: já está em processamento por outro worker, foi concluída ou não existe.",
                taskId);
            return;
        }

        _logger.LogInformation(
            "Tarefa {TaskId} ({TaskType}) adquirida para processamento. Status: '{Status}' | Tentativa: {Attempt}/{MaxAttempts}.",
            task.Id, task.Type, task.Status, task.Attempts, task.MaxAttempts);

        try
        {
            // 2. Resolve a estratégia correta (Strategy + Factory Pattern) e executa
            var strategy = _strategyFactory.GetStrategy(task.Type);
            await strategy.ExecuteAsync(task, cancellationToken);

            // 3. Sucesso: atualiza status para "Concluido" (Requisito 3.a)
            task.MarkAsCompleted();
            await _taskRepository.UpdateAsync(task, cancellationToken);

            _logger.LogInformation(
                "Tarefa {TaskId} ({TaskType}) finalizada com sucesso! Status atualizado para '{Status}'.",
                task.Id, task.Type, task.Status);
        }
        catch (Exception ex)
        {
            // 4. Sistema de Re-tentativa (Requisito 2.b):
            // A entidade encapsula a decisão: se Attempts < MaxAttempts volta para 'Pendente', senão vai para 'Erro'.
            task.RegisterFailure(ex.Message);
            await _taskRepository.UpdateAsync(task, cancellationToken);

            if (task.Status == TaskItemStatus.Pendente)
            {
                _logger.LogWarning(
                    ex,
                    "Falha na tentativa {Attempt}/{MaxAttempts} da Tarefa {TaskId}. Re-enfileirando para nova tentativa...",
                    task.Attempts, task.MaxAttempts, task.Id);

                var retryMessage = new TaskQueueMessage(task.Id, task.Type, DateTime.UtcNow);
                await _queuePublisher.PublishAsync(retryMessage, cancellationToken);
            }
            else
            {
                _logger.LogError(
                    ex,
                    "Tarefa {TaskId} atingiu o limite máximo de tentativas ({Attempt}/{MaxAttempts}). Status definitivo: '{Status}'.",
                    task.Id, task.Attempts, task.MaxAttempts, task.Status);
            }
        }
    }
}
