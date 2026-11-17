using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace F360.TaskProcessor.Application.Strategies;

/// <summary>
/// Estratégia concreta para o processamento de tarefas do tipo "GerarRelatorio".
/// Demonstra Polimorfismo (Strategy Pattern) e Composição sobre Herança.
/// </summary>
public class GenerateReportTaskStrategy : ITaskExecutionStrategy
{
    private readonly ITaskSimulationHelper _simulationHelper;
    private readonly ILogger<GenerateReportTaskStrategy> _logger;

    public string TaskType => "GerarRelatorio";

    public GenerateReportTaskStrategy(
        ITaskSimulationHelper simulationHelper,
        ILogger<GenerateReportTaskStrategy> logger)
    {
        _simulationHelper = simulationHelper;
        _logger = logger;
    }

    public async Task ExecuteAsync(JobTask task, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Strategy: {Strategy}] Iniciando geração de relatório financeiro para a Tarefa {TaskId} (Tentativa {Attempt}/{MaxAttempts}). Dados: {Data}",
            TaskType, task.Id, task.Attempts, task.MaxAttempts, task.DataJson);

        await _simulationHelper.SimulateExecutionAndCheckFailureAsync(
            task,
            TimeSpan.FromSeconds(4),
            cancellationToken);

        _logger.LogInformation(
            "[Strategy: {Strategy}] Relatório gerado com sucesso para a Tarefa {TaskId}.",
            TaskType, task.Id);
    }
}
