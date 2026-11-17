using F360.TaskProcessor.Domain.Entities;

namespace F360.TaskProcessor.Application.Interfaces;

/// <summary>
/// Interface segregada utilizada via COMPOSIÇÃO pelos processadores de tarefas
/// (Composição sobre Herança + Interface Segregation Principle).
/// Centraliza a leitura de parâmetros de simulação do payload JSON sem acoplar
/// os handlers a uma classe base rígida.
/// </summary>
public interface ITaskSimulationHelper
{
    /// <summary>
    /// Simula o tempo de processamento e avalia se o payload JSON solicitou uma falha
    /// proposital (ex.: {"simularFalha": true}) para demonstrar o mecanismo de re-tentativas.
    /// </summary>
    Task SimulateExecutionAndCheckFailureAsync(JobTask task, TimeSpan defaultDuration, CancellationToken cancellationToken = default);
}
