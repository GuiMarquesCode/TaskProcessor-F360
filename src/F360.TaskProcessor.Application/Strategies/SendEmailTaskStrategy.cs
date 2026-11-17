using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace F360.TaskProcessor.Application.Strategies;

/// <summary>
/// Estratégia concreta para o processamento de tarefas do tipo "EnviarEmail".
/// Demonstra Polimorfismo (Strategy Pattern) e Composição sobre Herança.
/// </summary>
public class SendEmailTaskStrategy : ITaskExecutionStrategy
{
    private readonly ITaskSimulationHelper _simulationHelper;
    private readonly ILogger<SendEmailTaskStrategy> _logger;

    public string TaskType => "EnviarEmail";

    public SendEmailTaskStrategy(
        ITaskSimulationHelper simulationHelper,
        ILogger<SendEmailTaskStrategy> logger)
    {
        _simulationHelper = simulationHelper;
        _logger = logger;
    }

    public async Task ExecuteAsync(JobTask task, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Strategy: {Strategy}] Iniciando processamento de envio de e-mail para a Tarefa {TaskId} (Tentativa {Attempt}/{MaxAttempts}). Dados: {Data}",
            TaskType, task.Id, task.Attempts, task.MaxAttempts, task.DataJson);

        await _simulationHelper.SimulateExecutionAndCheckFailureAsync(
            task,
            TimeSpan.FromSeconds(3),
            cancellationToken);

        _logger.LogInformation(
            "[Strategy: {Strategy}] E-mail processado com sucesso para a Tarefa {TaskId}.",
            TaskType, task.Id);
    }
}
